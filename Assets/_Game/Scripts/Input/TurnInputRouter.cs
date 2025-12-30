using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class TurnInputRouter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TurnDriver _turnDriver; // TurnStateMachine을 돌리는 MonoBehaviour

    [Header("Input Actions")]
    [SerializeField] private InputActionReference _moveAction;  // Vector2
    [SerializeField] private InputActionReference _turnCommit;
    [SerializeField] private InputActionReference _turnCancel;
    [SerializeField] private InputActionReference _rewindPrev;
    [SerializeField] private InputActionReference _rewindNext;

    [Header("Settings")]
    [SerializeField, Range(0.1f, 0.95f)] private float _deadZone = 0.35f;

    private TurnInputBuffer _buffer;

    public void Initialize(TurnInputBuffer buffer)
    {
        _buffer = buffer;
    }

    // 실제 입력 시스템 콜백에서 호출
    public void OnMoveUp()
    {
        if (_turnDriver.IsInputLocked) return; // 잠금 규칙
        _buffer.Enqueue(new TurnCommand(E_TurnCommandType.MoveUp));
    }

    #region Unity Lifecycle

    private void OnEnable()
    {
        Bind(_moveAction, OnMovePerformed);

        Bind(_turnCommit, OnTurnCommitPerformed);
        Bind(_turnCancel, OnTurnCancel);
        Bind(_rewindPrev, OnRewindPrev);
        Bind(_rewindNext, OnRewindNext);
    }

    private void OnDisable()
    {
        Unbind(_moveAction, OnMovePerformed);

        Unbind(_turnCommit, OnTurnCommitPerformed);
        Unbind(_turnCancel, OnTurnCancel);
        Unbind(_rewindPrev, OnRewindPrev);
        Unbind(_rewindNext, OnRewindNext);
    }

    #endregion

    #region Binding Helpers

    private void Bind(InputActionReference actionRef, Action<InputAction.CallbackContext> handler)
    {
        if (actionRef == null) return;
        actionRef.action.performed += handler;
        actionRef.action.Enable();
    }

    private void Unbind(InputActionReference actionRef, Action<InputAction.CallbackContext> handler)
    {
        if (actionRef == null) return;
        actionRef.action.performed -= handler;
        actionRef.action.Disable();
    }

    #endregion

    #region Input Callbacks

    private bool IsInputAllowed()
    {
        // 규칙: FatherAction~Resolve 입력 차단
        if (_turnDriver == null || _buffer == null) return false;
        if (_turnDriver.IsInputLocked) return false;

        return true;
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        if (!IsInputAllowed()) return;

        Vector2 v = ctx.ReadValue<Vector2>();
        if (v.sqrMagnitude < _deadZone * _deadZone) return;

        TurnCommand cmd = ToDigitalCommand(v);
        if (cmd.Type == E_TurnCommandType.None) return;

        _buffer.Enqueue(cmd);
    }

    private void OnTurnCommitPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsInputAllowed()) return;

        _buffer.Enqueue(new TurnCommand(E_TurnCommandType.TurnCommit));
    }

    private TurnCommand ToDigitalCommand(Vector2 v)
    {
        // 축 우선: 절댓값 큰 축 기준으로 4방향 스냅
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
        {
            if (v.x >= _deadZone) return new TurnCommand(E_TurnCommandType.MoveRight);
            if (v.x <= -_deadZone) return new TurnCommand(E_TurnCommandType.MoveLeft);
        }
        else
        {
            if (v.y >= _deadZone) return new TurnCommand(E_TurnCommandType.MoveUp);
            if (v.y <= -_deadZone) return new TurnCommand(E_TurnCommandType.MoveDown);
        }

        return new TurnCommand(E_TurnCommandType.None);
    }

    private void OnTurnCancel(InputAction.CallbackContext ctx)
    {
        if (!IsInputAllowed()) return;

        _buffer.Enqueue(new TurnCommand(E_TurnCommandType.TurnCancel));
    }

    private void OnRewindPrev(InputAction.CallbackContext ctx)
    {
        if (!IsInputAllowed()) return;

        _buffer.Enqueue(new TurnCommand(E_TurnCommandType.RewindPrev));
    }

    private void OnRewindNext(InputAction.CallbackContext ctx)
    {
        if (!IsInputAllowed()) return;

        _buffer.Enqueue(new TurnCommand(E_TurnCommandType.RewindNext));
    }

    #endregion
}
