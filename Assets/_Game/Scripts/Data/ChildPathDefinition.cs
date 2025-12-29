using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Data/Child Path Definition")]
public class ChildPathDefinition : ScriptableObject
{
    [Header("Path (1D indices)")]
    [SerializeField] private List<int> _pathIndices = new();

    public IReadOnlyList<int> PathIndices => _pathIndices;

    private void OnValidate()
    {
        if (_pathIndices == null) _pathIndices = new List<int>();

        // 최소 규칙: 길이 2 이상 (시작/다음)
        if (_pathIndices.Count < 2)
        {
            Debug.LogWarning($"[ChildPathDefinition] Path too short (<2): {name}", this);
        }

        // 최소 규칙: 음수 인덱스 금지
        for (int i = 0; i < _pathIndices.Count; i++)
        {
            if (_pathIndices[i] < 0)
            {
                Debug.LogWarning($"[ChildPathDefinition] Negative index at {i}: {_pathIndices[i]} ({name})", this);
                _pathIndices[i] = 0;
            }
        }
    }
}
