// StageGimmickSpawner.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public static class StageGimmickSpawner
{
    private static Sprite _whiteSprite;
    private static Texture2D _whiteTex;

    public static void SpawnDoorsAndToggleSwitches(StageRuntimeRefs refs, StageDefinition stageDef)
    {
        if (refs == null || refs._root == null || stageDef == null)
        {
            Debug.LogWarning("[StageGimmickSpawner] Spawn fallback: refs/root/stageDef is null.");
            return;
        }

        if (refs._grid == null || refs._gridPresenter == null)
        {
            Debug.LogWarning("[StageGimmickSpawner] Spawn fallback: grid/presenter is null.");
            return;
        }

        var gimmicksRoot = new GameObject("[Gimmicks]").transform;
        gimmicksRoot.SetParent(refs._root.transform, false);

        SpawnDoors(refs, stageDef, gimmicksRoot);
        SpawnToggleSwitches(refs, stageDef, gimmicksRoot);
    }

    private static void SpawnDoors(StageRuntimeRefs refs, StageDefinition stageDef, Transform parent)
    {
        var doors = stageDef.DoorSpawns;
        if (doors == null || doors.Length == 0)
            return;

        var doorRoot = new GameObject("[Doors]").transform;
        doorRoot.SetParent(parent, false);

        for (int i = 0; i < doors.Length; i++)
        {
            var d = doors[i];

            if (!refs._grid.IsInBounds(d._cell))
            {
                Debug.LogWarning($"[StageGimmickSpawner] Door spawn out-of-bounds. index={i} cell={d._cell}");
                continue;
            }

            var go = CreateSpriteGO($"Door_{i}", doorRoot, refs._gridPresenter.CellToWorld(d._cell), new Vector3(0.9f, 0.9f, 1f), new Color(0.15f, 0.60f, 0.95f, 1f), sortingOrder: 3);

            var key = go.AddComponent<RewindKey>();
            if (!string.IsNullOrWhiteSpace(d._guid))
            {
                if (!key.TrySetGuidString(d._guid, overwrite: true))
                    Debug.LogWarning($"[StageGimmickSpawner] Door guid set failed. index={i} raw={d._guid}");
            }
            else
            {
                Debug.LogWarning($"[StageGimmickSpawner] Door guid is empty. (ToggleSwitch link will fail) index={i}");
            }

            var door = go.AddComponent<DoorController>();

            int step = (d._anchor == E_DoorAnchor.ChildPathStep) ? d._pathStep : -1;

            door.Initialize(
                refs._grid,
                refs._gridPresenter,
                d._cell,
                d._startOpen,
                refs._childPathBlockers,
                step);
        }
    }

    private static void SpawnToggleSwitches(StageRuntimeRefs refs, StageDefinition stageDef, Transform parent)
    {
        var switches = stageDef.ToggleSwitchSpawns;
        if (switches == null || switches.Length == 0)
            return;

        var swRoot = new GameObject("[ToggleSwitches]").transform;
        swRoot.SetParent(parent, false);

        for (int i = 0; i < switches.Length; i++)
        {
            var s = switches[i];

            if (!refs._grid.IsInBounds(s._cell))
            {
                Debug.LogWarning($"[StageGimmickSpawner] ToggleSwitch spawn out-of-bounds. index={i} cell={s._cell}");
                continue;
            }

            var go = CreateSpriteGO($"ToggleSwitch_{i}", swRoot, refs._gridPresenter.CellToWorld(s._cell), new Vector3(0.75f, 0.75f, 1f), new Color(0.95f, 0.85f, 0.15f, 1f), sortingOrder: 3);

            go.AddComponent<RewindKey>();

            var sw = go.AddComponent<ToggleSwitchController>();
            sw.ConfigureRuntime(
                s._cell,
                s._mode,
                s._startOn,
                s._allowManualInteract,
                s._targetDoorGuids);
        }
    }

    private static GameObject CreateSpriteGO(string name, Transform parent, Vector3 worldPos, Vector3 scale, Color color, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = worldPos;
        go.transform.localScale = scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetWhiteSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        return go;
    }

    private static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;

        _whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _whiteTex.SetPixel(0, 0, Color.white);
        _whiteTex.Apply();

        _whiteSprite = Sprite.Create(_whiteTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _whiteSprite;
    }
}
