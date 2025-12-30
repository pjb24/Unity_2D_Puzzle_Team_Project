///
/// 표현 레이어: “월드 좌표 ↔ 셀” 변환 규칙 고정
/// DummyStageLoader가 타일을 “중앙 정렬 + tileSize”로 깔고 있다.
/// 그 기준을 그대로 사용해서 **GridPresenter(뷰/변환 전담)**를 만든다.
///

using UnityEngine;

public class GridPresenter
{
    public readonly float _tileSize;
    public readonly Vector3 _originLocal; // root 기준 원점(셀 0,0의 중심)
    public readonly Transform _root;      // StageRuntime root

    public GridPresenter(Transform root, int w, int h, float tileSize)
    {
        _root = root;
        _tileSize = tileSize;

        // DummyStageLoader와 동일한 중앙정렬 규칙
        _originLocal = new Vector3(-(w - 1) * 0.5f * tileSize, -(h - 1) * 0.5f * tileSize, 0f);
    }

    public Vector3 CellToWorld(Vector2Int c)
    {
        Vector3 local = _originLocal + new Vector3(c.x * _tileSize, c.y * _tileSize, 0f);
        return _root.TransformPoint(local);
    }
}
