// StagePrefabs.cs
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
    [SerializeField] private GameObject _toggleSwitch;

    [Header("CellOverlay01 Outer")]
    [SerializeField] private GameObject _door;
    [SerializeField] private GameObject _goal;

    [Header("CellOverlay02")]
    [SerializeField] private GameObject _fillerBlock;

    public GameObject Father => _father;
    public GameObject Child => _child;

    public GameObject Empty => _empty;
    public GameObject Floor => _floor;

    public GameObject Wall => _wall;
    public GameObject Hole => _hole;
    public GameObject FilledHole => _filledHole;
    public GameObject ToggleSwitch => _toggleSwitch;
    
    public GameObject Door => _door;
    public GameObject Goal => _goal;

    public GameObject FillerBlock => _fillerBlock;
}
