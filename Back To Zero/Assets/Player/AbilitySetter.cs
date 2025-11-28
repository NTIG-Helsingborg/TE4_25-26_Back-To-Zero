using UnityEngine;
using System.Collections;
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
    
    [Header("Forced Ability Keybinds")]
    [Tooltip("These abilities will always use their designated keybinds, regardless of which slot they're in.")]
    [SerializeField] private string harvestAbilityName = "Harvest";
    [SerializeField] private KeyCode harvestKeybind = KeyCode.Mouse1;
    [SerializeField] private string dashAbilityName = "Dash";
    [SerializeField] private KeyCode dashKeybind = KeyCode.Space;
    [SerializeField] private string bloodTransfusionAbilityName = "BloodTransfusion";
    [SerializeField] private KeyCode bloodTransfusionKeybind = KeyCode.X;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    // Internal ability holders for each slot
    private AbilityHolder[] abilityHolders = new AbilityHolder[4];
    
    // Dedicated ability holders for forced keybinds (always active, regardless of slots)
    private AbilityHolder harvestHolder = null;
    private AbilityHolder dashHolder = null;
    private AbilityHolder bloodTransfusionHolder = null;
    
    // Mapping from item name to Ability ScriptableObject
    private Dictionary<string, Ability> abilityMap = new Dictionary<string, Ability>();
    
    // Track current equipped abilities
    private ActiveSlot[] activeSlots;
    
    // Track previous slot states to detect changes
    private string[] previousSlotItemNames = new string[3];
    
    void Start()
    {
        Debug.Log("AbilitySetter: Start() called.");
        
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
                inventoryManager = FindFirstObjectByType<InventoryManager>();
            }
        }
        
        if (inventoryManager == null)
        {
            Debug.LogError("AbilitySetter: Could not find InventoryManager!");
            return;
        }

        // Acquire player Animator (component is directly on the player object, not a child)
        if (animator == null)
        {
            // Try on this GameObject first
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                // Try tagged Player object
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    animator = player.GetComponent<Animator>();
            }
            if (animator == null)
            {
                Debug.LogWarning("AbilitySetter: Player Animator not found. Spell holders will have no animator.");
            }
        }

        BuildAbilityMap();
        CreateForcedAbilityHolders(); // forced holders get NO animator

        // Create ability holders for spell slots (assign animator)
        for (int i = 0; i < abilityHolders.Length; i++)
        {
            GameObject holderObj = new GameObject($"AbilityHolder_{i}");
            holderObj.transform.SetParent(transform);
            abilityHolders[i] = holderObj.AddComponent<AbilityHolder>();
            if (animator != null)
                abilityHolders[i].SetAnimator(animator);
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
        
        // Delay auto-equip to ensure InventoryManager has finished initializing
        // Use coroutine to wait a frame so InventoryManager.Start() completes first
        StartCoroutine(DelayedStart());
    }
    
    private System.Collections.IEnumerator DelayedStart()
    {
        // Wait one frame to ensure InventoryManager.Start() has completed
        yield return null;
        
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
    /// Public method to force AbilitySetter to refresh and update ability assignments
    /// Call this when abilities are manually assigned to active slots
    /// </summary>
    public void RefreshAbilityAssignments()
    {
        Debug.Log("AbilitySetter: RefreshAbilityAssignments() called.");
        RefreshActiveSlots();
        UpdateEquippedAbilities();
    }
    
    /// <summary>
    /// Verifies that all ability holders have the correct keybinds set
    /// </summary>
    private void VerifyKeybinds()
    {
        if (activeSlots == null || activeSlots.Length < 4)
            return;
            
        for (int i = 0; i < 4 && i < abilityHolders.Length && i < activeSlots.Length; i++)
        {
            if (abilityHolders[i] != null && abilityHolders[i].ability != null && activeSlots[i] != null)
            {
                // Get expected keybind (with special overrides for Harvest, Dash, and BloodTransfusion)
                KeyCode expectedKey = GetExpectedKeybindForAbility(abilityHolders[i].ability, activeSlots[i]);
                
                if (abilityHolders[i].key != expectedKey)
                {
                    Debug.LogWarning($"AbilitySetter: Key mismatch detected in slot {i}! Expected {expectedKey}, got {abilityHolders[i].key}. Fixing...");
                    abilityHolders[i].key = expectedKey;
                }
            }
        }
    }
    
    /// <summary>
    /// Gets the expected keybind for an ability, with special overrides for Harvest, Dash, and BloodTransfusion
    /// Note: Harvest, Dash, and BloodTransfusion should not be in slots, they have dedicated holders
    /// </summary>
    private KeyCode GetExpectedKeybindForAbility(Ability ability, ActiveSlot slot)
    {
        if (ability == null)
            return KeyCode.None;
            
        string abilityName = GetAbilityName(ability);
        
        // Special keybind overrides (though these shouldn't be in slots)
        if (abilityName.Equals(harvestAbilityName, System.StringComparison.OrdinalIgnoreCase))
        {
            return harvestKeybind; // Right click
        }
        else if (abilityName.Equals(dashAbilityName, System.StringComparison.OrdinalIgnoreCase))
        {
            return dashKeybind; // Space bar
        }
        else if (abilityName.Equals(bloodTransfusionAbilityName, System.StringComparison.OrdinalIgnoreCase))
        {
            return bloodTransfusionKeybind; // X key
        }
        
        // Default: get from SlotNr
        return GetKeybindFromSlotNr(slot);
    }
    
    /// <summary>
    /// Gets the KeyCode from the SlotNr text in an ActiveSlot
    /// </summary>
    private KeyCode GetKeybindFromSlotNr(ActiveSlot slot)
    {
        if (slot == null)
        {
            Debug.LogWarning("AbilitySetter: GetKeybindFromSlotNr called with null slot!");
            // Fallback to default keybind if slot is null
            return slotKeybinds != null && slotKeybinds.Length > 0 ? slotKeybinds[0] : KeyCode.None;
        }
        
        string slotNrText = slot.GetSlotNrText();
        Debug.Log($"AbilitySetter: GetKeybindFromSlotNr for slot '{slot.gameObject.name}', SlotNr text: '{slotNrText}'");
        
        if (string.IsNullOrEmpty(slotNrText))
        {
            // Fallback to default keybind if SlotNr is empty
            int slotIndex = System.Array.IndexOf(activeSlots, slot);
            Debug.LogWarning($"AbilitySetter: SlotNr text is empty for slot '{slot.gameObject.name}' (index {slotIndex}). Using fallback keybind.");
            if (slotIndex >= 0 && slotIndex < slotKeybinds.Length)
            {
                return slotKeybinds[slotIndex];
            }
            return KeyCode.None;
        }
        
        // Parse the text to KeyCode
        KeyCode parsedKey = ParseKeyCodeFromText(slotNrText);
        if (parsedKey != KeyCode.None)
        {
            Debug.Log($"AbilitySetter: Successfully parsed '{slotNrText}' to KeyCode: {parsedKey}");
            return parsedKey;
        }
        
        // Fallback to default keybind if parsing failed
        int fallbackIndex = System.Array.IndexOf(activeSlots, slot);
        if (fallbackIndex >= 0 && fallbackIndex < slotKeybinds.Length)
        {
            Debug.LogWarning($"AbilitySetter: Could not parse SlotNr text '{slotNrText}' to KeyCode for slot '{slot.gameObject.name}' (index {fallbackIndex}). Using fallback keybind {slotKeybinds[fallbackIndex]}.");
            return slotKeybinds[fallbackIndex];
        }
        
        Debug.LogError($"AbilitySetter: Could not determine keybind for slot '{slot.gameObject.name}' - SlotNr parsing failed and slot index {fallbackIndex} is out of range!");
        return KeyCode.None;
    }
    
    /// <summary>
    /// Parses text string to KeyCode (handles numbers, letters, mouse buttons, etc.)
    /// </summary>
    private KeyCode ParseKeyCodeFromText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return KeyCode.None;
        
        // Trim whitespace
        text = text.Trim();
        
        // Try to parse as number (1-9, 0)
        if (int.TryParse(text, out int number))
        {
            if (number >= 0 && number <= 9)
            {
                // Map to Alpha keys: 0 = Alpha0, 1 = Alpha1, etc.
                return (KeyCode)((int)KeyCode.Alpha0 + number);
            }
        }
        
        // Try to parse as single letter (A-Z)
        if (text.Length == 1 && char.IsLetter(text[0]))
        {
            char upperChar = char.ToUpper(text[0]);
            string keyName = upperChar.ToString();
            
            // Try direct KeyCode parse
            if (System.Enum.TryParse<KeyCode>(keyName, out KeyCode key))
            {
                return key;
            }
        }
        
        // Try common key names (case-insensitive)
        string lowerText = text.ToLower();
        
        // Mouse buttons - support various formats
        if (lowerText == "mouse0" || lowerText == "lmb" || lowerText == "left mouse" || 
            lowerText == "leftclick" || lowerText == "left click" || lowerText == "leftmousebutton")
            return KeyCode.Mouse0;
        if (lowerText == "mouse1" || lowerText == "rmb" || lowerText == "right mouse" || 
            lowerText == "rightclick" || lowerText == "right click" || lowerText == "rightmousebutton")
            return KeyCode.Mouse1;
        if (lowerText == "mouse2" || lowerText == "mmb" || lowerText == "middle mouse" || 
            lowerText == "middleclick" || lowerText == "middle click" || lowerText == "middlemousebutton")
            return KeyCode.Mouse2;
        
        // Common keys
        if (lowerText == "space")
            return KeyCode.Space;
        if (lowerText == "shift")
            return KeyCode.LeftShift;
        if (lowerText == "ctrl" || lowerText == "control")
            return KeyCode.LeftControl;
        if (lowerText == "alt")
            return KeyCode.LeftAlt;
        if (lowerText == "tab")
            return KeyCode.Tab;
        if (lowerText == "enter" || lowerText == "return")
            return KeyCode.Return;
        if (lowerText == "escape" || lowerText == "esc")
            return KeyCode.Escape;
        
        // Try direct KeyCode enum parse (case-insensitive)
        if (System.Enum.TryParse<KeyCode>(text, true, out KeyCode parsedKey))
        {
            Debug.Log($"AbilitySetter: Parsed '{text}' to KeyCode {parsedKey} via direct enum parse.");
            return parsedKey;
        }
        
        Debug.LogWarning($"AbilitySetter: Could not parse '{text}' to KeyCode. Tried: number, letter, common names, and direct enum parse.");
        return KeyCode.None;
    }
    
    /// <summary>
    /// Creates dedicated AbilityHolders for forced keybind abilities (Harvest, Dash, and BloodTransfusion)
    /// These are always active regardless of whether they're equipped in slots
    /// </summary>
    private void CreateForcedAbilityHolders()
    {
        // Create Harvest holder
        GameObject harvestHolderObj = new GameObject("HarvestHolder");
        harvestHolderObj.transform.SetParent(transform);
        harvestHolder = harvestHolderObj.AddComponent<AbilityHolder>();
        harvestHolder.key = harvestKeybind;
        
        // Find and assign Harvest ability
        Ability harvestAbility = FindAbilityByName(harvestAbilityName);
        if (harvestAbility != null)
        {
            harvestHolder.ability = harvestAbility;
            Debug.Log($"AbilitySetter: Created dedicated Harvest holder with keybind {harvestKeybind}");
        }
        else
        {
            Debug.LogWarning($"AbilitySetter: Could not find '{harvestAbilityName}' ability for forced keybind holder.");
        }
        
        // Create Dash holder
        GameObject dashHolderObj = new GameObject("DashHolder");
        dashHolderObj.transform.SetParent(transform);
        dashHolder = dashHolderObj.AddComponent<AbilityHolder>();
        dashHolder.key = dashKeybind;
        
        // Find and assign Dash ability
        Ability dashAbility = FindAbilityByName(dashAbilityName);
        if (dashAbility != null)
        {
            dashHolder.ability = dashAbility;
            Debug.Log($"AbilitySetter: Created dedicated Dash holder with keybind {dashKeybind}");
        }
        else
        {
            Debug.LogWarning($"AbilitySetter: Could not find '{dashAbilityName}' ability for forced keybind holder.");
        }
        
        // Create BloodTransfusion holder
        GameObject bloodTransfusionHolderObj = new GameObject("BloodTransfusionHolder");
        bloodTransfusionHolderObj.transform.SetParent(transform);
        bloodTransfusionHolder = bloodTransfusionHolderObj.AddComponent<AbilityHolder>();
        bloodTransfusionHolder.key = bloodTransfusionKeybind;
        
        // Find and assign BloodTransfusion ability
        Ability bloodTransfusionAbility = FindAbilityByName(bloodTransfusionAbilityName);
        if (bloodTransfusionAbility != null)
        {
            bloodTransfusionHolder.ability = bloodTransfusionAbility;
            Debug.Log($"AbilitySetter: Created dedicated BloodTransfusion holder with keybind {bloodTransfusionKeybind}");
        }
        else
        {
            Debug.LogWarning($"AbilitySetter: Could not find '{bloodTransfusionAbilityName}' ability for forced keybind holder.");
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
            Debug.LogError("AbilitySetter: No abilities assigned in 'allAbilities' array! " +
                          "Please assign Ability ScriptableObjects in the inspector, or use an editor script to auto-load from the Ability's folder.");
            return;
        }
        
        int nullCount = 0;
        foreach (Ability ability in allAbilities)
        {
            if (ability == null)
            {
                nullCount++;
                continue;
            }
            
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
                    Debug.Log($"AbilitySetter: Mapped '{abilityName}'");
                }
            }
            else
            {
                Debug.LogWarning($"AbilitySetter: Could not determine name for ability of type {ability.GetType().Name}");
            }
        }
        
        if (nullCount > 0)
        {
            Debug.LogWarning($"AbilitySetter: Found {nullCount} null entries in allAbilities array. Please check your inspector assignments.");
        }
        
        Debug.Log($"AbilitySetter: Built ability map with {abilityMap.Count} abilities (from {allAbilities.Length} total entries).");
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
            Debug.LogWarning($"AbilitySetter: Cannot auto-equip defaults - need at least 4 active slots. Found: {(activeSlots == null ? "null" : activeSlots.Length.ToString())}");
            return;
        }
        
        if (defaultAbilityNames == null || defaultAbilityNames.Length != 4)
        {
            Debug.LogWarning("AbilitySetter: Default ability names array is not properly initialized.");
            return;
        }
        
        if (abilityMap == null || abilityMap.Count == 0)
        {
            Debug.LogWarning("AbilitySetter: Ability map is empty! Make sure 'allAbilities' array is populated in the inspector.");
            return;
        }
        
        Debug.Log($"AbilitySetter: Attempting to auto-equip {defaultAbilityNames.Length} default abilities. Ability map has {abilityMap.Count} abilities.");
        
        bool anyEquipped = false;
        for (int i = 0; i < 4 && i < activeSlots.Length; i++)
        {
            if (activeSlots[i] != null && string.IsNullOrEmpty(activeSlots[i].itemName))
            {
                string defaultAbilityName = defaultAbilityNames[i];
                if (!string.IsNullOrEmpty(defaultAbilityName))
                {
                    Debug.Log($"AbilitySetter: Processing slot {i} with ability name '{defaultAbilityName}'");
                    
                    // Find the ability
                    Ability ability = FindAbilityByName(defaultAbilityName);
                    if (ability != null)
                    {
                        Debug.Log($"AbilitySetter: Found ability '{defaultAbilityName}' in ability map.");
                        
                        // Find the ItemSO for this ability to add to inventory first
                        ItemSO itemSO = FindItemSOForAbility(defaultAbilityName);
                        if (itemSO != null && inventoryManager != null)
                        {
                            Debug.Log($"AbilitySetter: Found ItemSO '{itemSO.itemName}' for ability '{defaultAbilityName}'");
                            
                            // Add item to inventory first (type 2 = ability)
                            int leftover = inventoryManager.AddItem(itemSO.itemName, itemSO.itemSprite, 1, itemSO.itemDescription, 2);
                            if (leftover == 0)
                            {
                                // Then equip it to the active slot
                                activeSlots[i].AddItem(itemSO.itemName, itemSO.itemSprite, 1, itemSO.itemDescription);
                                anyEquipped = true;
                                Debug.Log($"AbilitySetter: ✓ Auto-equipped '{defaultAbilityName}' to slot {i}");
                            }
                            else
                            {
                                Debug.LogWarning($"AbilitySetter: Could not add '{defaultAbilityName}' to inventory (leftover: {leftover})");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"AbilitySetter: No ItemSO found for '{defaultAbilityName}'. Attempting direct equip...");
                            // If no ItemSO found, try to equip directly by name
                            activeSlots[i].AddItem(defaultAbilityName, null, 1, "");
                            anyEquipped = true;
                            Debug.Log($"AbilitySetter: Auto-equipped '{defaultAbilityName}' to slot {i} (no ItemSO found, direct equip)");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"AbilitySetter: Could not find ability '{defaultAbilityName}' in ability map. Available abilities: {string.Join(", ", abilityMap.Keys)}");
                    }
                }
            }
            else if (activeSlots[i] != null && !string.IsNullOrEmpty(activeSlots[i].itemName))
            {
                Debug.Log($"AbilitySetter: Slot {i} already has '{activeSlots[i].itemName}', skipping.");
            }
        }
        
        if (anyEquipped)
        {
            Debug.Log("AbilitySetter: ✓ Auto-equipped default abilities successfully.");
        }
        else
        {
            Debug.LogWarning("AbilitySetter: No abilities were auto-equipped. Check the console for details.");
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
            Debug.Log($"AbilitySetter: Looking for ability matching itemName '{itemName}' in slot {i}. Ability map has {abilityMap.Count} abilities.");
            if (abilityMap.Count > 0)
            {
                Debug.Log($"AbilitySetter: Available ability names in map: {string.Join(", ", abilityMap.Keys)}");
            }
            
            // First try to find ability by name
            Ability ability = FindAbilityByName(itemName);
            
            // If not found, try to find via ItemSO (since ActiveSlot stores ItemSO names)
            if (ability == null && inventoryManager != null && inventoryManager.itemSOs != null)
            {
                ItemSO matchingItemSO = FindItemSOForAbility(itemName);
                if (matchingItemSO != null)
                {
                    // Try to find ability that matches the ItemSO's name or the ability class name
                    ability = FindAbilityByName(matchingItemSO.itemName);
                    
                    // If still not found, try matching by ability class name patterns
                    if (ability == null)
                    {
                        // Try common patterns: "Bloodknife" -> "BloodKnife", "Blood Knife", etc.
                        string[] variations = new string[]
                        {
                            matchingItemSO.itemName,
                            matchingItemSO.itemName.Replace(" ", ""),
                            matchingItemSO.itemName.Replace("'", ""),
                            matchingItemSO.itemName.Replace("-", ""),
                            // Add space variations
                            matchingItemSO.itemName.Replace("knife", " Knife"),
                            matchingItemSO.itemName.Replace("spear", " Spear"),
                            matchingItemSO.itemName.Replace("explosion", " Explosion"),
                            matchingItemSO.itemName.Replace("slash", " Slash"),
                        };
                        
                        foreach (string variation in variations)
                        {
                            ability = FindAbilityByName(variation);
                            if (ability != null)
                            {
                                Debug.Log($"AbilitySetter: Found ability via ItemSO variation '{variation}' for '{itemName}'");
                                break;
                            }
                        }
                    }
                }
            }
            
            if (ability != null)
            {
                // Get ability name for special keybind overrides
                string abilityName = GetAbilityName(ability);
                
                // Skip Harvest, Dash, and BloodTransfusion - they have dedicated holders that are always active
                if (abilityName.Equals(harvestAbilityName, System.StringComparison.OrdinalIgnoreCase) ||
                    abilityName.Equals(dashAbilityName, System.StringComparison.OrdinalIgnoreCase) ||
                    abilityName.Equals(bloodTransfusionAbilityName, System.StringComparison.OrdinalIgnoreCase))
                {
                    // Don't assign Harvest/Dash/BloodTransfusion to slot holders - they have dedicated holders
                    Debug.Log($"AbilitySetter: Skipping '{abilityName}' in slot {i} - it has a dedicated forced keybind holder.");
                    abilityHolders[i].ability = null;
                    abilityHolders[i].key = KeyCode.None;
                    continue;
                }
                
                // Get keybind from SlotNr text in the ActiveSlot for other abilities
                KeyCode slotKeybind = GetKeybindFromSlotNr(activeSlots[i]);
                
                // IMPORTANT: Set key FIRST before assigning ability, to prevent any potential override
                abilityHolders[i].key = slotKeybind;
                
                // Then assign ability to holder
                abilityHolders[i].ability = ability;
                
                // Verify key is still correct (defensive check)
                if (abilityHolders[i].key != slotKeybind)
                {
                    Debug.LogWarning($"AbilitySetter: Key was changed! Re-setting slot {i} keybind from {abilityHolders[i].key} to {slotKeybind}");
                    abilityHolders[i].key = slotKeybind;
                }
                
                Debug.Log($"AbilitySetter: ✓ Slot {i} - Equipped '{itemName}' with keybind {slotKeybind} (from SlotNr: '{activeSlots[i].GetSlotNrText()}'). Current holder key: {abilityHolders[i].key}, Ability: {(abilityHolders[i].ability != null ? abilityHolders[i].ability.GetType().Name : "NULL")}");
            }
            else
            {
                Debug.LogError($"AbilitySetter: ✗ Could not find ability for '{itemName}' in slot {i}.");
                if (abilityMap.Count == 0)
                {
                    Debug.LogError($"AbilitySetter: Ability map is EMPTY! Make sure 'allAbilities' array is populated in the inspector.");
                }
                else
                {
                    Debug.LogWarning($"AbilitySetter: Available abilities in map: {string.Join(", ", abilityMap.Keys)}");
                }
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
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning("AbilitySetter: FindAbilityByName called with empty itemName!");
            return null;
        }
        
        if (abilityMap == null || abilityMap.Count == 0)
        {
            Debug.LogWarning($"AbilitySetter: Ability map is empty! Cannot find ability '{itemName}'. Make sure 'allAbilities' array is populated.");
            return null;
        }
        
        // Try exact match
        if (abilityMap.TryGetValue(itemName, out Ability ability))
        {
            Debug.Log($"AbilitySetter: Found exact match for '{itemName}'");
            return ability;
        }
        
        // Try case-insensitive match
        foreach (var kvp in abilityMap)
        {
            if (string.Equals(kvp.Key, itemName, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"AbilitySetter: Found case-insensitive match: '{kvp.Key}' matches '{itemName}'");
                return kvp.Value;
            }
        }
        
        // Try partial match (ability name contains itemName or vice versa)
        foreach (var kvp in abilityMap)
        {
            if (kvp.Key.Contains(itemName, System.StringComparison.OrdinalIgnoreCase) || 
                itemName.Contains(kvp.Key, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"AbilitySetter: Found partial match: '{kvp.Key}' partially matches '{itemName}'");
                return kvp.Value;
            }
        }
        
        // Try removing spaces and special characters for matching
        string normalizedItemName = itemName.Replace(" ", "").Replace("'", "").Replace("-", "");
        foreach (var kvp in abilityMap)
        {
            string normalizedKey = kvp.Key.Replace(" ", "").Replace("'", "").Replace("-", "");
            if (string.Equals(normalizedKey, normalizedItemName, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"AbilitySetter: Found normalized match: '{kvp.Key}' (normalized: '{normalizedKey}') matches '{itemName}' (normalized: '{normalizedItemName}')");
                return kvp.Value;
            }
        }
        
        Debug.LogWarning($"AbilitySetter: Could not find ability matching '{itemName}'. Available abilities: {string.Join(", ", abilityMap.Keys)}");
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
    
    /// <summary>
    /// Gets the Harvest ability holder (forced keybind)
    /// </summary>
    public AbilityHolder GetHarvestHolder() => harvestHolder;
    
    /// <summary>
    /// Gets the Dash ability holder (forced keybind - always active)
    /// </summary>
    public AbilityHolder GetDashHolder() => dashHolder;
    
    /// <summary>
    /// Gets the BloodTransfusion ability holder (forced keybind)
    /// </summary>
    public AbilityHolder GetBloodTransfusionHolder() => bloodTransfusionHolder;
}
