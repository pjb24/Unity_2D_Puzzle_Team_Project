// RewindKey.cs
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class RewindKey : MonoBehaviour
{
    [SerializeField] private string _guidRaw;

    private Guid _guidCache;
    private bool _initialized;

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

    private void EnsureGuid()
    {
        if (_initialized)
            return;

        if (string.IsNullOrEmpty(_guidRaw))
        {
            _guidCache = System.Guid.NewGuid();
            _guidRaw = _guidCache.ToString("N");
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        else
        {
            _guidCache = System.Guid.Parse(_guidRaw);
        }

        _initialized = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(_guidRaw))
        {
            _guidRaw = System.Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}
