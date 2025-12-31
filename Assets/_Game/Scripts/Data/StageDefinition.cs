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

    // ===== Public getters =====
    public string StageId => _stageId;
    public Vector2Int BoardSize => _boardSize;
    public string BoardText => _boardText;
    public E_CellType[] Cells => _cells;
    public SpawnInfo FatherSpawn => _fatherSpawn;
    public SpawnInfo ChildSpawn => _childSpawn;
    public IReadOnlyList<int> BlockedPathSteps => _blockedPathSteps;
    public E_StageTransitionType TransitionType => _transitionType;

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
    }

    private void ValidateCellInBoard(Vector2Int cell, string label)
    {
        if (cell.x < 0 || cell.y < 0 || cell.x >= _boardSize.x || cell.y >= _boardSize.y)
        {
            Debug.LogWarning($"[StageDefinition] {label} out of board: {cell} ({name})", this);
        }
    }
}
