using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class TurnInputRouter : MonoBehaviour
{
    public enum E_InputType
    {
        TurnCommit = 0,
        TurnCancel = 1,
        RewindPrev = 2,
        RewindNext = 3,
    }

    [Header("Input Actions")]
    [SerializeField] private InputActionReference _turnCommit;
    [SerializeField] private InputActionReference _turnCancel;
    [SerializeField] private InputActionReference _rewindPrev;
    [SerializeField] private InputActionReference _rewindNext;

    // internal state
    private bool _isLocked;

    // internal events (NOT exposed)
    private event Action<E_InputType> _onInput;

    #region Unity Lifecycle

    private void OnEnable()
    {
        Bind(_turnCommit, OnTurnCommit);
        Bind(_turnCancel, OnTurnCancel);
        Bind(_rewindPrev, OnRewindPrev);
        Bind(_rewindNext, OnRewindNext);
    }

    private void OnDisable()
    {
        Unbind(_turnCommit, OnTurnCommit);
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

    private void OnTurnCommit(InputAction.CallbackContext ctx)
    {
        Emit(E_InputType.TurnCommit);
    }

    private void OnTurnCancel(InputAction.CallbackContext ctx)
    {
        Emit(E_InputType.TurnCancel);
    }

    private void OnRewindPrev(InputAction.CallbackContext ctx)
    {
        Emit(E_InputType.RewindPrev);
    }

    private void OnRewindNext(InputAction.CallbackContext ctx)
    {
        Emit(E_InputType.RewindNext);
    }

    private void Emit(E_InputType type)
    {
        if (_isLocked) return;
        _onInput?.Invoke(type);
    }

    #endregion

    #region External Control (Public API)

    public void LockInput()
    {
        _isLocked = true;
    }

    public void UnlockInput()
    {
        _isLocked = false;
    }

    public void AddInputListener(Action<E_InputType> listener)
    {
        _onInput += listener;
    }

    public void RemoveInputListener(Action<E_InputType> listener)
    {
        _onInput -= listener;
    }

    #endregion
}
