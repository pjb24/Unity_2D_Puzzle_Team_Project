// StageDefinition.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public enum E_StageTransitionType
{
    None,
    Fade,
    Slide,
}

public enum E_CellType
{
    Empty,
    Wall,
    Obstacle,
    Goal,
}

[System.Serializable]
public struct SpawnInfo
{
    public Vector2Int _cell;   // 중앙 보드 좌표
    public Vector3 _world;     // 테두리/월드 스폰이 필요하면 사용(옵션)
}

[CreateAssetMenu(menuName = "Puzzle/Data/Stage Definition")]
public class StageDefinition : ScriptableObject
{
    [Header("Id")]
    [SerializeField] private string _stageId = "1-1";

    [Header("Board")]
    [SerializeField] private Vector2Int _boardSize = new(7, 7);

    // 최소 구현: 텍스트(행 단위)로 시작 -> 런타임에서 파싱해 grid 생성
    // 예) ".#..G"
    [TextArea(3, 10)]
    [SerializeField] private string _boardText;

    // 또는 배열 방식(초기부터 배열로 가도 됨)
    [SerializeField] private E_CellType[] _cells; // length = w*h

    [Header("Spawn")]
    [SerializeField] private SpawnInfo _fatherSpawn;
    [SerializeField] private SpawnInfo _childSpawn;

    [Header("Child Path")]
    [SerializeField] private int[] _blockedPathSteps; // 경로 step index 기준

    [Header("Transition")]
    [SerializeField] private E_StageTransitionType _transitionType = E_StageTransitionType.Fade;

    [SerializeField] private Vector2Int[] _holeCells;
    [SerializeField] private Vector2Int[] _gapFillerBlockCells;

    // ===== Public getters =====
    public string StageId => _stageId;
    public Vector2Int BoardSize => _boardSize;
    public string BoardText => _boardText;
    public E_CellType[] Cells => _cells;
    public SpawnInfo FatherSpawn => _fatherSpawn;
    public SpawnInfo ChildSpawn => _childSpawn;
    public IReadOnlyList<int> BlockedPathSteps => _blockedPathSteps;
    public E_StageTransitionType TransitionType => _transitionType;

    // 런타임에서만 정제된 결과를 쓰도록 새 API 제공
    public Vector2Int[] GetHoleCells_Runtime() => SanitizeCells(_holeCells, _boardSize, "[StageDefinition] HoleCells");
    public Vector2Int[] GetGapFillerBlockCells_Runtime() => SanitizeCells(_gapFillerBlockCells, _boardSize, "[StageDefinition] GapFillerBlockCells");

    private void OnValidate()
    {
        // 1) 보드 크기
        if (_boardSize.x < 1) _boardSize.x = 1;
        if (_boardSize.y < 1) _boardSize.y = 1;

        int total = _boardSize.x * _boardSize.y;

        // 2) cells 배열 길이 보정(배열 방식 사용할 경우)
        if (_cells == null || _cells.Length != total)
        {
            var newCells = new E_CellType[total];
            if (_cells != null)
            {
                Array.Copy(_cells, newCells, Mathf.Min(_cells.Length, newCells.Length));
            }
            _cells = newCells;
        }

        // 3) 스폰 유효성(중앙 보드 범위)
        ValidateCellInBoard(_fatherSpawn._cell, nameof(_fatherSpawn));
        ValidateCellInBoard(_childSpawn._cell, nameof(_childSpawn));

        // 5) (옵션) Goal 최소 1개 권장
        if (_cells != null)
        {
            bool hasGoal = false;
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] == E_CellType.Goal) { hasGoal = true; break; }
            }
            if (!hasGoal)
                Debug.LogWarning($"[StageDefinition] No Goal cell: {name}", this);
        }

        if (_blockedPathSteps != null)
        {
            int w = _boardSize.x;
            int h = _boardSize.y;
            var indices = PerimeterPathBuilder.Build(w, h);

            int n = indices?.Count ?? 0;
            for (int i = 0; i < _blockedPathSteps.Length; i++)
            {
                int s = _blockedPathSteps[i];
                if (s < 0) _blockedPathSteps[i] = 0;
                if (n > 0 && s >= n) _blockedPathSteps[i] = n - 1;
            }
        }

        // Inspector 편집 방해 금지: Remove/재할당 금지
        ValidateCellsInBounds_NoRemove(_holeCells, _boardSize, "[StageDefinition] HoleCells");
        ValidateCellsInBounds_NoRemove(_gapFillerBlockCells, _boardSize, "[StageDefinition] GapFillerBlockCells");

        // 데이터 상호 모순은 Warning만
        ValidateBlockOnHole_NoRemove();
    }

    private void ValidateCellsInBounds_NoRemove(Vector2Int[] arr, Vector2Int boardSize, string tag)
    {
        if (arr == null) return;

        var set = new HashSet<Vector2Int>();
        for (int i = 0; i < arr.Length; i++)
        {
            var c = arr[i];

            // 범위 체크: Warning만
            if ((uint)c.x >= (uint)boardSize.x || (uint)c.y >= (uint)boardSize.y)
                Debug.LogWarning($"{tag} out of bounds. index={i} cell={c} boardSize={boardSize}", this);

            // 중복 체크: Warning만
            if (!set.Add(c))
                Debug.LogWarning($"{tag} duplicated cell. index={i} cell={c}", this);
        }
    }

    private void ValidateBlockOnHole_NoRemove()
    {
        if (_holeCells == null || _gapFillerBlockCells == null) return;

        var holeSet = new HashSet<Vector2Int>(_holeCells);
        for (int i = 0; i < _gapFillerBlockCells.Length; i++)
        {
            var c = _gapFillerBlockCells[i];
            if (holeSet.Contains(c))
                Debug.LogWarning($"[StageDefinition] GapFillerBlock on Hole (data conflict). index={i} cell={c}", this);
        }
    }

    private void ValidateCellInBoard(Vector2Int cell, string label)
    {
        if (cell.x < 0 || cell.y < 0 || cell.x >= _boardSize.x || cell.y >= _boardSize.y)
        {
            Debug.LogWarning($"[StageDefinition] {label} out of board: {cell} ({name})", this);
        }
    }

    // 런타임 정제: out-of-bounds 제거 + 중복 제거 (원본 배열은 손대지 않음)
    private static Vector2Int[] SanitizeCells(Vector2Int[] src, Vector2Int boardSize, string tag)
    {
        if (src == null || src.Length == 0) return System.Array.Empty<Vector2Int>();

        var set = new HashSet<Vector2Int>();
        var list = new List<Vector2Int>(src.Length);

        for (int i = 0; i < src.Length; i++)
        {
            var c = src[i];

            if ((uint)c.x >= (uint)boardSize.x || (uint)c.y >= (uint)boardSize.y)
            {
                Debug.LogWarning($"{tag} runtime sanitize: removed out of bounds. index={i} cell={c} boardSize={boardSize}");
                continue;
            }

            if (!set.Add(c))
            {
                Debug.LogWarning($"{tag} runtime sanitize: removed duplicate. index={i} cell={c}");
                continue;
            }

            list.Add(c);
        }

        return list.ToArray();
    }
}
