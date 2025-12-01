using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    
    public float MoveSpeed
    {
        get { return moveSpeed; }
        set { moveSpeed = value; }
    }
    
    private Vector2 moveInput;
    private PlayerInput playerInput;
    private Rigidbody2D rb;
    private Health playerHealth;
    
    [SerializeField] private Animator animator; // assign in Inspector
    private const string RunDownParam = "RunDown";
    private const string RunRightParam = "RunRight";
    private const string RunLeftParam = "RunLeft";
    private const string RunUpParam = "RunUp";
    
    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<Health>(); 
        
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        if (animator == null)
            animator = GetComponent<Animator>();
    }
    
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();

        if (moveInput.magnitude > 1f)
        {
            moveInput.Normalize();
        }
        UpdateAnimationParameters();
        
    }
    
    void Update()
    {
        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * Time.deltaTime;
        transform.Translate(movement);

        // Keep animator updated while holding input
        UpdateAnimationParameters();
    }

    private void UpdateAnimationParameters()
    {
        if (animator == null) return;

        bool isMoving = moveInput.sqrMagnitude > 0.0001f;

        // RunDown true only if moving and direction is predominantly downward
        bool isMovingDown = moveInput.y < -0.2f && isMoving;
        animator.SetBool(RunDownParam, isMovingDown);

        bool isMovingRight = moveInput.x > 0.2f && isMoving;
        animator.SetBool(RunRightParam, isMovingRight);
        
        bool isMovingLeft = moveInput.x < -0.2f && isMoving;
        animator.SetBool(RunLeftParam, isMovingLeft);

        bool isMovingUp = moveInput.y > 0.2f && isMoving;
        animator.SetBool(RunUpParam, isMovingUp);
    }

    private void FixedUpdate()
    {
        // Physics-based movement can be added here if needed
    }
    
    // Public method to get current move input for abilities
    public Vector2 GetMoveInput()
    {
        return moveInput;
    }
}