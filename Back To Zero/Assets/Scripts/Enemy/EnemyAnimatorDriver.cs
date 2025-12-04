using UnityEngine;
using Pathfinding;

// Minimal driver: feed Animator with AIPath velocity-based params
public class EnemyAnimatorDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AIPath aiPath; // use A* AIPath velocity
    [SerializeField] private float idleThreshold = 0.01f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!aiPath) aiPath = GetComponent<AIPath>();
    }

    void Update()
    {
        Vector2 vel = aiPath ? (Vector2)aiPath.velocity : Vector2.zero;

        float speed = vel.magnitude;
        Vector2 dir = speed > idleThreshold ? vel.normalized : Vector2.zero;

        animator.SetFloat(SpeedHash, speed);
        animator.SetFloat(MoveXHash, dir.x);
        animator.SetFloat(MoveYHash, dir.y);
    }
}