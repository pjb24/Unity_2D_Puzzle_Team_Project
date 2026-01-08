// AudioLibraryVerifier.cs
// 플레이모드에서 누락/클립 null을 Warning으로 강제 노출

using System;
using UnityEngine;

[DisallowMultipleComponent]
public class AudioLibraryVerifier : MonoBehaviour
{
    [SerializeField] private bool _runOnStart = true;

    private void Start()
    {
        if (_runOnStart)
            ValidateAndLog();
    }

    [ContextMenu("Validate Audio Libraries")]
    public void ValidateAndLog()
    {
        var hub = AudioHub.Ensure();

        ValidateBgmLibrary();
        ValidateSfxLibrary();
    }

    private void ValidateBgmLibrary()
    {
        var bgmLib = Resources.Load<BgmLibrary>("Configs/BgmLibrary");
        if (bgmLib == null)
        {
            Debug.LogWarning("[AudioLibraryVerifier] BgmLibrary not found at Resources/Configs/BgmLibrary.asset (fallback).");
            return;
        }

        int missing = 0;
        int nullClip = 0;

        foreach (E_BgmId id in Enum.GetValues(typeof(E_BgmId)))
        {
            if (id == E_BgmId.None) continue;

            if (!bgmLib.TryGet(id, out var e))
            {
                Debug.LogWarning($"[AudioLibraryVerifier] Missing BGM entry. id={id}");
                missing++;
                continue;
            }

            if (e.Clip == null)
            {
                Debug.LogWarning($"[AudioLibraryVerifier] BGM clip is null. id={id}");
                nullClip++;
            }
        }

        if (missing == 0 && nullClip == 0)
            Debug.Log("[AudioLibraryVerifier] PASS: BgmLibrary complete.");
        else
            Debug.LogWarning($"[AudioLibraryVerifier] BgmLibrary issues. missing={missing}, nullClip={nullClip}");
    }

    private void ValidateSfxLibrary()
    {
        var sfxLib = Resources.Load<SfxLibrary>("Configs/SfxLibrary");
        if (sfxLib == null)
        {
            Debug.LogWarning("[AudioLibraryVerifier] SfxLibrary not found at Resources/Configs/SfxLibrary.asset (fallback).");
            return;
        }

        int missing = 0;
        int nullClip = 0;

        foreach (E_SfxId id in Enum.GetValues(typeof(E_SfxId)))
        {
            if (id == E_SfxId.None) continue;

            if (!sfxLib.TryGet(id, out var e))
            {
                Debug.LogWarning($"[AudioLibraryVerifier] Missing SFX entry. id={id}");
                missing++;
                continue;
            }

            if (e.Clip == null)
            {
                Debug.LogWarning($"[AudioLibraryVerifier] SFX clip is null. id={id}");
                nullClip++;
            }
        }

        if (missing == 0 && nullClip == 0)
            Debug.Log("[AudioLibraryVerifier] PASS: SfxLibrary complete.");
        else
            Debug.LogWarning($"[AudioLibraryVerifier] SfxLibrary issues. missing={missing}, nullClip={nullClip}");
    }
}
