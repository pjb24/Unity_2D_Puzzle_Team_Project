// MainMenuStartButton.cs
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuStartButton : MonoBehaviour
{
    [SerializeField] private Button _btnStart;

    private IStartGamePort _port;

    // 외부로 event 노출 금지: 포트만 주입 받는다.
    public void Bind(IStartGamePort port)
    {
        _port = port;
        _btnStart.onClick.AddListener(OnClickStart);
    }

    public void Unbind()
    {
        _btnStart.onClick.RemoveListener(OnClickStart);
        _port = null;
    }

    private void OnDestroy()
    {
        // 씬 파괴 시 리스너 정리
        if (_btnStart != null)
            _btnStart.onClick.RemoveListener(OnClickStart);
    }

    private void OnClickStart()
    {
        _port?.RequestStartGame();
    }
}
