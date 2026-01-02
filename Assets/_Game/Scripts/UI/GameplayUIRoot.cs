// GameplayUIRoot.cs
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class GameplayUIRoot : MonoBehaviour
{
    [Header("Views")]
    [SerializeField] private HUDView _hud;
    [SerializeField] private InputLockOverlayView _inputLockOverlay;
    [SerializeField] private RewindPanelView _rewindPanel;
    [SerializeField] private ResultPopupView _resultPopup;

    private bool _isBound;

    private GameFlowContext _ctx;
    private TurnDriver _turnDriver;
    private RewindController _rewind;
    private DifficultyProfile _profile;

    public void Bind(GameFlowContext ctx, TurnDriver turnDriver, RewindController rewind, DifficultyProfile profile)
    {
        _ctx = ctx;
        _turnDriver = turnDriver;
        _rewind = rewind;
        _profile = profile;

        _isBound = true;

        if (_turnDriver == null)
            Debug.LogWarning("[GameplayUIRoot] Bind fallback: TurnDriver is null.");

        if (_ctx == null)
            Debug.LogWarning("[GameplayUIRoot] Bind fallback: GameFlowContext is null.");

        if (_rewindPanel != null)
            _rewindPanel.Bind(_rewind);
        else
            Debug.LogWarning("[GameplayUIRoot] RewindPanelView is null (fallback).");

        if (_turnDriver != null)
            _turnDriver.AddListenerOnResolved(OnTurnResolved);

        RefreshAll();
    }

    public void Unbind()
    {
        if (_turnDriver != null)
            _turnDriver.RemoveListenerOnResolved(OnTurnResolved);

        if (_rewindPanel != null)
            _rewindPanel.Unbind();

        _isBound = false;

        _ctx = null;
        _turnDriver = null;
        _rewind = null;
        _profile = null;
    }

    private void Update()
    {
        if (!_isBound) return;

        RefreshAll();
    }

    private void RefreshAll()
    {
        int turnIndex = _turnDriver != null ? _turnDriver.TurnIndex : 0;
        int rewindRemaining = _rewind != null ? _rewind.RewindRemaining : 0;

        int chapter = _ctx != null ? _ctx._chapterIndex + 1 : 0;
        int stage = _ctx != null ? _ctx._stageIndex + 1 : 0;

        string difficulty = _profile != null ? _profile._difficulty.ToString() : "-";

        if (_hud != null)
            _hud.Refresh(turnIndex, difficulty, rewindRemaining, chapter, stage);
        else
            Debug.LogWarning("[GameplayUIRoot] HUDView is null (fallback).");

        bool locked = false;
        if (_turnDriver != null && _turnDriver.IsInputLocked) locked = true;
        if (_rewind != null && _rewind.IsRewindActive) locked = true;

        if (_inputLockOverlay != null)
            _inputLockOverlay.Refresh(locked);
        else
            Debug.LogWarning("[GameplayUIRoot] InputLockOverlayView is null (fallback).");

        if (_rewindPanel != null)
            _rewindPanel.Refresh();

        // ResultPopupView는 자체 타이머로 Update 처리
    }

    private void OnTurnResolved(E_TurnResolveOutcome outcome, E_StageFailReason reason, int turnIndex)
    {
        if (_resultPopup == null)
        {
            Debug.LogWarning("[GameplayUIRoot] ResultPopupView is null (fallback).");
            return;
        }

        switch (outcome)
        {
            case E_TurnResolveOutcome.StageCleared:
                _resultPopup.Show("CLEAR", 1.0f);
                break;

            case E_TurnResolveOutcome.StageFailed_Rewind:
                _resultPopup.Show("FAILED → REWIND", 1.0f);
                break;

            case E_TurnResolveOutcome.StageFailed_Reset:
                _resultPopup.Show("FAILED", 1.0f);
                break;

            default:
                break;
        }
    }
}
