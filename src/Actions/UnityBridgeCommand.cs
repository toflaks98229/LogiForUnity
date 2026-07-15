namespace Loupedeck.LogiForUnityPlugin
{
    using System;
    using System.Collections.Generic;

    // 컴패니언 브리지를 통해 Unity 에디터 API를 직접 호출하는 커맨드.
    //
    // 핫키와 달리 Unity의 Shortcut Manager 설정이나 에디터 버전에 영향을 받지 않는다.
    //
    // 브리지가 없을 때 핫키로 폴백하지 않는다. 폴백은 같은 버튼이 상황에 따라 다른 일을 하게 만들고
    // (브리지는 isPlaying을 뒤집지만, 핫키는 사용자가 Ctrl+P에 무엇을 바인딩했든 그걸 실행한다),
    // 하필 가장 자주 폴백이 발동하는 순간이 도메인 리로드 중이다. 조용히 엉뚱한 키를 쏘느니 아무것도 하지 않는다.

    public class UnityBridgeCommand : PluginDynamicCommand
    {
        private const String GroupPlayback = "Bridge · Playback";
        private const String GroupTools = "Bridge · Tools";
        private const String GroupScene = "Bridge · Scene";
        private const String GroupEdit = "Bridge · Edit";
        private const String GroupFile = "Bridge · File";
        private const String GroupWindows = "Bridge · Windows";
        private const String GroupDiagnostics = "Bridge · Diagnostics";

        // 그룹별 강조색. 아이콘이 아직 없는 버튼은 이 색 띠로 그룹을 구분한다.
        private static readonly Dictionary<String, BitmapColor> GroupAccents = new Dictionary<String, BitmapColor>
        {
            [GroupPlayback] = new BitmapColor(76, 175, 80),
            [GroupTools] = new BitmapColor(33, 150, 243),
            [GroupScene] = new BitmapColor(156, 39, 176),
            [GroupEdit] = new BitmapColor(255, 152, 0),
            [GroupFile] = new BitmapColor(0, 172, 193),
            [GroupWindows] = new BitmapColor(120, 124, 130),
            [GroupDiagnostics] = new BitmapColor(96, 96, 96),
        };

        private readonly Dictionary<String, String> _commands = new Dictionary<String, String>();

        public UnityBridgeCommand()
            : base()
        {
            // 플레이 모드
            this.AddAction("play", "Play", GroupPlayback);      // EditorApplication.isPlaying 토글
            this.AddAction("pause", "Pause", GroupPlayback);    // EditorApplication.isPaused 토글
            this.AddAction("step", "Step", GroupPlayback);      // 한 프레임 전진

            // 트랜스폼 툴 (Tools.current 직접 설정)
            this.AddAction("tool.hand", "Hand", GroupTools);
            this.AddAction("tool.move", "Move", GroupTools);
            this.AddAction("tool.rotate", "Rotate", GroupTools);
            this.AddAction("tool.scale", "Scale", GroupTools);
            this.AddAction("tool.rect", "Rect", GroupTools);
            this.AddAction("tool.transform", "Transform", GroupTools);

            // 씬 뷰
            this.AddAction("frame", "Frame Selected", GroupScene);      // 선택 오브젝트로 카메라 이동
            this.AddAction("pivot.toggle", "Pivot / Center", GroupScene);
            this.AddAction("space.toggle", "Global / Local", GroupScene);

            // 편집
            this.AddAction("undo", "Undo", GroupEdit);                          // Undo.PerformUndo()
            this.AddAction("redo", "Redo", GroupEdit);                          // Undo.PerformRedo()
            this.AddAction("duplicate", "Duplicate", GroupEdit);
            this.AddAction("gameobject.empty", "New GameObject", GroupEdit);

            // 파일
            this.AddAction("save", "Save Scenes", GroupFile);                   // EditorSceneManager.SaveOpenScenes()
            this.AddAction("build.settings", "Build Settings", GroupFile);

            // 에디터 창
            this.AddAction("window.scene", "Scene", GroupWindows);
            this.AddAction("window.game", "Game", GroupWindows);
            this.AddAction("window.inspector", "Inspector", GroupWindows);
            this.AddAction("window.hierarchy", "Hierarchy", GroupWindows);
            this.AddAction("window.project", "Project", GroupWindows);
            this.AddAction("window.console", "Console", GroupWindows);

            this.AddAction("ping", "Ping Bridge", GroupDiagnostics);            // 연결 확인용
        }

        // 아이콘을 그릴 때 필요한 그룹 색상. 파라미터에서 그룹을 되찾을 방법이 없어 따로 보관한다.
        private readonly Dictionary<String, String> _groups = new Dictionary<String, String>();

        private void AddAction(String name, String displayName, String groupName)
        {
            this._commands.Add(name, $"{{\"cmd\":\"{name}\"}}");
            this._groups.Add(name, groupName);
            this.AddParameter(name, displayName, groupName);
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            if (!this._groups.TryGetValue(actionParameter ?? String.Empty, out var groupName))
            {
                return base.GetCommandImage(actionParameter, imageSize);
            }

            var label = this.GetParameter(actionParameter)?.DisplayName ?? actionParameter;

            // 브리지가 없으면 회색으로 죽여서, 눌러도 아무 일이 없는 이유를 색으로 알린다.
            var accent = this.Bridge?.IsConnected == true
                ? GroupAccents[groupName]
                : new BitmapColor(70, 70, 70);

            return UnityIcons.Get(actionParameter, label, accent, imageSize);
        }

        private UnityBridgeServer Bridge => (this.Plugin as LogiForUnityPlugin)?.Bridge;

        protected override void RunCommand(String actionParameter)
        {
            if (!this._commands.TryGetValue(actionParameter ?? String.Empty, out var json))
            {
                PluginLog.Warning($"Unknown bridge parameter '{actionParameter}'");
                return;
            }

            var bridge = this.Bridge;
            if (bridge == null || !bridge.TrySend(json))
            {
                // 스크립트 재컴파일이나 도메인 리로드 중이면 몇 초 뒤 다시 연결된다. 그때 다시 누르면 된다.
                PluginLog.Info($"Bridge is not connected; '{actionParameter}' was ignored");
                this.ActionImageChanged(actionParameter);
                return;
            }

            PluginLog.Verbose($"Bridge command sent: {json}");
        }

        // 브리지 연결 여부를 버튼에 표시해 사용자가 왜 버튼이 반응하지 않는지 알 수 있게 한다.
        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            if (!this._commands.ContainsKey(actionParameter ?? String.Empty))
            {
                return base.GetCommandDisplayName(actionParameter, imageSize);
            }

            var label = this.GetParameter(actionParameter)?.DisplayName ?? actionParameter;
            return this.Bridge?.IsConnected == true ? label : $"{label}{Environment.NewLine}(no bridge)";
        }
    }
}
