using UnityEngine;

public class AbilityHolder : MonoBehaviour
{
    public Ability ability;
    float cooldownTimer;
    float activeTimer;

    enum AbilityState{
        ready,
        active,
        cooldown
    }
    AbilityState state = AbilityState.ready;

    public KeyCode key;

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
                if (key != KeyCode.None && Input.GetKeyDown(key)){
                    TriggerAbility();
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
            state = AbilityState.active;
            activeTimer = ability.activeTime;
        }
    }
    
    // Public method to check if ability is ready
    public bool IsAbilityReady()
    {
        return state == AbilityState.ready;
    }
}
