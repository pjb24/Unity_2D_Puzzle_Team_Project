// UiSfxAutoAttach.cs
// 씬의 모든 Selectable에 UiSfxBinder 자동 부착(런타임 스캔 방식, 1회만)
using UnityEngine;
using UnityEngine.UI;

public class UiSfxAutoAttach : MonoBehaviour
{
    [SerializeField] private bool _includeInactive = true;

    private void Start()
    {
        var selects = FindObjectsByType<Selectable>(
            _includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        int added = 0;

        foreach (var s in selects)
        {
            if (s == null) continue;
            if (s.GetComponent<UiSfxBinder>() != null) continue;

            s.gameObject.AddComponent<UiSfxBinder>();
            added++;
        }

        Debug.Log($"[UiSfxAutoAttach] UiSfxBinder attached. count={added}");
    }
}
