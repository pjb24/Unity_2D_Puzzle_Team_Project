// StageLoader.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class StageLoader
{
    // ===== Tunables =====
    private readonly float _defaultTileSize = 1.0f;
    private readonly float _defaultTileGap = 0.0f;

    private readonly HashSet<string> _warnedMissingPrefabs = new HashSet<string>();

    public void LoadStage(GameFlowContext ctx, Action onComplete)
    {
        if (ctx == null)
        {
            Debug.LogWarning("[StageLoader] LoadStage fallback: ctx is null.");
            return;
        }

        if (ctx._config == null)
        {
            Debug.LogError("[StageLoader] LoadStage failed: ctx._config is null.");
            return;
        }

        // 1) 이전 스테이지 정리
        UnloadStage(ctx);

        // 2) StageDefinition 가져오기
        var stageDef = GetStageDefinitionOrFail(ctx);
        if (stageDef == null) return;
        ctx._stageDefinition = stageDef;

        Debug.Log("[StageLoader] Chapter: " + ctx._chapterIndex + " , Stage: " + ctx._stageIndex);

        // 3) 런타임 refs 생성
        CreateStageRuntime(ctx);

        // Stage 생성 직후(턴 시작 전) 스냅샷 1회 저장 (turnIndex=0)
        CaptureStageCreatedSnapshot(ctx);

        onComplete?.Invoke();
    }

    public void UnloadStage(GameFlowContext ctx)
    {
        if (ctx == null)
        {
            Debug.LogWarning("[StageLoader] UnloadStage fallback: ctx is null.");
            return;
        }

        if (ctx._stageRuntime == null)
            return;

        // 1) 레지스트리/리스트 정리 (참조 끊기)
        try
        {
            ctx._stageRuntime._fatherController?.UnbindGapFillerRegistry();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[StageLoader] UnloadStage cleanup warn. ex={ex.Message}");
        }

        // 스냅샷은 스테이지 단위로 폐기(이전 스테이지 GUID가 다음 스테이지에서 “not found” 되는 원인 차단)
        if (ctx._stageRuntime._snapshot != null)
        {
            ctx._stageRuntime._snapshot.ClearAll();
            ctx._stageRuntime._snapshot.BindStageRoot(null);
        }

        if (ctx._stageRuntime._turnSystems != null)
            ctx._stageRuntime._turnSystems.Clear();

        // 2) 즉시 화면에서 제거(다음 프레임 Destroy 지연 잔상 방지)
        if (ctx._stageRuntime._root != null)
            ctx._stageRuntime._root.SetActive(false);

        // 3) 실제 파괴 예약
        if (ctx._stageRuntime._root != null)
            UnityEngine.Object.Destroy(ctx._stageRuntime._root);

        ctx._stageRuntime = null;
    }

    private void CaptureStageCreatedSnapshot(GameFlowContext ctx)
    {
        if (ctx == null || ctx._stageRuntime == null)
        {
            Debug.LogWarning("[StageLoader] StageStart snapshot skipped (fallback): ctx/stageRuntime is null.");
            return;
        }

        var snapshot = UnityEngine.Object.FindFirstObjectByType<TurnSnapshotRecorder>();
        if (snapshot == null)
        {
            Debug.LogWarning("[StageLoader] StageStart snapshot skipped (fallback): TurnSnapshotRecorder not found in scene.");
            return;
        }

        // 디버그/추적용으로 runtime refs에도 보관
        ctx._stageRuntime._snapshot = snapshot;

        if (ctx._stageRuntime._root != null)
            snapshot.BindStageRoot(ctx._stageRuntime._root.transform);
        else
            Debug.LogWarning("[StageLoader] Snapshot scope bind skipped (fallback): stageRoot is null.");

        // 정책: 스테이지마다 새로 시작해야 하므로 기존 스냅샷은 제거 후 0번 저장
        snapshot.ClearAll();
        snapshot.Capture(turnIndex: 0);

        Debug.Log("[StageLoader] Captured stage-created snapshot (turnIndex=0).");
    }

    private StageDefinition GetStageDefinitionOrFail(GameFlowContext ctx)
    {
        var stageDef = ctx._config.GetStageDefinition(ctx._chapterIndex, ctx._stageIndex);
        if (stageDef == null)
        {
            Debug.LogError("[StageLoader] StageDefinition is null");
            return null;
        }
        return stageDef;
    }

    private void CreateStageRuntime(GameFlowContext ctx)
    {
        var stageDef = ctx._stageDefinition;
        if (stageDef == null)
        {
            Debug.LogWarning("[StageLoader] CreateStageRuntime fallback: stageDef is null.");
            return;
        }

        // GameConfig (Prefabs 접근)
        var gameConfig = ctx._gameConfig;
        if (gameConfig == null)
        {
            Debug.LogWarning("[StageLoader] GameConfig is null. Reloading from provider (fallback).");
            gameConfig = ctx._config.LoadGameConfig();
            ctx._gameConfig = gameConfig;
        }

        StagePrefabs prefabs = gameConfig.Prefabs;

        var rt = new StageRuntimeRefs();
        rt._stageId = string.IsNullOrEmpty(stageDef.StageId) ? $"Chapter{ctx._chapterIndex}_Stage{ctx._stageIndex}" : stageDef.StageId;

        // ===== Layout =====
        rt._tileScale = _defaultTileSize;
        rt._tileGap = _defaultTileGap;
        rt._cellPitch = rt._tileScale + rt._tileGap;
        if (rt._cellPitch <= 0f)
        {
            rt._cellPitch = 1f;
            Debug.LogWarning($"[StageLoader] CellPitch fallback: invalid={rt._cellPitch} -> 1.0");
        }

        // ===== Root Tree =====
        rt._root = new GameObject($"[StageRuntime] {rt._stageId}");

        var tilesRoot = new GameObject("[Tiles]").transform;
        tilesRoot.SetParent(rt._root.transform, false);
        rt._tilesRoot = tilesRoot;

        var baseRoot = new GameObject("[Base]").transform;
        baseRoot.SetParent(tilesRoot, false);

        var overlayRoot = new GameObject("[Overlay]").transform;
        overlayRoot.SetParent(tilesRoot, false);

        var pathRoot = new GameObject("[Path]").transform;
        pathRoot.SetParent(tilesRoot, false);
        rt._pathRoot = pathRoot;

        // PathFadeFx 없으면 붙여서 “경고 없이” 정상 동작시키기
        if (pathRoot.GetComponent<PathFadeFx>() == null)
            pathRoot.gameObject.AddComponent<PathFadeFx>();

        var actorsRoot = new GameObject("[Actors]").transform;
        actorsRoot.SetParent(tilesRoot, false);

        var gimmicksRoot = new GameObject("[Gimmicks]").transform;
        gimmicksRoot.SetParent(tilesRoot, false);

        var blocksRoot = new GameObject("[Blocks]").transform;
        blocksRoot.SetParent(tilesRoot, false);

        // ===== Board Model & Presenter =====
        int w = stageDef.BoardSize.x;
        int h = stageDef.BoardSize.y;

        rt._grid = new BoardGrid(w, h, stageDef);

        var scaleApplier = new StageScaleApplier(rt._root.transform);
        float stageScale = scaleApplier.Apply(w, h);

        rt._gridPresenter = new GridPresenter();
        rt._gridPresenter._tileSize = rt._cellPitch;
        rt._gridPresenter.Initialize(tilesRoot, rt._grid);

        // ===== Path Runtime (points + blockers) =====
        var pathRuntime = new ChildPathRuntime(rt._grid, rt._gridPresenter);

        rt._pathPoints.Clear();
        for (int i = 0; i < pathRuntime.Points.Count; i++)
            rt._pathPoints.Add(pathRuntime.Points[i]);

        rt._childPathBlockers = new ChildPathBlockerRegistry(stageDef.BlockedPathSteps, pathRuntime.Count);

        // ===== GapFiller Registry (Mono) =====
        var gapRegGo = new GameObject("GapFillerBlockRegistry");
        gapRegGo.transform.SetParent(rt._root.transform, false);

        rt._gapFillerRegistry = gapRegGo.AddComponent<GapFillerBlockRegistry>();
        rt._gapFillerRegistry.ConfigureMoveBounds(stageDef.FatherMoveRect, rt._grid);

        // ===== Spawn: Base Tiles + Walls =====
        SpawnBaseAndWalls(stageDef, prefabs, baseRoot, overlayRoot, rt._gridPresenter, rt._tileScale);

        // ===== Spawn: Holes =====
        SpawnHoles(stageDef, prefabs, overlayRoot, rt._gridPresenter, rt._tileScale);

        // ===== Spawn: Doors =====
        SpawnDoors(stageDef, prefabs, gimmicksRoot, rt, rt._tileScale);

        // ===== Spawn: GapFiller Blocks =====
        SpawnGapFillerBlocks(stageDef, prefabs, blocksRoot, rt, rt._tileScale);

        // ===== Spawn: Characters =====
        SpawnCharacters(stageDef, prefabs, actorsRoot, rt, pathRuntime);

        // ===== Spawn: Toggle Switches =====
        SpawnToggleSwitches(stageDef, prefabs, gimmicksRoot, rt, rt._tileScale);

        // ===== Bind: Switch Links (Door GUID resolve) =====
        BindSwitchLinks(rt);

        // ctx에 등록
        ctx._stageRuntime = rt;
    }

    private void SpawnBaseAndWalls(StageDefinition stageDef, StagePrefabs prefabs, Transform baseRoot, Transform overlayRoot, GridPresenter presenter, float tileScale)
    {
        int w = stageDef.BoardSize.x;
        int h = stageDef.BoardSize.y;
        var cells = stageDef.Cells;

        // “base floor를 깔아야 하는 셀” 보강 (door/switch/block 같은 스폰 셀은 데이터상 Empty일 수 있음)
        var baseCells = new HashSet<Vector2Int>();

        var holes = stageDef.GetHoleCells_Runtime();
        for (int i = 0; i < holes.Length; i++) baseCells.Add(holes[i]);

        var blocks = stageDef.GetGapFillerBlockCells_Runtime();
        for (int i = 0; i < blocks.Length; i++) baseCells.Add(blocks[i]);

        var switches = stageDef.ToggleSwitchSpawns;
        for (int i = 0; i < switches.Length; i++) baseCells.Add(switches[i]._cell);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                var t = cells[idx];
                var cell = new Vector2Int(x, y);

                bool needFloor = !(t == E_CellType.Empty || t == E_CellType.Goal)
                    || baseCells.Contains(cell);
                if (needFloor)
                {
                    var floorGo = SpawnPrefabOrFallback(prefabs != null ? prefabs.Floor : null, "[Floor]", baseRoot, presenter.CellToLocal(cell), tileScale);
                }

                if (t == E_CellType.Wall)
                {
                    var wallGo = SpawnPrefabOrFallback(prefabs != null ? prefabs.Wall : null, "[Wall]", overlayRoot, presenter.CellToLocal(cell), tileScale);
                }

                // Goal은 StageJson이 cells에 박아두므로 여기서 렌더
                if (t == E_CellType.Goal)
                {
                    var goalGo = SpawnPrefabOrFallback(prefabs != null ? prefabs.Goal : null, "[Goal]", overlayRoot, presenter.CellToLocal(cell), tileScale);
                }
            }
    }

    private void SpawnHoles(StageDefinition stageDef, StagePrefabs prefabs, Transform overlayRoot, GridPresenter presenter, float tileScale)
    {
        var holes = stageDef.GetHoleCells_Runtime();
        for (int i = 0; i < holes.Length; i++)
        {
            var holeGo = SpawnPrefabOrFallback(prefabs != null ? prefabs.Hole : null, "[Hole]", overlayRoot, presenter.CellToLocal(holes[i]), tileScale);
        }
    }

    private void SpawnDoors(StageDefinition stageDef, StagePrefabs prefabs, Transform gimmicksRoot, StageRuntimeRefs rt, float tileScale)
    {
        var spawns = stageDef.DoorSpawns;
        for (int i = 0; i < spawns.Length; i++)
        {
            var d = spawns[i];

            var go = SpawnPrefabOrFallback(prefabs != null ? prefabs.Door : null, "[Door]", gimmicksRoot, rt._gridPresenter.CellToLocal(d._cell), tileScale);

            var key = go.GetComponent<RewindKey>();
            if (key == null) key = go.AddComponent<RewindKey>();

            if (!string.IsNullOrEmpty(d._guid))
            {
                // Door GUID는 스테이지 데이터가 “링크 키”로 쓰므로 overwrite 강제
                key.TrySetGuidString(d._guid, overwrite: true);
            }
            else
            {
                Debug.LogWarning($"[StageLoader] Door GUID missing. cell={d._cell} (fallback: auto guid)");
            }

            var door = go.GetComponent<DoorController>();
            if (door == null)
            {
                Debug.LogWarning($"[StageLoader] DoorController missing on prefab. cell={d._cell} (fallback: add component)");
                door = go.AddComponent<DoorController>();
            }

            door.Initialize(
                rt._grid,
                rt._gridPresenter,
                d._cell,
                d._startOpen,
                rt._childPathBlockers,
                d._pathStep
            );
        }
    }

    private void SpawnGapFillerBlocks(StageDefinition stageDef, StagePrefabs prefabs, Transform blocksRoot, StageRuntimeRefs rt, float tileScale)
    {
        var blocks = stageDef.GetGapFillerBlockCells_Runtime();
        for (int i = 0; i < blocks.Length; i++)
        {
            var cell = blocks[i];

            var go = SpawnPrefabOrFallback(prefabs != null ? prefabs.FillerBlock : null, "[GapFillerBlock]", blocksRoot, rt._gridPresenter.CellToLocal(cell), tileScale);

            var key = go.GetComponent<RewindKey>();
            if (key == null) key = go.AddComponent<RewindKey>(); // guid 자동 생성

            var ctrl = go.GetComponent<GapFillerBlockController>();
            if (ctrl == null)
            {
                Debug.LogWarning($"[StageLoader] GapFillerBlockController missing. cell={cell} (fallback: add component)");
                ctrl = go.AddComponent<GapFillerBlockController>();
            }

            ctrl.Initialize(rt._grid, rt._gridPresenter, rt._gapFillerRegistry, cell);
        }
    }

    private void SpawnCharacters(StageDefinition stageDef, StagePrefabs prefabs, Transform actorsRoot, StageRuntimeRefs rt, ChildPathRuntime pathRuntime)
    {
        // Father
        {
            var go = SpawnPrefabOrFallback(prefabs != null ? prefabs.Father : null, "[Father]", actorsRoot, Vector3.zero, 1f);

            rt._father = go;

            var ctrl = go.GetComponent<FatherController>();
            if (ctrl == null) ctrl = go.AddComponent<FatherController>();
            rt._fatherController = ctrl;

            ctrl.Initialize(rt._grid, rt._gridPresenter, stageDef.FatherSpawn._cell, stageDef.FatherMoveRect);
            ctrl.BindGapFillerRegistry(rt._gapFillerRegistry);
        }

        // Child
        {
            var go = SpawnPrefabOrFallback(prefabs != null ? prefabs.Child : null, "[Child]", actorsRoot, Vector3.zero, 1f);

            rt._child = go;

            var ctrl = go.GetComponent<ChildController>();
            if (ctrl == null) ctrl = go.AddComponent<ChildController>();
            rt._childController = ctrl;

            int startStep = stageDef.ChildStartPathStep;
            if (startStep < 0 || startStep >= pathRuntime.Count)
            {
                int clamped = Mathf.Clamp(startStep, 0, Mathf.Max(0, pathRuntime.Count - 1));
                Debug.LogWarning($"[StageLoader] ChildStartPathStep clamped. raw={startStep} clamped={clamped} pathCount={pathRuntime.Count}");
                startStep = clamped;
            }

            ctrl.Initialize(pathRuntime, rt._childPathBlockers, startStep);
        }
    }

    private void SpawnToggleSwitches(StageDefinition stageDef, StagePrefabs prefabs, Transform gimmicksRoot, StageRuntimeRefs rt, float tileScale)
    {
        var spawns = stageDef.ToggleSwitchSpawns;
        for (int i = 0; i < spawns.Length; i++)
        {
            var s = spawns[i];

            GameObject prefab = null;
            if (prefabs != null)
            {
                prefab = prefabs.ToggleSwitch;
            }

            var go = SpawnPrefabOrFallback(prefab, "[ToggleSwitch]", gimmicksRoot, rt._gridPresenter.CellToLocal(s._cell), tileScale);

            var key = go.GetComponent<RewindKey>();
            if (key == null) key = go.AddComponent<RewindKey>(); // guid 자동 생성(스냅샷 키용)

            var ctrl = go.GetComponent<ToggleSwitchController>();
            if (ctrl == null)
            {
                Debug.LogWarning($"[StageLoader] ToggleSwitchController missing. cell={s._cell} (fallback: add component)");
                ctrl = go.AddComponent<ToggleSwitchController>();
            }

            ctrl.ConfigureRuntime(s._cell, s._mode, s._startOn, s._targetDoorGuids);
            ctrl.InitializeGimmick(rt, rt._grid, rt._gridPresenter);

            // TurnSystem 등록 (Switch는 ITurnTickable)
            rt._turnSystems.Add(ctrl);
        }
    }

    private void BindSwitchLinks(StageRuntimeRefs rt)
    {
        if (rt == null || rt._root == null)
            return;

        var switches = rt._root.GetComponentsInChildren<ToggleSwitchController>(includeInactive: true);
        for (int i = 0; i < switches.Length; i++)
        {
            if (switches[i] == null) continue;
            switches[i].BindAllLinks(rt);
        }
    }

    private GameObject SpawnPrefabOrFallback(GameObject prefab, string name, Transform parent, Vector3 localPos, float uniformScale)
    {
        GameObject go;

        if (prefab != null)
        {
            go = UnityEngine.Object.Instantiate(prefab, parent);
            go.name = prefab.name;
        }
        else
        {
            WarnMissingPrefabOnce(name);
            go = new GameObject(name);
            go.transform.SetParent(parent, false);

            // 최소한의 Renderer는 붙여서 sorting/레이어 처리 가능하게
            if (go.GetComponentInChildren<SpriteRenderer>(includeInactive: true) == null)
                go.AddComponent<SpriteRenderer>();
        }

        go.transform.localPosition = localPos;
        if (uniformScale > 0f)
            go.transform.localScale *= uniformScale;

        return go;
    }

    private void WarnMissingPrefabOnce(string key)
    {
        if (_warnedMissingPrefabs.Contains(key))
            return;

        _warnedMissingPrefabs.Add(key);
        Debug.LogWarning($"[StageLoader] Prefab missing: {key}. Spawning minimal fallback objects (no sprite).");
    }
}
