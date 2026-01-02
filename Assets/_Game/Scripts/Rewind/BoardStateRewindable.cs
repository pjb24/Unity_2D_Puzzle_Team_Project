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
    private InteractRegistry _registry;

    private event Action _onRestored;

    public void AddListenerOnRestored(Action cb) => _onRestored += cb;
    public void RemoveListenerOnRestored(Action cb) => _onRestored -= cb;

    public void Initialize(BoardGrid grid, InteractRegistry registry)
    {
        _grid = grid;
        _registry = registry;
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

        // 정책: 회전/이동으로 좌표가 바뀔 수 있으니 “리빌드로 통일”
        if (_registry != null)
        {
            _registry.RebuildFromScene();
        }
        else
        {
            Debug.LogWarning("[BoardStateRewindable] Registry rebuild skipped (fallback): registry is null.");
        }

        _onRestored?.Invoke();
    }
}
