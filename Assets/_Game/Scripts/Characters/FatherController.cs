using System;
using UnityEngine;

public class FatherController : MonoBehaviour
{
    private event Action _onActionCompleted;

    public void AddListenerOnActionCompleted(Action cb) => _onActionCompleted += cb;
    public void RemoveListenerOnActionCompleted(Action cb) => _onActionCompleted -= cb;

    public void RequestAction(TurnCommand cmd)
    {
        // TODO: 실제 이동/벽충돌 등 처리
        // 프로토타입: 즉시 완료
        _onActionCompleted?.Invoke();
    }
}
