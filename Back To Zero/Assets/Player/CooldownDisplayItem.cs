using UnityEngine;
using UnityEngine.UI;

public class CooldownDisplayItem : MonoBehaviour
{
    private Image image;
    private AbilityHolder holder;
    private float maxCooldown;
    private bool isDash;
    
    void Start()
    {
        image = GetComponent<Image>();
    }
    
    public void Initialize(AbilityHolder h)
    {
        holder = h;
        maxCooldown = h.ability?.cooldownTime ?? 0f;
        isDash = h.ability?.name.Equals("Dash", System.StringComparison.OrdinalIgnoreCase) ?? false;
        
        HideDisplay();
    }
    
    void Update()
    {
        if (holder?.ability == null)
        {
            HideDisplay();
            return;
        }
        
        AbilityHolder.AbilityState state = holder.GetState();
        float remaining = holder.GetRemainingCooldown();
        
        bool shouldShow = state == AbilityHolder.AbilityState.active || state == AbilityHolder.AbilityState.cooldown;
        
        if (shouldShow)
        {
            ShowDisplay();
            if (image != null && maxCooldown > 0)
            {
                if (state == AbilityHolder.AbilityState.cooldown)
                {
                    image.fillAmount = 1f - (remaining / maxCooldown);
                }
                else
                {
                    image.fillAmount = 0f;
                }
            }
        }
        else
        {
            HideDisplay();
        }
    }
    
    private void ShowDisplay()
    {
        if (image != null)
        {
            Color c = image.color;
            c.a = 1f;
            image.color = c;
        }
    }
    
    private void HideDisplay()
    {
        if (image != null)
        {
            Color c = image.color;
            c.a = 0f;
            image.color = c;
        }
    }
}

