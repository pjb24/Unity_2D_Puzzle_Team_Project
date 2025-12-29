///
/// 목적
/// “상태가 바뀌었다” 같은 상위 흐름 신호를 외부로 event 노출 없이 전달
/// UI/HUD/로그/디버그 오버레이 등이 구독하는 용도
///

using System;

public class GameFlowSignalBus
{
    private Action<E_GameFlowState> _onFlowStateChanged;

    public void AddListenerOnFlowStateChanged(Action<E_GameFlowState> listener)
        => _onFlowStateChanged += listener;

    public void RemoveListenerOnFlowStateChanged(Action<E_GameFlowState> listener)
        => _onFlowStateChanged -= listener;

    public void RaiseFlowStateChanged(E_GameFlowState state)
        => _onFlowStateChanged?.Invoke(state);
}
