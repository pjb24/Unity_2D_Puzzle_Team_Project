using System.Collections.Generic;
using UnityEngine;

public class StageRuntimeRefs
{
    public GameObject _root;
    public List<Transform> _tiles = new();
    public List<Vector3> _pathPoints = new();

    public GameObject _father;
    public GameObject _child;

    public FatherController _fatherController;
    public ChildController _childController;
    public TurnSnapshotRecorder _snapshot;

    public BoardGrid _grid;
    public GridPresenter _gridPresenter;
}
