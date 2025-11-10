using UnityEngine;

public class BossAttack : MonoBehaviour
{
    [Header("Beam Attack Settings")]
    public GameObject beamPrefab;
    public float beamOffset = 1f;
    public float rotationSpeed = 45f;
    public float beamAttackDuration = 5f;
    public float beamAttackCooldown = 8f;
    
    [Header("Jump Attack Settings")]
    public GameObject aoePrefab;
    public float jumpHeight = 2f;
    public float jumpDuration = 1f;
    public float fallDuration = 0.5f;
    public float hangTime = 0.3f;
    public float aoeRadius = 3f;
    public float jumpAttackCooldown = 10f;
    
    [Header("Attack Pattern")]
    public bool alternateAttacks = false; // Changed to false for random attacks
    public float globalCooldown = 2f; // New: cooldown between any attacks
    
    [Header("References")]
    public Transform playerTransform;
    
    // Beam attack variables
    private GameObject[] activeBeams;
    private bool isBeamAttacking = false;
    private float beamAttackTimer = 0f;
    private float beamCooldownTimer = 0f;
    
    // Jump attack variables
    private bool isJumpAttacking = false;
    private Vector3 originalScale;
    private Vector3 targetPosition;
    private float jumpTimer = 0f;
    private float jumpCooldownTimer = 0f;
    private GameObject activeAOE;
    private enum JumpState { Idle, JumpingUp, Hanging, FallingDown, Landing }
    private JumpState currentJumpState = JumpState.Idle;
    private bool damageTriggered = false;
    
    // Attack pattern
    private bool lastAttackWasBeam = false;
    private int lastAttackType = 0;
    
    // New: Global cooldown
    private float globalCooldownTimer = 0f;
    private bool isAttacking = false;

    void Start()
    {
        originalScale = transform.localScale;
        
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }

    void Update()
    {
        // Handle beam attack
        if (isBeamAttacking)
        {
            HandleBeamAttack();
        }
        else
        {
            beamCooldownTimer -= Time.deltaTime;
        }
        
        // Handle jump attack
        if (isJumpAttacking)
        {
            HandleJumpAttack();
        }
        else
        {
            jumpCooldownTimer -= Time.deltaTime;
        }
        
        // New: Handle global cooldown and try to start attacks
        if (!isAttacking)
        {
            globalCooldownTimer -= Time.deltaTime;
            
            if (globalCooldownTimer <= 0f)
            {
                TryStartAttack();
            }
        }
    }

    void TryStartAttack()
    {
        if (alternateAttacks)
        {
            // Cycle through attacks
            if (lastAttackType == 0 && beamCooldownTimer <= 0f)
            {
                StartBeamAttack();
                lastAttackType = 1;
            }
            else if (lastAttackType == 1 && jumpCooldownTimer <= 0f)
            {
                StartJumpAttack();
                lastAttackType = 0;
            }
        }
        else
        {
            // Random attack selection
            System.Collections.Generic.List<int> availableAttacks = new System.Collections.Generic.List<int>();
            
            if (beamCooldownTimer <= 0f) availableAttacks.Add(0); // Beam
            if (jumpCooldownTimer <= 0f) availableAttacks.Add(1); // Jump
            
            if (availableAttacks.Count > 0)
            {
                int randomAttack = availableAttacks[Random.Range(0, availableAttacks.Count)];
                
                if (randomAttack == 0)
                {
                    StartBeamAttack();
                }
                else if (randomAttack == 1)
                {
                    StartJumpAttack();
                }
            }
        }
    }

    #region Beam Attack
    
    void StartBeamAttack()
    {
        isBeamAttacking = true;
        isAttacking = true; // New
        beamAttackTimer = beamAttackDuration;
        beamCooldownTimer = beamAttackCooldown;
        
        FireBeams();
    }

    void HandleBeamAttack()
    {
        RotateBeams();
        
        beamAttackTimer -= Time.deltaTime;
        if (beamAttackTimer <= 0f)
        {
            EndBeamAttack();
        }
    }

    void FireBeams()
    {
        activeBeams = new GameObject[4];

        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f;
            Quaternion rot = Quaternion.Euler(0, 0, angle);
            Vector3 offset = rot * Vector3.right * beamOffset;

            activeBeams[i] = Instantiate(beamPrefab, transform.position + offset, rot, transform);
        }
        
        Debug.Log("Boss firing beam attack!");
    }

    void RotateBeams()
    {
        if (activeBeams == null) return;

        float rotationThisFrame = rotationSpeed * Time.deltaTime;
        
        foreach (GameObject beam in activeBeams)
        {
            if (beam != null)
            {
                beam.transform.RotateAround(transform.position, Vector3.forward, rotationThisFrame);
            }
        }
    }

    void EndBeamAttack()
    {
        isBeamAttacking = false;
        isAttacking = false; // New
        globalCooldownTimer = globalCooldown; // New: Start global cooldown
        
        if (activeBeams != null)
        {
            foreach (GameObject beam in activeBeams)
            {
                if (beam != null)
                {
                    Destroy(beam);
                }
            }
            activeBeams = null;
        }
        
        Debug.Log("Beam attack ended!");
    }
    
    #endregion

    #region Jump Attack
    
    void StartJumpAttack()
    {
        if (playerTransform == null) return;
        
        isJumpAttacking = true;
        isAttacking = true; // New
        currentJumpState = JumpState.JumpingUp;
        jumpTimer = 0f;
        jumpCooldownTimer = jumpAttackCooldown;
        
        targetPosition = playerTransform.position;
        
        Debug.Log("Boss starting jump attack!");
    }

    void HandleJumpAttack()
    {
        jumpTimer += Time.deltaTime;
        
        switch (currentJumpState)
        {
            case JumpState.JumpingUp:
                HandleJumpUp();
                break;
                
            case JumpState.Hanging:
                HandleHanging();
                break;
                
            case JumpState.FallingDown:
                HandleFalling();
                break;
                
            case JumpState.Landing:
                HandleLanding();
                break;
        }
    }

    void HandleJumpUp()
    {
        float progress = jumpTimer / jumpDuration;
        
        if (progress >= 1f)
        {
            currentJumpState = JumpState.Hanging;
            jumpTimer = 0f;
            transform.localScale = originalScale * jumpHeight;
            
            ShowAOEWarning();
        }
        else
        {
            float scale = Mathf.Lerp(1f, jumpHeight, progress);
            transform.localScale = originalScale * scale;
        }
    }

    void HandleHanging()
    {
        if (jumpTimer >= hangTime)
        {
            currentJumpState = JumpState.FallingDown;
            jumpTimer = 0f;
            
            transform.position = targetPosition;
        }
    }

    void HandleFalling()
    {
        float progress = jumpTimer / fallDuration;
        float scale = Mathf.Lerp(jumpHeight, 1f, progress);
        transform.localScale = originalScale * scale;

        if (progress >= 1f)
        {
            transform.localScale = originalScale;
            currentJumpState = JumpState.Landing;
            jumpTimer = 0f;
            damageTriggered = false;
        }
    }

    void HandleLanding()
    {
        if (!damageTriggered)
        {
            TriggerAOEDamage();
            damageTriggered = true;
        }

        if (jumpTimer >= 0.2f)
        {
            isJumpAttacking = false;
            isAttacking = false; // New
            globalCooldownTimer = globalCooldown; // New: Start global cooldown
            currentJumpState = JumpState.Idle;
            damageTriggered = false;

            if (activeAOE != null)
            {
                Destroy(activeAOE);
                activeAOE = null;
            }
            Debug.Log("Jump attack ended!");
        }
    }

    void ShowAOEWarning()
    {
        if (aoePrefab != null)
        {
            activeAOE = Instantiate(aoePrefab, targetPosition, Quaternion.identity);
            activeAOE.transform.localScale = Vector3.one * aoeRadius * 2f;
            
            AOECircle aoeCircle = activeAOE.GetComponent<AOECircle>();
            if (aoeCircle != null)
            {
                aoeCircle.SetWarningMode();
            }
        }
    }

    void TriggerAOEDamage()
    {
        if (activeAOE != null)
        {
            AOECircle aoeCircle = activeAOE.GetComponent<AOECircle>();
            if (aoeCircle != null)
            {
                aoeCircle.SetDamageMode();
            }
            Destroy(activeAOE, 0.3f);
        }
        Debug.Log("Boss landed! AOE damage triggered!");
    }
    
    #endregion

    void OnDestroy()
    {
        EndBeamAttack();
        
        if (activeAOE != null)
        {
            Destroy(activeAOE);
        }
    }
}
