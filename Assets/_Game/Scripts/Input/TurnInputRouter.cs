// TurnInputRouter.cs
///
/// 되감기 모드(RewindController.IsRewindActive)일 때:
/// Move / Interact / TurnCancel 입력은 차단
/// RewindPrev/Next/Commit은 TurnCommand enqueue 하지 않고 즉시 RewindController로 전달
/// 되감기 모드가 아닐 때:
/// Prev / Next / Commit은 무시(안전)
///
using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class TurnInputRouter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TurnDriver _turnDriver; // TurnStateMachine을 돌리는 MonoBehaviour
    [SerializeField] private RewindController _rewind;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference _moveAction;  // Vector2
    [SerializeField] private InputActionReference _interactAction;
    [SerializeField] private InputActionReference _cancelAction;

    [SerializeField] private InputActionReference _rewindEnter;
    [SerializeField] private InputActionReference _rewindPrev;
    [SerializeField] private InputActionReference _rewindNext;
    [SerializeField] private InputActionReference _rewindCommit;
    [SerializeField] private InputActionReference _rewindCancel;

    [Header("Settings")]
    [SerializeField, Range(0.1f, 0.95f)] private float _deadZone = 0.35f;

    private TurnInputBuffer _buffer;

    private void Reset()
    {
        _turnDriver = FindAnyObjectByType<TurnDriver>();
        _rewind = FindAnyObjectByType<RewindController>();
    }

    public void Initialize(TurnInputBuffer buffer)
    {
        _buffer = buffer;
    }

    private bool IsInputAllowed()
    {
        // 규칙: FatherAction~Resolve 입력 차단
        if (_turnDriver == null || _buffer == null) return false;
        if (_turnDriver.IsInputLocked) return false;

        return true;
    }

    private bool IsRewindActive()
    {
        return _rewind != null && _rewind.IsRewindActive;
    }

    #region Unity Lifecycle

    private void OnEnable()
    {
        Bind(_moveAction, OnMovePerformed);
        Bind(_interactAction, OnInteractPerformed);
        Bind(_cancelAction, OnCancelPerformed);

        Bind(_rewindEnter, OnRewindEnter);
        Bind(_rewindPrev, OnRewindPrev);
        Bind(_rewindNext, OnRewindNext);
        Bind(_rewindCommit, OnRewindCommit);
        Bind(_rewindCancel, OnRewindCancel);
    }

    private void OnDisable()
    {
        Unbind(_moveAction, OnMovePerformed);
        Unbind(_interactAction, OnInteractPerformed);
        Unbind(_cancelAction, OnCancelPerformed);

        Unbind(_rewindEnter, OnRewindEnter);
        Unbind(_rewindPrev, OnRewindPrev);
        Unbind(_rewindNext, OnRewindNext);
        Unbind(_rewindCommit, OnRewindCommit);
        Unbind(_rewindCancel, OnRewindCancel);
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

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        if (IsRewindActive()) return;
        if (!IsInputAllowed()) return;

        Vector2 v = ctx.ReadValue<Vector2>();
        if (v.sqrMagnitude < _deadZone * _deadZone) return;

        TurnCommand cmd = ToDigitalCommand(v);
        if (cmd.Type == E_TurnCommandType.None) return;

        _buffer.Enqueue(cmd);
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (IsRewindActive()) return;
        if (!IsInputAllowed()) return;

        _buffer.Enqueue(new TurnCommand(E_TurnCommandType.Interact));
    }

    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        if (IsRewindActive()) return;
        if (!IsInputAllowed()) return;

        // _buffer.Enqueue(new TurnCommand(E_TurnCommandType.Cancel));
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

    // ===== Rewind =====

    private void OnRewindEnter(InputAction.CallbackContext ctx)
    {
        if (_rewind == null)
        {
            Debug.LogWarning("[TurnInputRouter] RewindController is null. RewindEnter ignored.");
            return;
        }

        if (IsRewindActive()) return;

        // 턴 처리 중(입력락)에는 진입 금지. 꼬임 방지.
        if (!IsInputAllowed()) return;

        _rewind.EnterRewind(E_RewindEnterSource.Player); // 능동 진입
    }

    private void OnRewindPrev(InputAction.CallbackContext ctx)
    {
        // 되감기 모드가 아닐 때는 무시(권장)
        if (!IsRewindActive()) return;

        if (_rewind == null)
        {
            Debug.LogWarning("[TurnInputRouter] RewindPrev ignored: RewindController is null.");
            return;
        }

        _rewind.RequestPrevTurn();
    }

    private void OnRewindNext(InputAction.CallbackContext ctx)
    {
        // 되감기 모드가 아닐 때는 무시(권장)
        if (!IsRewindActive()) return;

        if (_rewind == null)
        {
            Debug.LogWarning("[TurnInputRouter] RewindNext ignored: RewindController is null.");
            return;
        }

        _rewind.RequestNextTurn();
    }

    private void OnRewindCommit(InputAction.CallbackContext ctx)
    {
        if (!IsRewindActive()) return;

        if (_rewind == null)
        {
            Debug.LogWarning("[TurnInputRouter] RewindCommit ignored: RewindController is null.");
            return;
        }

        _rewind.RequestCommit();
    }

    private void OnRewindCancel(InputAction.CallbackContext ctx)
    {
        if (!IsRewindActive()) return;

        if (_rewind == null)
        {
            Debug.LogWarning("[TurnInputRouter] RewindCancel ignored: RewindController is null.");
            return;
        }

        _rewind.RequestCancel();
    }

    #endregion
}
