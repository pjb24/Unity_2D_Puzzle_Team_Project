// RewindPanelView.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class RewindPanelView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _btnEnter;
    [SerializeField] private Button _btnPrev;
    [SerializeField] private Button _btnNext;
    [SerializeField] private Button _btnCommit;
    [SerializeField] private Button _btnCancel;

    [Header("Texts")]
    [SerializeField] private TMP_Text _txtRemaining;

    private RewindController _rewind;

    public void Bind(RewindController rewind)
    {
        _rewind = rewind;

        if (_rewind == null)
            Debug.LogWarning("[RewindPanelView] Bind fallback: RewindController is null.");

        WireButtons(true);
        Refresh();
    }

    public void Unbind()
    {
        WireButtons(false);
        _rewind = null;
    }

    public void Refresh()
    {
        if (_txtRemaining == null)
            Debug.LogWarning("[RewindPanelView] _txtRemaining is null (fallback).");

        bool active = _rewind != null && _rewind.IsRewindActive;
        int remaining = _rewind != null ? _rewind.RewindRemaining : 0;

        if (_txtRemaining != null)
            _txtRemaining.text = $"Remaining: {remaining}";

        // 버튼 정책
        SetInteractable(_btnEnter, !active && remaining > 0);
        SetInteractable(_btnPrev, active);
        SetInteractable(_btnNext, active);
        SetInteractable(_btnCommit, active);
        SetInteractable(_btnCancel, active);
    }

    private void WireButtons(bool bind)
    {
        if (_btnEnter == null || _btnPrev == null || _btnNext == null || _btnCommit == null || _btnCancel == null)
        {
            Debug.LogWarning("[RewindPanelView] Buttons are not fully assigned (fallback).");
            return;
        }

        if (bind)
        {
            _btnEnter.onClick.AddListener(OnEnterClicked);
            _btnPrev.onClick.AddListener(OnPrevClicked);
            _btnNext.onClick.AddListener(OnNextClicked);
            _btnCommit.onClick.AddListener(OnCommitClicked);
            _btnCancel.onClick.AddListener(OnCancelClicked);
        }
        else
        {
            _btnEnter.onClick.RemoveListener(OnEnterClicked);
            _btnPrev.onClick.RemoveListener(OnPrevClicked);
            _btnNext.onClick.RemoveListener(OnNextClicked);
            _btnCommit.onClick.RemoveListener(OnCommitClicked);
            _btnCancel.onClick.RemoveListener(OnCancelClicked);
        }
    }

    private void OnEnterClicked()
    {
        if (_rewind == null)
        {
            Debug.LogWarning("[RewindPanelView] Enter fallback: RewindController is null.");
            return;
        }

        _rewind.EnterRewind(E_RewindEnterSource.Player);
        Refresh();
    }

    private void OnPrevClicked()
    {
        if (_rewind == null)
        {
            Debug.LogWarning("[RewindPanelView] Prev fallback: RewindController is null.");
            return;
        }

        _rewind.RequestPrevTurn();
        Refresh();
    }

    private void OnNextClicked()
    {
        if (_rewind == null)
        {
            Debug.LogWarning("[RewindPanelView] Next fallback: RewindController is null.");
            return;
        }

        _rewind.RequestNextTurn();
        Refresh();
    }

    private void OnCommitClicked()
    {
        if (_rewind == null)
        {
            Debug.LogWarning("[RewindPanelView] Commit fallback: RewindController is null.");
            return;
        }

        _rewind.RequestCommit();
        Refresh();
    }

    private void OnCancelClicked()
    {
        if (_rewind == null)
        {
            Debug.LogWarning("[RewindPanelView] Cancel fallback: RewindController is null.");
            return;
        }

        _rewind.RequestCancel();
        Refresh();
    }

    private static void SetInteractable(Button btn, bool on)
    {
        if (btn != null) btn.interactable = on;
    }
}
