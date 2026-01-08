// DoorController.cs
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Experimental.GraphView.GraphView;

[DisallowMultipleComponent]
public class DoorController : MonoBehaviour, IRewindable
{
    [Serializable]
    public struct DoorState
    {
        public bool _isOpen;
    }

    private BoardGrid _grid;
    private GridPresenter _presenter;
    private Vector2Int _cell;

    // Child path block 연동
    private ChildPathBlockerRegistry _childPathBlockers;
    private int _childPathStep = -1;

    private ITileSpriteProvider _tileSprites;
    private SpriteRenderer _sr;

    private bool _isOpen;

    public Vector2Int Cell => _cell;
    public bool IsOpen => _isOpen;

    public void Initialize(
        BoardGrid grid,
        GridPresenter presenter,
        Vector2Int cell,
        bool startOpen,
        ChildPathBlockerRegistry childPathBlockers,
        int childPathStep,
        ITileSpriteProvider tileSprites)
    {
        _grid = grid;
        _presenter = presenter;
        _cell = cell;

        _childPathBlockers = childPathBlockers;
        _childPathStep = childPathStep;

        _tileSprites = tileSprites;
        _sr = GetComponentInChildren<SpriteRenderer>(includeInactive: true);

        if (_grid == null || _presenter == null)
        {
            Debug.LogWarning("[DoorController] Initialize fallback: grid/presenter is null.");
            return;
        }

        if (!_grid.IsInBounds(_cell))
        {
            Debug.LogWarning($"[DoorController] Initialize fallback: out of bounds. cell={_cell}");
            _cell = new Vector2Int(
                Mathf.Clamp(_cell.x, 0, _grid._w - 1),
                Mathf.Clamp(_cell.y, 0, _grid._h - 1));
        }

        transform.position = _presenter.CellToWorld(_cell);
        SetOpen(startOpen, notify: false); // 여기서 ChildBlock도 함께 정리됨
    }

    public void SetOpen(bool open) => SetOpen(open, notify: true);

    private void SetOpen(bool open, bool notify)
    {
        if (_grid == null)
        {
            Debug.LogWarning("[DoorController] SetOpen fallback: grid is null.");
            _isOpen = open;
            ApplyVisual();
            SyncChildPathBlocker();
            return;
        }

        _isOpen = open;

        // 닫힘이면 Blocker 점유로 막기, 열림이면 해제
        if (_grid.IsInBounds(_cell))
            _grid.SetOcc(_cell, _isOpen ? E_Occupant.None : E_Occupant.Blocker);
        else
            Debug.LogWarning($"[DoorController] SetOpen fallback: cell out of bounds. cell={_cell}");

        ApplyVisual();

        // Door 상태에 맞춰 ChildPathBlock도 동기화
        SyncChildPathBlocker();
    }

    private void SyncChildPathBlocker()
    {
        if (_childPathStep < 0)
            return; // 이 Door는 ChildPath에 영향 없음

        if (_childPathBlockers == null)
        {
            Debug.LogWarning($"[DoorController] Child blocker sync fallback: blockers is null. step={_childPathStep}");
            return;
        }

        // Door가 열리면 블록 제거, 닫히면 블록 추가
        bool blocked = !_isOpen;
        _childPathBlockers.SetBlocked(_childPathStep, blocked, reason: "Door.SetOpen");
    }

    private void ApplyVisual()
    {
        // 기존 프로토 규칙 유지:
        // - Open 스프라이트가 있으면 "보이게"
        // - 없으면 "안 보이게" (이전과 동일 동작)
        if (_sr == null)
            _sr = GetComponentInChildren<SpriteRenderer>(includeInactive: true);

        if (_isOpen)
        {
            if (_tileSprites != null)
            {
                var selector = TileSelector.Make(E_TileLayer.Ring, E_TileVisualKey.DoorOpen);
                if (_tileSprites.TryGetSprite(selector, out var s) && s != null)
                {
                    _sr.enabled = true;
                    _sr.sprite = s;
                    _sr.color = Color.white;
                    transform.localScale = Vector3.one;
                }
            }
            else
            {
                // 스프라이트 없으면 이전 유지 + 숨김(프로토 동작)
                if (_sr != null) _sr.enabled = false;
                transform.localScale = Vector3.zero;
            }

            return;
        }

        // closed
        if (_sr != null) _sr.enabled = true;

        if (_tileSprites != null)
        {
            var selector = TileSelector.Make(E_TileLayer.Ring, E_TileVisualKey.DoorClosed);
            if (_tileSprites.TryGetSprite(selector, out var c) && c != null)
            {
                _sr.sprite = c;
                _sr.color = Color.white;
                transform.localScale = Vector3.one;
            }
        }
        else
        {
            // 스프라이트 없으면 이전 유지 + 기존 스케일 표현 유지
            transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        }
    }

    public object CaptureState()
    {
        return new DoorState { _isOpen = _isOpen };
    }

    public void RestoreState(object state)
    {
        if (state is not DoorState s)
        {
            Debug.LogWarning("[DoorController] RestoreState fallback: invalid state type.");
            return;
        }

        if (_grid == null || _presenter == null)
        {
            Debug.LogWarning("[DoorController] RestoreState fallback: grid/presenter is null.");
            return;
        }

        // Restore에서도 SyncChildPathBlocker가 같이 돌아서 되감기 정합성 유지
        SetOpen(s._isOpen, notify: false);
    }
}
