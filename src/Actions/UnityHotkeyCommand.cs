namespace Loupedeck.LogiForUnityPlugin
{
    using System;
    using System.Collections.Generic;

    // Unity 에디터의 기본 단축키를 Loupedeck 커맨드로 노출하는 클래스.
    // 단축키 하나를 액션 파라미터 하나로 등록하므로, 커맨드 클래스는 이 하나만으로 충분하다.

    public class UnityHotkeyCommand : PluginDynamicCommand
    {
        private const String GroupTools = "Tools";
        private const String GroupPlayback = "Playback";
        private const String GroupScene = "Scene View";
        private const String GroupEdit = "Edit";
        private const String GroupFile = "File";
        private const String GroupWindows = "Windows";

        private readonly Dictionary<String, KeyboardShortcut> _shortcuts = new Dictionary<String, KeyboardShortcut>();

        public UnityHotkeyCommand()
            : base()
        {
            // 트랜스폼 툴 (씬 뷰 좌측 상단 툴바)
            this.AddHotkey("tool-hand", "Hand", GroupTools, VirtualKeyCode.KeyQ);                    // Q: 손 툴 (뷰 이동)
            this.AddHotkey("tool-move", "Move", GroupTools, VirtualKeyCode.KeyW);                    // W: 이동 툴
            this.AddHotkey("tool-rotate", "Rotate", GroupTools, VirtualKeyCode.KeyE);                // E: 회전 툴
            this.AddHotkey("tool-scale", "Scale", GroupTools, VirtualKeyCode.KeyR);                  // R: 스케일 툴
            this.AddHotkey("tool-rect", "Rect", GroupTools, VirtualKeyCode.KeyT);                    // T: 렉트 툴 (UI/2D)
            this.AddHotkey("tool-transform", "Transform", GroupTools, VirtualKeyCode.KeyY);          // Y: 통합 트랜스폼 툴

            // 플레이 모드 제어
            this.AddHotkey("play", "Play", GroupPlayback, VirtualKeyCode.KeyP, ModifierKey.Control);                                 // Ctrl+P: 플레이 시작/정지
            this.AddHotkey("pause", "Pause", GroupPlayback, VirtualKeyCode.KeyP, ModifierKey.Control | ModifierKey.Shift);           // Ctrl+Shift+P: 일시정지
            this.AddHotkey("step", "Step", GroupPlayback, VirtualKeyCode.KeyP, ModifierKey.Control | ModifierKey.Alt);               // Ctrl+Alt+P: 한 프레임 전진

            // 씬 뷰 탐색
            this.AddHotkey("frame-selected", "Frame Selected", GroupScene, VirtualKeyCode.KeyF);                       // F: 선택 오브젝트로 카메라 이동
            this.AddHotkey("lock-view", "Lock View", GroupScene, VirtualKeyCode.KeyF, ModifierKey.Shift);              // Shift+F: 선택 오브젝트에 뷰 고정
            this.AddHotkey("toggle-pivot", "Pivot / Center", GroupScene, VirtualKeyCode.KeyZ);                         // Z: 기즈모 기준을 피벗/중심으로 전환
            this.AddHotkey("toggle-space", "Global / Local", GroupScene, VirtualKeyCode.KeyX);                         // X: 기즈모 좌표계를 전역/로컬로 전환
            this.AddHotkey("maximize-view", "Maximize View", GroupScene, VirtualKeyCode.Space, ModifierKey.Shift);     // Shift+Space: 마우스가 올라간 뷰를 최대화

            // 편집
            this.AddHotkey("undo", "Undo", GroupEdit, VirtualKeyCode.KeyZ, ModifierKey.Control);                                     // Ctrl+Z: 실행 취소
            this.AddHotkey("redo", "Redo", GroupEdit, VirtualKeyCode.KeyY, ModifierKey.Control);                                     // Ctrl+Y: 다시 실행
            this.AddHotkey("duplicate", "Duplicate", GroupEdit, VirtualKeyCode.KeyD, ModifierKey.Control);                           // Ctrl+D: 선택 오브젝트 복제
            this.AddHotkey("new-empty", "New GameObject", GroupEdit, VirtualKeyCode.KeyN, ModifierKey.Control | ModifierKey.Shift);  // Ctrl+Shift+N: 빈 게임오브젝트 생성

            // Shift+Del(확인 없이 삭제)은 의도적으로 넣지 않는다. Unity 안에서도 되돌릴 수 없는 유일한 핫키라,
            // 물리 버튼 하나에 매핑해 두기에는 위험하다.
            this.AddHotkey("new-child", "New Child", GroupEdit, VirtualKeyCode.KeyN, ModifierKey.Alt | ModifierKey.Shift);           // Alt+Shift+N: 선택 오브젝트의 자식으로 생성

            // 파일 및 빌드
            this.AddHotkey("save", "Save", GroupFile, VirtualKeyCode.KeyS, ModifierKey.Control);                                         // Ctrl+S: 씬 저장
            this.AddHotkey("save-as", "Save As", GroupFile, VirtualKeyCode.KeyS, ModifierKey.Control | ModifierKey.Shift);               // Ctrl+Shift+S: 다른 이름으로 씬 저장
            this.AddHotkey("build-and-run", "Build and Run", GroupFile, VirtualKeyCode.KeyB, ModifierKey.Control);                       // Ctrl+B: 빌드 후 실행
            this.AddHotkey("build-settings", "Build Settings", GroupFile, VirtualKeyCode.KeyB, ModifierKey.Control | ModifierKey.Shift); // Ctrl+Shift+B: 빌드 설정 창 열기

            // 에디터 창 전환
            this.AddHotkey("window-scene", "Scene", GroupWindows, VirtualKeyCode.Key1, ModifierKey.Control);                            // Ctrl+1: 씬 뷰
            this.AddHotkey("window-game", "Game", GroupWindows, VirtualKeyCode.Key2, ModifierKey.Control);                              // Ctrl+2: 게임 뷰
            this.AddHotkey("window-inspector", "Inspector", GroupWindows, VirtualKeyCode.Key3, ModifierKey.Control);                    // Ctrl+3: 인스펙터
            this.AddHotkey("window-hierarchy", "Hierarchy", GroupWindows, VirtualKeyCode.Key4, ModifierKey.Control);                    // Ctrl+4: 하이어라키
            this.AddHotkey("window-project", "Project", GroupWindows, VirtualKeyCode.Key5, ModifierKey.Control);                        // Ctrl+5: 프로젝트
            this.AddHotkey("window-animation", "Animation", GroupWindows, VirtualKeyCode.Key6, ModifierKey.Control);                    // Ctrl+6: 애니메이션
            this.AddHotkey("window-profiler", "Profiler", GroupWindows, VirtualKeyCode.Key7, ModifierKey.Control);                      // Ctrl+7: 프로파일러
            this.AddHotkey("window-console", "Console", GroupWindows, VirtualKeyCode.KeyC, ModifierKey.Control | ModifierKey.Shift);    // Ctrl+Shift+C: 콘솔
        }

        private void AddHotkey(String name, String displayName, String groupName, VirtualKeyCode key, ModifierKey modifiers = ModifierKey.None)
        {
            this._shortcuts.Add(name, new KeyboardShortcut(key, modifiers));
            this.AddParameter(name, displayName, groupName);
        }

        protected override void RunCommand(String actionParameter)
        {
            if (!this._shortcuts.TryGetValue(actionParameter ?? String.Empty, out var shortcut))
            {
                PluginLog.Warning($"Unknown hotkey parameter '{actionParameter}'");
                return;
            }

            // 합성 키는 포커스를 가진 창으로 간다. Unity가 활성 상태가 아니면 쏘지 않는다.
            // 이 가드가 없으면 'delete'(Shift+Del) 같은 되돌릴 수 없는 단축키가 엉뚱한 창에 꽂힐 수 있다.
            // SendKeyboardShortcut 이 대상 앱을 먼저 활성화하는지는 확인되지 않았으므로, 여기서 명시적으로 막는다.
            var unity = this.Plugin.ClientApplication;
            if (!unity.IsRunning() || !unity.IsActive())
            {
                PluginLog.Info($"Unity is not the active application; hotkey '{actionParameter}' was ignored");
                return;
            }

            unity.SendKeyboardShortcut(shortcut);
        }
    }
}
