// DummyStageLoader.cs
using System;
using UnityEngine;

public class DummyStageLoader : IStageLoader
{
    // ===== Tunables =====
    private readonly float _tileSize = 1.0f;

    // Registry GameObject name (under StageRuntime root)
    private const string InteractRegistryName = "InteractRegistry";

    public void LoadStage(GameFlowContext ctx, Action onComplete)
    {
        if (ctx == null)
        {
            Debug.LogWarning("[StageLoader] LoadStage fallback: ctx is null.");
            return;
        }

        // 1) 이전 스테이지 정리
        UnloadStage(ctx);

        // 2) StageDefinition 가져오기
        var stageDef = ctx._config.GetStageDefinition(ctx._chapterIndex, ctx._stageIndex);
        if (stageDef == null)
        {
            Debug.LogError("[StageLoader] StageDefinition is null");
            return;
        }

        Debug.Log("[StageLoader] Chapter: " + ctx._chapterIndex + " , Stage: " + ctx._stageIndex);

        // 3) 런타임 refs 생성
        ctx._stageRuntime = new StageRuntimeRefs();
        ctx._stageRuntime._root = new GameObject($"[StageRuntime] C{ctx._chapterIndex}_S{ctx._stageIndex}");
        ctx._stageDefinition = stageDef;

        // 3.1) InteractRegistry 생성(루트 하위)
        var registry = EnsureInteractRegistry(ctx);

        // 4) 더미 보드 생성
        CreateDummyBoard(ctx, stageDef);

        // 5) 더미 경로 생성
        CreateDummyPath(ctx, stageDef);

        // 6) 스폰(그리드/프리젠터/컨트롤러 포함)
        SpawnDummyCharacters(ctx);

        // 6.1) Father에 InteractPort 주입
        BindFatherInteractPort(ctx, registry);

        // 6.2) 씬에 존재하는 다수 Interactable 등록(프리팹 배치 + 런타임 생성 모두 커버)
        // - 런타임 생성 Interactable이 Initialize에서 Register를 안 해도 여기서 잡힘
        if (registry != null)
        {
            registry.RebuildFromScene();
        }
        else
        {
            Debug.LogWarning("[StageLoader] RegisterInteractables fallback: registry is null.");
        }

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

    // ===== (3) Father에 포트 주입 =====
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

        // FatherController에 아래 API가 있어야 함:
        fatherCtrl.BindInteractPort(new InteractPort_Registry(registry));
    }

    private void CreateDummyBoard(GameFlowContext ctx, StageDefinition stageDef)
    {
        if (ctx == null || ctx._stageRuntime == null || ctx._stageRuntime._root == null)
        {
            Debug.LogWarning("[StageLoader] CreateDummyBoard fallback: runtime/root is null.");
            return;
        }

        int w = Mathf.Max(1, stageDef.BoardSize.x);
        int h = Mathf.Max(1, stageDef.BoardSize.y);

        var tilesRoot = new GameObject("[Tiles]");
        tilesRoot.transform.SetParent(ctx._stageRuntime._root.transform, false);

        // 원점 기준 중앙정렬
        Vector3 origin = new Vector3(-(w - 1) * 0.5f * _tileSize, -(h - 1) * 0.5f * _tileSize, 0f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"Tile_{x}_{y}";
                tile.transform.SetParent(tilesRoot.transform, false);
                tile.transform.localScale = new Vector3(_tileSize, _tileSize, 0.1f);
                tile.transform.localPosition = origin + new Vector3(x * _tileSize, y * _tileSize, 0f);

                // 충돌이 필요없으면 제거 가능(지금은 바닥으로 사용 가능)
                ctx._stageRuntime._tiles.Add(tile.transform);
            }
        }
    }

    private void CreateDummyPath(GameFlowContext ctx, StageDefinition stageDef)
    {
        if (ctx == null || ctx._stageRuntime == null || ctx._stageRuntime._root == null)
        {
            Debug.LogWarning("[StageLoader] CreateDummyPath fallback: runtime/root is null.");
            return;
        }

        int w = Mathf.Max(1, stageDef.BoardSize.x);
        int h = Mathf.Max(1, stageDef.BoardSize.y);

        var indices = PerimeterPathBuilder.Build(w, h);
        if (indices == null || indices.Count <= 0)
        {
            Debug.LogWarning("[StageLoader] CreateDummyPath fallback: perimeter indices is null/empty.");
            return;
        }

        ctx._stageRuntime._pathPoints.Clear();

        for (int i = 0; i < indices.Count; i++)
        {
            int idx = indices[i];
            int x = idx % w;
            int y = idx / w;

            Vector3 pLocal = GetTileCenterLocal(stageDef, x, y);
            ctx._stageRuntime._pathPoints.Add(ToWorld(ctx, pLocal));
        }

        // 디버그용 path marker 생성(선택)
        var pathRoot = new GameObject("[Path]");
        pathRoot.transform.SetParent(ctx._stageRuntime._root.transform, false);

        for (int i = 0; i < ctx._stageRuntime._pathPoints.Count; i++)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"Path_{i}";
            marker.transform.SetParent(pathRoot.transform, false);
            marker.transform.position = ctx._stageRuntime._pathPoints[i] + Vector3.up * 0.2f;
            marker.transform.localScale = Vector3.one * 0.2f;

            // 물리 필요 없으면 콜라이더 제거
            var col = marker.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);
        }
    }

    private void SpawnDummyCharacters(GameFlowContext ctx)
    {
        if (ctx == null || ctx._stageRuntime == null || ctx._stageRuntime._root == null)
        {
            Debug.LogWarning("[StageLoader] SpawnDummyCharacters fallback: runtime/root is null.");
            return;
        }

        var stageDef = ctx._stageDefinition;
        if (stageDef == null)
        {
            Debug.LogWarning("[StageLoader] SpawnDummyCharacters fallback: stageDef is null.");
            return;
        }

        // Father
        ctx._stageRuntime._father = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ctx._stageRuntime._father.name = "Father(Dummy)";
        ctx._stageRuntime._father.transform.SetParent(ctx._stageRuntime._root.transform, true);

        // Child
        ctx._stageRuntime._child = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ctx._stageRuntime._child.name = "Child(Dummy)";
        ctx._stageRuntime._child.transform.SetParent(ctx._stageRuntime._root.transform, true);

        // Grid 생성(Cells 배열 기반)
        int w = Mathf.Max(1, stageDef.BoardSize.x);
        int h = Mathf.Max(1, stageDef.BoardSize.y);
        ctx._stageRuntime._grid = new BoardGrid(w, h, stageDef.Cells);
        ctx._stageRuntime._gridPresenter = new GridPresenter(ctx._stageRuntime._root.transform, w, h, _tileSize);

        // FatherController 부착 + 초기화
        ctx._stageRuntime._fatherController = ctx._stageRuntime._father.GetComponent<FatherController>();
        ctx._stageRuntime._father.AddComponent<RewindKey>();
        if (ctx._stageRuntime._fatherController == null)
            ctx._stageRuntime._fatherController = ctx._stageRuntime._father.AddComponent<FatherController>();
        
        var fatherCtrl = ctx._stageRuntime._fatherController;
        fatherCtrl.Initialize(ctx._stageRuntime._grid, ctx._stageRuntime._gridPresenter, stageDef.FatherSpawn._cell);

        // ChildPathRuntime 생성
        var pathRuntime = new ChildPathRuntime(ctx._stageRuntime._grid, ctx._stageRuntime._gridPresenter);

        // blocked steps: StageDefinition에 추가한 BlockedPathSteps 사용
        var blocked = stageDef.BlockedPathSteps; // IReadOnlyList<int>

        // ChildController 부착 + 초기화
        ctx._stageRuntime._childController = ctx._stageRuntime._child.GetComponent<ChildController>();
        ctx._stageRuntime._child.AddComponent<RewindKey>();
        if (ctx._stageRuntime._childController == null)
            ctx._stageRuntime._childController = ctx._stageRuntime._child.AddComponent<ChildController>();

        var childCtrl = ctx._stageRuntime._childController;
        childCtrl.Initialize(pathRuntime, blocked, startPos: 0);
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
}
