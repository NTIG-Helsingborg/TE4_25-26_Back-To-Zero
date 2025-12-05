using UnityEngine;

public class AbilityHolder : MonoBehaviour
{
    public Ability ability;
    float cooldownTimer;
    float activeTimer;

    public enum AbilityState{
        ready,
        active,
        cooldown
    }
    AbilityState state = AbilityState.ready;

    public KeyCode key;
    [SerializeField] private Animator animator;
    private const string IsAttackingParam = "IsAttacking";
    private Health playerHealth;
    
    void Awake()
    {
        key = KeyCode.None;
        playerHealth = GetComponent<Health>();
    }

    // Allow AbilitySetter to inject the player's Animator
    public void SetAnimator(Animator anim)
    {
        animator = anim;
    }

    void Update()
    {
        // Don't process input if game is paused (e.g., inventory is open)
        if (Time.timeScale == 0f)
            return;
        
        // Don't process if no ability assigned
        if (ability == null)
            return;
            
        switch (state){
            case AbilityState.ready:
                if (Input.GetKeyDown(key)){
                    // Don't allow ability use if player is dead (0 or less HP)
                    if (playerHealth != null && playerHealth.GetCurrentHealth() <= 0)
                        break;
                        
                    if (ability != null && ability.CanActivate())
                    {
                        TriggerAbility();
                        
                    }
                    
                }
            break;
            case AbilityState.active:
                if (activeTimer > 0){
                    activeTimer -= Time.deltaTime;
                    
                }
                else{
                    state = AbilityState.cooldown;
                    cooldownTimer = ability.cooldownTime;
                }
                if (animator) animator.SetBool(IsAttackingParam, false);
            break;
            case AbilityState.cooldown:
                if (cooldownTimer > 0){
                    cooldownTimer -= Time.deltaTime;
                }
                else{
                    state = AbilityState.ready;
                }
            break;
        }
    }
    
    // Public method to manually trigger the ability
    public void TriggerAbility()
    {
        // Don't allow ability use if player is dead (0 or less HP)
        if (playerHealth != null && playerHealth.GetCurrentHealth() <= 0)
            return;
            
        if (state == AbilityState.ready && ability != null)
        {
            ability.Activate();
            if (animator) animator.SetBool(IsAttackingParam, true);
            state = AbilityState.active;
            activeTimer = ability.activeTime;
        }
    }
    
    // Public method to check if ability is ready
    public bool IsAbilityReady()
    {
        return state == AbilityState.ready;
    }
    
    // Public methods for cooldown display system
    public float GetRemainingCooldown()
    {
        if (state == AbilityState.cooldown)
            return cooldownTimer;
        return 0f;
    }
    
    public float GetCooldownProgress()
    {
        if (ability == null || ability.cooldownTime <= 0)
            return 0f;
        
        if (state == AbilityState.cooldown)
            return cooldownTimer / ability.cooldownTime;
        
        return 0f;
    }
    
    public bool IsOnCooldown()
    {
        return state == AbilityState.cooldown;
    }
    
    public AbilityState GetState()
    {
        return state;
    }
}
