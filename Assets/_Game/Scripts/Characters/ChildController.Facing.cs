// ChildController.Facing.cs
using UnityEngine;

public partial class ChildController : MonoBehaviour
{
    [Header("Facing")]
    [SerializeField] private bool _rotateToFacing = true;

    public E_Facing Facing { get; private set; } = E_Facing.Right;

    private void ApplyFacingVisual()
    {
        if (!_rotateToFacing)
            return;

        float z = Facing switch
        {
            E_Facing.Up => 90f,
            E_Facing.Right => 0f,
            E_Facing.Down => -90f,
            E_Facing.Left => 180f,
            _ => 0f
        };

        transform.rotation = Quaternion.Euler(0f, 0f, z);
    }

    private void UpdateFacingByNextStepWorld(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;

        const float eps = 0.0001f;
        float ax = Mathf.Abs(d.x);
        float ay = Mathf.Abs(d.y);

        if (ax < eps && ay < eps)
            return; // 정지: 유지

        bool diagonal = (ax >= eps && ay >= eps);
        if (diagonal)
        {
            Debug.LogWarning($"[ChildController] Facing fallback: diagonal delta detected. from={from} to={to} delta={d}");
        }

        if (ax >= ay)
            Facing = (d.x >= 0f) ? E_Facing.Right : E_Facing.Left;
        else
            Facing = (d.y >= 0f) ? E_Facing.Up : E_Facing.Down;

        ApplyFacingVisual();
    }
}
