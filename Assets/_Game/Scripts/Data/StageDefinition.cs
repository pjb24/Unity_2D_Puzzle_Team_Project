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
    Floor,
    OuterBorder,

    Wall,
    Hole,
    FilledHole,
    SwitchOn,
    SwitchOff,
    Door,
    Goal,

    FillerBlock,
}

[System.Serializable]
public struct SpawnInfo
{
    public Vector2Int _cell;   // 중앙 보드 좌표
    public Vector3 _world;     // 테두리/월드 스폰이 필요하면 사용(옵션)
}

[Serializable]
public struct DoorSpawnData
{
    // anchor=ChildPathStep일 때 사용
    public int _pathStep;
    public bool _startOpen;
    // ToggleSwitch가 참조할 GUID (RewindKey GuidString, N or D)
    public string _guid;
    public Vector2Int _cell;
}

[Serializable]
public struct ToggleSwitchSpawnData
{
    public Vector2Int _cell;
    public E_SwitchMode _mode;
    public bool _startOn;
    // DoorSpawnData._guid 목록과 매칭
    public string[] _targetDoorGuids;
}

public struct StageDefinitionRuntimeData
{
    public string StageId;
    public Vector2Int BoardSize;
    public E_CellType[] Cells;

    public Vector2Int FatherSpawnCell;
    public Vector2Int ChildSpawnCell;

    // Father 이동 가능 영역(InnerBase). RectInt(x,y,w,h)
    public RectInt FatherMoveRect;

    public int ChildStartPathStep;
    public int ChildGoalPathStep;

    public int[] BlockedPathSteps;

    public Vector2Int[] HoleCells;
    public Vector2Int[] GapFillerBlockCells;

    public DoorSpawnData[] DoorSpawns;
    public ToggleSwitchSpawnData[] ToggleSwitchSpawns;
}

public class StageDefinition
{
    [Header("Id")]
    [SerializeField] private string _stageId = "1-1";

    [Header("Board")]
    [SerializeField] private Vector2Int _boardSize = new(7, 7);

    [SerializeField] private E_CellType[] _cells; // length = w*h

    [Header("Spawn")]
    [SerializeField] private SpawnInfo _fatherSpawn;
    [SerializeField] private SpawnInfo _childSpawn;

    [Header("Father Move Bounds (InnerBase)")]
    [Tooltip("Father가 이동 가능한 보드 영역. (JSON 스테이지는 InnerBase로 자동 세팅됨)")]
    [SerializeField] private RectInt _fatherMoveRect;

    [Header("Child Path")]
    [SerializeField] private int[] _blockedPathSteps; // 경로 step index 기준

    [Header("Transition")]
    [SerializeField] private E_StageTransitionType _transitionType = E_StageTransitionType.Fade;

    [Header("Holes / GapFiller")]
    [SerializeField] private Vector2Int[] _holeCells;
    [SerializeField] private Vector2Int[] _gapFillerBlockCells;

    [Header("Gimmicks: Doors")]
    [SerializeField] private DoorSpawnData[] _doorSpawns;

    [Header("Gimmicks: Toggle Switches")]
    [SerializeField] private ToggleSwitchSpawnData[] _toggleSwitchSpawns;

    [Header("Child Path (Runtime)")]
    [SerializeField] private int _childStartPathStep = 0;

    [SerializeField] private int _childGoalPathStep = -1;

    // ===== Public getters =====
    public string StageId => _stageId;
    public Vector2Int BoardSize => _boardSize;
    public E_CellType[] Cells => _cells;
    public SpawnInfo FatherSpawn => _fatherSpawn;
    public SpawnInfo ChildSpawn => _childSpawn;
    public RectInt FatherMoveRect => _fatherMoveRect;

    public IReadOnlyList<int> BlockedPathSteps => _blockedPathSteps;
    public E_StageTransitionType TransitionType => _transitionType;
    public DoorSpawnData[] DoorSpawns => _doorSpawns;
    public ToggleSwitchSpawnData[] ToggleSwitchSpawns => _toggleSwitchSpawns;

    // 런타임에서만 정제된 결과를 쓰도록 API 제공
    public Vector2Int[] GetHoleCells_Runtime() => SanitizeCells(_holeCells, _boardSize, "[StageDefinition] HoleCells");
    public Vector2Int[] GetGapFillerBlockCells_Runtime() => SanitizeCells(_gapFillerBlockCells, _boardSize, "[StageDefinition] GapFillerBlockCells");

    public int ChildStartPathStep => _childStartPathStep;
    public int ChildGoalPathStep => _childGoalPathStep;

    private static RectInt ClampRectToBoard(RectInt r, Vector2Int boardSize)
    {
        int w = Mathf.Max(1, boardSize.x);
        int h = Mathf.Max(1, boardSize.y);

        int xMin = Mathf.Clamp(r.xMin, 0, w - 1);
        int yMin = Mathf.Clamp(r.yMin, 0, h - 1);

        int xMax = Mathf.Clamp(r.xMax, xMin + 1, w);
        int yMax = Mathf.Clamp(r.yMax, yMin + 1, h);

        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
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

    // 런타임 JSON 생성 스테이지용
    public void ApplyRuntimeData(in StageDefinitionRuntimeData data)
    {
        _stageId = string.IsNullOrWhiteSpace(data.StageId) ? "RuntimeStage" : data.StageId;

        _boardSize = new Vector2Int(Mathf.Max(1, data.BoardSize.x), Mathf.Max(1, data.BoardSize.y));

        int total = _boardSize.x * _boardSize.y;

        _cells = new E_CellType[total];
        if (data.Cells != null)
        {
            int n = Mathf.Min(data.Cells.Length, total);
            for (int i = 0; i < n; i++)
                _cells[i] = data.Cells[i];
        }

        _fatherSpawn = new SpawnInfo { _cell = data.FatherSpawnCell, _world = Vector3.zero };
        _childSpawn = new SpawnInfo { _cell = data.ChildSpawnCell, _world = Vector3.zero };

        // Father 이동 bounds 적용(없으면 폴백 + Warning)
        _fatherMoveRect = data.FatherMoveRect;
        if (_fatherMoveRect.width <= 0 || _fatherMoveRect.height <= 0)
        {
            Debug.LogWarning($"[StageDefinition] ApplyRuntimeData: FatherMoveRect invalid. fallback to full board. stageId={_stageId}");
            _fatherMoveRect = new RectInt(0, 0, _boardSize.x, _boardSize.y);
        }
        else
        {
            _fatherMoveRect = ClampRectToBoard(_fatherMoveRect, _boardSize);
        }

        _childStartPathStep = data.ChildStartPathStep;
        _childGoalPathStep = data.ChildGoalPathStep;

        _blockedPathSteps = data.BlockedPathSteps ?? System.Array.Empty<int>();
        _holeCells = data.HoleCells ?? System.Array.Empty<Vector2Int>();
        _gapFillerBlockCells = data.GapFillerBlockCells ?? System.Array.Empty<Vector2Int>();

        _doorSpawns = data.DoorSpawns ?? System.Array.Empty<DoorSpawnData>();
        _toggleSwitchSpawns = data.ToggleSwitchSpawns ?? System.Array.Empty<ToggleSwitchSpawnData>();
    }
}
