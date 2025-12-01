using UnityEngine;

public class Aim : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform pivot;          // Optional: the transform to rotate (defaults to this)
    [SerializeField] private Transform spellTransform; // Optional: where spells spawn from
    [SerializeField] private float muzzleOffset = 0.5f;

    [Header("Scaling")]
    [SerializeField] private bool compensateParentScale = true; // Keep pivot at unit scale even if parent is scaled

    [Header("Centering")]
    [SerializeField] private bool keepPivotCentered = true;     // Recenter pivot to sprite(s) center
    [SerializeField] private Renderer[] centerFromRenderers;     // Leave empty to auto-detect SpriteRenderer(s)

    private Camera mainCamera;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main != null ? Camera.main : GameObject.FindWithTag("MainCamera")?.GetComponent<Camera>();
        if (pivot == null) pivot = transform;

        // Auto-pick renderers for centering
        if (keepPivotCentered && (centerFromRenderers == null || centerFromRenderers.Length == 0))
        {
            var sr = GetComponentInParent<SpriteRenderer>();
            if (sr != null) centerFromRenderers = new Renderer[] { sr };
            else centerFromRenderers = GetComponentsInParent<Renderer>();
        }
    }

    void Update()
    {
        if (mainCamera == null || pivot == null) return;

        // Keep pivot at the visual center
        if (keepPivotCentered) RecenterPivot();

        // Get mouse world position with correct depth
        var t = pivot;
        Vector3 mouseScreen = Input.mousePosition;
        float depth = Mathf.Abs(mainCamera.transform.position.z - t.position.z);
        mouseScreen.z = depth;
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(mouseScreen);
        if (mainCamera.orthographic) mouseWorld.z = t.position.z;

        // Aim: rotate so +X points at the cursor
        Vector3 dir = (mouseWorld - t.position);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        t.rotation = Quaternion.Euler(0f, 0f, angle);

        // Keep spawn transform on the muzzle along the aim direction
        if (spellTransform != null)
            spellTransform.position = t.position + (Vector3)(new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * muzzleOffset);
    }

    void LateUpdate()
    {
        // Neutralize parent scaling so pivot behaves predictably
        if (compensateParentScale && pivot != null && pivot.parent != null)
        {
            Vector3 p = pivot.parent.lossyScale;
            float inv = 1f / Mathf.Max(Mathf.Abs(p.x), 0.0001f);
            pivot.localScale = new Vector3(inv, inv, 1f);
        }
    }

    private void RecenterPivot()
    {
        if (pivot == null || centerFromRenderers == null || centerFromRenderers.Length == 0) return;
        int count = 0;
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        foreach (var r in centerFromRenderers)
        {
            if (r == null) continue;
            if (count == 0) b = new Bounds(r.bounds.center, r.bounds.size);
            else b.Encapsulate(r.bounds);
            count++;
        }
        if (count == 0) return;
        Vector3 c = b.center;
        c.z = pivot.position.z; // keep current Z
        pivot.position = c;
    }
}
