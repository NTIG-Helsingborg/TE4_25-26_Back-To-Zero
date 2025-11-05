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
    }
    
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        
        if (moveInput.magnitude > 1f)
        {
            moveInput.Normalize();
        }
    }
    
    void Update()
    {
        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * Time.deltaTime;
        transform.Translate(movement);
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