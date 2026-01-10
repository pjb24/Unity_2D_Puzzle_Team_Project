// StageJsonStageFactory.cs
// - 1stage.json -> StageDefinition 런타임 생성
// - 매핑 규칙 반영:
//   innerBase: 0 Floor, 1 Wall, 2 ToggleSwitch, 4 Hole
//   ringTiles: 0 path, 1 Door(기본 닫힘), 2 switch-open wall(기본 닫힘), 3 Goal, 4 switch-close wall(기본 열림)
// - Clear 조건: Child가 Goal(step) 도달

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class StageJsonStageFactory
{
    private const int InnerBase_Floor = 0;
    private const int InnerBase_Wall = 1;
    private const int InnerBase_ToggleSwitch = 2;
    private const int InnerBase_Hole = 4;

    private const int Ring_Path = 0;
    private const int Ring_Door = 1;
    private const int Ring_SwitchOpenWall = 2;
    private const int Ring_Goal = 3;
    private const int Ring_SwitchCloseWall = 4;

    public static StageDefinition BuildOrNull(string stageId, string jsonText)
    {
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            Debug.LogWarning($"[StageJson] BuildOrNull fallback: jsonText is empty. stageId={stageId}");
            return null;
        }

        object rootObj = MiniJson.Deserialize(jsonText);
        if (rootObj is not Dictionary<string, object> root)
        {
            Debug.LogWarning($"[StageJson] BuildOrNull fallback: root is not object. stageId={stageId}");
            return null;
        }

        int outerSize = GetInt(root, "outerSize", 7);
        int gap = GetInt(root, "gap", 1);
        int innerW = GetInt(root, "innerW", 3);
        int innerH = GetInt(root, "innerH", 3);

        int innerOffset = gap + 1;

        var stage = ScriptableObject.CreateInstance<StageDefinition>();

        // ---------- base cells ----------
        int w = Mathf.Max(1, outerSize);
        int h = Mathf.Max(1, outerSize);
        var cells = new E_CellType[w * h];
        for (int i = 0; i < cells.Length; i++)
            cells[i] = E_CellType.Empty; // Floor

        // Father 이동 bounds = InnerBase 영역 (요구사항)
        RectInt fatherMoveRect = new RectInt(innerOffset, innerOffset, innerW, innerH);
        fatherMoveRect = ClampRectToBoardOrFallback(stageId, fatherMoveRect, w, h);

        // ---------- innerBase ----------
        var toggleSwitchSpawns = new List<ToggleSwitchSpawnData>(8);
        var holeCells = new List<Vector2Int>(16);

        var innerBase = Get2DIntArray(root, "innerBase");
        if (innerBase == null)
        {
            Debug.LogWarning($"[StageJson] innerBase missing. stageId={stageId} (fallback: all floor)");
        }
        else
        {
            for (int y = 0; y < innerH; y++)
            {
                for (int x = 0; x < innerW; x++)
                {
                    int v = SafeGet2D(innerBase, x, y, InnerBase_Floor);

                    Vector2Int cell = new Vector2Int(innerOffset + x, innerOffset + y);
                    if ((uint)cell.x >= (uint)w || (uint)cell.y >= (uint)h)
                    {
                        Debug.LogWarning($"[StageJson] innerBase out-of-bounds ignored. stageId={stageId} inner=({x},{y}) cell={cell}");
                        continue;
                    }

                    switch (v)
                    {
                        case InnerBase_Floor:
                            break;

                        case InnerBase_Wall:
                            cells[cell.y * w + cell.x] = E_CellType.Wall;
                            break;

                        case InnerBase_ToggleSwitch:
                            toggleSwitchSpawns.Add(new ToggleSwitchSpawnData
                            {
                                _cell = cell,
                                _mode = E_SwitchMode.HoldWhilePressed,
                                _startOn = false,
                                _targetDoorGuids = Array.Empty<string>(),
                            });
                            break;

                        case InnerBase_Hole:
                            holeCells.Add(cell);
                            break;

                        default:
                            Debug.LogWarning($"[StageJson] innerBase unknown value. stageId={stageId} v={v} at ({x},{y}) (fallback: floor)");
                            break;
                    }
                }
            }
        }

        // ---------- ringTiles ----------
        List<int> perimeter = PerimeterPathBuilder.Build(w, h);
        int pathCount = perimeter.Count;

        var doorSpawns = new List<DoorSpawnData>(32);
        var doorGuidByStep = new Dictionary<int, string>();

        var ringTiles = GetIntArray(root, "ringTiles");
        if (ringTiles == null)
        {
            Debug.LogWarning($"[StageJson] ringTiles missing. stageId={stageId} (fallback: no doors/goal)");
        }
        else
        {
            int n = Mathf.Min(ringTiles.Count, pathCount);
            if (ringTiles.Count != pathCount)
                Debug.LogWarning($"[StageJson] ringTiles length mismatch. stageId={stageId} ring={ringTiles.Count} path={pathCount} (fallback: min length)");

            for (int step = 0; step < n; step++)
            {
                int idx = perimeter[step];
                var cell = new Vector2Int(idx % w, idx / w);

                int t = ringTiles[step];

                if (t == Ring_Goal)
                {
                    cells[cell.y * w + cell.x] = E_CellType.Goal;
                    continue;
                }

                bool isDoorTile = (t == Ring_Door) || (t == Ring_SwitchOpenWall) || (t == Ring_SwitchCloseWall);
                if (!isDoorTile)
                    continue;

                bool startOpen = (t == Ring_SwitchCloseWall); // 4만 true, 나머지 false

                string guidN = MakeDeterministicGuidN($"{stageId}|Door|Step|{step}");
                doorGuidByStep[step] = guidN;

                doorSpawns.Add(new DoorSpawnData
                {
                    _cell = cell,
                    _pathStep = step,
                    _startOpen = startOpen,
                    _guid = guidN,
                });
            }
        }

        // ---------- child start / goal step ----------
        int childStartIndex = GetInt(root, "childStartIndex", 0);
        int goalIndex = GetInt(root, "goalIndex", -1);

        // goalIndex는 ringTiles==3을 우선 신뢰
        int goalStep = FindGoalStepFromCells(perimeter, w, h, cells);
        if (goalStep >= 0)
        {
            if (goalIndex >= 0 && goalIndex != goalStep)
                Debug.LogWarning($"[StageJson] goalIndex mismatch. stageId={stageId} jsonGoal={goalIndex} ringGoal={goalStep} (use ringGoal)");
        }
        else
        {
            // ringTiles에 3이 없으면 goalIndex를 사용 (fallback)
            if (goalIndex >= 0 && goalIndex < pathCount)
            {
                int idx = perimeter[goalIndex];
                var cell = new Vector2Int(idx % w, idx / w);
                cells[cell.y * w + cell.x] = E_CellType.Goal;
                goalStep = goalIndex;

                Debug.LogWarning($"[StageJson] ring goal missing. stageId={stageId} fallback to goalIndex={goalIndex}");
            }
            else
            {
                Debug.LogWarning($"[StageJson] goal not found. stageId={stageId} (fallback: no clear)");
                goalStep = -1;
            }
        }

        int childStartStep = Mathf.Clamp(childStartIndex, 0, Mathf.Max(0, pathCount - 1));
        if (childStartIndex != childStartStep)
            Debug.LogWarning($"[StageJson] childStartIndex clamped. stageId={stageId} raw={childStartIndex} clamped={childStartStep}");

        // child spawn cell도 같이 맞춰둠 (디버그/일관성)
        Vector2Int childSpawnCell = Vector2Int.zero;
        if (pathCount > 0)
        {
            int idx = perimeter[childStartStep];
            childSpawnCell = new Vector2Int(idx % w, idx / w);
        }

        // ---------- father spawn ----------
        Vector2Int fatherCell = GetInnerCell(root, "fatherSpawn", innerOffset, w, h, stageId);

        // father spawn이 InnerBase 밖이면 로더에서 clamp할 예정이지만, 여기서도 Warning을 남김
        if (!fatherMoveRect.Contains(fatherCell))
            Debug.LogWarning($"[StageJson] fatherSpawn out of InnerBase(FatherMoveRect). stageId={stageId} spawn={fatherCell} rect={fatherMoveRect}");

        // ---------- blocks ----------
        var gapBlocks = new List<Vector2Int>(16);
        if (root.TryGetValue("blocks", out object blocksObj) && blocksObj is List<object> blocksList)
        {
            for (int i = 0; i < blocksList.Count; i++)
            {
                if (blocksList[i] is not Dictionary<string, object> b)
                    continue;

                int bx = GetInt(b, "x", 0);
                int by = GetInt(b, "y", 0);

                Vector2Int cell = new Vector2Int(innerOffset + bx, innerOffset + by);
                if ((uint)cell.x >= (uint)w || (uint)cell.y >= (uint)h)
                {
                    Debug.LogWarning($"[StageJson] block out-of-bounds ignored. stageId={stageId} inner=({bx},{by}) cell={cell}");
                    continue;
                }

                gapBlocks.Add(cell);
            }
        }

        // Hole 위 Block 충돌 체크 (Warning)
        if (holeCells.Count > 0 && gapBlocks.Count > 0)
        {
            var holeSet = new HashSet<Vector2Int>(holeCells);
            for (int i = 0; i < gapBlocks.Count; i++)
            {
                if (holeSet.Contains(gapBlocks[i]))
                    Debug.LogWarning($"[StageJson] block on hole conflict. stageId={stageId} cell={gapBlocks[i]} (keep data, runtime may behave odd)");
            }
        }

        // ---------- switchLinks -> switch target door guids ----------
        ApplySwitchLinks(
            stageId,
            root,
            innerOffset,
            w,
            h,
            doorGuidByStep,
            ref doorSpawns,
            toggleSwitchSpawns);

        // ---------- blocked steps initial ----------
        // Door가 닫힌 상태면 시작 시 막힘이어야 함.
        var blockedSteps = new List<int>(32);
        for (int i = 0; i < doorSpawns.Count; i++)
        {
            var d = doorSpawns[i];
            if (!d._startOpen)
                blockedSteps.Add(d._pathStep);
        }

        // ---------- apply to StageDefinition ----------
        stage.ApplyRuntimeData(new StageDefinitionRuntimeData
        {
            StageId = stageId,
            BoardSize = new Vector2Int(w, h),
            Cells = cells,

            FatherSpawnCell = fatherCell,
            ChildSpawnCell = childSpawnCell,

            FatherMoveRect = fatherMoveRect,

            ChildStartPathStep = childStartStep,
            ChildGoalPathStep = goalStep,

            BlockedPathSteps = blockedSteps.ToArray(),
            HoleCells = holeCells.ToArray(),
            GapFillerBlockCells = gapBlocks.ToArray(),

            DoorSpawns = doorSpawns.ToArray(),
            ToggleSwitchSpawns = toggleSwitchSpawns.ToArray(),
        });

        return stage;
    }

    private static RectInt ClampRectToBoardOrFallback(string stageId, RectInt r, int w, int h)
    {
        if (r.width <= 0 || r.height <= 0)
        {
            Debug.LogWarning($"[StageJson] FatherMoveRect invalid. stageId={stageId} raw={r} (fallback: full board)");
            return new RectInt(0, 0, w, h);
        }

        int xMin = Mathf.Clamp(r.xMin, 0, w - 1);
        int yMin = Mathf.Clamp(r.yMin, 0, h - 1);

        int xMax = Mathf.Clamp(r.xMax, xMin + 1, w);
        int yMax = Mathf.Clamp(r.yMax, yMin + 1, h);

        var clamped = new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        if (clamped != r)
            Debug.LogWarning($"[StageJson] FatherMoveRect clamped. stageId={stageId} raw={r} clamped={clamped}");

        return clamped;
    }

    // ---------------- helpers ----------------

    private static void ApplySwitchLinks(
        string stageId,
        Dictionary<string, object> root,
        int innerOffset,
        int w,
        int h,
        Dictionary<int, string> doorGuidByStep,
        ref List<DoorSpawnData> doorSpawns,
        List<ToggleSwitchSpawnData> toggleSwitchSpawns)
    {
        if (!root.TryGetValue("switchLinks", out object linksObj) || linksObj is not Dictionary<string, object> links)
            return;

        // quick map: cell -> index
        var idxByCell = new Dictionary<Vector2Int, int>();
        for (int i = 0; i < toggleSwitchSpawns.Count; i++)
            idxByCell[toggleSwitchSpawns[i]._cell] = i;

        foreach (var kv in links)
        {
            if (!TryParseKeyXY(kv.Key, out int ix, out int iy))
            {
                Debug.LogWarning($"[StageJson] switchLinks key parse failed. stageId={stageId} key={kv.Key}");
                continue;
            }

            Vector2Int swCell = new Vector2Int(innerOffset + ix, innerOffset + iy);
            if ((uint)swCell.x >= (uint)w || (uint)swCell.y >= (uint)h)
            {
                Debug.LogWarning($"[StageJson] switchLinks key out-of-bounds. stageId={stageId} key={kv.Key} cell={swCell}");
                continue;
            }

            if (!idxByCell.TryGetValue(swCell, out int swIndex))
            {
                Debug.LogWarning($"[StageJson] switchLinks exists but innerBase has no switch. stageId={stageId} cell={swCell} (ignored)");
                continue;
            }

            if (kv.Value is not Dictionary<string, object> linkObj)
            {
                Debug.LogWarning($"[StageJson] switchLinks value invalid. stageId={stageId} cell={swCell}");
                continue;
            }

            // enable/disable/invert 구분 없이 "연결"로 취급 (현재 ToggleSwitchController는 door invert만 지원)
            var steps = new HashSet<int>();
            AddStepsFromArray(linkObj, "enable", steps);
            AddStepsFromArray(linkObj, "disable", steps);
            AddStepsFromArray(linkObj, "invert", steps);

            if (steps.Count == 0)
            {
                Debug.LogWarning($"[StageJson] switchLinks has empty steps. stageId={stageId} cell={swCell}");
                return;
            }

            var guidList = new List<string>(steps.Count);
            foreach (int step in steps)
            {
                if (doorGuidByStep.TryGetValue(step, out string guidN))
                {
                    guidList.Add(guidN);
                    continue;
                }

                // 링에 Door가 없는데 링크가 걸린 경우: Door를 강제 생성(폴백) + Warning
                Debug.LogWarning($"[StageJson] switchLinks references missing door step. stageId={stageId} step={step} (fallback: create closed Door)");

                string newGuidN = MakeDeterministicGuidN($"{stageId}|Door|Auto|Step|{step}");
                doorGuidByStep[step] = newGuidN;
                guidList.Add(newGuidN);

                // step -> cell
                var perimeter = PerimeterPathBuilder.Build(w, h);
                int pathCount = perimeter.Count;
                if (step < 0 || step >= pathCount)
                {
                    Debug.LogWarning($"[StageJson] fallback door create failed: step out of range. stageId={stageId} step={step} pathCount={pathCount}");
                    continue;
                }

                int idx = perimeter[step];
                var cell = new Vector2Int(idx % w, idx / w);

                doorSpawns.Add(new DoorSpawnData
                {
                    _cell = cell,
                    _pathStep = step,
                    _startOpen = false,
                    _guid = newGuidN,
                });
            }

            var sw = toggleSwitchSpawns[swIndex];
            sw._targetDoorGuids = guidList.ToArray();
            toggleSwitchSpawns[swIndex] = sw;
        }
    }

    private static void AddStepsFromArray(Dictionary<string, object> obj, string key, HashSet<int> dst)
    {
        if (!obj.TryGetValue(key, out object v) || v is not List<object> list) return;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] is long l) dst.Add((int)l);
            else if (list[i] is double d) dst.Add((int)d);
        }
    }

    private static int FindGoalStepFromCells(List<int> perimeter, int w, int h, E_CellType[] cells)
    {
        for (int step = 0; step < perimeter.Count; step++)
        {
            int idx = perimeter[step];
            int x = idx % w;
            int y = idx / w;
            if (cells[y * w + x] == E_CellType.Goal)
                return step;
        }
        return -1;
    }

    private static Vector2Int GetInnerCell(Dictionary<string, object> root, string key, int innerOffset, int w, int h, string stageId)
    {
        if (!root.TryGetValue(key, out object v) || v is not Dictionary<string, object> obj)
        {
            Debug.LogWarning($"[StageJson] {key} missing. stageId={stageId} (fallback: 0,0)");
            return Vector2Int.zero;
        }

        int ix = GetInt(obj, "x", 0);
        int iy = GetInt(obj, "y", 0);

        var cell = new Vector2Int(innerOffset + ix, innerOffset + iy);
        if ((uint)cell.x >= (uint)w || (uint)cell.y >= (uint)h)
        {
            Debug.LogWarning($"[StageJson] {key} out-of-bounds. stageId={stageId} inner=({ix},{iy}) cell={cell} (fallback: clamp)");
            cell = new Vector2Int(Mathf.Clamp(cell.x, 0, w - 1), Mathf.Clamp(cell.y, 0, h - 1));
        }

        return cell;
    }

    private static List<int> GetIntArray(Dictionary<string, object> root, string key)
    {
        if (!root.TryGetValue(key, out object v) || v is not List<object> list)
            return null;

        var res = new List<int>(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] is long l) res.Add((int)l);
            else if (list[i] is double d) res.Add((int)d);
            else res.Add(0);
        }
        return res;
    }

    private static int[][] Get2DIntArray(Dictionary<string, object> root, string key)
    {
        if (!root.TryGetValue(key, out object v) || v is not List<object> rows)
            return null;

        int[][] a = new int[rows.Count][];
        for (int y = 0; y < rows.Count; y++)
        {
            if (rows[y] is not List<object> cols)
            {
                a[y] = Array.Empty<int>();
                continue;
            }

            a[y] = new int[cols.Count];
            for (int x = 0; x < cols.Count; x++)
            {
                if (cols[x] is long l) a[y][x] = (int)l;
                else if (cols[x] is double d) a[y][x] = (int)d;
                else a[y][x] = 0;
            }
        }
        return a;
    }

    private static int SafeGet2D(int[][] a, int x, int y, int fallback)
    {
        if (a == null || y < 0 || y >= a.Length) return fallback;
        if (a[y] == null || x < 0 || x >= a[y].Length) return fallback;
        return a[y][x];
    }

    private static int GetInt(Dictionary<string, object> obj, string key, int fallback)
    {
        if (!obj.TryGetValue(key, out object v) || v == null)
            return fallback;

        if (v is long l) return (int)l;
        if (v is double d) return (int)d;

        return fallback;
    }

    private static bool TryParseKeyXY(string key, out int x, out int y)
    {
        x = 0; y = 0;
        if (string.IsNullOrWhiteSpace(key)) return false;

        int comma = key.IndexOf(',');
        if (comma <= 0 || comma >= key.Length - 1) return false;

        string sx = key.Substring(0, comma);
        string sy = key.Substring(comma + 1);

        return int.TryParse(sx, out x) && int.TryParse(sy, out y);
    }

    private static string MakeDeterministicGuidN(string seed)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(seed);

        byte[] hash;
        using (var md5 = MD5.Create())
            hash = md5.ComputeHash(bytes); // 16 bytes

        var guid = new Guid(hash);
        return guid.ToString("N");
    }
}
