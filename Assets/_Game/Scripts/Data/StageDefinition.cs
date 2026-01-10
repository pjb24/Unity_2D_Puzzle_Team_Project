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
    Wall,
    ToggleSwitch,
    Hole,
    GapFillerBlock,
    Door,
    Goal,

    Obstacle,
}

public enum E_DoorAnchor
{
    Cell = 0,
    ChildPathStep = 1,
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
    public E_DoorAnchor _anchor;

    // anchor=Cell일 때만 직접 편집
    public Vector2Int _cell;

    // anchor=ChildPathStep일 때 사용
    public int _pathStep;

    public bool _startOpen;

    // ToggleSwitch가 참조할 GUID (RewindKey GuidString, N or D)
    public string _guid;
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

[CreateAssetMenu(menuName = "Puzzle/Data/Stage Definition")]
public class StageDefinition : ScriptableObject
{
    [Header("Id")]
    [SerializeField] private string _stageId = "1-1";

    [Header("Board")]
    [SerializeField] private Vector2Int _boardSize = new(7, 7);

    // 배열 방식
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

    [Header("Audio")]
    [SerializeField] private StageAudioProfile _audioProfile;

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

    public StageAudioProfile AudioProfile => _audioProfile;

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

        // FatherMoveRect 검증/보정
        ValidateFatherMoveRect();

        // 4) ChildPath 길이 계산
        int w = _boardSize.x;
        int h = _boardSize.y;
        var perimeter = PerimeterPathBuilder.Build(w, h);
        int pathCount = perimeter?.Count ?? 0;

        // 5) BlockedPathSteps 클램프
        ClampBlockedPathSteps(pathCount);

        // 6) Doors: GUID 자동 생성 + (ChildPathStep 앵커면) cell 자동 정렬 + blockedSteps 자동 동기화
        ValidateDoorSpawnsAndSyncToBlockedSteps(perimeter, pathCount);

        // 7) ToggleSwitch 검증
        ValidateToggleSwitchSpawns();

        // (옵션) Goal 최소 1개 권장
        ValidateGoalRecommended();

        // 8) Inspector 편집 방해 금지: Remove/재할당 금지
        ValidateCellsInBounds_NoRemove(_holeCells, _boardSize, "[StageDefinition] HoleCells");
        ValidateCellsInBounds_NoRemove(_gapFillerBlockCells, _boardSize, "[StageDefinition] GapFillerBlockCells");
        ValidateBlockOnHole_NoRemove();
    }

    private void ValidateFatherMoveRect()
    {
        // width/height가 0 이하면 폴백(보드 전체) + Warning (무음 금지)
        if (_fatherMoveRect.width <= 0 || _fatherMoveRect.height <= 0)
        {
            Debug.LogWarning($"[StageDefinition] FatherMoveRect invalid. fallback to full board. stageId={_stageId}");
            _fatherMoveRect = new RectInt(0, 0, _boardSize.x, _boardSize.y);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            return;
        }

        var clamped = ClampRectToBoard(_fatherMoveRect, _boardSize);
        if (clamped != _fatherMoveRect)
        {
            Debug.LogWarning($"[StageDefinition] FatherMoveRect clamped. raw={_fatherMoveRect} clamped={clamped} stageId={_stageId}");
            _fatherMoveRect = clamped;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        // 스폰이 bounds 밖이면 Warning (스폰 자체 clamp는 로더에서)
        if (!_fatherMoveRect.Contains(_fatherSpawn._cell))
        {
            Debug.LogWarning($"[StageDefinition] FatherSpawn out of FatherMoveRect. stageId={_stageId} spawn={_fatherSpawn._cell} rect={_fatherMoveRect}");
        }
    }

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

    private void ClampBlockedPathSteps(int pathCount)
    {
        if (_blockedPathSteps == null || _blockedPathSteps.Length == 0)
            return;

        for (int i = 0; i < _blockedPathSteps.Length; i++)
        {
            int s = _blockedPathSteps[i];
            if (s < 0) _blockedPathSteps[i] = 0;
            if (pathCount > 0 && s >= pathCount) _blockedPathSteps[i] = pathCount - 1;
        }
    }

    private void ValidateDoorSpawnsAndSyncToBlockedSteps(List<int> perimeter, int pathCount)
    {
        if (_doorSpawns == null) return;

        var guidSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stepSet = new HashSet<int>();
        var cellSet = new HashSet<Vector2Int>();

        for (int i = 0; i < _doorSpawns.Length; i++)
        {
            var d = _doorSpawns[i];

            // GUID 자동 생성(비어있을 때만)
            if (string.IsNullOrWhiteSpace(d._guid))
            {
                d._guid = Guid.NewGuid().ToString("N");
                _doorSpawns[i] = d;

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }

            if (!TryParseGuid(d._guid, out Guid g))
            {
                Debug.LogWarning($"[StageDefinition] DoorSpawns[{i}] guid invalid. raw={d._guid}", this);
            }
            else
            {
                string n = g.ToString("N");
                if (!guidSet.Add(n))
                    Debug.LogWarning($"[StageDefinition] DoorSpawns duplicated guid detected. guid={n} index={i}", this);
            }

            // ChildPathStep 앵커면: step -> cell 자동 정렬 + blockedSteps 자동 포함
            if (d._anchor == E_DoorAnchor.ChildPathStep)
            {
                if (pathCount <= 0 || perimeter == null)
                {
                    Debug.LogWarning($"[StageDefinition] DoorSpawns[{i}] anchor=ChildPathStep but path is empty.", this);
                    continue;
                }

                int step = d._pathStep;
                if (step < 0) step = 0;
                if (step >= pathCount) step = pathCount - 1;

                if (step != d._pathStep)
                {
                    d._pathStep = step;
                    _doorSpawns[i] = d;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
                }

                int idx = perimeter[step];
                int x = idx % _boardSize.x;
                int y = idx / _boardSize.x;
                var cell = new Vector2Int(x, y);

                if (d._cell != cell)
                {
                    d._cell = cell;
                    _doorSpawns[i] = d;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
                }

                // step 중복 경고
                if (!stepSet.Add(step))
                    Debug.LogWarning($"[StageDefinition] DoorSpawns duplicated ChildPathStep. step={step} index={i}", this);

                // blockedSteps에 없으면 자동 추가 + Warning(무음 금지)
                if (!ContainsStep(_blockedPathSteps, step))
                {
                    Debug.LogWarning($"[StageDefinition] BlockedPathSteps auto-sync: added step from Door. step={step} doorIndex={i}", this);
                    AppendBlockedStep(step);
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
                }

                // cell 중복 경고
                if (!cellSet.Add(cell))
                    Debug.LogWarning($"[StageDefinition] DoorSpawns duplicated cell. cell={cell} index={i}", this);

                continue;
            }

            // anchor=Cell: cell 범위만 검증
            ValidateCellInBoard(d._cell, $"DoorSpawns[{i}] Cell");

            if (!cellSet.Add(d._cell))
                Debug.LogWarning($"[StageDefinition] DoorSpawns duplicated cell. cell={d._cell} index={i}", this);
        }
    }

    private void AppendBlockedStep(int step)
    {
        if (_blockedPathSteps == null)
        {
            _blockedPathSteps = new[] { step };
            return;
        }

        int n = _blockedPathSteps.Length;
        var next = new int[n + 1];
        Array.Copy(_blockedPathSteps, next, n);
        next[n] = step;
        _blockedPathSteps = next;
    }

    private static bool ContainsStep(int[] arr, int step)
    {
        if (arr == null) return false;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == step)
                return true;
        }
        return false;
    }

    private void ValidateToggleSwitchSpawns()
    {
        if (_toggleSwitchSpawns == null) return;

        for (int i = 0; i < _toggleSwitchSpawns.Length; i++)
        {
            var s = _toggleSwitchSpawns[i];
            ValidateCellInBoard(s._cell, $"ToggleSwitchSpawns[{i}] Cell");

            if (s._targetDoorGuids == null || s._targetDoorGuids.Length == 0)
            {
                Debug.LogWarning($"[StageDefinition] ToggleSwitchSpawns[{i}] targetDoorGuids is empty. (switch will do nothing)", this);
                continue;
            }

            for (int k = 0; k < s._targetDoorGuids.Length; k++)
            {
                string raw = s._targetDoorGuids[k];
                if (string.IsNullOrWhiteSpace(raw))
                {
                    Debug.LogWarning($"[StageDefinition] ToggleSwitchSpawns[{i}] targetDoorGuids[{k}] is empty.", this);
                    continue;
                }

                if (!TryParseGuid(raw, out _))
                    Debug.LogWarning($"[StageDefinition] ToggleSwitchSpawns[{i}] targetDoorGuids[{k}] invalid. raw={raw}", this);
            }
        }
    }

    private void ValidateGoalRecommended()
    {
        if (_cells == null) return;

        bool hasGoal = false;
        for (int i = 0; i < _cells.Length; i++)
        {
            if (_cells[i] == E_CellType.Goal)
            {
                hasGoal = true;
                break;
            }
        }

        if (!hasGoal)
            Debug.LogWarning($"[StageDefinition] Goal is recommended but not found. ({name})", this);
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

    private static bool TryParseGuid(string raw, out Guid guid)
    {
        guid = Guid.Empty;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        raw = raw.Trim();

        if (Guid.TryParseExact(raw, "N", out guid)) return true;
        if (Guid.TryParseExact(raw, "D", out guid)) return true;

        return Guid.TryParse(raw, out guid);
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
