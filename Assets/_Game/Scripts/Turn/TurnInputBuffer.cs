// TurnInputBuffer.cs
///
/// 입력 수집은 계속 가능(키 입력 감지)
/// 하지만 턴 명령 enqueue는 InputUnlocked에서만 허용(또는 dequeue만 Input에서 수행)
/// 즉, TurnInputRouter는 “현재 입력 잠금인지” 확인 후 Enqueue 차단
/// 또는 더 단순하게: router는 무조건 enqueue, InputPhase에서만 dequeue
/// (이 방식이면 “잠금 중 입력이 쌓이는” 문제가 생김)
/// 프로토타입은 잠금 중 enqueue 자체를 차단이 깔끔함.
///

using System.Collections.Generic;

public class TurnInputBuffer
{
    private readonly Queue<TurnCommand> _q = new Queue<TurnCommand>();

    public void Enqueue(TurnCommand cmd) => _q.Enqueue(cmd);

    public bool TryDequeue(out TurnCommand cmd)
    {
        if (_q.Count > 0) { cmd = _q.Dequeue(); return true; }
        cmd = default;
        return false;
    }

    public void Clear() => _q.Clear();
}
