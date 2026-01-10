// BoardStateRewindable.cs
using System;
using UnityEngine;

public class BoardStateRewindable : MonoBehaviour, IRewindable
{
    [Serializable]
    public struct BoardState
    {
        public CellMeta[] _meta;
    }

    private BoardGrid _grid;

    private event Action _onRestored;

    public void AddListenerOnRestored(Action cb) => _onRestored += cb;
    public void RemoveListenerOnRestored(Action cb) => _onRestored -= cb;

    public void Initialize(BoardGrid grid)
    {
        _grid = grid;
    }

    public object CaptureState()
    {
        if (_grid == null)
        {
            Debug.LogWarning("[BoardStateRewindable] Capture fallback: grid is null.");
            return null;
        }

        return new BoardState
        {
            _meta = _grid.CopyMetaArray(),
        };
    }

    public void RestoreState(object state)
    {
        if (_grid == null)
        {
            Debug.LogWarning("[BoardStateRewindable] Restore fallback: grid is null.");
            return;
        }

        if (state is not BoardState s)
        {
            Debug.LogWarning("[BoardStateRewindable] Restore fallback: state type mismatch.");
            return;
        }

        _grid.RestoreMetaArray(s._meta, notifyAll: true);

        _onRestored?.Invoke();
    }
}
