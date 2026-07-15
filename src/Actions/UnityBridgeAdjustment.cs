namespace Loupedeck.LogiForUnityPlugin
{
    using System;
    using System.Collections.Generic;

    // 다이얼(회전 노브)로 Unity 의 연속값을 조정한다.
    //
    // 핫키로는 불가능한 영역이다. Unity 에는 "값을 조금씩 증감"하는 단축키가 없다.
    // 다이얼을 돌린 틱 수(diff)를 브리지로 보내면, 컴패니언이 실제 값 변화로 환산해 적용하고
    // 새 값을 되돌려 보고한다. 그 값이 다이얼 옆에 표시된다.

    public class UnityBridgeAdjustment : PluginDynamicAdjustment
    {
        private const String GroupTransform = "Bridge · Transform";
        private const String GroupView = "Bridge · View";

        private readonly HashSet<String> _targets = new HashSet<String>(StringComparer.Ordinal);

        // hasReset: true 면 각 다이얼에 대응하는 리셋 커맨드가 자동으로 만들어진다(다이얼 누름).
        public UnityBridgeAdjustment()
            : base(hasReset: true)
        {
            this.AddTarget("move.x", "Move X", GroupTransform);
            this.AddTarget("move.y", "Move Y", GroupTransform);
            this.AddTarget("move.z", "Move Z", GroupTransform);

            this.AddTarget("rotate.x", "Rotate X", GroupTransform);
            this.AddTarget("rotate.y", "Rotate Y", GroupTransform);
            this.AddTarget("rotate.z", "Rotate Z", GroupTransform);

            this.AddTarget("scale.uniform", "Scale", GroupTransform);

            this.AddTarget("scene.zoom", "Scene Zoom", GroupView);
            this.AddTarget("time.scale", "Time Scale", GroupView);
        }

        private UnityBridgeServer Bridge => (this.Plugin as LogiForUnityPlugin)?.Bridge;

        private void AddTarget(String name, String displayName, String groupName)
        {
            this._targets.Add(name);
            this.AddParameter(name, displayName, groupName);
        }

        // PluginDynamicAction.Load/Unload 는 virtual 이 아니고, 생성자 시점에는 아직 Plugin 이 없다.
        // 그래서 브리지에 처음 접근할 때 구독한다. 브리지는 플러그인 수명 내내 같은 인스턴스다.
        private UnityBridgeServer _subscribedBridge;

        private void EnsureSubscribed()
        {
            var bridge = this.Bridge;
            if (bridge == null || ReferenceEquals(bridge, this._subscribedBridge))
            {
                return;
            }

            bridge.AdjustmentValueReported += this.OnAdjustmentValueReported;
            this._subscribedBridge = bridge;
        }

        // 컴패니언이 새 값을 보고했다. 다이얼 옆의 표시를 갱신한다.
        private void OnAdjustmentValueReported(Object sender, String target) => this.AdjustmentValueChanged(target);

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            if (!this._targets.Contains(actionParameter ?? String.Empty))
            {
                PluginLog.Warning($"Unknown adjustment target '{actionParameter}'");
                return;
            }

            this.EnsureSubscribed();
            this.Send($"{{\"cmd\":\"adjust\",\"target\":\"{actionParameter}\",\"diff\":{diff}}}", actionParameter);
        }

        // 다이얼을 누르면 불린다.
        protected override void RunCommand(String actionParameter)
        {
            if (!this._targets.Contains(actionParameter ?? String.Empty))
            {
                return;
            }

            this.Send($"{{\"cmd\":\"reset\",\"target\":\"{actionParameter}\"}}", actionParameter);
        }

        private void Send(String json, String actionParameter)
        {
            var bridge = this.Bridge;
            if (bridge == null || !bridge.TrySend(json))
            {
                // 브리지가 없으면 조용히 무시한다. 키보드로는 흉내낼 수 없는 동작이라 폴백이 존재하지 않는다.
                PluginLog.Info($"Bridge is not connected; adjustment '{actionParameter}' was ignored");
            }
        }

        // 선택된 오브젝트가 없으면 컴패니언이 값을 보고하지 않는다. 그때는 대시를 보여준다.
        protected override String GetAdjustmentValue(String actionParameter)
        {
            this.EnsureSubscribed();
            return this.Bridge?.TryGetAdjustmentValue(actionParameter) ?? "—";
        }
    }
}
