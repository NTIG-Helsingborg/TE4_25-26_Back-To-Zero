using UnityEngine;
using System.Collections;

[CreateAssetMenu]
public class DashAbility : Ability
{
    [Header("Dash Settings")]
    public float dashingPower = 20f;
    public float dashingTime = 0.2f;
    
    private GameObject playerObject;
    private PlayerMove playerMove;
    private TrailRenderer trailRenderer;
    private Rigidbody2D playerRigidbody;
    private Health playerHealth;
    
    public override void Activate()
    {
        // Get references from the player
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerMove = playerObject.GetComponent<PlayerMove>();
                trailRenderer = playerObject.GetComponentInChildren<TrailRenderer>();
                playerRigidbody = playerObject.GetComponent<Rigidbody2D>();
                playerHealth = playerObject.GetComponent<Health>();
            }
        }
        
        if (playerObject != null)
        {
            // Get any MonoBehaviour component to run the coroutine
            MonoBehaviour monoBehaviour = playerObject.GetComponent<MonoBehaviour>();
            if (monoBehaviour != null)
            {
                monoBehaviour.StartCoroutine(PerformDash());
            }
        }
    }
    
    private IEnumerator PerformDash()
    {
        // Make player invincible during dash
        if (playerHealth != null)
        {
            playerHealth.isInvincible = true;
        }
        
        // Get dash direction from player input
        Vector2 dashDirection = Vector2.zero;
        
        // Try to get input from PlayerMove first
        if (playerMove != null)
        {
            dashDirection = playerMove.GetMoveInput();
        }
        
        // If no input from PlayerMove, try to get current input directly
        if (dashDirection.magnitude == 0)
        {
            // Check for current input keys
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) dashDirection.y += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dashDirection.y -= 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dashDirection.x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dashDirection.x += 1;
        }
        
        // If still no input, dash in the direction the player is facing
        if (dashDirection.magnitude == 0)
        {
            dashDirection = Vector2.right * Mathf.Sign(playerObject.transform.localScale.x);
        }
        
        dashDirection.Normalize();
        
        // Apply dash force
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = dashDirection * dashingPower;
        }
        
        // Enable trail effect
        if (trailRenderer != null)
        {
            trailRenderer.emitting = true;
        }
        
        // Wait for dash duration
        yield return new WaitForSeconds(dashingTime);
        
        // Disable trail effect
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }
        
        // Remove invincibility after dash ends
        if (playerHealth != null)
        {
            playerHealth.isInvincible = false;
        }
        
        // Stop movement
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
        }
    }
}
