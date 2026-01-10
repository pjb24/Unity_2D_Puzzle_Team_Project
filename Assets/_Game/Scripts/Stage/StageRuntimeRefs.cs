// StageRuntimeRefs.cs
using System.Collections.Generic;
using UnityEngine;

public class StageRuntimeRefs
{
    // ===== Root =====
    public GameObject _root;

    // ===== Visual =====
    public string _stageId;
    public StageVisualOverride _stageVisualOverride;
    public ITileSpriteProvider _tileSpriteProvider;
    public Sprite _resolvedFatherSprite;
    public Sprite _resolvedChildSprite;

    // ===== Layout =====
    // tileScale: 스프라이트 스케일
    // tileGap  : 셀 간격
    // cellPitch: 타일 배치 간격(tileScale + tileGap)
    public float _tileScale = 1f;
    public float _tileGap = 0f;
    public float _cellPitch = 1f;

    // InnerBase Background
    public GameObject _innerBaseBackground;

    // ===== Board =====
    public Transform _tilesRoot; // Slide 대상
    public List<Transform> _tiles = new();
    public BoardGrid _grid;
    public GridPresenter _gridPresenter;

    // ===== Path =====
    public Transform _pathRoot; // Path Fade 대상
    public List<Vector3> _pathPoints = new();

    // ===== Characters =====
    public GameObject _father;
    public GameObject _child;

    public FatherController _fatherController;
    public ChildController _childController;

    // ===== Snapshot =====
    public TurnSnapshotRecorder _snapshot;

    public BoardStateRewindable _boardStateRewindable;
    public List<ITurnTickable> _turnSystems = new List<ITurnTickable>(8);

    public ChildPathBlockerRegistry _childPathBlockers;

    // ===== GapFiller =====
    public GapFillerBlockRegistry _gapFillerRegistry;
}
