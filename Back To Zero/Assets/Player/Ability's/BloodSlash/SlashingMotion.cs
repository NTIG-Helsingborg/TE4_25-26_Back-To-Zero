using UnityEngine;

public class SlashingMotion : MonoBehaviour
{
    [Header("Arc")]
    public float arcDegrees = 120f;          // total sweep
    public float startAngleOffset = -60f;    // where to begin relative to aim
    public float lifetime = 0.15f;

    [Header("Radius")]
    public float radius = 1f;                // distance from pivot (reach)

    private Transform pivotTransform;        // usually the player
    private Vector3 pivotPos;                // cached if no transform
    private float baseAngleDeg;              // from firePoint
    private float t;

    // Call this right after Instantiate
    public void Initialize(Transform pivot, float baseAngleDeg, float radius, float lifetime)
    {
        this.pivotTransform = pivot;
        this.pivotPos = pivot ? pivot.position : transform.position;
        this.baseAngleDeg = baseAngleDeg;
        this.radius = radius;
        this.lifetime = Mathf.Max(0.01f, lifetime);
    }

    public void SetLifetime(float v) => lifetime = Mathf.Max(0.01f, v);

    void Update()
    {
        t += Time.deltaTime;
        float p = Mathf.Clamp01(t / lifetime);
        float eased = 1f - (1f - p) * (1f - p);

        // Keep pivot up to date if we track a transform
        var pivot = pivotTransform ? pivotTransform.position : pivotPos;

        float angle = baseAngleDeg + startAngleOffset + arcDegrees * eased;
        Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;

        // Move the slash along the arc and face its travel direction
        transform.position = pivot + (Vector3)(dir * radius);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}