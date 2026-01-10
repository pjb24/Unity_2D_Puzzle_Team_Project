// StageLoader.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class StageLoader
{
    // ===== Tunables =====
    private readonly float _defaultTileSize = 1.0f;
    private readonly float _defaultTileGap = 0.0f;

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
        ctx._stageRuntime = new StageRuntimeRefs();
        ctx._stageRuntime._root = new GameObject($"[StageRuntime] C{ctx._chapterIndex}_S{ctx._stageIndex}");
        var stageDef = ctx._stageDefinition;

        var config = ctx._config.LoadGameConfig();
        var prefabs = config.Prefabs;

        string stageId = stageDef != null ? stageDef.StageId : $"C{ctx._chapterIndex}_S{ctx._stageIndex}";
        ctx._stageRuntime._stageId = stageId;

        // ===== Layout resolve (tile scale + gap) =====
        float tileScale = _defaultTileSize;
        float tileGap = _defaultTileGap;

        ctx._stageRuntime._tileScale = tileScale;
        ctx._stageRuntime._tileGap = tileGap;
        ctx._stageRuntime._cellPitch = Mathf.Max(0.01f, tileScale + tileGap);
    }
}
