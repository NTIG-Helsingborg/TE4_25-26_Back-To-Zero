using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CooldownDisplayManager : MonoBehaviour
{
    [SerializeField] private Transform cooldownShowcasePanel;
    [SerializeField] private GameObject cooldownHolderPrefab;
    
    private AbilitySetter abilitySetter;
    private Dictionary<AbilityHolder, GameObject> displays = new Dictionary<AbilityHolder, GameObject>();
    
    void Start()
    {
        abilitySetter = FindFirstObjectByType<AbilitySetter>();
        if (cooldownShowcasePanel == null)
            cooldownShowcasePanel = GameObject.Find("CooldownShowcase")?.transform;
        
        // Ensure cooldownShowcasePanel is a scene object, not a prefab
        if (cooldownShowcasePanel != null && !cooldownShowcasePanel.gameObject.scene.IsValid())
        {
            Debug.LogError("CooldownDisplayManager: cooldownShowcasePanel is a prefab asset, not a scene object! Please assign a scene GameObject.");
            cooldownShowcasePanel = null;
        }
    }
    
    void Update()
    {
        if (abilitySetter == null || cooldownShowcasePanel == null || cooldownHolderPrefab == null) return;
        
        RefreshDisplays();
        UpdateAllDisplays();
    }
    
    private void RefreshDisplays()
    {
        List<AbilityHolder> activeHolders = GetActiveHolders();
        
        foreach (var kvp in displays)
        {
            if (!activeHolders.Contains(kvp.Key) || kvp.Key?.ability == null)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
                displays.Remove(kvp.Key);
            }
        }
        
        foreach (var holder in activeHolders)
        {
            if (holder?.ability != null && !displays.ContainsKey(holder))
            {
                GameObject display = Instantiate(cooldownHolderPrefab, cooldownShowcasePanel);
                SetupDisplay(display, holder);
                displays[holder] = display;
            }
        }
    }
    
    private void SetupDisplay(GameObject display, AbilityHolder holder)
    {
        CooldownDisplayItem item = display.GetComponent<CooldownDisplayItem>();
        if (item == null) item = display.AddComponent<CooldownDisplayItem>();
        
        TMP_Text keybindText = item.GetKeybindText();
        if (keybindText != null)
            keybindText.text = FormatKeybind(holder.key);
        
        // Set ability sprite if available
        if (holder.ability != null && holder.ability.abilitySprite != null)
        {
            Image abilityImage = item.GetAbilityImage();
            if (abilityImage != null)
            {
                abilityImage.sprite = holder.ability.abilitySprite;
                abilityImage.enabled = true;
            }
        }
        
        Transform fillTransform = display.transform.Find("AbilityCooldown");
        if (fillTransform != null)
        {
            Image fillImage = fillTransform.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
        }
    }
    
    private void UpdateAllDisplays()
    {
        foreach (var kvp in displays)
        {
            if (kvp.Key?.ability == null || kvp.Value == null) continue;
            
            AbilityHolder holder = kvp.Key;
            GameObject display = kvp.Value;
            
            Transform fillTransform = display.transform.Find("AbilityCooldown");
            if (fillTransform != null)
            {
                Image fillImage = fillTransform.GetComponent<Image>();
                if (fillImage != null)
                {
                    AbilityHolder.AbilityState state = holder.GetState();
                    float remaining = holder.GetRemainingCooldown();
                    float maxCooldown = holder.ability.cooldownTime;
                    
                    if (maxCooldown > 0)
                    {
                        if (state == AbilityHolder.AbilityState.cooldown)
                            fillImage.fillAmount = 1f - (remaining / maxCooldown);
                        else
                            fillImage.fillAmount = 0f;
                    }
                }
            }
        }
    }
    
    private List<AbilityHolder> GetActiveHolders()
    {
        List<AbilityHolder> holders = new List<AbilityHolder>();
        if (abilitySetter == null) return holders;
        
        for (int i = 0; i < 4; i++)
        {
            AbilityHolder h = abilitySetter.GetAbilityHolder(i);
            if (h?.ability != null) holders.Add(h);
        }
        
        AbilityHolder dash = abilitySetter.GetDashHolder();
        if (dash?.ability != null && !holders.Contains(dash))
            holders.Add(dash);
        
        return holders;
    }
    
    private string FormatKeybind(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Mouse0: return "LMB";
            case KeyCode.Mouse1: return "RMB";
            case KeyCode.Mouse2: return "MMB";
            case KeyCode.Space: return "SPACE";
            case KeyCode.LeftShift:
            case KeyCode.RightShift: return "SHIFT";
            case KeyCode.LeftControl:
            case KeyCode.RightControl: return "CTRL";
            case KeyCode.LeftAlt:
            case KeyCode.RightAlt: return "ALT";
            default:
                string keyString = key.ToString();
                return keyString.Length == 1 ? keyString : keyString;
        }
    }
}
