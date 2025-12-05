using UnityEngine;
using Pathfinding;

// Minimal driver: feed Animator with AIPath velocity-based params
public class EnemyAnimatorDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AIPath aiPath; // use A* AIPath velocity
    [SerializeField] private float idleThreshold = 0.05f;
    [SerializeField] private Transform player; // optional: face player when idle
    [SerializeField] private bool facePlayerWhenIdle = true;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!aiPath) aiPath = GetComponent<AIPath>();
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void Update()
    {
        Vector2 vel = aiPath ? (Vector2)aiPath.velocity : Vector2.zero;

        float speed = vel.magnitude;
        Vector2 dir;

        if (speed > idleThreshold)
        {
            dir = vel.normalized;
        }
        else if (facePlayerWhenIdle && player != null)
        {
            // Face the player when idle; clamp to cardinal directions to pick one of 4 idles
            Vector2 toPlayer = ((Vector2)(player.position - transform.position)).normalized;
            dir = ToCardinal(toPlayer);
            speed = 0f; // ensure idle
        }
        else
        {
            // Default idle facing (e.g., down)
            dir = Vector2.down;
            speed = 0f;
        }

        animator.SetFloat(SpeedHash, speed);
        animator.SetFloat(MoveXHash, dir.x);
        animator.SetFloat(MoveYHash, dir.y);
    }

    private static Vector2 ToCardinal(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return new Vector2(Mathf.Sign(v.x), 0f); // left/right
        else
            return new Vector2(0f, Mathf.Sign(v.y)); // up/down
    }
}