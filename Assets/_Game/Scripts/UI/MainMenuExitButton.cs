// MainMenuExitButton.cs
using UnityEngine;
using UnityEngine.UI;

public class MainMenuExitButton : MonoBehaviour
{
    [Header("Optional. If not set, will try GetComponent<Button>().")]
    [SerializeField] private Button _button;

    private bool _isBound;

    private void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_button == null)
        {
            Debug.LogWarning("[MainMenuExitButton] Button reference missing. Assign a Button or attach this component to a Button GameObject.");
            enabled = false;
            return;
        }

        _button.onClick.AddListener(OnClickExit);
        _isBound = true;
    }

    private void OnDestroy()
    {
        if (_isBound && _button != null)
        {
            _button.onClick.RemoveListener(OnClickExit);
        }
    }

    // Unity Button OnClick에서도 직접 연결 가능
    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
