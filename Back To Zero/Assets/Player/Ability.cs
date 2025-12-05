using UnityEngine;

public class Ability : ScriptableObject
{
    public new string name;
    public Sprite abilitySprite;
    public float cooldownTime;
    public float activeTime;
    
    public virtual bool CanActivate()
    {
        // Check if player is alive before allowing any ability activation
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var health = player.GetComponent<Health>();
            if (health != null && health.GetCurrentHealth() <= 0)
            {
                return false; // Player is dead, cannot activate
            }
        }
        return true;
    }
    
    public virtual void Activate(){

    }
}
