// StageRuntimeRefs.cs
using System.Collections.Generic;
using UnityEngine;

public class StageRuntimeRefs
{
    // ===== Root =====
    public GameObject _root;

    // ===== Board =====
    public List<Transform> _tiles = new();
    public BoardGrid _grid;
    public GridPresenter _gridPresenter;

    // ===== Path =====
    public List<Vector3> _pathPoints = new();

    // ===== Characters =====
    public GameObject _father;
    public GameObject _child;

    public FatherController _fatherController;
    public ChildController _childController;

    // ===== Snapshot =====
    public TurnSnapshotRecorder _snapshot;

    // ===== Interact =====
    public InteractRegistry _interactRegistry;
}
