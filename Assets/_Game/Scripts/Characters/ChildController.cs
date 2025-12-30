using System;
using UnityEngine;

public class ChildController : MonoBehaviour
{
    private event Action<bool> _onStepCompleted; // bool = blocked

    public void AddListenerOnStepCompleted(Action<bool> cb) => _onStepCompleted += cb;
    public void RemoveListenerOnStepCompleted(Action<bool> cb) => _onStepCompleted -= cb;

    public void RequestStep()
    {
        // TODO: 실제 경로/점유 판정
        bool blocked = false; // 프로토타입
        _onStepCompleted?.Invoke(blocked);
    }
}
