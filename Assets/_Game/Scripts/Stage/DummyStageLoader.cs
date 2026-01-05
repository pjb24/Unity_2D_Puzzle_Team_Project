// DummyStageLoader.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public interface IStageGimmickInitializable
{
    void InitializeGimmick(StageRuntimeRefs refs, BoardGrid grid, GridPresenter presenter, InteractRegistry registry);
}

public interface ILinkBinder
{
    void BindAllLinks(StageRuntimeRefs refs);
}

public class DummyStageLoader : IStageLoader
{
    // ===== Tunables =====
    private readonly float _tileSize = 1.0f;

    // Registry GameObject name (under StageRuntime root)
    private const string InteractRegistryName = "InteractRegistry";
    private const string GapFillerRegistryName = "GapFillerBlockRegistry";
    private const string HolesRootName = "[Holes]";

    // Sorting order (2D)
    private const int Sorting_Tile = 0;
    private const int Sorting_Hole = 1;
    private const int Sorting_Path = 2;
    private const int Sorting_Block = 3;
    private const int Sorting_Character = 4;

    // Colors (구분 가능)
    private static readonly Color Color_Floor = new(0.85f, 0.85f, 0.85f, 1f);
    private static readonly Color Color_Wall = new(0.15f, 0.15f, 0.15f, 1f);
    private static readonly Color Color_Obstacle = new(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color Color_Goal = new(0.25f, 1.00f, 0.25f, 1f);
    private static readonly Color Color_Path = new(1.00f, 0.80f, 0.10f, 1f);
    private static readonly Color Color_Father = new(0.10f, 0.80f, 1.00f, 1f);
    private static readonly Color Color_Child = new(1.00f, 0.20f, 1.00f, 1f);
    private static readonly Color Color_Block = new(0.70f, 0.40f, 0.10f, 1f);
    private static readonly Color Color_Hole = new(0.05f, 0.05f, 0.05f, 1f);

    private static Sprite _whiteSprite;
    private static Texture2D _whiteTex;

    public void LoadStage(GameFlowContext ctx, Action onComplete)
    {
        if (!ValidateLoadContext(ctx)) return;

        // 1) 이전 스테이지 정리
        UnloadStage(ctx);

        // 2) StageDefinition 가져오기
        var stageDef = GetStageDefinitionOrFail(ctx);
        if (stageDef == null) return;

        Debug.Log("[StageLoader] Chapter: " + ctx._chapterIndex + " , Stage: " + ctx._stageIndex);

        // 3) 런타임 refs 생성
        CreateStageRuntime(ctx, stageDef);

        // 3.1) InteractRegistry 생성(루트 하위)
        var registry = EnsureInteractRegistry(ctx);
        ctx._stageRuntime._interactRegistry = registry;

        // 5) 테두리 경로 생성(2D Sprite 마커)
        CreateDummyPath(ctx, stageDef);

        // 6) 스폰(그리드/프리젠터/컨트롤러 포함)
        SpawnStageActorsAndSystems(ctx, stageDef, registry);

        // 런타임 기믹 스폰(Doors + ToggleSwitches)
        StageGimmickSpawner.SpawnDoorsAndToggleSwitches(ctx._stageRuntime, stageDef);

        // ---- (0-5) 기믹 초기화 파이프라인 ----
        RunPostSpawnPipeline(ctx, registry);

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

        if (ctx._stageRuntime != null)
        {
            if (ctx._stageRuntime._root != null)
                UnityEngine.Object.Destroy(ctx._stageRuntime._root);

            ctx._stageRuntime = null;
        }
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

        // 정책: 스테이지마다 새로 시작해야 하므로 기존 스냅샷은 제거 후 0번 저장
        snapshot.ClearAll();
        snapshot.Capture(turnIndex: 0);

        Debug.Log("[StageLoader] Captured stage-created snapshot (turnIndex=0).");
    }

    private bool ValidateLoadContext(GameFlowContext ctx)
    {
        if (ctx == null)
        {
            Debug.LogWarning("[StageLoader] LoadStage fallback: ctx is null.");
            return false;
        }

        if (ctx._config == null)
        {
            Debug.LogError("[StageLoader] LoadStage failed: ctx._config is null.");
            return false;
        }

        return true;
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

    private void CreateStageRuntime(GameFlowContext ctx, StageDefinition stageDef)
    {
        ctx._stageRuntime = new StageRuntimeRefs();
        ctx._stageRuntime._root = new GameObject($"[StageRuntime] C{ctx._chapterIndex}_S{ctx._stageIndex}");
        ctx._stageDefinition = stageDef;
    }

    // ===== (6) 캐릭터 생성 + Initialize + GapFiller =====
    private void SpawnStageActorsAndSystems(GameFlowContext ctx, StageDefinition stageDef, InteractRegistry registry)
    {
        var profile = ctx._chapterVisualProfile;
        if (profile == null)
            Debug.LogWarning("[StageLoader] ChapterVisualProfile is null. Use sprite fallback visuals.");

        // 6) Father/Child 생성 + 초기화
        SpawnActorVisuals(ctx, profile);
        // ===== (4) 보드 생성 (2D Sprite) =====
        BuildGridAndTiles(ctx, stageDef);
        InitializeControllers(ctx, stageDef);

        // 6.1) Father에 InteractPort 주입
        BindFatherInteractPort(ctx, registry);

        // Hole 적용 + 메움 블록 스폰/바인딩
        ApplyHolesFromStageDef(ctx, stageDef);
        EnsureHoleVisualLayer(ctx); // Hole 변화(메움/복원)도 화면에 반영

        var gapRegistry = EnsureGapFillerRegistry(ctx);
        SpawnGapFillerBlocks(ctx, stageDef, gapRegistry);
        BindGapFillerToFather(ctx, gapRegistry);
    }

    private void RunPostSpawnPipeline(GameFlowContext ctx, InteractRegistry registry)
    {
        // 1) BoardStateRewindable 추가(보드 자체 변화 복원)
        EnsureBoardStateRewindable(ctx, registry);
        // 2) 보드/프리젠터 생성 후 기믹 Initialize
        InitializeGimmicks(ctx, registry);

        // 3) 씬에 존재하는 다수 Interactable 등록(프리팹 배치 + 런타임 생성 모두 커버)
        // - 런타임 생성 Interactable이 Initialize에서 Register를 안 해도 여기서 잡힘
        if (registry != null) registry.RebuildFromScene();
        else Debug.LogWarning("[StageLoader] Registry rebuild skipped (fallback): registry is null.");

        // 4) 링크(스위치→문 등) 바인딩
        BindLinks(ctx);
        // 5) TurnSystem(ITurnTickable) 수집
        CollectTurnSystems(ctx);
    }

    private void SpawnActorVisuals(GameFlowContext ctx, ChapterVisualProfile profile)
    {
        // Father
        ctx._stageRuntime._father = SpawnVisual(
            prefab: profile != null ? profile.FatherPrefab : null,
            sprite: profile != null ? profile.FatherSprite : null,
            name: "Father(Dummy)",
            parent: ctx._stageRuntime._root.transform,
            fallbackColor: Color_Father);

        // Child
        ctx._stageRuntime._child = SpawnVisual(
            prefab: profile != null ? profile.ChildPrefab : null,
            sprite: profile != null ? profile.ChildSprite : null,
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
        ctx._stageRuntime._gridPresenter = new GridPresenter(ctx._stageRuntime._root.transform, w, h, _tileSize);

        // ===== 타일 생성 =====
        ctx._stageRuntime._tilesRoot = ctx._stageRuntime._gridPresenter.BuildTiles(ctx._stageRuntime._grid, ctx._stageRuntime._tiles);
        if (ctx._stageRuntime._tilesRoot == null)
            Debug.LogWarning("[StageLoader] Tiles build skipped (fallback): tilesRoot is null.");
    }

    private void InitializeControllers(GameFlowContext ctx, StageDefinition stageDef)
    {
        // FatherController 부착 + 초기화
        ctx._stageRuntime._fatherController = EnsureController<FatherController>(ctx._stageRuntime._father);
        EnsureRewindKey(ctx._stageRuntime._father);

        ctx._stageRuntime._fatherController.Initialize(
            ctx._stageRuntime._grid,
            ctx._stageRuntime._gridPresenter,
            stageDef.FatherSpawn._cell);

        // ChildController 부착 + 초기화
        ctx._stageRuntime._childController = EnsureController<ChildController>(ctx._stageRuntime._child);
        EnsureRewindKey(ctx._stageRuntime._child);

        // ChildPathRuntime 생성
        var pathRuntime = new ChildPathRuntime(ctx._stageRuntime._grid, ctx._stageRuntime._gridPresenter);

        // 런타임 블로커 레지스트리 생성(초기값 = StageDefinition.BlockedPathSteps)
        ctx._stageRuntime._childPathBlockers = new ChildPathBlockerRegistry(stageDef.BlockedPathSteps, pathRuntime.Count);

        // ChildController는 Registry 버전 Initialize 사용
        ctx._stageRuntime._childController.Initialize(pathRuntime, ctx._stageRuntime._childPathBlockers, startPos: 0);
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

    private void EnsureBoardStateRewindable(GameFlowContext ctx, InteractRegistry registry)
    {
        if (ctx?._stageRuntime?._root == null)
        {
            Debug.LogWarning("[StageLoader] EnsureBoardStateRewindable fallback: stage root is null.");
            return;
        }

        var root = ctx._stageRuntime._root;

        var bsr = root.GetComponent<BoardStateRewindable>();
        if (bsr == null) bsr = root.AddComponent<BoardStateRewindable>();

        bsr.Initialize(ctx._stageRuntime._grid, registry);

        // RewindKey 보장
        EnsureRewindKey(root);

        ctx._stageRuntime._boardStateRewindable = bsr;
    }

    private void InitializeGimmicks(GameFlowContext ctx, InteractRegistry registry)
    {
        if (ctx?._stageRuntime?._root == null) return;

        var stageRoot = ctx._stageRuntime._root;
        var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is not IStageGimmickInitializable init) continue;

            // 같은 씬 + (스테이지 루트 하위거나, 스테이지 씬 오브젝트)
            if (behaviours[i].gameObject.scene != stageRoot.scene) continue;

            try
            {
                init.InitializeGimmick(ctx._stageRuntime, ctx._stageRuntime._grid, ctx._stageRuntime._gridPresenter, registry);
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

        var stageRoot = ctx._stageRuntime._root;
        var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is not ILinkBinder binder) continue;
            if (behaviours[i].gameObject.scene != stageRoot.scene) continue;

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

        var stageRoot = ctx._stageRuntime._root;
        var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is not ITurnTickable sys) continue;
            if (behaviours[i].gameObject.scene != stageRoot.scene) continue;

            ctx._stageRuntime._turnSystems.Add(sys);
        }
    }

    // ===== (3) InteractRegistry 생성 =====
    private InteractRegistry EnsureInteractRegistry(GameFlowContext ctx)
    {
        if (ctx == null || ctx._stageRuntime == null || ctx._stageRuntime._root == null)
        {
            Debug.LogWarning("[StageLoader] EnsureInteractRegistry fallback: runtime/root is null.");
            return null;
        }

        var root = ctx._stageRuntime._root.transform;

        // 이미 있으면 재사용
        var existing = root.GetComponentInChildren<InteractRegistry>(includeInactive: true);
        if (existing != null)
            return existing;

        var go = new GameObject(InteractRegistryName);
        go.transform.SetParent(root, false);
        return go.AddComponent<InteractRegistry>();
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
    private void CreateDummyPath(GameFlowContext ctx, StageDefinition stageDef)
    {
        if (ctx == null || ctx._stageRuntime == null || ctx._stageRuntime._root == null)
        {
            Debug.LogWarning("[StageLoader] CreateDummyPath fallback: runtime/root is null.");
            return;
        }

        int w = Mathf.Max(1, stageDef.BoardSize.x);
        int h = Mathf.Max(1, stageDef.BoardSize.y);

        var pathRoot = new GameObject("[Path]");
        pathRoot.transform.SetParent(ctx._stageRuntime._root.transform, false);
        ctx._stageRuntime._pathRoot = pathRoot.transform;

        // PathFadeFx 부착(프로토타입 대체 연출)
        if (pathRoot.GetComponent<PathFadeFx>() == null)
            pathRoot.AddComponent<PathFadeFx>();

        ctx._stageRuntime._pathPoints.Clear();

        // 테두리(오른쪽→위→왼쪽→아래) 셀 경로
        void AddCell(int x, int y)
        {
            Vector3 local = GetTileCenterLocal(stageDef, x, y);
            ctx._stageRuntime._pathPoints.Add(ToWorld(ctx, local));

            var marker = CreateSpriteObject(
                name: $"Path_{x}_{y}",
                parent: pathRoot.transform,
                localPosition: local,
                localScale: Vector3.one * 0.22f,
                sprite: GetWhiteSprite(),
                color: Color_Path,
                sortingOrder: Sorting_Path);

            // 경로 마커는 타일 위에 올라오게(시각적)
            marker.transform.localPosition = new Vector3(marker.transform.localPosition.x, marker.transform.localPosition.y, -0.05f);
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
            grid.SetMeta(c, meta, notify: true);
        }
    }

    private void EnsureHoleVisualLayer(GameFlowContext ctx)
    {
        if (ctx?._stageRuntime?._root == null || ctx._stageRuntime._grid == null || ctx._stageRuntime._gridPresenter == null)
        {
            Debug.LogWarning("[StageLoader] EnsureHoleVisualLayer fallback: root/grid/presenter is null.");
            return;
        }

        var root = ctx._stageRuntime._root.transform;
        var existing = root.Find(HolesRootName);
        if (existing != null) return; // 중복 생성 방지

        var grid = ctx._stageRuntime._grid;
        var presenter = ctx._stageRuntime._gridPresenter;

        var holesRoot = new GameObject(HolesRootName);
        holesRoot.transform.SetParent(root, false);

        var map = BuildHoleRenderersMap(holesRoot.transform, grid, presenter);
        SubscribeHoleVisualUpdates(grid, map);
        RefreshHoleVisualsAll(grid, map);
    }

    private Dictionary<int, SpriteRenderer> BuildHoleRenderersMap(Transform holesRoot, BoardGrid grid, GridPresenter presenter)
    {
        int w = grid._w;
        int h = grid._h;

        // 인덱스로 빠르게 접근 (딕셔너리지만 셀 수가 작아서 충분)
        var map = new Dictionary<int, SpriteRenderer>(w * h);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                var cell = new Vector2Int(x, y);

                Vector3 world = presenter.CellToWorld(cell);

                var go = new GameObject($"Hole_{x}_{y}");
                go.transform.SetParent(holesRoot, false);
                go.transform.position = new Vector3(world.x, world.y, -0.02f);
                go.transform.localScale = Vector3.one * 0.92f;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetWhiteSprite();
                sr.color = Color_Hole;
                sr.sortingOrder = Sorting_Hole;
                // 초기 비활성(아래에서 Refresh)
                sr.enabled = false;

                map[idx] = sr;
            }
        }

        return map;
    }

    private void SubscribeHoleVisualUpdates(BoardGrid grid, Dictionary<int, SpriteRenderer> map)
    {
        int w = grid._w;

        // meta 변경에 따라 표시 갱신
        grid.AddListenerOnMetaChanged((cell, meta) =>
        {
            int idx = cell.y * w + cell.x;
            if (!map.TryGetValue(idx, out var sr)) return;
            sr.enabled = meta.IsHole;
        });
    }

    private void RefreshHoleVisualsAll(BoardGrid grid, Dictionary<int, SpriteRenderer> map)
    {
        int w = grid._w;
        int h = grid._h;

        // 초기 상태 반영
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var cell = new Vector2Int(x, y);
                int idx = y * w + x;

                if (!map.TryGetValue(idx, out var sr)) continue;
                sr.enabled = grid.GetMeta(cell).IsHole;
            }
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
        var parent = ctx._stageRuntime._root.transform;

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
                color: Color_Block,
                sortingOrder: Sorting_Block);

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
        sr.sortingOrder = Sorting_Character;

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

    // ===== (6.1) Father InteractPort 주입 =====
    private void BindFatherInteractPort(GameFlowContext ctx, InteractRegistry registry)
    {
        if (ctx == null || ctx._stageRuntime == null)
        {
            Debug.LogWarning("[StageLoader] BindFatherInteractPort fallback: runtime is null.");
            return;
        }

        var fatherCtrl = ctx._stageRuntime._fatherController;
        if (fatherCtrl == null)
        {
            Debug.LogWarning("[StageLoader] BindFatherInteractPort fallback: fatherController is null.");
            return;
        }

        if (registry == null)
        {
            Debug.LogWarning("[StageLoader] BindFatherInteractPort fallback: registry is null. Interact will not work.");
            return;
        }

        fatherCtrl.BindInteractPort(new InteractPort_Registry(registry));
    }

    private Vector3 GetTileCenterLocal(StageDefinition stageDef, int x, int y)
    {
        int w = Mathf.Max(1, stageDef.BoardSize.x);
        int h = Mathf.Max(1, stageDef.BoardSize.y);

        Vector3 origin = new Vector3(-(w - 1) * 0.5f * _tileSize, -(h - 1) * 0.5f * _tileSize, 0f);
        return origin + new Vector3(x * _tileSize, y * _tileSize, 0f);
    }

    private Vector3 ToWorld(GameFlowContext ctx, Vector3 localInRoot)
    {
        // StageRuntime root 기준 로컬 좌표를 월드로 변환
        return ctx._stageRuntime._root.transform.TransformPoint(localInRoot);
    }

    private static GameObject CreateSpriteObject(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Sprite sprite,
        Color color,
        int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : GetWhiteSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;

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
