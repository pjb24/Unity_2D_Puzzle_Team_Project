// Proto2DVisual.cs
using UnityEngine;

public static class Proto2DVisual
{
    // 공용 1x1 스프라이트(런타임 생성)
    private static Sprite _sprite;

    public static Sprite Sprite
    {
        get
        {
            if (_sprite != null) return _sprite;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _sprite;
        }
    }

    // ===== 색상 규칙(구분용) =====
    public static readonly Color TileFloor = new Color(0.85f, 0.85f, 0.85f, 1f);
    public static readonly Color TileWall = new Color(0.20f, 0.20f, 0.20f, 1f);
    public static readonly Color TileObstacle = new Color(0.35f, 0.35f, 0.35f, 1f); // 추가
    public static readonly Color TileGoal = new Color(1.00f, 0.85f, 0.20f, 1f);
    public static readonly Color TileHole = new Color(0.05f, 0.05f, 0.05f, 1f);

    public static readonly Color PathNode = new Color(0.20f, 0.90f, 0.90f, 1f);

    public static readonly Color Father = new Color(0.25f, 0.55f, 1.00f, 1f);
    public static readonly Color Child = new Color(1.00f, 0.30f, 0.80f, 1f);

    public static readonly Color GapBlock = new Color(1.00f, 0.55f, 0.15f, 1f);

    public static GameObject CreateSpriteObject(string name, Transform parent, int sortingOrder, Color color, Vector3 localScale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = localScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        return go;
    }

    public static SpriteRenderer EnsureSpriteRenderer(GameObject go, int sortingOrder, Color color)
    {
        if (go == null) return null;

        // 3D 렌더러가 있으면 꺼서 2D만 보이게 한다.
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        var mf = go.GetComponent<MeshFilter>();
        if (mf != null) Object.Destroy(mf);

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();

        sr.sprite = Sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return sr;
    }
}

public enum E_ProtoSort
{
    Tile = 0,
    Path = 5,
    Actor = 10,
}
