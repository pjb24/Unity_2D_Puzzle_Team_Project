using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Data/Stage Prefabs")]
public class StagePrefabs : ScriptableObject
{
    [Header("CellBase")]
    [SerializeField] private GameObject _father;
    [SerializeField] private GameObject _child;

    [Header("CellBase")]
    [SerializeField] private GameObject _empty;
    [SerializeField] private GameObject _floor;

    [Header("CellOverlay01 Inner")]
    [SerializeField] private GameObject _wall;
    [SerializeField] private GameObject _hole;
    [SerializeField] private GameObject _filledHole;
    [SerializeField] private GameObject _switchOn;
    [SerializeField] private GameObject _switchOff;

    [Header("CellOverlay01 Outer")]
    [SerializeField] private GameObject _door;
    [SerializeField] private GameObject _goal;

    [Header("CellOverlay02")]
    [SerializeField] private GameObject _fillerBlock;
}
