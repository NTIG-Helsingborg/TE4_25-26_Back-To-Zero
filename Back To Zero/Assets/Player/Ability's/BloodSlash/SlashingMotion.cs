using UnityEngine;

public class SlashingMotion : MonoBehaviour
{
    [Header("Arc")]
    [Tooltip("Total sweep of the slash in degrees.")]
    [SerializeField] private float arcDegrees = 120f;          // total sweep
    [Tooltip("Starting angle offset relative to the current aim (degrees).")]
    [SerializeField] private float startAngleOffset = -60f;    // where to begin relative to aim
    [Tooltip("Time to complete the sweep.")]
    [SerializeField] private float lifetime = 0.15f;

    [Header("Radius")]
    [Tooltip("Distance from pivot (reach) in world units.")]
    [SerializeField] private float radius = 1f;                // distance from pivot (reach)

    private Transform pivotTransform;        // usually the player
    private Vector3 pivotPos;                // cached if no transform
    private float baseAngleDeg;              // from firePoint
    private float t;

    [Header("Visual")]
    [Tooltip("Extra rotation to align the sprite's forward with the slash direction. Common values: 0, 90, -90, 180.")]
    [SerializeField] private float visualRotationOffsetDeg = 0f;

    // Call this right after Instantiate
    public void Initialize(Transform pivot, float baseAngleDeg, float radius, float lifetime)
    {
        this.pivotTransform = pivot;
        this.pivotPos = pivot ? pivot.position : transform.position;
        this.baseAngleDeg = baseAngleDeg;
        this.radius = radius;
        this.lifetime = Mathf.Max(0.01f, lifetime);
        t = 0f;
    }

    public void SetLifetime(float v) => lifetime = Mathf.Max(0.01f, v);

    public void SnapToStart()
    {
        var pivot = pivotTransform ? pivotTransform.position : pivotPos;
        float angle = baseAngleDeg + startAngleOffset; // start of arc
        Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;

        transform.position = pivot + (Vector3)(dir * radius);
        transform.rotation = Quaternion.Euler(0f, 0f, angle + visualRotationOffsetDeg);
    }

    void Update()
    {
        t += Time.deltaTime;
        float p = Mathf.Clamp01(t / lifetime);
        float eased = 1f - (1f - p) * (1f - p);

        var pivot = pivotTransform ? pivotTransform.position : pivotPos;
        float angle = baseAngleDeg + startAngleOffset + arcDegrees * eased;

        Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;
        Vector3 pos = pivot + (Vector3)(dir * radius);

        transform.position = pos;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + visualRotationOffsetDeg);
    }
}