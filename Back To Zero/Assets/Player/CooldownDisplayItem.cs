using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component for displaying a single ability's cooldown information
/// Attached to each dynamically created Holder GameObject
/// </summary>
public class CooldownDisplayItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image abilityIcon; // Image Background
    [SerializeField] private Image cooldownFillImage; // Overlay for cooldown fill
    [SerializeField] private TMP_Text keybindText; // The KeybindTxt child component
    [SerializeField] private TMP_Text cooldownText; // Countdown text
    
    private AbilityHolder abilityHolder;
    private float maxCooldownTime;
    private InventoryManager inventoryManager;
    
    void Start()
    {
        // Auto-find components if not assigned
        if (abilityIcon == null)
            abilityIcon = GetComponent<Image>();
            
        if (keybindText == null)
        {
            Transform keybindTxtTransform = transform.Find("KeybindTxt");
            if (keybindTxtTransform != null)
                keybindText = keybindTxtTransform.GetComponent<TMP_Text>();
        }
        
        // Find InventoryManager for getting ability icons
        if (inventoryManager == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
                inventoryManager = player.GetComponent<InventoryManager>();
        }
    }
    
    /// <summary>
    /// Initialize this cooldown display with an ability holder
    /// </summary>
    public void Initialize(AbilityHolder holder, Sprite icon, string abilityName, KeyCode keybind)
    {
        abilityHolder = holder;
        maxCooldownTime = holder.ability != null ? holder.ability.cooldownTime : 0f;
        
        // Set ability icon if available
        if (abilityIcon != null && icon != null)
        {
            abilityIcon.sprite = icon;
        }
        else if (abilityIcon != null && icon == null)
        {
            // Try to find icon from ItemSO
            icon = GetAbilityIcon(abilityName);
            if (icon != null)
                abilityIcon.sprite = icon;
        }
        
        // Set keybind text
        if (keybindText != null)
        {
            keybindText.text = FormatKeybind(keybind);
        }
    }
    
    void Update()
    {
        if (abilityHolder == null || abilityHolder.ability == null)
        {
            // Hide if ability is no longer valid
            gameObject.SetActive(false);
            return;
        }
        
        UpdateCooldownDisplay();
    }
    
    private void UpdateCooldownDisplay()
    {
        float remainingCooldown = abilityHolder.GetRemainingCooldown();
        float cooldownProgress = abilityHolder.GetCooldownProgress();
        
        // Update fill image (if using fill amount overlay)
        if (cooldownFillImage != null)
        {
            cooldownFillImage.fillAmount = cooldownProgress;
            cooldownFillImage.gameObject.SetActive(remainingCooldown > 0);
        }
        
        // Update countdown text
        if (cooldownText != null)
        {
            if (remainingCooldown > 0)
            {
                cooldownText.text = remainingCooldown.ToString("F1");
                cooldownText.gameObject.SetActive(true);
            }
            else
            {
                cooldownText.text = "";
                cooldownText.gameObject.SetActive(false);
            }
        }
        
        // Optionally dim the icon when on cooldown
        if (abilityIcon != null)
        {
            Color iconColor = abilityIcon.color;
            if (remainingCooldown > 0)
            {
                iconColor.a = 0.5f; // Dimmed when on cooldown
            }
            else
            {
                iconColor.a = 1f; // Full opacity when ready
            }
            abilityIcon.color = iconColor;
        }
    }
    
    private string FormatKeybind(KeyCode key)
    {
        // Format keybind for display (e.g., "Mouse0" -> "LMB", "Space" -> "SPACE")
        switch (key)
        {
            case KeyCode.Mouse0:
                return "LMB";
            case KeyCode.Mouse1:
                return "RMB";
            case KeyCode.Mouse2:
                return "MMB";
            case KeyCode.Space:
                return "SPACE";
            case KeyCode.LeftShift:
            case KeyCode.RightShift:
                return "SHIFT";
            case KeyCode.LeftControl:
            case KeyCode.RightControl:
                return "CTRL";
            case KeyCode.LeftAlt:
            case KeyCode.RightAlt:
                return "ALT";
            default:
                // For letter keys, return uppercase letter
                string keyString = key.ToString();
                if (keyString.Length == 1)
                    return keyString;
                // For other keys, return as-is (e.g., "E", "Q", "Alpha1")
                return keyString;
        }
    }
    
    private Sprite GetAbilityIcon(string abilityName)
    {
        if (inventoryManager == null || inventoryManager.itemSOs == null)
            return null;
        
        // Find ItemSO for this ability
        foreach (ItemSO itemSO in inventoryManager.itemSOs)
        {
            if (itemSO != null && itemSO.isAbility == 1 && 
                (itemSO.itemName.Equals(abilityName, System.StringComparison.OrdinalIgnoreCase) ||
                 abilityName.Contains(itemSO.itemName, System.StringComparison.OrdinalIgnoreCase) ||
                 itemSO.itemName.Contains(abilityName, System.StringComparison.OrdinalIgnoreCase)))
            {
                return itemSO.itemSprite;
            }
        }
        
        return null;
    }
}

