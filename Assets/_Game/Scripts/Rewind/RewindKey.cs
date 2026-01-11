// RewindKey.cs
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class RewindKey : MonoBehaviour
{
    private Guid _guidCache;
    private bool _initialized;

    private void Awake()
    {
        EnsureGuid();
    }

    public Guid Guid
    {
        get
        {
            EnsureGuid();
            return _guidCache;
        }
    }

    public string GuidString
    {
        get
        {
            EnsureGuid();
            return _guidCache.ToString("N");
        }
    }

    public bool TrySetGuidString(string raw, bool overwrite)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            Debug.LogWarning("[RewindKey] TrySetGuidString fallback: raw is empty.");
            return false;
        }

        raw = raw.Trim();

        if (!TryParseGuid(raw, out Guid g))
        {
            Debug.LogWarning($"[RewindKey] TrySetGuidString fallback: invalid guid. raw={raw}");
            return false;
        }

        return TrySetGuid(g, overwrite);
    }

    public bool TrySetGuid(Guid guid, bool overwrite)
    {
        if (guid == Guid.Empty)
        {
            Debug.LogWarning("[RewindKey] TrySetGuid fallback: guid is empty.");
            return false;
        }

        if (_initialized && !overwrite)
        {
            Debug.LogWarning($"[RewindKey] TrySetGuid fallback: already initialized. current={_guidCache:N}");
            return false;
        }

        _guidCache = guid;
        _initialized = true;
        return true;
    }

    private void EnsureGuid()
    {
        if (_initialized)
            return;

        _guidCache = System.Guid.NewGuid();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        _initialized = true;
    }

    private static bool TryParseGuid(string raw, out Guid guid)
    {
        guid = Guid.Empty;

        if (Guid.TryParseExact(raw, "N", out guid)) return true;
        if (Guid.TryParseExact(raw, "D", out guid)) return true;

        return Guid.TryParse(raw, out guid);
    }
}
