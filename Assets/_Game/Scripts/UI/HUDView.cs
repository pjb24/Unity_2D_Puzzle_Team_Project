// HUDView.cs
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class HUDView : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtTurn;
    [SerializeField] private TMP_Text _txtDifficulty;
    [SerializeField] private TMP_Text _txtRewind;
    [SerializeField] private TMP_Text _txtStage;

    public void Refresh(int turnIndex, string difficulty, int rewindRemaining, int chapterIndex1Based, int stageIndex1Based)
    {
        if (_txtTurn == null) Debug.LogWarning("[HUDView] _txtTurn is null (fallback).");
        if (_txtDifficulty == null) Debug.LogWarning("[HUDView] _txtDifficulty is null (fallback).");
        if (_txtRewind == null) Debug.LogWarning("[HUDView] _txtRewind is null (fallback).");
        if (_txtStage == null) Debug.LogWarning("[HUDView] _txtStage is null (fallback).");

        if (_txtTurn != null) _txtTurn.text = $"Turn: {turnIndex}";
        if (_txtDifficulty != null) _txtDifficulty.text = $"Difficulty: {difficulty}";
        if (_txtRewind != null) _txtRewind.text = $"Rewind: {rewindRemaining}";
        if (_txtStage != null) _txtStage.text = $"Stage: {chapterIndex1Based}-{stageIndex1Based}";
    }
}
