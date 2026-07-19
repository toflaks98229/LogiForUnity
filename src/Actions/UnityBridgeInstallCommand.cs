namespace Loupedeck.LogiForUnityPlugin
{
    using System;

    // 컴패니언 패키지의 설치와 제거.
    //
    // 남의 Unity 프로젝트에 컴파일되는 코드를 쓰는 일이므로, 최초 설치는 반드시 사용자가 이 커맨드를 눌러야 한다.
    // (SDK의 PluginPreference는 계정 입력용이고, INativeGui에는 모달 대화상자가 없어서 버튼이 유일한 동의 수단이다.)
    // 한 번 설치하면 동의가 기록되고, 이후 Unity가 뜰 때마다 플러그인이 자동으로 버전을 맞춘다.

    public class UnityBridgeInstallCommand : PluginDynamicCommand
    {
        private const String Group = "Bridge";

        public UnityBridgeInstallCommand()
            : base()
        {
            this.AddParameter("install", "Install Bridge", Group);
            this.AddParameter("uninstall", "Remove Bridge", Group);
        }

        private LogiForUnityPlugin UnityPlugin => this.Plugin as LogiForUnityPlugin;

        protected override void RunCommand(String actionParameter)
        {
            var projectPath = UnityProjectLocator.TryGetAnyProjectPath();
            if (projectPath == null)
            {
                PluginLog.Warning("No running Unity editor with an open project was found");
                this.ActionImageChanged();
                return;
            }

            switch (actionParameter)
            {
                case "install":
                    // force: 사용자가 명시적으로 눌렀으므로 최신이어도 다시 쓴다. 손상된 설치를 복구하는 유일한 수단이다.
                    UnityBridgeInstaller.Install(projectPath, force: true);
                    this.UnityPlugin?.SetBridgeAutoInstallConsent(true);
                    break;

                case "uninstall":
                    UnityBridgeInstaller.Uninstall(projectPath);
                    this.UnityPlugin?.SetBridgeAutoInstallConsent(false);
                    break;

                default:
                    PluginLog.Warning($"Unknown install parameter '{actionParameter}'");
                    return;
            }

            this.UnityPlugin?.RefreshBridgeStatus();
            this.ActionImageChanged();
        }

        // 커스텀 SVG/PNG 가 있으면 그것을, 없으면 코드 벡터 아이콘(설치=트레이 화살표, 제거=휴지통)을 굽는다.
        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var label = actionParameter == "uninstall" ? "Remove Bridge" : "Install Bridge";
            var accent = actionParameter == "uninstall"
                ? new BitmapColor(229, 57, 53)
                : new BitmapColor(76, 175, 80);
            return UnityIcons.Get(actionParameter, label, accent, imageSize);
        }

        // 버튼에 현재 설치 상태를 그대로 보여준다. 사용자가 무슨 일이 일어났는지 알 수 있어야 한다.
        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            var projectPath = UnityProjectLocator.TryGetAnyProjectPath();
            var state = projectPath == null ? BridgeInstallState.NoProject : UnityBridgeInstaller.GetState(projectPath);

            var status = state switch
            {
                BridgeInstallState.NoProject => "no Unity",
                BridgeInstallState.NotInstalled => "not installed",
                BridgeInstallState.Outdated => "outdated",
                BridgeInstallState.UpToDate => "installed",
                _ => "?",
            };

            var label = actionParameter == "uninstall" ? "Remove Bridge" : "Install Bridge";
            return $"{label}{Environment.NewLine}({status})";
        }
    }
}
