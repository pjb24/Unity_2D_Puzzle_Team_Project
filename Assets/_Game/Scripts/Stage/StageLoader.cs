// StageLoader.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class StageLoader
{
    // ===== Tunables =====
    private readonly float _defaultTileSize = 1.0f;
    private readonly float _defaultTileGap = 0.0f;

    // Registry GameObject name (under StageRuntime root)
    private const string GapFillerRegistryName = "GapFillerBlockRegistry";

    // Colors (구분 가능)
    private static readonly Color Color_Father = new(0.10f, 0.80f, 1.00f, 1f);
    private static readonly Color Color_Child = new(1.00f, 0.20f, 1.00f, 1f);
    private static readonly Color Color_Block = new(0.70f, 0.40f, 0.10f, 1f);

    private static Sprite _whiteSprite;
    private static Texture2D _whiteTex;

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

        // 6) 스폰(그리드/프리젠터/컨트롤러 포함)
        SpawnStageActorsAndSystems(ctx);

        // 5) 테두리 경로 생성(2D Sprite 마커)
        CreateDummyPath(ctx);

        // 런타임 기믹 스폰(Doors + ToggleSwitches)
        StageGimmickSpawner.SpawnDoorsAndToggleSwitches(ctx._stageRuntime, stageDef);

        // ---- (0-5) 기믹 초기화 파이프라인 ----
        RunPostSpawnPipeline(ctx);

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
        ctx._stageRuntime = new StageRuntimeRefs();
        ctx._stageRuntime._root = new GameObject($"[StageRuntime] C{ctx._chapterIndex}_S{ctx._stageIndex}");
        var stageDef = ctx._stageDefinition;

        string stageId = stageDef != null ? stageDef.StageId : $"C{ctx._chapterIndex}_S{ctx._stageIndex}";
        ctx._stageRuntime._stageId = stageId;

        // ===== Layout resolve (tile scale + gap) =====
        float tileScale = _defaultTileSize;
        float tileGap = _defaultTileGap;

        ctx._stageRuntime._tileScale = tileScale;
        ctx._stageRuntime._tileGap = tileGap;
        ctx._stageRuntime._cellPitch = Mathf.Max(0.01f, tileScale + tileGap);
    }

    // ===== (6) 캐릭터 생성 + Initialize + GapFiller =====
    private void SpawnStageActorsAndSystems(GameFlowContext ctx)
    {
        var stageDef = ctx._stageDefinition;

        // ===== (4) 보드 생성 (2D Sprite) =====
        BuildGridAndTiles(ctx, stageDef);

        // 6) Father/Child 생성 + 초기화
        SpawnActorVisuals(ctx);
        InitializeControllers(ctx, stageDef);

        // Hole 적용 + 메움 블록 스폰/바인딩
        ApplyHolesFromStageDef(ctx, stageDef);

        var gapRegistry = EnsureGapFillerRegistry(ctx);
        // GapFillerBlock도 InnerBase 규칙 적용
        if (stageDef != null && ctx?._stageRuntime?._grid != null)
        {
            gapRegistry.ConfigureMoveBounds(stageDef.FatherMoveRect, ctx._stageRuntime._grid);
        }
        else
        {
            Debug.LogWarning("[StageLoader] GapFiller MoveBounds fallback: stageDef/grid is null. (use full board)");
            gapRegistry.ConfigureMoveBounds(default, ctx?._stageRuntime?._grid);
        }
        // ToggleSwitchController가 블록을 감지할 수 있도록 refs에 보관
        if (ctx?._stageRuntime != null)
            ctx._stageRuntime._gapFillerRegistry = gapRegistry;

        SpawnGapFillerBlocks(ctx, stageDef, gapRegistry);
        BindGapFillerToFather(ctx, gapRegistry);
    }

    private void RunPostSpawnPipeline(GameFlowContext ctx)
    {
        // 1) BoardStateRewindable 추가(보드 자체 변화 복원)
        EnsureBoardStateRewindable(ctx);
        // 2) 보드/프리젠터 생성 후 기믹 Initialize
        InitializeGimmicks(ctx);

        // 4) 링크(스위치→문 등) 바인딩
        BindLinks(ctx);
        // 5) TurnSystem(ITurnTickable) 수집
        CollectTurnSystems(ctx);
    }

    private void SpawnActorVisuals(GameFlowContext ctx)
    {
        // Father
        ctx._stageRuntime._father = SpawnVisual(
            prefab: null,
            sprite: null,
            name: "Father(Dummy)",
            parent: ctx._stageRuntime._root.transform,
            fallbackColor: Color_Father);

        // Child
        ctx._stageRuntime._child = SpawnVisual(
            prefab: null,
            sprite: null,
            name: "Child(Dummy)",
            parent: ctx._stageRuntime._root.transform,
            fallbackColor: Color_Child);
    }

    // ===== (4) 보드 생성 (2D Sprite) =====
    private void BuildGridAndTiles(GameFlowContext ctx, StageDefinition stageDef)
    {
        // Grid 생성(Cells 배열 기반)
        int w = Mathf.Max(1, stageDef.BoardSize.x);
        int h = Mathf.Max(1, stageDef.BoardSize.y);

        ctx._stageRuntime._grid = new BoardGrid(w, h, stageDef.Cells);

        ctx._stageRuntime._gridPresenter = new GridPresenter();
        ctx._stageRuntime._gridPresenter.Initialize(
            ctx._stageRuntime._root.transform,
            ctx._stageRuntime._grid
            );

        // ===== 타일 생성 =====
        ctx._stageRuntime._tilesRoot = ctx._stageRuntime._gridPresenter._root;
        ctx._stageRuntime._gridPresenter.RebuildAll(E_Dir4.None);
        if (ctx._stageRuntime._tilesRoot == null)
            Debug.LogWarning("[StageLoader] Tiles build skipped (fallback): tilesRoot is null.");
    }

    private void InitializeControllers(GameFlowContext ctx, StageDefinition stageDef)
    {
        var stageId = ctx?._stageRuntime?._stageId ?? "UNKNOWN_STAGE";

        // FatherController 부착 + 초기화
        ctx._stageRuntime._fatherController = EnsureController<FatherController>(ctx._stageRuntime._father);
        EnsureRewindKey(ctx._stageRuntime._father);

        // VisualMoveAgent(없으면 생성)
        var fatherMoveAgent = EnsureController<VisualMoveAgent>(ctx._stageRuntime._father);
        ctx._stageRuntime._fatherController.BindVisualMoveAgent(fatherMoveAgent);

        // Father 이동 애니메이션 드라이버(없으면 생성)
        var fatherAnim = EnsureController<FatherAnimDriver>(ctx._stageRuntime._father);
        ctx._stageRuntime._fatherController.BindAnimDriver(fatherAnim);

        // Father bounds 주입 (InnerBase)
        RectInt moveRect = stageDef != null ? stageDef.FatherMoveRect : default;
        if (stageDef == null)
            Debug.LogWarning("[StageLoader] FatherMoveRect fallback: stageDef is null. (use full board)");

        ctx._stageRuntime._fatherController.Initialize(
            ctx._stageRuntime._grid,
            ctx._stageRuntime._gridPresenter,
            stageDef.FatherSpawn._cell,
            moveRect
            );

        // ChildController 부착 + 초기화
        ctx._stageRuntime._childController = EnsureController<ChildController>(ctx._stageRuntime._child);
        EnsureRewindKey(ctx._stageRuntime._child);

        // VisualMoveAgent(없으면 생성)
        var childMoveAgent = EnsureController<VisualMoveAgent>(ctx._stageRuntime._child);
        ctx._stageRuntime._childController.BindVisualMoveAgent(childMoveAgent);

        // Child 이동 애니메이션 드라이버(없으면 생성)
        var childAnim = EnsureController<ChildAnimDriver>(ctx._stageRuntime._child);
        ctx._stageRuntime._childController.BindAnimDriver(childAnim);

        // ChildPathRuntime 생성
        var pathRuntime = new ChildPathRuntime(ctx._stageRuntime._grid, ctx._stageRuntime._gridPresenter);

        // 런타임 블로커 레지스트리 생성(초기값 = StageDefinition.BlockedPathSteps)
        ctx._stageRuntime._childPathBlockers = new ChildPathBlockerRegistry(stageDef.BlockedPathSteps, pathRuntime.Count);

        int startPos = 0;
        if (stageDef != null)
        {
            startPos = Mathf.Clamp(stageDef.ChildStartPathStep, 0, Mathf.Max(0, pathRuntime.Count - 1));
            if (startPos != stageDef.ChildStartPathStep)
                Debug.LogWarning($"[StageLoader] ChildStartPathStep clamped. raw={stageDef.ChildStartPathStep} clamped={startPos}");
        }

        // ChildController는 Registry 버전 Initialize 사용
        ctx._stageRuntime._childController.Initialize(
            pathRuntime,
            ctx._stageRuntime._childPathBlockers,
            startPos);
    }

    private T EnsureController<T>(GameObject go) where T : MonoBehaviour
    {
        if (go == null) return null;

        var c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    private void BindGapFillerToFather(GameFlowContext ctx, GapFillerBlockRegistry gapRegistry)
    {
        var fatherCtrl = ctx?._stageRuntime?._fatherController;
        if (fatherCtrl == null)
        {
            Debug.LogWarning("[StageLoader] GapFiller bind skipped (fallback): fatherCtrl is null.");
            return;
        }

        if (gapRegistry == null)
        {
            Debug.LogWarning("[StageLoader] GapFiller bind skipped (fallback): gapRegistry is null.");
            return;
        }

        fatherCtrl.BindGapFillerRegistry(gapRegistry);
    }

    // ---- helpers ----

    private void EnsureBoardStateRewindable(GameFlowContext ctx)
    {
        if (ctx?._stageRuntime?._root == null)
        {
            Debug.LogWarning("[StageLoader] EnsureBoardStateRewindable fallback: stage root is null.");
            return;
        }

        var root = ctx._stageRuntime._root;

        var bsr = root.GetComponent<BoardStateRewindable>();
        if (bsr == null) bsr = root.AddComponent<BoardStateRewindable>();

        bsr.Initialize(ctx._stageRuntime._grid);

        // RewindKey 보장
        EnsureRewindKey(root);

        ctx._stageRuntime._boardStateRewindable = bsr;
    }

    private void InitializeGimmicks(GameFlowContext ctx)
    {
        if (ctx?._stageRuntime?._root == null) return;

        var root = ctx._stageRuntime._root.transform;
        var behaviours = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null) continue;
            if (behaviours[i] is not ToggleSwitchController init) continue;

            try
            {
                init.InitializeGimmick(ctx._stageRuntime, ctx._stageRuntime._grid, ctx._stageRuntime._gridPresenter);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StageLoader] Gimmick Initialize failed. ex={ex.Message}");
            }
        }
    }

    private void BindLinks(GameFlowContext ctx)
    {
        if (ctx?._stageRuntime?._root == null) return;

        var root = ctx._stageRuntime._root.transform;
        var behaviours = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null) continue;
            if (behaviours[i] is not ToggleSwitchController binder) continue;

            try
            {
                binder.BindAllLinks(ctx._stageRuntime);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StageLoader] Link bind failed. ex={ex.Message}");
            }
        }
    }

    private void CollectTurnSystems(GameFlowContext ctx)
    {
        if (ctx?._stageRuntime?._root == null) return;

        ctx._stageRuntime._turnSystems.Clear();

        var root = ctx._stageRuntime._root.transform;
        var behaviours = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null) continue;
            if (behaviours[i] is not ITurnTickable sys) continue;

            ctx._stageRuntime._turnSystems.Add(sys);
        }
    }

    private GapFillerBlockRegistry EnsureGapFillerRegistry(GameFlowContext ctx)
    {
        if (ctx == null || ctx._stageRuntime == null || ctx._stageRuntime._root == null)
        {
            Debug.LogWarning("[StageLoader] EnsureGapFillerRegistry fallback: runtime/root is null.");
            return null;
        }

        var root = ctx._stageRuntime._root.transform;

        var existing = root.GetComponentInChildren<GapFillerBlockRegistry>(includeInactive: true);
        if (existing != null)
            return existing;

        var go = new GameObject(GapFillerRegistryName);
        go.transform.SetParent(root, false);
        return go.AddComponent<GapFillerBlockRegistry>();
    }

    // ===== (5) 테두리 경로 생성 (2D Sprite 마커) =====
    private void CreateDummyPath(GameFlowContext ctx)
    {
        if (ctx == null || ctx._stageRuntime == null || ctx._stageRuntime._root == null)
        {
            Debug.LogWarning("[StageLoader] CreateDummyPath fallback: runtime/root is null.");
            return;
        }

        var stageDef = ctx._stageDefinition;

        int w = Mathf.Max(1, stageDef.BoardSize.x);
        int h = Mathf.Max(1, stageDef.BoardSize.y);

        ResolveTileMetrics(ctx, out float tileSize, out float tileGap, out float tilePitch);

        var pathRoot = new GameObject("[Path]");
        pathRoot.transform.SetParent(ctx._stageRuntime._root.transform, false);
        ctx._stageRuntime._pathRoot = pathRoot.transform;

        // PathFadeFx는 "Path 관련 렌더러"를 페이드시키는 용도였음.
        // Path 마커는 없어졌지만 Border 렌더러는 여기에 붙으므로 유지해도 정상 동작함.
        if (pathRoot.GetComponent<PathFadeFx>() == null)
            pathRoot.AddComponent<PathFadeFx>();

        ctx._stageRuntime._pathPoints.Clear();

        Vector3 origin = GetTileOriginLocal(w, h, tilePitch);

        var orderedPathCells = new List<Vector2Int>(w * 2 + h * 2);

        // 테두리(오른쪽→위→왼쪽→아래) 셀 경로
        void AddCell(int x, int y)
        {
            Vector3 local = origin + new Vector3(x * tilePitch, y * tilePitch, 0f);
            ctx._stageRuntime._pathPoints.Add(ToWorld(ctx, local));
            orderedPathCells.Add(new Vector2Int(x, y));
        }

        // bottom (0,0) -> (w-1,0)
        for (int x = 0; x < w; x++) AddCell(x, 0);
        // right (w-1,1) -> (w-1,h-1)
        for (int y = 1; y < h; y++) AddCell(w - 1, y);
        // top (w-2,h-1) -> (0,h-1)
        for (int x = w - 2; x >= 0; x--) AddCell(x, h - 1);
        // left (0,h-2) -> (0,1)
        for (int y = h - 2; y >= 1; y--) AddCell(0, y);
    }

    private void ResolveTileMetrics(GameFlowContext ctx, out float tileSize, out float tileGap, out float tilePitch)
    {
        tileSize = 1f;
        tileGap = 0f;
        tilePitch = tileSize + tileGap;
    }

    private static Vector3 GetTileOriginLocal(int w, int h, float tilePitch)
    {
        return new Vector3(
            -(w - 1) * 0.5f * tilePitch,
            -(h - 1) * 0.5f * tilePitch,
            0f
        );
    }

    private void ApplyHolesFromStageDef(GameFlowContext ctx, StageDefinition stageDef)
    {
        if (ctx?._stageRuntime?._grid == null)
        {
            Debug.LogWarning("[StageLoader] ApplyHoles fallback: grid is null.");
            return;
        }

        var holes = stageDef.GetHoleCells_Runtime();
        if (holes == null || holes.Length == 0) return;

        var grid = ctx._stageRuntime._grid;

        for (int i = 0; i < holes.Length; i++)
        {
            var c = holes[i];
            if (!grid.IsInBounds(c))
            {
                Debug.LogWarning($"[StageLoader] ApplyHoles fallback: out of bounds. cell={c}");
                continue;
            }

            // 정적 지형이 막힘이면 Hole 금지(데이터가 잘못된 상태)
            var cellType = grid.GetCell(c);
            if (grid.IsBlockedCell(cellType) || cellType == E_CellType.Goal)
            {
                Debug.LogWarning($"[StageLoader] ApplyHoles fallback: invalid static cell for hole. cell={c} cellType={cellType}");
                continue;
            }

            var meta = grid.GetMeta(c);
            meta._surface = E_CellSurface.Hole;
            meta._isFilledHole = false;
            grid.SetMeta(c, meta, notify: true);
        }
    }

    private void SpawnGapFillerBlocks(GameFlowContext ctx, StageDefinition stageDef, GapFillerBlockRegistry gapRegistry)
    {
        if (ctx?._stageRuntime?._grid == null || ctx._stageRuntime._gridPresenter == null || ctx._stageRuntime._root == null)
        {
            Debug.LogWarning("[StageLoader] SpawnGapFillerBlocks fallback: grid/presenter/root is null.");
            return;
        }

        if (gapRegistry == null)
        {
            Debug.LogWarning("[StageLoader] SpawnGapFillerBlocks fallback: gapRegistry is null.");
            return;
        }

        var blocks = stageDef.GetGapFillerBlockCells_Runtime();
        if (blocks == null || blocks.Length == 0) return;

        var grid = ctx._stageRuntime._grid;
        var presenter = ctx._stageRuntime._gridPresenter;
        var parent = gapRegistry.transform;

        for (int i = 0; i < blocks.Length; i++)
        {
            var c = blocks[i];
            if (!grid.IsInBounds(c))
            {
                Debug.LogWarning($"[StageLoader] Block spawn fallback: out of bounds. cell={c}");
                continue;
            }

            // Hole 위 스폰 금지
            if (grid.GetMeta(c).IsHole)
            {
                Debug.LogWarning($"[StageLoader] Block spawn fallback: on Hole cell. cell={c}");
                continue;
            }

            // 점유 중이면 스폰 금지
            if (grid.GetOcc(c) != E_Occupant.None)
            {
                Debug.LogWarning($"[StageLoader] Block spawn fallback: occupied. cell={c} occ={grid.GetOcc(c)}");
                continue;
            }

            // 2D Sprite 블록
            var go = CreateSpriteObject(
                name: $"GapFillerBlock({c.x},{c.y})",
                parent: parent,
                localPosition: Vector3.zero,
                localScale: Vector3.one * 0.85f,
                sprite: GetWhiteSprite(),
                color: Color_Block);

            // 시각상 셀 위치로 이동(컨트롤러가 SnapToCell도 수행)
            go.transform.position = presenter.CellToWorld(c);
            go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, -0.01f);

            EnsureRewindKey(go);

            var ctrl = go.AddComponent<GapFillerBlockController>();
            ctrl.Initialize(grid, presenter, gapRegistry, c);
        }
    }

    private GameObject SpawnVisual(GameObject prefab, Sprite sprite, string name, Transform parent, Color fallbackColor)
    {
        GameObject go;

        if (prefab != null)
        {
            go = UnityEngine.Object.Instantiate(prefab, parent, true);
            go.name = name;
            return go;
        }

        Debug.LogWarning($"[StageLoader] {name} prefab missing. Create 2D sprite (fallback).");

        go = new GameObject(name);
        go.transform.SetParent(parent, true);

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : GetWhiteSprite();
        sr.color = fallbackColor;

        // 기본 크기(프로필 프리팹 없을 때만)
        go.transform.localScale = Vector3.one * 0.85f;

        return go;
    }

    private void EnsureRewindKey(GameObject go)
    {
        if (go == null) return;

        if (go.GetComponent<RewindKey>() == null)
            go.AddComponent<RewindKey>();
    }

    private Vector3 ToWorld(GameFlowContext ctx, Vector3 localInRoot)
    {
        // StageRuntime root 기준 로컬 좌표를 월드로 변환
        if (ctx?._stageRuntime?._root == null) return localInRoot;
        return ctx._stageRuntime._root.transform.TransformPoint(localInRoot);
    }

    private static GameObject CreateSpriteObject(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Sprite sprite,
        Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : GetWhiteSprite();
        sr.color = color;

        return go;
    }

    private static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;

        _whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _whiteTex.SetPixel(0, 0, Color.white);
        _whiteTex.Apply();

        _whiteSprite = Sprite.Create(_whiteTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        _whiteSprite.name = "RuntimeWhiteSprite";
        return _whiteSprite;
    }
}
