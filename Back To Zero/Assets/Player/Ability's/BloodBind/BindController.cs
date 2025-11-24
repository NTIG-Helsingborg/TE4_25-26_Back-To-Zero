using System.Collections;
using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(LineRenderer))]
public class BindController : MonoBehaviour
{
    private LineRenderer lr;
    private Transform origin;
    private Transform target;
    private float extendTime;
    private float holdTime;
    private float retractTime;
    private float bindDuration;

    private bool boundApplied;
    private AIPath ai;
    private Rigidbody2D rb;
    private Vector3 originalTargetPos;

    public void Initialize(Transform origin, Transform target, float extendTime, float holdTime, float retractTime, float bindDuration)
    {
        this.origin = origin;
        this.target = target;
        this.extendTime = extendTime;
        this.holdTime = holdTime;
        this.retractTime = retractTime;
        this.bindDuration = bindDuration;

        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        ai = target ? target.GetComponent<AIPath>() : null;
        rb = target ? target.GetComponent<Rigidbody2D>() : null;

        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (!origin || !target)
        {
            Destroy(gameObject);
            yield break;
        }

        float t = 0f;
        Vector3 oPos = origin.position;
        Vector3 tgtPos = target.position;

        while (t < extendTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / extendTime);
            Vector3 tip = Vector3.Lerp(oPos, tgtPos, EaseOutQuad(p));
            Draw(oPos, tip);
            yield return null;
        }
        Draw(oPos, tgtPos);

        ApplyBind();
        originalTargetPos = target.position;

        t = 0f;
        float bindTimer = 0f;
        while (t < holdTime || bindTimer < bindDuration)
        {
            oPos = origin ? origin.position : oPos;
            tgtPos = target ? target.position : originalTargetPos;
            Draw(oPos, tgtPos);
            t += Time.deltaTime;
            bindTimer += Time.deltaTime;
            yield return null;
        }

        ReleaseBind();

        t = 0f;
        oPos = origin ? origin.position : oPos;
        Vector3 startTip = target ? target.position : tgtPos;
        while (t < retractTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / retractTime);
            Vector3 tip = Vector3.Lerp(startTip, oPos, EaseInQuad(p));
            Draw(oPos, tip);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void ApplyBind()
    {
        if (boundApplied || !target) return;
        if (ai) { ai.isStopped = true; ai.canMove = false; }
        if (rb) { rb.linearVelocity = Vector2.zero; rb.isKinematic = true; }
        boundApplied = true;
    }

    private void ReleaseBind()
    {
        if (!boundApplied) return;
        if (ai) { ai.isStopped = false; ai.canMove = true; }
        if (rb) rb.isKinematic = false;
        boundApplied = false;
    }

    private void Draw(Vector3 a, Vector3 b)
    {
        if (!lr) return;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
    }

    private static float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
    private static float EaseInQuad(float x) => x * x;
}