// ToggleSwitchController.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public enum E_SwitchMode
{
    OneShotLatch = 0,       // 한 번만 눌리면 고정
    ToggleOnEnter = 1,      // 토글 ON/OFF
    HoldWhilePressed = 2,   // 누르고 있는 동안만 ON
}

[DisallowMultipleComponent]
public class ToggleSwitchController : MonoBehaviour,
    IRewindable, ITurnTickable
{
    [Serializable]
    public struct ToggleSwitchState
    {
        public bool _isOn;          // 시각/상태용(모드에 따라 의미 다름)
        public bool _isPressed;     // 직전 턴에 스위치 위에 있었는지
        public bool _consumed;      // OneShotLatch에서 1회 사용 완료
    }

    [Header("Placement")]
    [SerializeField] private Vector2Int _cell;

    [Header("Mode")]
    [SerializeField] private E_SwitchMode _mode = E_SwitchMode.HoldWhilePressed;

    [Header("Behavior")]
    [SerializeField] private bool _startOn = false;

    [Header("GUID Links (Door RewindKey GUID, N or D format)")]
    [SerializeField] private List<string> _targetGuids = new();

    [Header("On/Off Sprite")]
    [SerializeField] private Sprite _offSprite;
    [SerializeField] private Sprite _onSprite;

    // ===== runtime refs =====
    private BoardGrid _grid;
    private GridPresenter _presenter;

    private FatherController _father;
    private GapFillerBlockRegistry _gapFillerRegistry;

    private StageRuntimeRefs _refs; // root 기반 링크 바인딩용

    private readonly List<DoorController> _doors = new(8);

    // ===== state =====
    private bool _isOn;
    private bool _isPressed;
    private bool _consumed;

    private bool _warnedNotInitialized;
    private bool _warnedNoTargets;

    private SpriteRenderer _sr;

    private bool _warnedMissingOnSprite;
    private bool _warnedMissingOffSprite;

    public Vector2Int Cell => _cell;
    public bool IsOn => _isOn;

    // =========================
    // Stage pipeline hooks
    // =========================

    // 런타임 스폰에서 호출(필수)
    public void ConfigureRuntime(Vector2Int cell, E_SwitchMode mode, bool startOn, IReadOnlyList<string> targetDoorGuids)
    {
        _cell = cell;
        _mode = mode;
        _startOn = startOn;

        _targetGuids.Clear();
        if (targetDoorGuids != null)
        {
            for (int i = 0; i < targetDoorGuids.Count; i++)
                _targetGuids.Add(targetDoorGuids[i]);
        }
    }

    public void InitializeGimmick(StageRuntimeRefs refs, BoardGrid grid, GridPresenter presenter)
    {
        _refs = refs;
        _grid = grid;
        _presenter = presenter;

        _father = refs != null ? refs._fatherController : null;
        _gapFillerRegistry = refs != null ? refs._gapFillerRegistry : null;

        _sr = GetComponentInChildren<SpriteRenderer>(includeInactive: true);

        // scope에 registry를 못 넣은 경우 1회 폴백(무음 금지)
        if (_gapFillerRegistry == null && refs != null && refs._root != null)
        {
            _gapFillerRegistry = refs._root.GetComponentInChildren<GapFillerBlockRegistry>(includeInactive: true);
            if (_gapFillerRegistry == null)
                Debug.LogWarning("[ToggleSwitchController] InitializeGimmick: GapFillerBlockRegistry not found. (Gap blocks won't press switches)");
        }

        _isOn = _startOn;
        _isPressed = false;
        _consumed = false;

        if (_grid == null || _presenter == null)
        {
            Debug.LogWarning("[ToggleSwitchController] InitializeGimmick fallback: grid/presenter is null.");
            return;
        }

        // 위치 스냅(필요 없으면 삭제)
        transform.position = _presenter.CellToWorld(_cell) + new Vector3(0f, 0f, -0.2f);

        ApplyVisual();
    }

    public void ApplyInitialPressState()
    {
        if (_grid == null)
            return;

        ApplyPressState(IsAnyPresserOnCell(), playSfx: false);
    }

    public void BindAllLinks(StageRuntimeRefs refs)
    {
        _doors.Clear();

        if (refs == null || refs._root == null)
        {
            Debug.LogWarning("[ToggleSwitchController] BindAllLinks fallback: refs/root is null.");
            return;
        }

        if (_targetGuids == null || _targetGuids.Count == 0)
        {
            if (!_warnedNoTargets)
            {
                _warnedNoTargets = true;
                Debug.LogWarning("[ToggleSwitchController] BindAllLinks fallback: target GUID list is empty. (switch will do nothing)");
            }
            return;
        }

        // 1) 현재 스테이지 StageRoot 하위의 DoorController 스캔
        var allDoors = refs._root.GetComponentsInChildren<DoorController>(includeInactive: true);
        var dict = new Dictionary<Guid, DoorController>(allDoors.Length);

        for (int i = 0; i < allDoors.Length; i++)
        {
            var door = allDoors[i];
            if (door == null) continue;

            var key = door.GetComponent<RewindKey>();
            if (key == null)
            {
                Debug.LogWarning($"[ToggleSwitchController] BindAllLinks fallback: Door has no RewindKey. name={door.name}");
                continue;
            }

            Guid g = key.Guid;
            if (g == Guid.Empty)
            {
                Debug.LogWarning($"[ToggleSwitchController] BindAllLinks fallback: Door RewindKey GUID is empty. name={door.name}");
                continue;
            }

            if (!dict.ContainsKey(g))
                dict.Add(g, door);
            else
                Debug.LogWarning($"[ToggleSwitchController] BindAllLinks fallback: duplicated Door GUID. guid={g} name={door.name}");
        }

        // 2) switch의 GUID 리스트를 실제 DoorController로 resolve
        for (int i = 0; i < _targetGuids.Count; i++)
        {
            string raw = _targetGuids[i];

            if (!TryParseGuid(raw, out Guid guid))
            {
                Debug.LogWarning($"[ToggleSwitchController] BindAllLinks fallback: invalid GUID string. raw={raw}");
                continue;
            }

            if (!dict.TryGetValue(guid, out var door) || door == null)
            {
                Debug.LogWarning($"[ToggleSwitchController] BindAllLinks fallback: target Door not found. guid={guid}");
                continue;
            }

            _doors.Add(door);
        }

        if (_doors.Count == 0)
        {
            Debug.LogWarning("[ToggleSwitchController] BindAllLinks fallback: resolved Door list is empty. (switch will do nothing)");
        }
    }

    // =========================
    // Turn hook (OnEnter 1회 반전, 압력 스위치: Father + GapFillerBlock)
    // =========================
    public void OnTurnBegin(int turnIndex)
    {
        if (_grid == null)
        {
            if (!_warnedNotInitialized)
            {
                _warnedNotInitialized = true;
                Debug.LogWarning("[ToggleSwitchController] OnTurnBegin fallback: not initialized (father/grid is null).");
            }
            return;
        }

        ApplyPressState(IsAnyPresserOnCell(), playSfx: true);
    }

    private void ApplyPressState(bool onCell, bool playSfx)
    {
        bool entered = onCell && !_isPressed;
        bool exited = !onCell && _isPressed;
        bool prevIsOn = _isOn;

        switch (_mode)
        {
            case E_SwitchMode.OneShotLatch:
                {
                    if (entered && !_consumed)
                    {
                        InvertDoorsOrWarn("OneShotLatch.Enter");
                        _isOn = true;
                        _consumed = true;
                        ApplyVisual();
                    }
                    break;
                }

            case E_SwitchMode.ToggleOnEnter:
                {
                    if (entered)
                    {
                        InvertDoorsOrWarn("ToggleOnEnter.Enter");
                        _isOn = !_isOn;
                        ApplyVisual();
                    }
                    break;
                }

            case E_SwitchMode.HoldWhilePressed:
                {
                    if (entered)
                    {
                        InvertDoorsOrWarn("Hold.Enter");
                        _isOn = true;
                        ApplyVisual();
                    }
                    else if (exited)
                    {
                        InvertDoorsOrWarn("Hold.Exit");
                        _isOn = false;
                        ApplyVisual();
                    }
                    break;
                }
        }

        bool changed = (_isOn != prevIsOn);

        if (changed && playSfx)
        {
            E_SfxId sfxId = _isOn ? E_SfxId.Switch_On : E_SfxId.Switch_Off;
            AudioHub.Ensure().PlaySfx(sfxId);
        }

        _isPressed = onCell;
    }

    public void OnTurnEnd(int turnIndex)
    {
        // no-op
    }

    private bool IsAnyPresserOnCell()
    {
        // 1) Father
        if (_father != null && _father.Cell == _cell)
            return true;

        // 2) GapFillerBlock
        if (_gapFillerRegistry != null
            && _gapFillerRegistry.TryGet(_cell, out var block)
            && block != null
            && block.IsAlive)
        {
            return true;
        }

        return false;
    }

    // =========================
    // Toggle execution
    // =========================
    private void InvertDoorsOrWarn(string reason)
    {
        if (_doors.Count == 0)
        {
            Debug.LogWarning($"[ToggleSwitchController] InvertDoors fallback: no bound doors. reason={reason}");
            return;
        }

        for (int i = 0; i < _doors.Count; i++)
        {
            var door = _doors[i];
            if (door == null)
            {
                Debug.LogWarning($"[ToggleSwitchController] InvertDoors fallback: door is null. index={i} reason={reason}");
                continue;
            }

            // 문은 반드시 SetOpen()만 사용
            door.SetOpen(!door.IsOpen);
        }
    }

    private void ApplyVisual()
    {
        if (_sr == null)
            _sr = GetComponentInChildren<SpriteRenderer>(includeInactive: true);

        if (_sr == null)
        {
            Debug.LogWarning("[ToggleSwitchController] ApplyStateSprite fallback: SpriteRenderer missing.");
            return;
        }

        if (_isOn)
        {
            if (_onSprite != null)
            {
                _sr.sprite = _onSprite;
                return;
            }

            if (!_warnedMissingOnSprite)
            {
                _warnedMissingOnSprite = true;
                Debug.LogWarning("[ToggleSwitchController] Sprite missing: SwitchOn (keep previous).");
            }
        }
        else
        {
            if (_offSprite != null)
            {
                _sr.sprite = _offSprite;
                return;
            }

            if (!_warnedMissingOffSprite)
            {
                _warnedMissingOffSprite = true;
                Debug.LogWarning("[ToggleSwitchController] Sprite missing: SwitchOff (keep previous).");
            }
        }
    }

    // =========================
    // Rewind
    // =========================
    public object CaptureState()
    {
        return new ToggleSwitchState
        {
            _isOn = _isOn,
            _isPressed = _isPressed,
            _consumed = _consumed,
        };
    }

    public void RestoreState(object state)
    {
        if (state is not ToggleSwitchState s)
        {
            Debug.LogWarning("[ToggleSwitchController] RestoreState fallback: invalid state type.");
            return;
        }

        _isOn = s._isOn;

        // 중요: OnEnter 중복 토글 방지용
        _isPressed = s._isPressed;
        _consumed = s._consumed;

        ApplyVisual();
    }

    private static bool TryParseGuid(string raw, out Guid guid)
    {
        guid = Guid.Empty;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        raw = raw.Trim();

        // RewindKey 기본이 "N"을 쓰는 경우가 많음(32 hex)
        if (Guid.TryParseExact(raw, "N", out guid)) return true;
        if (Guid.TryParseExact(raw, "D", out guid)) return true;

        // 마지막 폴백 (경고는 호출부에서)
        return Guid.TryParse(raw, out guid);
    }
}
