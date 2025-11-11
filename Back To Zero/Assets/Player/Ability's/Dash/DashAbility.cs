using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private Collider2D[] playerColliders;
    private readonly List<Collider2D> disabledPlayerColliders = new();
    
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
            playerColliders = playerObject.GetComponentsInChildren<Collider2D>();

            // Get a MonoBehaviour component to run the coroutine (prefer PlayerMove)
            MonoBehaviour runner = playerMove != null ? playerMove : playerObject.GetComponent<MonoBehaviour>();
            if (runner != null)
            {
                runner.StartCoroutine(PerformDash());
            }
        }
    }
    
    private IEnumerator PerformDash()
    {
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
        
        SetPlayerCollidersEnabled(false);

        try
        {
            if (playerHealth != null)
            {
                playerHealth.isInvincible = true;
            }

            if (trailRenderer != null)
            {
                trailRenderer.emitting = true;
            }

            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = dashDirection * dashingPower;
            }

            // Wait for dash duration
            yield return new WaitForSeconds(dashingTime);
        }
        finally
        {
            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = Vector2.zero;
            }

            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
            }

            if (playerHealth != null)
            {
                playerHealth.isInvincible = false;
            }

            SetPlayerCollidersEnabled(true);
        }
    }

    private void SetPlayerCollidersEnabled(bool enabled)
    {
        if (playerColliders == null) return;

        if (!enabled)
        {
            disabledPlayerColliders.Clear();
            foreach (var col in playerColliders)
            {
                if (col == null || col.isTrigger || !col.enabled) continue;
                col.enabled = false;
                disabledPlayerColliders.Add(col);
            }
        }
        else
        {
            if (disabledPlayerColliders.Count == 0) return;
            foreach (var col in disabledPlayerColliders)
            {
                if (col != null)
                {
                    col.enabled = true;
                }
            }
            disabledPlayerColliders.Clear();
        }
    }
}
