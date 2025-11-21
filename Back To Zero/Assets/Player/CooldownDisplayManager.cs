using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the dynamic cooldown display system
/// Reads abilities from AbilitySetter and creates/destroys cooldown displays as needed
/// Dash is always displayed (if available)
/// </summary>
public class CooldownDisplayManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The CooldownShowcase panel where cooldown displays will be created")]
    [SerializeField] private Transform cooldownShowcasePanel;
    
    [Tooltip("Prefab for individual cooldown display (Holder GameObject structure). Leave null to use existing Holders.")]
    [SerializeField] private GameObject cooldownItemPrefab;
    
    [Header("Settings")]
    [Tooltip("How often to refresh the display (in seconds). Lower = more responsive but more CPU usage.")]
    [SerializeField] private float refreshInterval = 0.1f;
    
    [Tooltip("Always show Dash ability even if not equipped")]
    [SerializeField] private bool alwaysShowDash = true;
    
    private AbilitySetter abilitySetter;
    private InventoryManager inventoryManager;
    private Dictionary<AbilityHolder, GameObject> activeDisplays = new Dictionary<AbilityHolder, GameObject>();
    private float lastRefreshTime;
    
    void Start()
    {
        // Find AbilitySetter
        abilitySetter = FindFirstObjectByType<AbilitySetter>();
        if (abilitySetter == null)
        {
            Debug.LogError("CooldownDisplayManager: Could not find AbilitySetter!");
            return;
        }
        
        // Find InventoryManager for getting ability icons
        GameObject player = GameObject.Find("Player");
        if (player != null)
            inventoryManager = player.GetComponent<InventoryManager>();
        
        // Find CooldownShowcase panel if not assigned
        if (cooldownShowcasePanel == null)
        {
            GameObject showcase = GameObject.Find("CooldownShowcase");
            if (showcase != null)
                cooldownShowcasePanel = showcase.transform;
        }
        
        if (cooldownShowcasePanel == null)
        {
            Debug.LogError("CooldownDisplayManager: CooldownShowcase panel not found! Please assign it in the inspector.");
            return;
        }
        
        // Clear existing placeholder Holders (Holder1, Holder2, etc.)
        ClearPlaceholderHolders();
        
        // Initial refresh
        RefreshCooldownDisplays();
    }
    
    void Update()
    {
        // Refresh periodically
        if (Time.time - lastRefreshTime >= refreshInterval)
        {
            RefreshCooldownDisplays();
            lastRefreshTime = Time.time;
        }
    }
    
    /// <summary>
    /// Clears the placeholder Holders (Holder1-4) from the prefab
    /// </summary>
    private void ClearPlaceholderHolders()
    {
        if (cooldownShowcasePanel == null) return;
        
        // Find and destroy placeholder Holders
        int clearedCount = 0;
        for (int i = cooldownShowcasePanel.childCount - 1; i >= 0; i--)
        {
            Transform child = cooldownShowcasePanel.GetChild(i);
            if (child.name.StartsWith("Holder"))
            {
                Destroy(child.gameObject);
                clearedCount++;
            }
        }
        
        if (clearedCount > 0)
        {
            Debug.Log($"CooldownDisplayManager: Cleared {clearedCount} placeholder Holder(s). New holders will be created dynamically.");
        }
    }
    
    /// <summary>
    /// Refreshes all cooldown displays based on current active abilities
    /// </summary>
    private void RefreshCooldownDisplays()
    {
        if (abilitySetter == null || cooldownShowcasePanel == null) return;
        
        // Get all active ability holders
        List<AbilityHolder> currentHolders = GetAllActiveAbilityHolders();
        
        // Remove displays for holders that no longer exist or have no ability
        List<AbilityHolder> toRemove = new List<AbilityHolder>();
        foreach (var kvp in activeDisplays)
        {
            if (!currentHolders.Contains(kvp.Key) || kvp.Key == null || kvp.Key.ability == null)
            {
                toRemove.Add(kvp.Key);
            }
        }
        
        foreach (var holder in toRemove)
        {
            RemoveCooldownDisplay(holder);
        }
        
        // Add displays for new holders
        foreach (var holder in currentHolders)
        {
            if (holder != null && holder.ability != null && !activeDisplays.ContainsKey(holder))
            {
                AddCooldownDisplay(holder);
            }
        }
    }
    
    /// <summary>
    /// Gets all active ability holders from AbilitySetter
    /// </summary>
    private List<AbilityHolder> GetAllActiveAbilityHolders()
    {
        List<AbilityHolder> holders = new List<AbilityHolder>();
        
        if (abilitySetter == null) return holders;
        
        // Get slot-based holders (from inventory)
        for (int i = 0; i < 4; i++)
        {
            AbilityHolder holder = abilitySetter.GetAbilityHolder(i);
            if (holder != null && holder.ability != null)
            {
                holders.Add(holder);
            }
        }
        
        // Always add Dash holder if available (forced keybind)
        AbilityHolder dashHolder = abilitySetter.GetDashHolder();
        if (alwaysShowDash && dashHolder != null && dashHolder.ability != null)
        {
            // Only add if not already in list (shouldn't happen, but safety check)
            if (!holders.Contains(dashHolder))
            {
                holders.Add(dashHolder);
            }
        }
        
        // Optionally add other forced keybind holders (Harvest, BloodTransfusion)
        // Uncomment if you want them always visible:
        // AbilityHolder harvestHolder = abilitySetter.GetHarvestHolder();
        // if (harvestHolder != null && harvestHolder.ability != null && !holders.Contains(harvestHolder))
        //     holders.Add(harvestHolder);
        
        // AbilityHolder bloodTransfusionHolder = abilitySetter.GetBloodTransfusionHolder();
        // if (bloodTransfusionHolder != null && bloodTransfusionHolder.ability != null && !holders.Contains(bloodTransfusionHolder))
        //     holders.Add(bloodTransfusionHolder);
        
        return holders;
    }
    
    /// <summary>
    /// Creates a new cooldown display for an ability holder
    /// </summary>
    private void AddCooldownDisplay(AbilityHolder holder)
    {
        if (holder == null || holder.ability == null || cooldownShowcasePanel == null)
            return;
        
        GameObject displayObj;
        
        // Use prefab if available, otherwise create from scratch
        if (cooldownItemPrefab != null)
        {
            displayObj = Instantiate(cooldownItemPrefab, cooldownShowcasePanel);
        }
        else
        {
            // Create Holder structure manually
            displayObj = CreateHolderFromScratch();
        }
        
        // Get or add CooldownDisplayItem component
        CooldownDisplayItem display = displayObj.GetComponent<CooldownDisplayItem>();
        if (display == null)
        {
            display = displayObj.AddComponent<CooldownDisplayItem>();
        }
        
        // Get ability icon
        Sprite icon = GetAbilityIcon(holder.ability);
        string abilityName = holder.ability.name;
        KeyCode keybind = holder.key;
        
        // Initialize the display
        display.Initialize(holder, icon, abilityName, keybind);
        
        // Store reference
        activeDisplays[holder] = displayObj;
        
        Debug.Log($"CooldownDisplayManager: Created display for '{abilityName}' with keybind {keybind}");
    }
    
    /// <summary>
    /// Creates a Holder GameObject structure from scratch (matches prefab structure)
    /// </summary>
    private GameObject CreateHolderFromScratch()
    {
        if (cooldownShowcasePanel == null)
        {
            Debug.LogError("CooldownDisplayManager: Cannot create holder - cooldownShowcasePanel is null!");
            return null;
        }
        
        // Create Holder GameObject
        GameObject holder = new GameObject("Holder");
        holder.transform.SetParent(cooldownShowcasePanel, false);
        
        // Add RectTransform
        RectTransform rectTransform = holder.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.sizeDelta = new Vector2(100, 100);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        
        // Add Image component (red background)
        Image holderImage = holder.AddComponent<Image>();
        if (holderImage != null)
        {
            holderImage.color = new Color(0.8867924f, 0f, 0f, 1f); // Red color from prefab
        }
        
        // Create KeybindTxt child
        GameObject keybindTxt = new GameObject("KeybindTxt");
        keybindTxt.transform.SetParent(holder.transform, false);
        
        RectTransform keybindRect = keybindTxt.AddComponent<RectTransform>();
        keybindRect.anchorMin = new Vector2(0.5f, 0.5f);
        keybindRect.anchorMax = new Vector2(0.5f, 0.5f);
        keybindRect.sizeDelta = new Vector2(200, 50);
        keybindRect.pivot = new Vector2(0.5f, 0.5f);
        keybindRect.anchoredPosition = new Vector2(0, -20);
        
        TextMeshProUGUI keybindText = keybindTxt.AddComponent<TextMeshProUGUI>();
        if (keybindText != null)
        {
            keybindText.text = "A";
            keybindText.fontSize = 36;
            keybindText.alignment = TMPro.TextAlignmentOptions.Center;
            keybindText.color = Color.white;
        }
        
        return holder;
    }
    
    /// <summary>
    /// Removes a cooldown display
    /// </summary>
    private void RemoveCooldownDisplay(AbilityHolder holder)
    {
        if (activeDisplays.TryGetValue(holder, out GameObject displayObj))
        {
            activeDisplays.Remove(holder);
            if (displayObj != null)
            {
                Destroy(displayObj);
            }
        }
    }
    
    /// <summary>
    /// Gets the icon sprite for an ability from ItemSO
    /// </summary>
    private Sprite GetAbilityIcon(Ability ability)
    {
        if (ability == null || inventoryManager == null || inventoryManager.itemSOs == null)
            return null;
        
        string abilityName = ability.name;
        
        // Find ItemSO for this ability
        foreach (ItemSO itemSO in inventoryManager.itemSOs)
        {
            if (itemSO != null && itemSO.isAbility == 1)
            {
                // Try various matching strategies
                if (itemSO.itemName.Equals(abilityName, System.StringComparison.OrdinalIgnoreCase) ||
                    abilityName.Contains(itemSO.itemName, System.StringComparison.OrdinalIgnoreCase) ||
                    itemSO.itemName.Contains(abilityName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return itemSO.itemSprite;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Public method to force a refresh (useful when abilities change)
    /// </summary>
    public void ForceRefresh()
    {
        RefreshCooldownDisplays();
    }
}

