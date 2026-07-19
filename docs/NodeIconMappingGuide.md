# LogiForUnity — 노드 레퍼런스 & 아이콘 매핑 가이드

이 문서는 세 부분으로 구성됩니다.

- **[PART A] 각 노드의 기능 설명문** — 현재 존재하는 모든 노드(버튼·다이얼)의 역할.
- **[PART B] 필요 매핑 이미지 아이콘 목록** — 각 노드에 매핑할 아이콘 파일 이름.
- **[PART C] 경로 & 매핑 안정장치** — 아이콘을 어디에 어떤 이름으로 넣는지, 없을 때 무엇이 대신 출력되는지.

> **용어:** 여기서 "노드"는 Logi 기기에 배치할 수 있는 하나의 액션 파라미터(버튼 한 개 또는 다이얼 한 개)를 뜻합니다. 하나의 C# 클래스가 여러 노드를 등록합니다.

---

## 매핑 메커니즘 개요 (먼저 읽으세요)

Logi Plugin Service가 노드 아이콘을 찾는 경로는 두 가지입니다.

### ① `actionicons/` 폴더 방식 (클래스 단위) — ⚠️ 이 플러그인에는 부적합
`.lplug4` 패키지 루트의 `actionicons/` 폴더에 **`<전체 클래스명>.svg`** 파일을 두면 코드 수정 없이 자동으로 매핑됩니다. 그러나 파일 이름이 **클래스 전체 이름 하나**뿐이라, `UnityBridgeCommand`(25개 노드)·`UnityHotkeyCommand`(31개 노드)처럼 **한 클래스가 여러 노드를 갖는 이 플러그인에서는 노드별로 다른 아이콘을 줄 수 없습니다.** (모든 Play/Pause/Move 버튼이 같은 그림이 됩니다.)

### ② `GetCommandImage` / `GetAdjustmentImage` 방식 (노드 단위) — ✅ 이 플러그인이 써야 하는 방식
메서드가 `actionParameter`를 받으므로 **노드마다 다른 이미지**를 반환할 수 있습니다. 이 플러그인은 이미 이 경로를 사용합니다([UnityIcons.Get](../src/Helpers/UnityIcons.cs)). 반환 이미지는 PNG(래스터) 또는 SVG(벡터) 모두 가능합니다.

```csharp
// SVG를 노드 아이콘으로 반환하는 최소 예시
protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
    => BitmapImage.FromResource(this.Plugin.Assembly,
        "Loupedeck.LogiForUnityPlugin.Resources.Icons.tool-move.svg");
```

### SVG 색상 규칙 (Logi 사양)
- **흑백(monochrome) SVG:** 프로필/플러그인의 색상 설정이 SVG 색을 **덮어씁니다(틴팅 가능).**
- **다색(multicolor) SVG:** SVG 자체 색이 **그대로 유지**됩니다.
- 두 경우 모두 **배경색과 텍스트(타이틀) 색**은 설정에 따라 바뀝니다.
- 👉 그룹 강조색이나 "브리지 끊김=회색" 같은 **동적 색 반영을 원하면 흑백 SVG로 제작**하세요. 색을 고정하고 싶으면 다색 SVG로 제작합니다. (이 트레이드오프는 PART C 참조.)

### 현재 코드 상태 요약
| 클래스 | 커스텀 아이콘 파이프라인 | 현재 지원 형식 | 미지정 시 출력 |
|---|---|---|---|
| `UnityBridgeCommand` | ✅ 연결됨(`GetCommandImage`→`UnityIcons`) | **PNG만** | 코드 벡터 아트 → 그룹색 띠 + 라벨 |
| `UnityHotkeyCommand` | ❌ 미연결 | — | SDK 기본(**타이틀 이름만**) |
| `UnityBridgeAdjustment` | ❌ 미연결 | — | SDK 기본(**이름 + 현재값**) |
| `UnityBridgeInstallCommand` | ❌ 미연결 | — | SDK 기본(**이름 + 설치상태**) |

> SVG를 쓰려면 `UnityIcons.Render`에 `.svg` 탐색 단계를 추가하고, 아래 3개 클래스에 `GetCommandImage`/`GetAdjustmentImage` 오버라이드를 더해야 합니다. 구현 방법은 PART C 마지막 절에 정리했습니다.

---

# PART A — 각 노드의 기능 설명문

## A-1. `UnityHotkeyCommand` — 핫키 폴백 (합성 키보드 입력)
전체 클래스명: `Loupedeck.LogiForUnityPlugin.UnityHotkeyCommand`
동작: Unity가 실행 중이고 **활성 창일 때만** 합성 키를 전송합니다([RunCommand 가드](../src/Actions/UnityHotkeyCommand.cs#L87-L92)). 사용자의 Shortcut Manager 설정에 따라 결과가 달라질 수 있습니다.

| 노드(파라미터) | 표시명 | 그룹 | 단축키 | 기능 |
|---|---|---|---|---|
| `tool-hand` | Hand | Tools | Q | 손(뷰 이동) 툴 선택 |
| `tool-move` | Move | Tools | W | 이동 툴 |
| `tool-rotate` | Rotate | Tools | E | 회전 툴 |
| `tool-scale` | Scale | Tools | R | 스케일 툴 |
| `tool-rect` | Rect | Tools | T | 렉트 툴(UI/2D) |
| `tool-transform` | Transform | Tools | Y | 통합 트랜스폼 툴 |
| `play` | Play | Playback | Ctrl+P | 플레이 모드 시작/정지 |
| `pause` | Pause | Playback | Ctrl+Shift+P | 일시정지 토글 |
| `step` | Step | Playback | Ctrl+Alt+P | 한 프레임 전진 |
| `frame-selected` | Frame Selected | Scene View | F | 선택 오브젝트로 카메라 이동 |
| `lock-view` | Lock View | Scene View | Shift+F | 선택 오브젝트에 뷰 고정(추적) |
| `toggle-pivot` | Pivot / Center | Scene View | Z | 기즈모 기준을 피벗/중심으로 전환 |
| `toggle-space` | Global / Local | Scene View | X | 기즈모 좌표계를 전역/로컬로 전환 |
| `maximize-view` | Maximize View | Scene View | Shift+Space | 마우스가 올라간 뷰 최대화 |
| `undo` | Undo | Edit | Ctrl+Z | 실행 취소 |
| `redo` | Redo | Edit | Ctrl+Y | 다시 실행 |
| `duplicate` | Duplicate | Edit | Ctrl+D | 선택 오브젝트 복제 |
| `new-empty` | New GameObject | Edit | Ctrl+Shift+N | 빈 게임오브젝트 생성 |
| `new-child` | New Child | Edit | Alt+Shift+N | 선택 오브젝트의 자식으로 생성 |
| `save` | Save | File | Ctrl+S | 씬 저장 |
| `save-as` | Save As | File | Ctrl+Shift+S | 다른 이름으로 씬 저장 |
| `build-and-run` | Build and Run | File | Ctrl+B | 빌드 후 실행 |
| `build-settings` | Build Settings | File | Ctrl+Shift+B | 빌드 설정 창 열기 |
| `window-scene` | Scene | Windows | Ctrl+1 | 씬 뷰 |
| `window-game` | Game | Windows | Ctrl+2 | 게임 뷰 |
| `window-inspector` | Inspector | Windows | Ctrl+3 | 인스펙터 |
| `window-hierarchy` | Hierarchy | Windows | Ctrl+4 | 하이어라키 |
| `window-project` | Project | Windows | Ctrl+5 | 프로젝트 |
| `window-animation` | Animation | Windows | Ctrl+6 | 애니메이션 |
| `window-profiler` | Profiler | Windows | Ctrl+7 | 프로파일러 |
| `window-console` | Console | Windows | Ctrl+Shift+C | 콘솔 |

## A-2. `UnityBridgeCommand` — 브리지 직접 호출
전체 클래스명: `Loupedeck.LogiForUnityPlugin.UnityBridgeCommand`
동작: 컴패니언을 통해 에디터 API를 직접 호출합니다. 사용자의 단축키 설정·Unity 버전과 무관합니다. **브리지 미연결 시 아무 동작도 하지 않으며**(폴백 없음), 버튼이 회색으로 죽고 타이틀에 `(no bridge)`가 붙습니다.

| 노드(파라미터) | 표시명 | 그룹 | 강조색(RGB) | 에디터 동작 |
|---|---|---|---|---|
| `play` | Play | Playback | 76,175,80 | `EditorApplication.isPlaying` 토글 |
| `pause` | Pause | Playback | 76,175,80 | `isPaused` 토글 |
| `step` | Step | Playback | 76,175,80 | `EditorApplication.Step()` |
| `tool.hand` | Hand | Tools | 33,150,243 | `Tools.current = View` |
| `tool.move` | Move | Tools | 33,150,243 | `Tools.current = Move` |
| `tool.rotate` | Rotate | Tools | 33,150,243 | `Tools.current = Rotate` |
| `tool.scale` | Scale | Tools | 33,150,243 | `Tools.current = Scale` |
| `tool.rect` | Rect | Tools | 33,150,243 | `Tools.current = Rect` |
| `tool.transform` | Transform | Tools | 33,150,243 | `Tools.current = Transform` |
| `frame` | Frame Selected | Scene | 156,39,176 | `SceneView.FrameSelected()` |
| `pivot.toggle` | Pivot / Center | Scene | 156,39,176 | `Tools.pivotMode` 토글 |
| `space.toggle` | Global / Local | Scene | 156,39,176 | `Tools.pivotRotation` 토글 |
| `undo` | Undo | Edit | 255,152,0 | `Undo.PerformUndo()` |
| `redo` | Redo | Edit | 255,152,0 | `Undo.PerformRedo()` |
| `duplicate` | Duplicate | Edit | 255,152,0 | 메뉴 `Edit/Duplicate` |
| `gameobject.empty` | New GameObject | Edit | 255,152,0 | 메뉴 `GameObject/Create Empty` |
| `save` | Save Scenes | File | 0,172,193 | `EditorSceneManager.SaveOpenScenes()` |
| `build.settings` | Build Settings | File | 0,172,193 | 메뉴 `File/Build Settings...` |
| `window.scene` | Scene | Windows | 120,124,130 | 메뉴 `Window/General/Scene` |
| `window.game` | Game | Windows | 120,124,130 | 메뉴 `Window/General/Game` |
| `window.inspector` | Inspector | Windows | 120,124,130 | 메뉴 `Window/General/Inspector` |
| `window.hierarchy` | Hierarchy | Windows | 120,124,130 | 메뉴 `Window/General/Hierarchy` |
| `window.project` | Project | Windows | 120,124,130 | 메뉴 `Window/General/Project` |
| `window.console` | Console | Windows | 120,124,130 | 메뉴 `Window/General/Console` |
| `ping` | Ping Bridge | Diagnostics | 96,96,96 | 연결 확인(pong 왕복) |

## A-3. `UnityBridgeAdjustment` — 다이얼 (연속값 조정)
전체 클래스명: `Loupedeck.LogiForUnityPlugin.UnityBridgeAdjustment`
동작: 다이얼을 돌린 틱 수(diff)를 브리지로 보내고, 컴패니언이 실제 값으로 환산·적용한 뒤 새 값을 회신하면 다이얼 옆에 표시합니다. **다이얼 누름 = 리셋**(`hasReset:true`). 선택 오브젝트가 없으면 값 표시가 `—`.

| 노드(파라미터) | 표시명 | 그룹 | 틱당 변화 | 누름(리셋) |
|---|---|---|---|---|
| `move.x` | Move X | Transform | localPosition.x ±0.1 | 0 으로 |
| `move.y` | Move Y | Transform | localPosition.y ±0.1 | 0 으로 |
| `move.z` | Move Z | Transform | localPosition.z ±0.1 | 0 으로 |
| `rotate.x` | Rotate X | Transform | localEulerAngles.x ±1° | 0 으로 |
| `rotate.y` | Rotate Y | Transform | localEulerAngles.y ±1° | 0 으로 |
| `rotate.z` | Rotate Z | Transform | localEulerAngles.z ±1° | 0 으로 |
| `scale.uniform` | Scale | Transform | localScale ±0.05(≥0) | (1,1,1) 으로 |
| `scene.zoom` | Scene Zoom | View | 씬 카메라 size ×(0.95)^diff | Frame Selected |
| `time.scale` | Time Scale | View | Time.timeScale ±0.05(0~10) | 1.0 으로 |

> 참고: `hasReset:true`로 각 다이얼에 자동 생성되는 "리셋(누름)" 커맨드는 다이얼과 **같은 파라미터 이름을 공유**하므로 별도 아이콘 이름이 필요 없습니다.

## A-4. `UnityBridgeInstallCommand` — 컴패니언 설치/제거
전체 클래스명: `Loupedeck.LogiForUnityPlugin.UnityBridgeInstallCommand`
동작: 남의 Unity 프로젝트에 코드를 쓰는 작업이므로 **최초 설치는 사용자가 직접 이 버튼을 눌러야** 합니다. 타이틀에 현재 설치 상태(`installed`/`outdated`/`not installed`/`no Unity`)를 표시합니다.

| 노드(파라미터) | 표시명 | 그룹 | 기능 |
|---|---|---|---|
| `install` | Install Bridge | Bridge | 강제 설치(손상 복구 포함) + 자동설치 동의 기록 |
| `uninstall` | Remove Bridge | Bridge | 패키지 제거 + 동의 취소 |

---

# PART B — 필요 매핑 이미지 아이콘 목록

**규칙:** 아이콘 파일 base 이름 = **노드 파라미터 이름의 점(`.`)을 하이픈(`-`)으로 치환**한 값. 확장자는 `.svg`(권장) 또는 `.png`.
아이콘은 파라미터 이름으로 매핑되므로, **여러 클래스가 같은 파라미터 이름을 쓰면 하나의 파일을 공유**합니다(예: 브리지 `tool.move`와 핫키 `tool-move`는 모두 `tool-move.svg`).

### B-1. 노드 아이콘 매니페스트 (권장 SVG 파일명)

| 아이콘 파일 (base) | 쓰는 노드 | 현재 코드 벡터 아트 |
|---|---|---|
| `play.svg` | 핫키 play, 브리지 play | ✅ 있음 |
| `pause.svg` | 핫키 pause, 브리지 pause | ✅ |
| `step.svg` | 핫키 step, 브리지 step | ✅ |
| `tool-hand.svg` | 핫키 tool-hand, 브리지 tool.hand | ✅ |
| `tool-move.svg` | 핫키 tool-move, 브리지 tool.move | ✅ |
| `tool-rotate.svg` | 핫키 tool-rotate, 브리지 tool.rotate | ✅ |
| `tool-scale.svg` | 핫키 tool-scale, 브리지 tool.scale | ✅ |
| `tool-rect.svg` | 핫키 tool-rect, 브리지 tool.rect | ✅ |
| `tool-transform.svg` | 핫키 tool-transform, 브리지 tool.transform | ✅ |
| `frame.svg` | 브리지 frame | ✅ |
| `frame-selected.svg` | 핫키 frame-selected | ➖ (별칭 필요, 아래 주석) |
| `pivot-toggle.svg` | 브리지 pivot.toggle | ✅ |
| `toggle-pivot.svg` | 핫키 toggle-pivot | ➖ |
| `space-toggle.svg` | 브리지 space.toggle | ✅ |
| `toggle-space.svg` | 핫키 toggle-space | ➖ |
| `undo.svg` | 핫키 undo, 브리지 undo | ✅ |
| `redo.svg` | 핫키 redo, 브리지 redo | ✅ |
| `duplicate.svg` | 핫키 duplicate, 브리지 duplicate | ✅ |
| `gameobject-empty.svg` | 브리지 gameobject.empty | ✅ |
| `new-empty.svg` | 핫키 new-empty | ➖ |
| `new-child.svg` | 핫키 new-child | ➖ |
| `save.svg` | 핫키 save, 브리지 save | ✅ |
| `save-as.svg` | 핫키 save-as | ➖ |
| `build-settings.svg` | 핫키 build-settings, 브리지 build.settings | ✅ |
| `build-and-run.svg` | 핫키 build-and-run | ➖ |
| `lock-view.svg` | 핫키 lock-view | ➖ |
| `maximize-view.svg` | 핫키 maximize-view | ➖ |
| `window-scene.svg` | 핫키 window-scene, 브리지 window.scene | ✅ |
| `window-game.svg` | 핫키 window-game, 브리지 window.game | ✅ |
| `window-inspector.svg` | 핫키 window-inspector, 브리지 window.inspector | ✅ |
| `window-hierarchy.svg` | 핫키 window-hierarchy, 브리지 window.hierarchy | ✅ |
| `window-project.svg` | 핫키 window-project, 브리지 window.project | ✅ |
| `window-console.svg` | 핫키 window-console, 브리지 window.console | ✅ |
| `window-animation.svg` | 핫키 window-animation | ➖ |
| `window-profiler.svg` | 핫키 window-profiler | ➖ |
| `ping.svg` | 브리지 ping | ✅ |
| `move-x.svg` | 다이얼 move.x | ➖ |
| `move-y.svg` | 다이얼 move.y | ➖ |
| `move-z.svg` | 다이얼 move.z | ➖ |
| `rotate-x.svg` | 다이얼 rotate.x | ➖ |
| `rotate-y.svg` | 다이얼 rotate.y | ➖ |
| `rotate-z.svg` | 다이얼 rotate.z | ➖ |
| `scale-uniform.svg` | 다이얼 scale.uniform | ➖ |
| `scene-zoom.svg` | 다이얼 scene.zoom | ➖ |
| `time-scale.svg` | 다이얼 time.scale | ➖ |
| `install.svg` | 설치 install | ➖ |
| `uninstall.svg` | 설치 uninstall | ➖ |

- **✅ 있음:** [UnityIconArt.cs](../src/Helpers/UnityIconArt.cs)가 코드로 그려주므로 SVG를 넣지 않아도 그림이 나옵니다(브리지 커맨드 한정). SVG를 추가하면 그 SVG가 우선합니다.
- **➖ 없음:** 코드 벡터 아트가 없어, SVG/PNG를 넣지 않으면 **이름(타이틀)만** 표시됩니다.

> **네이밍 불일치 주의:** 핫키와 브리지가 같은 개념인데 파라미터 이름이 다른 경우가 있습니다 — `frame`↔`frame-selected`, `pivot.toggle`↔`toggle-pivot`, `space.toggle`↔`toggle-space`, `gameobject.empty`↔`new-empty`. 아이콘을 공유하려면 (a) 같은 SVG를 두 이름으로 각각 두거나, (b) 파라미터 이름을 통일하는 리팩토링을 권장합니다.

---

# PART C — 경로 & 매핑 안정장치

## C-1. 아이콘을 두는 위치 (경로)

권장(노드 단위, `GetCommandImage` 방식) — **소스 폴더:**
```
src/Resources/Icons/<base-name>.svg      (예: src/Resources/Icons/tool-move.svg)
src/Resources/Icons/<base-name>.png      (PNG도 허용)
```
빌드 시 임베디드 리소스로 포함되며, 어셈블리 내부 리소스 이름은 다음이 됩니다:
```
Loupedeck.LogiForUnityPlugin.Resources.Icons.<base-name>.svg
```
> 현재 `.csproj`는 `Resources\Icons\**\*.png`만 임베딩합니다([LogiForUnityPlugin.csproj:60](../src/LogiForUnityPlugin.csproj#L60)). **SVG를 쓰려면 `*.svg`도 임베딩하도록 아래 한 줄을 추가**해야 합니다:
> ```xml
> <EmbeddedResource Include="Resources\Icons\**\*.svg" />
> ```
> 현재 폴더에는 매핑에 쓰이지 않는 `Unity_Icon.png` 한 개만 있습니다(어떤 파라미터 이름과도 일치하지 않음 → 플러그인 전역/로고 용도로만 사용).

대안(클래스 단위, `actionicons/` 방식) — **패키지 루트:** `actionicons/Loupedeck.LogiForUnityPlugin.<클래스명>.svg`. 앞서 설명한 대로 노드별 구분이 불가능하여 이 플러그인에는 권장하지 않습니다.

## C-2. 매핑 안정장치 (Fallback 체인)

"이미지가 없으면 이름이 출력되게" 하는 안전장치는 **아래 우선순위로 자동 강등**됩니다. 어느 단계에서 실패해도 버튼은 절대 비어 있지 않습니다.

**브리지 커맨드(현재 구현, [UnityIcons.Render](../src/Helpers/UnityIcons.cs#L48)):**
```
1) 임베디드 아트워크가 있으면 사용        (SVG → PNG 순, ※SVG 단계는 추가 필요)
        └ 렌더 실패 시 경고 로그 남기고 다음 단계로
2) 코드 벡터 아트(UnityIconArt)            (그룹 강조색; 브리지 끊김 시 회색)
        └ 그릴 줄 모르는 파라미터면 다음 단계로
3) 그룹색 띠 + 줄바꿈된 라벨(이름) 출력     ← 최종 안전망: 항상 "이름"이 보임
```
**핫키·다이얼·설치 커맨드(현재):** 커스텀 이미지가 없으므로 SDK 기본 렌더가 **타이틀(이름)만** 그립니다. 즉 지금도 "이미지 없으면 이름 출력"은 보장됩니다(다만 그림은 없음).

### 색상(틴팅) 안정장치 — SVG 제작 규칙
- 그룹 강조색·연결상태 회색 처리 등 **동적 색을 유지하려면 흑백 SVG**로 제작(설정이 색을 덮어씀).
- **다색 SVG를 넣으면** 동적 회색 처리가 사라지고 SVG 색이 고정됩니다. → 브리지 커맨드는 "끊김=회색" 피드백을 잃으므로, 브리지 커맨드용 SVG는 **흑백 권장**.
- 배경색·타이틀 색은 두 경우 모두 설정을 따릅니다.

### 제작 규격
- **형식:** SVG(권장) 또는 정사각형 PNG. 벡터가 50/80/116px 전 크기에서 선명합니다.
- **비율:** 정사각형. 렌더 시 버튼을 가득 채우므로 여백은 SVG 내부에서 확보하세요.
- **색:** 위 규칙에 따라 흑백/다색 결정.

## C-3. SVG 지원을 켜기 위한 코드 변경 요약 (선택)

현재는 문서상의 규격이며, 실제로 SVG를 노드에 매핑하려면 다음 작업이 필요합니다.

1. **csproj:** `Resources\Icons\**\*.svg` 임베디드 리소스 추가(C-1).
2. **[UnityIcons.cs](../src/Helpers/UnityIcons.cs):** `DiscoverIcons`가 `.svg`도 수집하도록 하고, `Render`에서 `<param>.svg`가 있으면 `BitmapImage.FromResource(assembly, "…Resources.Icons.<param>.svg")`를 최우선 반환. 실패 시 기존 PNG→벡터→라벨 체인으로 강등.
3. **핫키/다이얼/설치 클래스:** 각각 `GetCommandImage`/`GetAdjustmentImage`를 오버라이드해 동일한 `UnityIcons.Get(...)` 파이프라인을 태우면, 이 세 클래스의 노드에도 아이콘 + 이름-폴백 안전망이 적용됩니다.

> 이 변경들은 동작에 영향을 주므로, 원하시면 별도 작업으로 구현해 드리겠습니다. 문서(PART A/B/C)만으로도 아이콘 제작·배치 규격은 완결됩니다.
