// TurnStateMachine.cs
using System;
using UnityEngine;

public interface ITurnPhase
{
    E_TurnPhase Phase { get; }
    void Enter(TurnContext ctx);
    void Tick(TurnContext ctx);
    void Exit(TurnContext ctx);
}

public enum E_TurnPhase
{
    Input = 0,
    FatherAction = 1,
    ChildStep = 2,
    Resolve = 3,
    Snapshot = 4,
    End = 5,
}

public class TurnStateMachine
{
    private readonly TurnContext _ctx;
    private ITurnPhase[] _phases;
    private ITurnPhase _current;
    private bool _isStarted;

    public E_TurnPhase CurrentPhase => _current.Phase;

    public TurnStateMachine(TurnContext ctx)
    {
        _ctx = ctx;
    }

    public void SetPhases(ITurnPhase[] phases)
    {
        if (phases == null || phases.Length == 0)
        {
            throw new ArgumentException("[Turn] phases is null or empty");
        }

        _phases = phases;
    }

    public void Start(E_TurnPhase entry = E_TurnPhase.Input)
    {
        if (_isStarted) return;
        if (_phases == null || _phases.Length == 0)
            throw new InvalidOperationException("[Turn] SetPhases() first");

        _current = Get(entry);
        _isStarted = true;

        Debug.Log($"[Turn] Phase => {_current.Phase} (TurnIndex={_ctx.TurnIndex})");
        _current.Enter(_ctx);
    }

    public void Tick()
    {
        if (!_isStarted || _current == null) return;
        _current.Tick(_ctx);
    }

    public void Change(E_TurnPhase next)
    {
        if (!_isStarted) return;
        if (_current.Phase == next) return;

        _current.Exit(_ctx);
        _current = Get(next);
        Debug.Log($"[Turn] Phase => {_current.Phase} (TurnIndex={_ctx.TurnIndex})");
        _current.Enter(_ctx);
    }

    private ITurnPhase Get(E_TurnPhase phase)
    {
        for (int i = 0; i < _phases.Length; i++)
            if (_phases[i].Phase == phase) return _phases[i];

        throw new InvalidOperationException($"[Turn] Phase not found: {phase}");
    }
}
