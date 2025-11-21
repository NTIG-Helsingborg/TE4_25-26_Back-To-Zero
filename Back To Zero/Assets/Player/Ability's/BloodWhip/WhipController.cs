using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class WhipController : MonoBehaviour
{
    [Header("Curve")]
    [Tooltip("0..1 curve shaping how much the whip bends during motion.")]
    public AnimationCurve bendOverTime = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Tooltip("Extra outward sinusoidal bulge strength along arc (0 = none).")]
    public float arcBulgeFactor = 0.15f;

    [Header("Orientation")]
    [Tooltip("If true the arc sweeps in the opposite direction (mirrors curve).")]
    public bool invertArcDirection = false;
    [Tooltip("If true the bulge curves the opposite way (convex flips).")]
    public bool invertBulge = false;

    // Remove unused arc fields (baseAngleDeg etc.) and keep config.

    // NEW: how much lateral curve at peak (scaled by maxLength)
    [Tooltip("Max sideways curve factor (0 = straight).")]
    public float maxCurveFactor = 0.35f;

    // Internal phase enum for clarity
    private enum Phase { ExtendBack, SwingForward, Retract }
    private Phase phase;

    private LineRenderer lr;
    private EdgeCollider2D edge;
    private Rigidbody2D rb;

    private Transform owner;
    private Transform origin;      // fire point
    private float damage;
    private float knockback;
    private float maxLength;
    private float thickness;
    private int segments;
    private float extendTime;
    private float holdTime;
    private float retractTime;
    private LayerMask hitLayers;

    private readonly HashSet<Collider2D> hitThisSwing = new HashSet<Collider2D>();
    private bool running;
    private Vector2 aimDir; // new

    public void Initialize(Transform owner, Transform origin, float damage, float knockback,
        float maxLength, float thickness, int segments, float extendTime, float holdTime,
        float retractTime, LayerMask hitLayers, Vector3 aimPoint) // added aimPoint
    {
        this.owner = owner;
        this.origin = origin;
        this.damage = damage;
        this.knockback = knockback;
        this.maxLength = maxLength;
        this.thickness = thickness;
        this.segments = Mathf.Max(2, segments);
        this.extendTime = extendTime;
        this.holdTime = holdTime;
        this.retractTime = retractTime;
        this.hitLayers = hitLayers;

        Vector3 dir3 = (aimPoint - origin.position);
        aimDir = dir3.sqrMagnitude > 0.0001f ? ((Vector2)dir3).normalized : (Vector2)origin.right;

        EnsureComponents();
        SetupRendererAndCollider();

        if (!running)
            StartCoroutine(WhipRoutine());
    }

    private void EnsureComponents()
    {
        if (!lr) lr = GetComponent<LineRenderer>();
        if (!edge) edge = GetComponent<EdgeCollider2D>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
    }

    private void SetupRendererAndCollider()
    {
        // Rigidbody for trigger events
        rb.isKinematic = true;
        rb.gravityScale = 0f;

        // Visuals
        lr.positionCount = segments;
        lr.useWorldSpace = true;
        lr.startWidth = thickness;
        lr.endWidth = thickness;

        // Collider setup (as trigger so it doesn't push things)
        edge.isTrigger = true;
        edge.edgeRadius = Mathf.Max(0f, thickness * 0.5f);
    }

    private IEnumerator WhipRoutine()
    {
        running = true;

        phase = Phase.ExtendBack;
        float t = 0f;
        while (t < extendTime)
        {
            float p = t / extendTime;
            float length = Mathf.Lerp(0f, maxLength, EaseOutQuad(p));
            Vector2 forward = aimDir;
            Vector2 tip = (Vector2)origin.position - forward * length;
            float curve = maxCurveFactor * maxLength * p;
            RebuildWhip(tip, forward, curve);
            t += Time.deltaTime;
            yield return null;
        }

        phase = Phase.SwingForward;
        t = 0f;
        while (t < holdTime)
        {
            float p = t / Mathf.Max(0.0001f, holdTime);
            Vector2 forward = aimDir;
            float baseAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            float ang = Mathf.LerpAngle(baseAngle + 180f, baseAngle, EaseOutQuad(p));
            Vector2 dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
            Vector2 tip = (Vector2)origin.position + dir * maxLength;
            float swingCurveStrength = maxCurveFactor * maxLength * (1f - Mathf.Abs(p - 0.5f) * 2f);
            RebuildWhip(tip, forward, swingCurveStrength);
            t += Time.deltaTime;
            yield return null;
        }

        phase = Phase.Retract;
        t = 0f;
        while (t < retractTime)
        {
            float p = t / retractTime;
            Vector2 forward = aimDir;
            float length = Mathf.Lerp(maxLength, 0f, EaseInQuad(p));
            Vector2 tip = (Vector2)origin.position + forward * length;
            float curve = maxCurveFactor * maxLength * (1f - p) * 0.25f;
            RebuildWhip(tip, forward, curve);
            t += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    // Replace previous RebuildGeometry / RebuildArc with unified whip builder
    private void RebuildWhip(Vector2 tipPos, Vector2 forwardDir, float curveStrength)
    {
        if (!origin) return;

        if (lr.positionCount != segments) lr.positionCount = segments;
        var colliderPts = new Vector2[segments];

        Vector2 originPos = origin.position;
        Vector2 perp = new Vector2(-forwardDir.y, forwardDir.x);

        // Quadratic Bezier control point for lateral curve
        Vector2 control = originPos + perp * curveStrength;

        for (int i = 0; i < segments; i++)
        {
            float f = (segments == 1) ? 0f : (float)i / (segments - 1);

            // Quadratic Bezier (origin -> control -> tip)
            Vector2 a = Vector2.Lerp(originPos, control, f);
            Vector2 b = Vector2.Lerp(control, tipPos, f);
            Vector2 point = Vector2.Lerp(a, b, f);

            // Trailing slack: earlier segments lag during swing
            if (phase == Phase.SwingForward)
            {
                float slack = (1f - f) * 0.15f; // tune slack factor
                point = Vector2.Lerp(point, originPos, slack);
            }

            lr.SetPosition(i, point);
            colliderPts[i] = point;
        }

        // Convert to local for EdgeCollider2D
        Vector2 offset = transform.position;
        for (int i = 0; i < colliderPts.Length; i++)
            colliderPts[i] -= offset;

        edge.SetPoints(new List<Vector2>(colliderPts));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!MatchesMask(other.gameObject.layer))
            return;
        if (owner && other.transform.IsChildOf(owner))
            return;
        if (hitThisSwing.Contains(other))
            return;

        hitThisSwing.Add(other);

        // Apply damage (generic)
        other.SendMessage("TakeDamage", Mathf.RoundToInt(damage), SendMessageOptions.DontRequireReceiver);

        // Knockback
        var rb2 = other.attachedRigidbody;
        if (rb2)
        {
            Vector2 fromOrigin = (Vector2)other.bounds.center - (Vector2)(origin ? origin.position : transform.position);
            if (fromOrigin.sqrMagnitude < 0.0001f) fromOrigin = Vector2.right;
            rb2.AddForce(fromOrigin.normalized * knockback, ForceMode2D.Impulse);
        }
    }

    private bool MatchesMask(int layer) => (hitLayers.value & (1 << layer)) != 0;

    private static float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
    private static float EaseInQuad(float x) => x * x;
}