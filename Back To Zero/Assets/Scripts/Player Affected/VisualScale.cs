using UnityEngine;

[ExecuteAlways]
public class VisualScaleCompensator : MonoBehaviour
{
    [SerializeField] private Vector3 targetWorldScale = Vector3.one;

    private Transform parent;

    void OnEnable()
    {
        parent = transform.parent;
        ApplyCompensation();
    }

    void LateUpdate()
    {
        ApplyCompensation();
    }

    private void ApplyCompensation()
    {
        if (parent == null) return;
        var pls = parent.lossyScale;
        transform.localScale = new Vector3(
            SafeMul(targetWorldScale.x, SafeInv(pls.x)),
            SafeMul(targetWorldScale.y, SafeInv(pls.y)),
            SafeMul(targetWorldScale.z, SafeInv(pls.z))
        );
    }

    private static float SafeInv(float v) => Mathf.Approximately(v, 0f) ? 1f : 1f / v;
    private static float SafeMul(float a, float b) => Mathf.Clamp(a * b, -1000f, 1000f);
}