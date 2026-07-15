namespace Loupedeck.LogiForUnityPlugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    internal enum BridgeInstallState
    {
        // 프로젝트를 찾지 못했거나 Unity가 실행 중이 아니다.
        NoProject,

        // 프로젝트는 있으나 컴패니언 패키지가 없다.
        NotInstalled,

        // 설치되어 있으나 플러그인에 들어 있는 버전보다 낮다.
        Outdated,

        UpToDate,
    }

    // 플러그인 어셈블리에 embedded resource로 들어 있는 컴패니언 패키지를 Unity 프로젝트에 설치한다.
    //
    // Assets/가 아니라 Packages/에 embedded UPM 패키지로 심는다. Unity가 자동으로 컴파일해주고,
    // asmdef가 Editor 전용이라 플레이어 빌드에는 절대 포함되지 않으며, 폴더 하나만 지우면 완전히 제거된다.
    //
    // 설치는 멱등이다. 버전 스탬프는 package.json의 version 필드 자체를 쓴다. 별도 스탬프 파일을 두면
    // 두 값이 어긋날 수 있으므로, 진실의 출처를 하나로 유지한다.

    internal static class UnityBridgeInstaller
    {
        public const String PackageId = "com.logi.unity-bridge";

        // 파일 이름 -> 패키지 안에서의 상대 경로.
        //
        // 손으로 관리하면 반드시 빠뜨린다(실제로 LogiBridgeAdjustments.cs 를 빠뜨려 Unity 컴파일이 깨졌다).
        // 대신 임베디드 리소스 목록에서 유도한다. 리소스 이름은 점으로 구분되므로,
        // 마지막 두 조각이 파일 이름이고 그 앞은 디렉터리다. (Editor.LogiBridgeClient.cs -> Editor/LogiBridgeClient.cs)
        private static readonly Lazy<Dictionary<String, String>> LazyPackageFiles =
            new Lazy<Dictionary<String, String>>(DiscoverPackageFiles);

        private static Dictionary<String, String> PackageFiles => LazyPackageFiles.Value;

        private static Dictionary<String, String> DiscoverPackageFiles()
        {
            var prefix = $"{typeof(UnityBridgeInstaller).Namespace}.UnityBridge.";
            var files = new Dictionary<String, String>(StringComparer.Ordinal);

            foreach (var resourceName in PluginResources.FindFiles($"^{Regex.Escape(prefix)}"))
            {
                var segments = resourceName.Substring(prefix.Length).Split('.');
                if (segments.Length < 2)
                {
                    continue;
                }

                var fileName = $"{segments[segments.Length - 2]}.{segments[segments.Length - 1]}";
                var pathSegments = new String[segments.Length - 1];
                Array.Copy(segments, pathSegments, segments.Length - 2);
                pathSegments[segments.Length - 2] = fileName;

                files[fileName] = Path.Combine(pathSegments);
            }

            PluginLog.Verbose($"Companion package contains {files.Count} files: {String.Join(", ", files.Values)}");
            return files;
        }

        private static readonly Regex VersionField =
            new Regex(@"""version""\s*:\s*""(?<v>[^""]+)""", RegexOptions.Compiled);

        // 플러그인 서비스는 이 플러그인을 여러 인스턴스로 로드하고, 각 인스턴스가 같은 앱 이벤트를 받는다.
        // 락이 없으면 두 인스턴스가 동시에 같은 파일을 쓰고, 상태 판정과 쓰기 사이에 경쟁이 생긴다.
        private static readonly Object InstallLock = new Object();

        // 플러그인에 내장된 컴패니언 버전.
        public static String EmbeddedVersion => ReadVersion(PluginResources.ReadTextFile("package.json"));

        public static String GetPackageDirectory(String projectPath) =>
            Path.Combine(projectPath, "Packages", PackageId);

        public static String GetInstalledVersion(String projectPath)
        {
            var manifest = Path.Combine(GetPackageDirectory(projectPath), "package.json");
            return File.Exists(manifest) ? ReadVersion(File.ReadAllText(manifest)) : null;
        }

        public static BridgeInstallState GetState(String projectPath)
        {
            if (!UnityProjectLocator.IsUnityProject(projectPath))
            {
                return BridgeInstallState.NoProject;
            }

            // 버전만 보고 판단하면 안 된다. package.json은 남아 있는데 스크립트만 지워진 설치는
            // UpToDate로 잘못 읽히고, 그러면 영영 복구되지 않는다. 모든 파일이 있어야 설치된 것이다.
            var target = GetPackageDirectory(projectPath);
            foreach (var relativePath in PackageFiles.Values)
            {
                if (!File.Exists(Path.Combine(target, relativePath)))
                {
                    return BridgeInstallState.NotInstalled;
                }
            }

            var installed = GetInstalledVersion(projectPath);
            if (installed == null)
            {
                return BridgeInstallState.NotInstalled;
            }

            return installed == EmbeddedVersion ? BridgeInstallState.UpToDate : BridgeInstallState.Outdated;
        }

        // 설치 또는 업그레이드. 이미 최신이면 아무것도 쓰지 않고 false를 돌려준다.
        public static Boolean Install(String projectPath, Boolean force = false)
        {
            lock (InstallLock)
            {
                // 상태 판정과 쓰기는 원자적이어야 한다. 그렇지 않으면 두 인스턴스가 모두 "설치 필요"로 읽고
                // 둘 다 쓴 뒤, 둘 다 사용자에게 알림을 띄운다.
                var state = GetState(projectPath);
                if (state == BridgeInstallState.NoProject)
                {
                    PluginLog.Warning($"Cannot install the bridge: '{projectPath}' is not a Unity project");
                    return false;
                }

                if (state == BridgeInstallState.UpToDate && !force)
                {
                    PluginLog.Verbose($"Unity bridge {EmbeddedVersion} is already installed in '{projectPath}'");
                    return false;
                }

                var target = GetPackageDirectory(projectPath);

                foreach (var entry in PackageFiles)
                {
                    var destination = Path.Combine(target, entry.Value);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    File.WriteAllBytes(destination, PluginResources.ReadBinaryFile(entry.Key));
                }

                PluginLog.Info($"Unity bridge {EmbeddedVersion} installed into '{target}' (was: {state})");
                return true;
            }
        }

        public static Boolean Uninstall(String projectPath)
        {
            var target = GetPackageDirectory(projectPath);
            if (!Directory.Exists(target))
            {
                return false;
            }

            Directory.Delete(target, recursive: true);

            // Unity가 만든 폴더 메타 파일도 같이 지운다. 남겨두면 에디터가 경고를 낸다.
            var meta = target + ".meta";
            if (File.Exists(meta))
            {
                File.Delete(meta);
            }

            PluginLog.Info($"Unity bridge removed from '{target}'");
            return true;
        }

        private static String ReadVersion(String manifestJson)
        {
            var match = VersionField.Match(manifestJson ?? String.Empty);
            return match.Success ? match.Groups["v"].Value : null;
        }
    }
}
