///
/// Phase들이 TurnStateMachine을 참조하니, 팩토리 패턴으로 한 번에 조립한다.
/// 
/// 위 예시는 “생성자에서 sm이 null” 문제가 생길 수 있다.
/// 실무적으로는:
/// TurnStateMachine을 phases 없이 생성 → SetPhases()로 주입 가능하게 바꾸거나
/// 각 Phase가 Action<E_TurnPhase> 전이 함수만 받게 만들어라.
/// 프로토타입 최소 변경안(추천): TurnStateMachine에 SetPhases(ITurnPhase[]) 추가.
///

using UnityEngine;

[DisallowMultipleComponent]
public class TurnDriver : MonoBehaviour
{
    [SerializeField] private FatherController _father;
    [SerializeField] private ChildController _child;
    [SerializeField] private TurnSnapshotRecorder _snapshot;
    [SerializeField] private TurnInputRouter _inputRouter;

    private TurnStateMachine _sm;
    private TurnContext _ctx;
    private TurnInputBuffer _input;

    public bool IsInputLocked => _ctx != null && _ctx.IsInputLocked;

    private void Awake()
    {
        _input = new TurnInputBuffer();
        _ctx = new TurnContext(_father, _child, _snapshot);

        _sm = new TurnStateMachine(_ctx);

        // Router에 버퍼 주입
        if (_inputRouter != null)
        {
            _inputRouter.Initialize(_input);
        }

        // Phase 생성 (이제 sm이 이미 존재하므로 주입 가능)
        var phases = new ITurnPhase[]
        {
            new TurnPhase_Input(_input, _sm),
            new TurnPhase_FatherAction(_sm),
            new TurnPhase_ChildStep(_sm),
            new TurnPhase_Resolve(_sm),
            new TurnPhase_Snapshot(_sm),
            new TurnPhase_End(_sm),
        };

        _sm.SetPhases(phases);
        _sm.Start();
    }

    private void Update()
    {
        _sm.Tick();
    }

    public TurnInputBuffer GetInputBuffer() => _input;
}
