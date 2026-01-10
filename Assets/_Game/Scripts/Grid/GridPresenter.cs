// GridPresenter.cs
using UnityEngine;

public class GridPresenter
{
    [Header("Tuning")]
    public float _tileSize = 1f;

    private BoardGrid _grid;

    public Vector3 _originLocal; // root 기준 원점(셀 0,0의 중심)
    public Transform _root;      // StageRuntime root

    public void Initialize(Transform root, BoardGrid grid)
    {
        _root = root;
        _grid = grid;

        if (_grid == null)
        {
            Debug.LogWarning("[GridPresenter] Initialize failed: grid is null");
            return;
        }

        _originLocal = new Vector3(
            -(_grid._w - 1) * 0.5f * _tileSize,
            -(_grid._h - 1) * 0.5f * _tileSize,
            0f);
    }

    public void RebuildAll(E_Dir4 childFacing)
    {
        if (!IsReady())
            return;

        int w = _grid._w;
        int h = _grid._h;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                RefreshCell(x, y, childFacing);
            }
    }

    public void RefreshCell(int x, int y, E_Dir4 childFacing)
    {
        if (!IsReady())
            return;

        BuildKeysAtCell(x, y, childFacing, out TileVisualKey baseKey, out bool hasOv1, out TileVisualKey ov1Key, out bool hasOv2, out TileVisualKey ov2Key);
    }

    // =========================
    // Key Build (상태 읽기 1곳)
    // =========================
    private void BuildKeysAtCell(
        int x,
        int y,
        E_Dir4 childFacing,
        out TileVisualKey baseKey,
        out bool hasOv1,
        out TileVisualKey ov1Key,
        out bool hasOv2,
        out TileVisualKey ov2Key)
    {
        Vector2Int cell = new Vector2Int(x, y);
        E_CellType cellType = _grid.GetCell(cell);
        E_Occupant occupant = _grid.GetOcc(cell);
        CellMeta meta = _grid.GetMeta(cell);

        // 1) Base
        baseKey = TileVisualKey.Create(E_TileVisualLayer.Base, E_TileVisualType.Floor);

        // 2) Overlay_1 (0~1)
        hasOv1 = false;
        ov1Key = default;

        if (cellType == E_CellType.Wall)
        {
            hasOv1 = true;
            ov1Key = TileVisualKey.Create(E_TileVisualLayer.Overlay_1, E_TileVisualType.Wall);
        }
        // 예: 스위치
        else if (cellType == E_CellType.ToggleSwitch)
        {
            bool isOn = meta.SwitchOn; // TODO: meta 필드명 맞추기
            hasOv1 = true;
            ov1Key = TileVisualKey.Create(E_TileVisualLayer.Overlay_1, isOn ? E_TileVisualType.SwitchOn : E_TileVisualType.SwitchOff);
        }
        // 예: 구멍/메워진 구멍
        else if (cellType == E_CellType.Hole)
        {
            bool filled = meta.IsFilledHole; // TODO
            hasOv1 = true;
            ov1Key = TileVisualKey.Create(E_TileVisualLayer.Overlay_1, filled ? E_TileVisualType.FilledHole : E_TileVisualType.Hole);
        }
        // 예: 갭필러 블록 점유
        else if (occupant == E_Occupant.GapFillerBlock)
        {
            hasOv1 = true;
            ov1Key = TileVisualKey.Create(E_TileVisualLayer.Overlay_1, E_TileVisualType.GapFillerBlock);
        }
        // 예: 문
        else if (cellType == E_CellType.Door)
        {
            bool doorOn = meta.DoorOn; // TODO
            hasOv1 = true;
            ov1Key = TileVisualKey.Create(E_TileVisualLayer.Overlay_1, doorOn ? E_TileVisualType.DoorOn : E_TileVisualType.DoorOff);
        }

        // 3) Overlay_2 (0~1) + 방향
        hasOv2 = false;
        ov2Key = default;

        // 예: 목표
        if (cellType == E_CellType.Goal)
        {
            hasOv2 = true;
            ov2Key = TileVisualKey.CreateDirectional(E_TileVisualLayer.Overlay_2, E_TileVisualType.Goal, childFacing);
        }
        // 예: 외곽 테두리
        else if (meta.IsChildPathOuterBorder) // TODO
        {
            hasOv2 = true;
            ov2Key = TileVisualKey.CreateDirectional(E_TileVisualLayer.Overlay_2, E_TileVisualType.ChildPathOuterBorder, childFacing);
        }
    }

    // =========================
    // Renderer build
    // =========================
    private bool IsReady()
    {
        return _grid != null;
    }

    public Vector3 CellToWorld(Vector2Int c)
    {
        // 2D 탑뷰: (x,y) 그대로 매핑
        Vector3 local = _originLocal + new Vector3(c.x * _tileSize, c.y * _tileSize, 0f);
        return _root.TransformPoint(local);
    }
}
