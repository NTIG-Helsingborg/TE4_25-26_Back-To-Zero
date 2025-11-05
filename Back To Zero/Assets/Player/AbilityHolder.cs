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

    void Update()
    {
        switch (state){
            case AbilityState.ready:
                if (Input.GetKeyDown(key)){
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
