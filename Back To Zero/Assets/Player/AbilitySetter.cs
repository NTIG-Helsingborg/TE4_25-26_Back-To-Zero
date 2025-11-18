using UnityEngine;
using System.Collections.Generic;

public class AbilitySetter : MonoBehaviour
{
    [Header("Ability References")]
    [Tooltip("Array of all Ability ScriptableObjects. Auto-loaded from Ability's folder via editor script.")]
    [SerializeField] private Ability[] allAbilities;
    
    [Header("Slot References")]
    [Tooltip("Reference to InventoryManager to access active slots. Will auto-find if not assigned.")]
    [SerializeField] private InventoryManager inventoryManager;
    
    [Header("Slot Keybinds")]
    [Tooltip("Keybinds for each slot. These override the ability's own keybind.")]
    [SerializeField] private KeyCode[] slotKeybinds = new KeyCode[4] { KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.E, KeyCode.Q };
    
    [Header("Default Abilities")]
    [Tooltip("Default abilities to auto-equip if slots are empty. Leave empty to disable auto-equip.")]
    [SerializeField] private string[] defaultAbilityNames = new string[4] { "Bloodknife", "Harvest", "BloodSpear", "BloodExplosion" };
    
    [Tooltip("Enable auto-equip of default abilities on start")]
    [SerializeField] private bool autoEquipDefaults = true;
    
    // Internal ability holders for each slot
    private AbilityHolder[] abilityHolders = new AbilityHolder[4];
    
    // Mapping from item name to Ability ScriptableObject
    private Dictionary<string, Ability> abilityMap = new Dictionary<string, Ability>();
    
    // Track current equipped abilities
    private ActiveSlot[] activeSlots;
    
    // Track previous slot states to detect changes
    private string[] previousSlotItemNames = new string[3];
    
    void Start()
    {
        // Find InventoryManager if not assigned
        if (inventoryManager == null)
        {
            inventoryManager = GetComponent<InventoryManager>();
            if (inventoryManager == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    inventoryManager = player.GetComponent<InventoryManager>();
                }
            }
            if (inventoryManager == null)
            {
                inventoryManager = FindObjectOfType<InventoryManager>();
            }
        }
        
        if (inventoryManager == null)
        {
            Debug.LogError("AbilitySetter: Could not find InventoryManager!");
            return;
        }
        
        // Build ability mapping dictionary
        BuildAbilityMap();
        
        // Create ability holders for each slot (4 slots now)
        for (int i = 0; i < abilityHolders.Length; i++)
        {
            GameObject holderObj = new GameObject($"AbilityHolder_{i}");
            holderObj.transform.SetParent(transform);
            abilityHolders[i] = holderObj.AddComponent<AbilityHolder>();
        }
        
        // Ensure slot keybinds array is initialized
        if (slotKeybinds == null || slotKeybinds.Length != 4)
        {
            slotKeybinds = new KeyCode[4] { KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.E, KeyCode.Q };
        }
        
        // Ensure default ability names array is initialized
        if (defaultAbilityNames == null || defaultAbilityNames.Length != 4)
        {
            defaultAbilityNames = new string[4] { "Bloodknife", "Harvest", "BloodSpear", "BloodExplosion" };
        }
        
        // Get active slots from InventoryManager
        RefreshActiveSlots();
        
        // Auto-equip default abilities if enabled and slots are empty
        if (autoEquipDefaults)
        {
            EquipDefaultAbilities();
        }
        
        // Initial update
        UpdateEquippedAbilities();
    }
    
    void Update()
    {
        // Refresh active slots reference if needed
        if (activeSlots == null || activeSlots.Length < 3)
        {
            RefreshActiveSlots();
        }
        
        // Only proceed if we have valid slots
        if (activeSlots == null || activeSlots.Length < 3)
        {
            return;
        }
        
        // Check for slot changes
        if (HasSlotsChanged())
        {
            UpdateEquippedAbilities();
        }
        
        // Defensive: Ensure keys are still correct (in case something else changed them)
        VerifyKeybinds();
    }
    
    /// <summary>
    /// Verifies that all ability holders have the correct keybinds set
    /// </summary>
    private void VerifyKeybinds()
    {
        if (slotKeybinds == null || slotKeybinds.Length < 4)
            return;
            
        for (int i = 0; i < 4 && i < abilityHolders.Length; i++)
        {
            if (abilityHolders[i] != null && abilityHolders[i].ability != null)
            {
                KeyCode expectedKey = slotKeybinds[i];
                if (abilityHolders[i].key != expectedKey)
                {
                    Debug.LogWarning($"AbilitySetter: Key mismatch detected in slot {i}! Expected {expectedKey}, got {abilityHolders[i].key}. Fixing...");
                    abilityHolders[i].key = expectedKey;
                }
            }
        }
    }
    
    /// <summary>
    /// Builds a dictionary mapping item names to Ability ScriptableObjects
    /// </summary>
    private void BuildAbilityMap()
    {
        abilityMap.Clear();
        
        if (allAbilities == null || allAbilities.Length == 0)
        {
            Debug.LogWarning("AbilitySetter: No abilities assigned! Use the editor script to auto-load from Ability's folder.");
            return;
        }
        
        foreach (Ability ability in allAbilities)
        {
            if (ability == null)
                continue;
            
            // Get ability name - use name field, asset name, or class name
            string abilityName = GetAbilityName(ability);
            
            if (!string.IsNullOrEmpty(abilityName))
            {
                if (abilityMap.ContainsKey(abilityName))
                {
                    Debug.LogWarning($"AbilitySetter: Duplicate ability name '{abilityName}'. Using first occurrence.");
                }
                else
                {
                    abilityMap[abilityName] = ability;
                    Debug.Log($"AbilitySetter: Mapped '{abilityName}' (ability keybind: {ability.keybind})");
                }
            }
        }
        
        Debug.Log($"AbilitySetter: Built ability map with {abilityMap.Count} abilities.");
    }
    
    /// <summary>
    /// Gets the name for an ability (name field, asset name, or class name)
    /// </summary>
    private string GetAbilityName(Ability ability)
    {
        // Try name field first
        if (!string.IsNullOrEmpty(ability.name))
        {
            return ability.name;
        }
        
        // Try Unity asset name
        string assetName = (ability as UnityEngine.Object).name;
        if (!string.IsNullOrEmpty(assetName))
        {
            return assetName;
        }
        
        // Fallback to class name
        string className = ability.GetType().Name;
        if (className.EndsWith("Ability"))
        {
            className = className.Substring(0, className.Length - 7);
        }
        return className;
    }
    
    /// <summary>
    /// Refreshes the reference to active slots from InventoryManager
    /// </summary>
    private void RefreshActiveSlots()
    {
        if (inventoryManager != null)
        {
            activeSlots = inventoryManager.GetActiveSlots();
            
            if (activeSlots == null)
            {
                Debug.LogWarning("AbilitySetter: Could not get active slots from InventoryManager.");
                return;
            }
            
            if (activeSlots.Length < 4)
            {
                Debug.LogWarning($"AbilitySetter: Only found {activeSlots.Length} active slots. Need 4 for full functionality.");
            }
            else if (activeSlots.Length > 4)
            {
                // Use only first 4 slots
                ActiveSlot[] firstFour = new ActiveSlot[4];
                System.Array.Copy(activeSlots, firstFour, 4);
                activeSlots = firstFour;
            }
        }
    }
    
    /// <summary>
    /// Checks if any active slots have changed
    /// </summary>
    private bool HasSlotsChanged()
    {
        if (activeSlots == null || activeSlots.Length < 3)
            return false;
        
        int slotsToCheck = Mathf.Min(4, activeSlots.Length);
        for (int i = 0; i < slotsToCheck; i++)
        {
            string currentName = activeSlots[i] != null && !string.IsNullOrEmpty(activeSlots[i].itemName) 
                ? activeSlots[i].itemName 
                : string.Empty;
            
            if (currentName != previousSlotItemNames[i])
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Auto-equips default abilities if slots are empty
    /// </summary>
    private void EquipDefaultAbilities()
    {
        if (activeSlots == null || activeSlots.Length < 4)
        {
            Debug.LogWarning("AbilitySetter: Cannot auto-equip defaults - need at least 4 active slots.");
            return;
        }
        
        if (defaultAbilityNames == null || defaultAbilityNames.Length != 4)
        {
            return;
        }
        
        bool anyEquipped = false;
        for (int i = 0; i < 4 && i < activeSlots.Length; i++)
        {
            if (activeSlots[i] != null && string.IsNullOrEmpty(activeSlots[i].itemName))
            {
                string defaultAbilityName = defaultAbilityNames[i];
                if (!string.IsNullOrEmpty(defaultAbilityName))
                {
                    // Find the ability
                    Ability ability = FindAbilityByName(defaultAbilityName);
                    if (ability != null)
                    {
                        // Find the ItemSO for this ability to add to inventory first
                        ItemSO itemSO = FindItemSOForAbility(defaultAbilityName);
                        if (itemSO != null && inventoryManager != null)
                        {
                            // Add item to inventory first
                            int leftover = inventoryManager.AddItem(itemSO.itemName, itemSO.itemSprite, 1, itemSO.itemDescription, 2);
                            if (leftover == 0)
                            {
                                // Then equip it to the active slot
                                activeSlots[i].AddItem(itemSO.itemName, itemSO.itemSprite, 1, itemSO.itemDescription);
                                anyEquipped = true;
                                Debug.Log($"AbilitySetter: Auto-equipped '{defaultAbilityName}' to slot {i}");
                            }
                        }
                        else
                        {
                            // If no ItemSO found, try to equip directly by name
                            activeSlots[i].AddItem(defaultAbilityName, null, 1, "");
                            anyEquipped = true;
                            Debug.Log($"AbilitySetter: Auto-equipped '{defaultAbilityName}' to slot {i} (no ItemSO found)");
                        }
                    }
                }
            }
        }
        
        if (anyEquipped)
        {
            Debug.Log("AbilitySetter: Auto-equipped default abilities.");
        }
    }
    
    /// <summary>
    /// Finds ItemSO for an ability name
    /// </summary>
    private ItemSO FindItemSOForAbility(string abilityName)
    {
        if (inventoryManager == null || inventoryManager.itemSOs == null)
            return null;
        
        foreach (ItemSO itemSO in inventoryManager.itemSOs)
        {
            if (itemSO != null && itemSO.isAbility == 1 && 
                (itemSO.itemName.Equals(abilityName, System.StringComparison.OrdinalIgnoreCase) ||
                 abilityName.Contains(itemSO.itemName, System.StringComparison.OrdinalIgnoreCase) ||
                 itemSO.itemName.Contains(abilityName, System.StringComparison.OrdinalIgnoreCase)))
            {
                return itemSO;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Updates equipped abilities based on active slots
    /// </summary>
    private void UpdateEquippedAbilities()
    {
        if (activeSlots == null || activeSlots.Length < 3)
        {
            return;
        }
        
        int slotsToProcess = Mathf.Min(4, activeSlots.Length);
        for (int i = 0; i < slotsToProcess; i++)
        {
            if (activeSlots[i] == null || abilityHolders[i] == null)
            {
                if (abilityHolders[i] != null)
                {
                    abilityHolders[i].ability = null;
                    abilityHolders[i].key = KeyCode.None;
                }
                previousSlotItemNames[i] = string.Empty;
                continue;
            }
            
            string itemName = activeSlots[i].itemName;
            previousSlotItemNames[i] = itemName;
            
            // Clear ability if slot is empty
            if (string.IsNullOrEmpty(itemName))
            {
                abilityHolders[i].ability = null;
                abilityHolders[i].key = KeyCode.None;
                continue;
            }
            
            // Find ability by name (try exact, case-insensitive, then partial match)
            Ability ability = FindAbilityByName(itemName);
            
            if (ability != null)
            {
                // IMPORTANT: Set key FIRST before assigning ability, to prevent any potential override
                KeyCode slotKeybind = slotKeybinds[i];
                abilityHolders[i].key = slotKeybind;
                
                // Then assign ability to holder
                abilityHolders[i].ability = ability;
                
                // Verify key is still correct (defensive check)
                if (abilityHolders[i].key != slotKeybind)
                {
                    Debug.LogWarning($"AbilitySetter: Key was changed! Re-setting slot {i} keybind from {abilityHolders[i].key} to {slotKeybind}");
                    abilityHolders[i].key = slotKeybind;
                }
                
                Debug.Log($"AbilitySetter: Slot {i} - Equipped '{itemName}' with keybind {slotKeybind} (ability's keybind {ability.keybind} was overridden). Current holder key: {abilityHolders[i].key}");
            }
            else
            {
                Debug.LogWarning($"AbilitySetter: Could not find ability for '{itemName}' in slot {i}. Available: {string.Join(", ", abilityMap.Keys)}");
                abilityHolders[i].ability = null;
                abilityHolders[i].key = KeyCode.None;
            }
        }
    }
    
    /// <summary>
    /// Finds an ability by name with flexible matching
    /// </summary>
    private Ability FindAbilityByName(string itemName)
    {
        // Try exact match
        if (abilityMap.TryGetValue(itemName, out Ability ability))
        {
            return ability;
        }
        
        // Try case-insensitive match
        foreach (var kvp in abilityMap)
        {
            if (string.Equals(kvp.Key, itemName, System.StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }
        
        // Try partial match
        foreach (var kvp in abilityMap)
        {
            if (kvp.Key.Contains(itemName, System.StringComparison.OrdinalIgnoreCase) || 
                itemName.Contains(kvp.Key, System.StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Public method to manually refresh abilities
    /// </summary>
    public void RefreshAbilities()
    {
        RefreshActiveSlots();
        UpdateEquippedAbilities();
    }
    
    /// <summary>
    /// Gets the ability holder for a specific slot index
    /// </summary>
    public AbilityHolder GetAbilityHolder(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < abilityHolders.Length)
        {
            return abilityHolders[slotIndex];
        }
        return null;
    }
}
