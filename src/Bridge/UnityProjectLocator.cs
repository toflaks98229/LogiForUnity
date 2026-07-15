namespace Loupedeck.LogiForUnityPlugin
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Management;
    using System.Runtime.Versioning;
    using System.Text.RegularExpressions;

    // 실행 중인 Unity 에디터가 어떤 프로젝트를 열고 있는지 찾아낸다.
    //
    // 유일하게 신뢰할 수 있는 출처는 프로세스의 커맨드라인에 있는 -projectPath다.
    // Editor.log에는 커맨드라인이 없고, Unity Hub의 projects-v1.json은 "최근 목록"일 뿐 현재 열린 프로젝트가 아니다.
    // 에셋 임포트 워커도 Unity.exe라는 같은 이름으로 뜨지만 -projectPath가 없으므로 자연히 걸러진다.

    internal static class UnityProjectLocator
    {
        private static readonly Regex ProjectPathArgument =
            new Regex(@"-projectPath\s+(?:""(?<p>[^""]+)""|(?<p>\S+))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // WMI 쿼리는 수십 밀리초가 든다. GetCommandDisplayName은 버튼을 그릴 때마다 불리므로 짧게 캐싱한다.
        private const Int64 CacheLifetimeMilliseconds = 2000;

        private static readonly Object CacheLock = new Object();
        private static String _cachedAnyProjectPath;
        private static Boolean _hasCachedValue;
        private static Int64 _cachedAt;

        // 지정한 Unity 프로세스가 연 프로젝트 경로. 찾지 못하면 null.
        public static String TryGetProjectPath(Int32 processId)
        {
            var commandLine = TryGetCommandLine($"ProcessId = {processId}");
            return commandLine == null ? null : ParseAndValidate(commandLine);
        }

        // 실행 중인 아무 Unity 에디터의 프로젝트 경로. 여러 개가 떠 있으면 첫 번째를 쓴다.
        public static String TryGetAnyProjectPath()
        {
            lock (CacheLock)
            {
                // 첫 호출에서는 반드시 조회한다. 센티널 타임스탬프를 쓰면 뺄셈이 오버플로우해서
                // 영원히 캐시가 유효한 것처럼 보인다.
                var now = Environment.TickCount64;
                if (_hasCachedValue && now - _cachedAt < CacheLifetimeMilliseconds)
                {
                    return _cachedAnyProjectPath;
                }

                var commandLine = TryGetCommandLine("Name = 'Unity.exe'");
                _cachedAnyProjectPath = commandLine == null ? null : ParseAndValidate(commandLine);
                _cachedAt = now;
                _hasCachedValue = true;
                return _cachedAnyProjectPath;
            }
        }

        [SupportedOSPlatform("windows")]
        private static String TryGetCommandLineWindows(String whereClause)
        {
            using (var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE {whereClause}"))
            using (var results = searcher.Get())
            {
                return results.Cast<ManagementObject>()
                    .Select(o => o["CommandLine"] as String)
                    .FirstOrDefault(cl => !String.IsNullOrEmpty(cl) && ProjectPathArgument.IsMatch(cl));
            }
        }

        private static String TryGetCommandLine(String whereClause)
        {
            if (!OperatingSystem.IsWindows())
            {
                // macOS 지원 시 `ps -o command` 등으로 대체해야 한다.
                PluginLog.Warning("Unity project detection is only implemented on Windows");
                return null;
            }

            try
            {
                return TryGetCommandLineWindows(whereClause);
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "Failed to read the Unity process command line");
                return null;
            }
        }

        private static String ParseAndValidate(String commandLine)
        {
            var match = ProjectPathArgument.Match(commandLine);
            if (!match.Success)
            {
                return null;
            }

            var path = match.Groups["p"].Value.TrimEnd('\\', '/');
            if (!IsUnityProject(path))
            {
                PluginLog.Warning($"'{path}' does not look like a Unity project");
                return null;
            }

            return path;
        }

        // Assets/와 Packages/가 둘 다 있어야 컴패니언을 심을 수 있는 프로젝트다.
        public static Boolean IsUnityProject(String path) =>
            !String.IsNullOrEmpty(path)
            && Directory.Exists(Path.Combine(path, "Assets"))
            && Directory.Exists(Path.Combine(path, "Packages"));
    }
}
