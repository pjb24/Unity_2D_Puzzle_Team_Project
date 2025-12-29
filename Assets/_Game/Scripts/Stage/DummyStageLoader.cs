using System;
using UnityEngine;

public class DummyStageLoader : IStageLoader
{
    // ===== Tunables =====
    private readonly float _tileSize = 1.0f;

    public void LoadStage(GameFlowContext ctx, Action onComplete)
    {
        if (ctx == null) return;

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

        // 4) 더미 보드 생성
        CreateDummyBoard(ctx, stageDef);

        // 5) 더미 경로 생성(간단히 좌→우→상→하 같은 식)
        CreateDummyPath(ctx, stageDef);

        // 6) 스폰
        SpawnDummyCharacters(ctx);

        onComplete?.Invoke();
    }

    public void UnloadStage(GameFlowContext ctx)
    {
        if (ctx == null) return;

        if (ctx._stageRuntime != null)
        {
            if (ctx._stageRuntime._root != null)
                UnityEngine.Object.Destroy(ctx._stageRuntime._root);

            ctx._stageRuntime = null;
        }
    }

    private void CreateDummyBoard(GameFlowContext ctx, StageDefinition stageDef)
    {
        int w = Mathf.Max(1, stageDef.BoardSize.x);
        int h = Mathf.Max(1, stageDef.BoardSize.y);

        var tilesRoot = new GameObject("[Tiles]");
        tilesRoot.transform.SetParent(ctx._stageRuntime._root.transform, false);

        // 원점 기준 중앙정렬
        Vector3 origin = new Vector3(-(w - 1) * 0.5f * _tileSize, 0f, -(h - 1) * 0.5f * _tileSize);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"Tile_{x}_{y}";
                tile.transform.SetParent(tilesRoot.transform, false);
                tile.transform.localScale = new Vector3(_tileSize, 0.1f, _tileSize);
                tile.transform.localPosition = origin + new Vector3(x * _tileSize, 0f, y * _tileSize);

                // 충돌이 필요없으면 제거 가능(지금은 바닥으로 사용 가능)
                ctx._stageRuntime._tiles.Add(tile.transform);
            }
        }
    }

    private void CreateDummyPath(GameFlowContext ctx, StageDefinition stageDef)
    {
        int w = Mathf.Max(1, stageDef.BoardSize.x);
        int h = Mathf.Max(1, stageDef.BoardSize.y);

        // 간단 경로: (0,0) -> (w-1,0) -> (w-1,h-1)
        Vector3 p0 = GetTileCenterLocal(stageDef, 0, 0);
        Vector3 p1 = GetTileCenterLocal(stageDef, w - 1, 0);
        Vector3 p2 = GetTileCenterLocal(stageDef, w - 1, h - 1);

        ctx._stageRuntime._pathPoints.Clear();
        ctx._stageRuntime._pathPoints.Add(ToWorld(ctx, p0));
        ctx._stageRuntime._pathPoints.Add(ToWorld(ctx, p1));
        ctx._stageRuntime._pathPoints.Add(ToWorld(ctx, p2));

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
        // Father
        ctx._stageRuntime._father = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ctx._stageRuntime._father.name = "Father(Dummy)";
        ctx._stageRuntime._father.transform.SetParent(ctx._stageRuntime._root.transform, true);

        // Child
        ctx._stageRuntime._child = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ctx._stageRuntime._child.name = "Child(Dummy)";
        ctx._stageRuntime._child.transform.SetParent(ctx._stageRuntime._root.transform, true);

        // 위치: 경로 첫 점, 두 번째 점에 배치
        Vector3 fatherPos = ctx._stageRuntime._pathPoints.Count > 0 ? ctx._stageRuntime._pathPoints[0] : Vector3.zero;
        Vector3 childPos = ctx._stageRuntime._pathPoints.Count > 1 ? ctx._stageRuntime._pathPoints[1] : (fatherPos + Vector3.right);

        ctx._stageRuntime._father.transform.position = fatherPos + Vector3.up * 0.9f;
        ctx._stageRuntime._child.transform.position = childPos + Vector3.up * 0.9f;
    }

    private Vector3 GetTileCenterLocal(StageDefinition stageDef, int x, int y)
    {
        int w = Mathf.Max(1, stageDef.BoardSize.x);
        int h = Mathf.Max(1, stageDef.BoardSize.y);

        Vector3 origin = new Vector3(-(w - 1) * 0.5f * _tileSize, 0f, -(h - 1) * 0.5f * _tileSize);
        return origin + new Vector3(x * _tileSize, 0f, y * _tileSize);
    }

    private Vector3 ToWorld(GameFlowContext ctx, Vector3 localInRoot)
    {
        // StageRuntime root 기준 로컬 좌표를 월드로 변환
        return ctx._stageRuntime._root.transform.TransformPoint(localInRoot);
    }
}
