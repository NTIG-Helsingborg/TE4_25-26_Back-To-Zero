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
    void Awake()
    {
        // Ensure key starts as None - AbilitySetter will set it
        key = KeyCode.None;
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
                    if (animator) animator.SetBool(IsAttackingParam, false);
                    state = AbilityState.cooldown;
                    cooldownTimer = ability.cooldownTime;
                }
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
