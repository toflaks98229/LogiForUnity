namespace Loupedeck.LogiForUnityPlugin
{
    using System;
    using System.Collections.Generic;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class LogiForUnityPlugin : Plugin
    {
        // 사용자가 브리지 설치에 동의했는지 기록하는 설정 키.
        private const String AutoInstallConsentSetting = "UnityBridgeAutoInstallConsent";

        // 트레이 알림을 이미 띄운 프로젝트. 에디터를 열 때마다 한 번씩만 알리고, 같은 프로젝트로 반복해서 귀찮게 하지 않는다.
        // 플러그인이 리로드되면 초기화되므로, 사용자가 알림을 놓쳤다면 Options+ 경고 배너가 남아 있다.
        private readonly HashSet<String> _notifiedProjects = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

        // 에디터가 바빠서 미뤄둔 설치. 에디터가 한가해지면 다시 시도한다.
        private volatile String _deferredInstallProjectPath;

        // Unity 에디터 컴패니언과의 연결. 커맨드는 이걸 먼저 시도하고, 없으면 핫키로 폴백한다.
        internal UnityBridgeServer Bridge { get; private set; }

        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => false;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => false;

        // Initializes a new instance of the plugin class.
        public LogiForUnityPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);
        }

        // This method is called when the plugin is loaded.
        public override void Load()
        {
            // The Unity Editor drops synthetic key events that arrive faster than one editor frame,
            // so hold each key down long enough for the editor to sample it.
            if (this.KeyboardShortcutSettings != null)
            {
                this.KeyboardShortcutSettings.DelayBetweenKeyDownAndUp = 40;
                this.KeyboardShortcutSettings.DelayBetweenShortcuts = 40;
            }

            this.Bridge = new UnityBridgeServer();
            this.Bridge.EditorStateChanged += this.OnEditorStateChanged;
            this.Bridge.Start();

            // 에디터가 새로 뜰 때 버전을 맞춘다.
            this.ClientApplication.ApplicationStarted += this.OnUnityStarted;

            // 에디터가 이미 떠 있는 동안 패키지가 지워질 수 있다. 폴더를 감시하는 것보다,
            // 사용자가 Unity로 돌아올 때 다시 확인하는 편이 단순하고 충분하다.
            //
            // 주의: ApplicationInstanceStarted / ApplicationInstanceActivated 는 실제로 발생하지 않는다(계측으로 확인).
            // Instance 접미사가 없는 쪽만 온다.
            this.ClientApplication.ApplicationActivated += this.OnUnityActivated;

            // 플러그인 서비스는 이 플러그인을 여러 인스턴스로 로드하고, 로그 파일은 인스턴스마다 새로 만들어진다.
            // 어느 인스턴스가 무엇을 했는지 구분하려면 인스턴스 식별자가 필요하다.
            PluginLog.Verbose($"[instance {this.GetHashCode():x}] loaded; native GUI available: {this.NativeGui != null}");

            this.SyncBridgeInstallation(null);
        }

        // This method is called when the plugin is unloaded.
        public override void Unload()
        {
            this.ClientApplication.ApplicationStarted -= this.OnUnityStarted;
            this.ClientApplication.ApplicationActivated -= this.OnUnityActivated;

            if (this.Bridge != null)
            {
                this.Bridge.EditorStateChanged -= this.OnEditorStateChanged;
                this.Bridge.Dispose();
                this.Bridge = null;
            }
        }

        private void OnUnityStarted(Object sender, ClientApplicationChangedEventArgs e)
        {
            PluginLog.Verbose($"[instance {this.GetHashCode():x}] ApplicationStarted pid={e.ProcessId}");
            this.SyncBridgeInstallation(e.ProcessId);
        }

        private void OnUnityActivated(Object sender, ClientApplicationChangedEventArgs e)
        {
            PluginLog.Verbose($"[instance {this.GetHashCode():x}] ApplicationActivated pid={e.ProcessId}");
            this.SyncBridgeInstallation(e.ProcessId);
        }

        // 컴패니언이 "이제 한가하다"고 알려왔다. 미뤄둔 설치가 있으면 지금 한다.
        // 브리지의 읽기 스레드에서 불리므로, 예외가 새어 나가면 연결이 끊긴다.
        private void OnEditorStateChanged(Object sender, EventArgs e)
        {
            var projectPath = this._deferredInstallProjectPath;
            if (projectPath == null || this.Bridge?.IsEditorBusy != false)
            {
                return;
            }

            try
            {
                this.RepairBridge(projectPath, UnityBridgeInstaller.GetState(projectPath));
                this.RefreshBridgeStatus();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Deferred bridge install failed");
            }
        }

        // 동의한 경우에만, 이미 설치된 컴패니언을 내장 버전으로 맞춘다. 최초 설치는 여기서 하지 않는다.
        private void SyncBridgeInstallation(Int32? processId)
        {
            try
            {
                var projectPath = processId.HasValue
                    ? UnityProjectLocator.TryGetProjectPath(processId.Value)
                    : UnityProjectLocator.TryGetAnyProjectPath();

                if (projectPath == null)
                {
                    this.RefreshBridgeStatus();
                    return;
                }

                var state = UnityBridgeInstaller.GetState(projectPath);
                if (state == BridgeInstallState.UpToDate)
                {
                    this.RefreshBridgeStatus();
                    return;
                }

                if (this.HasBridgeAutoInstallConsent())
                {
                    this.RepairBridge(projectPath, state);
                }
                else
                {
                    this.PromptForBridgeInstall(projectPath);
                }

                this.RefreshBridgeStatus();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to synchronize the Unity bridge installation");
            }
        }

        // 동의는 받았지만 패키지가 없거나 낡았다. 조용히 고치되, 사용자의 프로젝트에 파일을 쓴 사실은 반드시 알린다.
        private void RepairBridge(String projectPath, BridgeInstallState state)
        {
            // 플레이 중이거나 컴파일 중에 파일을 쓰면 에디터가 스크립트를 다시 컴파일하면서 플레이 모드가 튕긴다.
            // 에디터가 한가해지면 컴패니언이 알려주고(EditorStateChanged), 그때 다시 시도한다.
            if (this.Bridge?.IsEditorBusy == true)
            {
                this._deferredInstallProjectPath = projectPath;
                PluginLog.Info($"Unity is busy; deferring the bridge install for '{projectPath}'");
                return;
            }

            this._deferredInstallProjectPath = null;

            if (!UnityBridgeInstaller.Install(projectPath))
            {
                return;
            }

            var verb = state == BridgeInstallState.Outdated ? "updated" : "reinstalled";
            this.ShowTrayNotification(
                $"Unity bridge {verb}",
                $"Version {UnityBridgeInstaller.EmbeddedVersion} was written to '{System.IO.Path.GetFileName(projectPath)}'. The editor will recompile its scripts.");
        }

        // 아직 동의하지 않았다. 설치는 절대 하지 않고, 어떻게 켜는지만 알려준다.
        // SDK에는 모달 대화상자가 없어서(INativeGui에는 balloon tip과 beep뿐) 이게 사용자를 붙잡을 수 있는 유일한 수단이다.
        private void PromptForBridgeInstall(String projectPath)
        {
            // 같은 프로젝트로 반복해서 귀찮게 하지 않는다. 놓쳤다면 Options+ 경고 배너가 남아 있다.
            if (!this._notifiedProjects.Add(projectPath))
            {
                return;
            }

            this.ShowTrayNotification(
                "Unity bridge is not installed",
                $"Press the 'Install Bridge' command to control '{System.IO.Path.GetFileName(projectPath)}' directly. Until then, commands fall back to keyboard shortcuts.");
        }

        private void ShowTrayNotification(String title, String text)
        {
            if (this.NativeGui == null)
            {
                PluginLog.Warning($"No native GUI; cannot show tray notification '{title}'");
                return;
            }

            this.NativeGui.ShowBalloonTip(text, title, BalloonTipIcon.Info);
            PluginLog.Info($"Tray notification shown: '{title}'");
        }

        internal Boolean HasBridgeAutoInstallConsent() =>
            this.TryGetPluginSetting(AutoInstallConsentSetting, out var value) && value == "1";

        internal void SetBridgeAutoInstallConsent(Boolean consented)
        {
            // 사용자가 브리지를 제거하면 다음 Unity 실행 때 다시 알릴 수 있어야 한다.
            this._notifiedProjects.Clear();

            if (consented)
            {
                this.SetPluginSetting(AutoInstallConsentSetting, "1");
            }
            else
            {
                this.DeletePluginSetting(AutoInstallConsentSetting);
            }
        }

        // 브리지가 없으면 Options+ 에 경고 배너를 띄워, 사용자가 왜 커맨드가 핫키로 동작하는지 알 수 있게 한다.
        // 참고: Plugin.PluginStatus 프로퍼티가 동명의 enum을 가리므로 네임스페이스를 명시해야 한다.
        internal void RefreshBridgeStatus()
        {
            var projectPath = UnityProjectLocator.TryGetAnyProjectPath();

            if (projectPath == null)
            {
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Normal, "");
                return;
            }

            var state = UnityBridgeInstaller.GetState(projectPath);
            if (state == BridgeInstallState.UpToDate)
            {
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Normal, "");
                return;
            }

            this.OnPluginStatusChanged(
                Loupedeck.PluginStatus.Warning,
                "The Unity bridge is not installed. Press the 'Install Bridge' command to enable direct editor control.");
        }
    }
}
