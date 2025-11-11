using System.Collections;
using Pathfinding;
using UnityEngine;

public class KnockbackReceiver : MonoBehaviour
{
    [Tooltip("Scale incoming knockback distance")]
    public float resistance = 1f;

    [Tooltip("Default knockback duration (seconds)")]
    public float defaultDuration = 0.15f;

    private AIPath aiPath;
    private Coroutine routine;

    void Awake()
    {
        aiPath = GetComponent<AIPath>();
    }

    // distance = how far to push (derive from force)
    public void ApplyKnockback(Vector2 direction, float force, float duration = -1f)
    {
        if (duration <= 0f) duration = defaultDuration;
        if (direction.sqrMagnitude <= 0.0001f) return;

        direction.Normalize();

        float distance = force * Mathf.Max(0f, resistance); // convert force to distance
        if (distance <= 0f) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(KnockbackRoutine(direction, distance, duration));
    }

    private IEnumerator KnockbackRoutine(Vector2 dir, float distance, float duration)
    {
        // Pause pathfinding
        if (aiPath != null)
        {
            aiPath.isStopped = true;
            aiPath.canMove = false;
        }

        Vector3 start = transform.position;
        Vector3 target = start + (Vector3)(dir * distance);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            // Ease-out (quadratic)
            float eased = 1f - (1f - p) * (1f - p);
            transform.position = Vector3.Lerp(start, target, eased);
            yield return null;
        }

        // Resume pathfinding
        if (aiPath != null)
        {
            aiPath.isStopped = false;
            aiPath.canMove = true;
        }

        routine = null;
    }
}