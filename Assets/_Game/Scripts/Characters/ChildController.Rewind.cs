// ChildController.Rewind.cs
///
/// 저장: _pathPos, _lastStepBlocked
/// 복구: 인덱스 세팅 +transform 스냅
///
using System;
using UnityEngine;

public partial class ChildController : IRewindable
{
    [Serializable]
    public struct ChildState
    {
        public int _pathPos;
        public bool _lastBlocked;
        public E_Facing _facing;
    }

    public object CaptureState()
    {
        return new ChildState
        {
            _pathPos = _pathPos,
            _lastBlocked = _lastStepBlocked,
            _facing = Facing
        };
    }

    public void RestoreState(object state)
    {
        if (state is not ChildState s)
        {
            Debug.LogWarning("[ChildController] RestoreState fallback: invalid state type.");
            return;
        }

        if (_path == null || _path.Count <= 0)
        {
            Debug.LogWarning("[ChildController] RestoreState fallback: path is null/empty.");
            return;
        }

        // 이동 연출 중이면 중단 (정상 동작)
        StopMoveFxIfAny();

        int fromPos = _pathPos;

        _pathPos = Mathf.Clamp(s._pathPos, 0, _path.Count - 1);
        _lastStepBlocked = s._lastBlocked;

        Facing = s._facing;

        ApplyFacingVisual();

        bool moved = fromPos != _pathPos;
        Vector3 toWorld = _path.Points[_pathPos];

        if (moved)
            _animDriver?.PlayMove();

        if (moved)
        {
            StartMoveFx(
            toWorld: toWorld,
            onDone: () => _onStepCompleted?.Invoke(false));

            return;
        }

        transform.position = toWorld;
    }
}
