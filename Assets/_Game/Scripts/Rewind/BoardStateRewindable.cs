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

        // NOTE:
        // InteractRegistry 리빌드는 여기서 하지 않는다.
        // RewindController에서 stageRoot 기준으로 1회 수행한다.
        // (RebuildFromScene는 deprecated + 씬 전수 스캔은 언로드 잔존 재등록 원인)

        _onRestored?.Invoke();
    }
}
