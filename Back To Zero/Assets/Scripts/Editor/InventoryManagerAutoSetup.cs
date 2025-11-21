using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using System.Linq;

[CustomEditor(typeof(InventoryManager))]
public class InventoryManagerAutoSetup : Editor
{
    public override void OnInspectorGUI()
    {
        InventoryManager inventoryManager = (InventoryManager)target;

        // Draw default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Auto Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Auto-Load ItemSOs from Items Folder", GUILayout.Height(30)))
        {
            AutoLoadItemSOsFromItemsFolder(inventoryManager);
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Auto-Populate All References", GUILayout.Height(30)))
        {
            AutoPopulateReferences(inventoryManager);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Click 'Auto-Load ItemSOs from Items Folder' to automatically find and assign all ItemSOs from the Items folder.\n\nClick 'Auto-Populate All References' to automatically find and assign all other required references for InventoryManager.", MessageType.Info);
    }

    private void AutoLoadItemSOsFromItemsFolder(InventoryManager inventoryManager)
    {
        serializedObject.Update();
        Undo.RecordObject(inventoryManager, "Auto-load ItemSOs from Items folder");
        
        SerializedProperty itemSOsProp = serializedObject.FindProperty("itemSOs");
        if (itemSOsProp == null)
        {
            Debug.LogError("InventoryManager: Could not find 'itemSOs' property!");
            return;
        }
        
        // Find all ItemSO ScriptableObjects in the Items folder
        string[] guids = AssetDatabase.FindAssets("t:ItemSO");
        ItemSO[] allItemSOs = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(itemSO => itemSO != null)
            .ToArray();
        
        // Filter to only ItemSOs from the Items folder
        ItemSO[] itemsFromFolder = allItemSOs
            .Where(itemSO => 
            {
                string path = AssetDatabase.GetAssetPath(itemSO);
                // Check if path contains "Items" folder (case-insensitive)
                return path.Contains("/Items/") || path.Contains("\\Items\\");
            })
            .ToArray();
        
        if (itemsFromFolder.Length > 0)
        {
            itemSOsProp.arraySize = itemsFromFolder.Length;
            for (int i = 0; i < itemsFromFolder.Length; i++)
            {
                itemSOsProp.GetArrayElementAtIndex(i).objectReferenceValue = itemsFromFolder[i];
            }
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(inventoryManager);
            
            Debug.Log($"InventoryManager: ✓ Auto-loaded {itemsFromFolder.Length} ItemSOs from Items folder:");
            foreach (var itemSO in itemsFromFolder)
            {
                Debug.Log($"  - {itemSO.itemName} (isAbility={itemSO.isAbility}, isArtifact={itemSO.isArtifact})");
            }
        }
        else if (allItemSOs.Length > 0)
        {
            // Fallback: use all ItemSOs if none found in Items folder
            itemSOsProp.arraySize = allItemSOs.Length;
            for (int i = 0; i < allItemSOs.Length; i++)
            {
                itemSOsProp.GetArrayElementAtIndex(i).objectReferenceValue = allItemSOs[i];
            }
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(inventoryManager);
            
            Debug.LogWarning($"InventoryManager: No ItemSOs found in Items folder. Auto-loaded {allItemSOs.Length} ItemSOs from entire project instead.");
        }
        else
        {
            Debug.LogWarning("InventoryManager: No ItemSO ScriptableObjects found in project.");
        }
    }

    private void AutoPopulateReferences(InventoryManager inventoryManager)
    {
        serializedObject.Update();
        Undo.RecordObject(inventoryManager, "Auto-populate InventoryManager references");
        bool hasChanges = false;

        // 1. Find and assign ItemSOs (only if not already assigned)
        SerializedProperty itemSOsProp = serializedObject.FindProperty("itemSOs");
        if (itemSOsProp != null && (itemSOsProp.arraySize == 0 || itemSOsProp.GetArrayElementAtIndex(0).objectReferenceValue == null))
        {
            // Use the Items folder method
            AutoLoadItemSOsFromItemsFolder(inventoryManager);
            hasChanges = true;
        }

        // 2. Find GameObjects by name - prioritize "Inventory Canvas 1"
        // Note: FindObjectsOfType doesn't work with GameObject directly, so we use Transform
        Transform[] allTransforms = FindObjectsOfType<Transform>(true);
        GameObject[] allObjects = allTransforms.Select(t => t.gameObject).ToArray();
        
        // First, try to find the "Inventory Canvas 1" parent
        GameObject inventoryCanvas = allObjects.FirstOrDefault(go => 
            go.name == "Inventory Canvas 1" || 
            go.name.Contains("Inventory Canvas 1"));
        
        // Find InventoryMenu - prioritize children of "Inventory Canvas 1"
        SerializedProperty inventoryMenuProp = serializedObject.FindProperty("InventoryMenu");
        if (inventoryMenuProp != null && inventoryMenuProp.objectReferenceValue == null)
        {
            GameObject inventoryMenu = null;
            
            // First, try to find within "Inventory Canvas 1"
            if (inventoryCanvas != null)
            {
                inventoryMenu = inventoryCanvas.GetComponentsInChildren<Transform>(true)
                    .Select(t => t.gameObject)
                    .FirstOrDefault(go => 
                        go.name == "InventoryMenu" || 
                        (go.name.Contains("InventoryMenu") && !go.name.Contains("Equipment") && !go.name.Contains("Ability")));
            }
            
            // Fallback to searching all objects
            if (inventoryMenu == null)
            {
                inventoryMenu = allObjects.FirstOrDefault(go => 
                    go.name == "InventoryMenu" || 
                    (go.name.Contains("InventoryMenu") && !go.name.Contains("Equipment") && !go.name.Contains("Ability")));
            }
            
            if (inventoryMenu != null)
            {
                inventoryMenuProp.objectReferenceValue = inventoryMenu;
                Debug.Log($"InventoryManager: Auto-assigned InventoryMenu: {inventoryMenu.name}");
                hasChanges = true;
            }
            else
            {
                Debug.LogWarning("InventoryManager: Could not find InventoryMenu GameObject.");
            }
        }

        // Find InventoryMenuEquipment - prioritize children of "Inventory Canvas 1"
        SerializedProperty inventoryMenuEquipmentProp = serializedObject.FindProperty("InventoryMenuEquipment");
        if (inventoryMenuEquipmentProp != null && inventoryMenuEquipmentProp.objectReferenceValue == null)
        {
            GameObject inventoryMenuEquipment = null;
            
            // First, try to find within "Inventory Canvas 1"
            if (inventoryCanvas != null)
            {
                inventoryMenuEquipment = inventoryCanvas.GetComponentsInChildren<Transform>(true)
                    .Select(t => t.gameObject)
                    .FirstOrDefault(go => 
                        go.name == "InventoryMenuEquipment" || 
                        (go.name.Contains("InventoryMenu") && go.name.Contains("Equipment")) ||
                        go.name.Contains("Inventory Equipment"));
            }
            
            // Fallback to searching all objects
            if (inventoryMenuEquipment == null)
            {
                inventoryMenuEquipment = allObjects.FirstOrDefault(go => 
                    go.name == "InventoryMenuEquipment" || 
                    (go.name.Contains("InventoryMenu") && go.name.Contains("Equipment")) ||
                    go.name.Contains("Inventory Equipment"));
            }
            
            if (inventoryMenuEquipment != null)
            {
                inventoryMenuEquipmentProp.objectReferenceValue = inventoryMenuEquipment;
                Debug.Log($"InventoryManager: Auto-assigned InventoryMenuEquipment: {inventoryMenuEquipment.name}");
                hasChanges = true;
            }
            else
            {
                Debug.LogWarning("InventoryManager: Could not find InventoryMenuEquipment GameObject.");
            }
        }

        // Find InventoryMenuAbility - prioritize children of "Inventory Canvas 1"
        SerializedProperty inventoryMenuAbilityProp = serializedObject.FindProperty("InventoryMenuAbility");
        if (inventoryMenuAbilityProp != null && inventoryMenuAbilityProp.objectReferenceValue == null)
        {
            GameObject inventoryMenuAbility = null;
            
            // First, try to find within "Inventory Canvas 1"
            if (inventoryCanvas != null)
            {
                inventoryMenuAbility = inventoryCanvas.GetComponentsInChildren<Transform>(true)
                    .Select(t => t.gameObject)
                    .FirstOrDefault(go => 
                        go.name == "InventoryMenuAbility" || 
                        (go.name.Contains("InventoryMenu") && go.name.Contains("Ability")) ||
                        go.name.Contains("Inventory Ability"));
            }
            
            // Fallback to searching all objects
            if (inventoryMenuAbility == null)
            {
                inventoryMenuAbility = allObjects.FirstOrDefault(go => 
                    go.name == "InventoryMenuAbility" || 
                    (go.name.Contains("InventoryMenu") && go.name.Contains("Ability")) ||
                    go.name.Contains("Inventory Ability"));
            }
            
            if (inventoryMenuAbility != null)
            {
                inventoryMenuAbilityProp.objectReferenceValue = inventoryMenuAbility;
                Debug.Log($"InventoryManager: Auto-assigned InventoryMenuAbility: {inventoryMenuAbility.name}");
                hasChanges = true;
            }
            else
            {
                Debug.LogWarning("InventoryManager: Could not find InventoryMenuAbility GameObject.");
            }
        }

        // Find TopPanel - prioritize children of "Inventory Canvas 1"
        SerializedProperty topPanelProp = serializedObject.FindProperty("TopPanel");
        if (topPanelProp != null && topPanelProp.objectReferenceValue == null)
        {
            GameObject topPanel = null;
            
            // First, try to find within "Inventory Canvas 1"
            if (inventoryCanvas != null)
            {
                topPanel = inventoryCanvas.GetComponentsInChildren<Transform>(true)
                    .Select(t => t.gameObject)
                    .FirstOrDefault(go => 
                        go.name.Contains("TopPanel") || 
                        go.name.Contains("Top Panel"));
            }
            
            // Fallback to searching all objects
            if (topPanel == null)
            {
                topPanel = allObjects.FirstOrDefault(go => 
                    go.name.Contains("TopPanel") || 
                    go.name.Contains("Top Panel"));
            }
            
            if (topPanel != null)
            {
                topPanelProp.objectReferenceValue = topPanel;
                Debug.Log($"InventoryManager: Auto-assigned TopPanel: {topPanel.name}");
                hasChanges = true;
            }
        }

        // 3. Find Transforms (slots parents) - prioritize within "Inventory Canvas 1"
        // Find artifactSlotsParent - search for "ArtifactSlots" GameObject
        SerializedProperty artifactSlotsParentProp = serializedObject.FindProperty("artifactSlotsParent");
        if (artifactSlotsParentProp != null && artifactSlotsParentProp.objectReferenceValue == null)
        {
            Transform artifactSlotsParent = null;
            
            // First try to find "ArtifactSlots" within "Inventory Canvas 1"
            if (inventoryCanvas != null)
            {
                GameObject artifactSlots = inventoryCanvas.GetComponentsInChildren<Transform>(true)
                    .Select(t => t.gameObject)
                    .FirstOrDefault(go => go.name == "ArtifactSlots");
                
                if (artifactSlots != null)
                {
                    artifactSlotsParent = artifactSlots.transform;
                }
            }
            
            // Fallback: search all objects for "ArtifactSlots"
            if (artifactSlotsParent == null)
            {
                GameObject artifactSlots = allObjects.FirstOrDefault(go => go.name == "ArtifactSlots");
                if (artifactSlots != null)
                {
                    artifactSlotsParent = artifactSlots.transform;
                }
            }
            
            if (artifactSlotsParent != null)
            {
                artifactSlotsParentProp.objectReferenceValue = artifactSlotsParent;
                Debug.Log($"InventoryManager: Auto-assigned artifactSlotsParent: {artifactSlotsParent.name}");
                hasChanges = true;
            }
            else
            {
                Debug.LogWarning("InventoryManager: Could not find artifactSlotsParent. Please assign manually.");
            }
        }

        // Find abilitySlotsParent - search for "AbilitySlots" GameObject
        SerializedProperty abilitySlotsParentProp = serializedObject.FindProperty("abilitySlotsParent");
        if (abilitySlotsParentProp != null && abilitySlotsParentProp.objectReferenceValue == null)
        {
            Transform abilitySlotsParent = null;
            
            // First try to find "AbilitySlots" within "Inventory Canvas 1"
            if (inventoryCanvas != null)
            {
                GameObject abilitySlots = inventoryCanvas.GetComponentsInChildren<Transform>(true)
                    .Select(t => t.gameObject)
                    .FirstOrDefault(go => go.name == "AbilitySlots");
                
                if (abilitySlots != null)
                {
                    abilitySlotsParent = abilitySlots.transform;
                }
            }
            
            // Fallback: search all objects for "AbilitySlots"
            if (abilitySlotsParent == null)
            {
                GameObject abilitySlots = allObjects.FirstOrDefault(go => go.name == "AbilitySlots");
                if (abilitySlots != null)
                {
                    abilitySlotsParent = abilitySlots.transform;
                }
            }
            
            if (abilitySlotsParent != null)
            {
                abilitySlotsParentProp.objectReferenceValue = abilitySlotsParent;
                Debug.Log($"InventoryManager: Auto-assigned abilitySlotsParent: {abilitySlotsParent.name}");
                hasChanges = true;
            }
            else
            {
                Debug.LogWarning("InventoryManager: Could not find abilitySlotsParent. Please assign manually.");
            }
        }

        // 4. Find ActiveSlot components
        SerializedProperty activeSlotsProp = serializedObject.FindProperty("activeSlots");
        if (activeSlotsProp != null && (activeSlotsProp.arraySize == 0 || activeSlotsProp.GetArrayElementAtIndex(0).objectReferenceValue == null))
        {
            ActiveSlot[] activeSlots = FindObjectsOfType<ActiveSlot>(true);
            if (activeSlots.Length > 0)
            {
                activeSlotsProp.arraySize = activeSlots.Length;
                for (int i = 0; i < activeSlots.Length; i++)
                {
                    activeSlotsProp.GetArrayElementAtIndex(i).objectReferenceValue = activeSlots[i];
                }
                Debug.Log($"InventoryManager: Auto-assigned {activeSlots.Length} ActiveSlots.");
                hasChanges = true;
            }
            else
            {
                Debug.LogWarning("InventoryManager: Could not find any ActiveSlot components in the scene.");
            }
        }

        // 5. Find ActiveEquipmentSlot components
        SerializedProperty activeEquipmentSlotsProp = serializedObject.FindProperty("activeEquipmentSlots");
        if (activeEquipmentSlotsProp != null && (activeEquipmentSlotsProp.arraySize == 0 || activeEquipmentSlotsProp.GetArrayElementAtIndex(0).objectReferenceValue == null))
        {
            ActiveEquipmentSlot[] activeEquipmentSlots = FindObjectsOfType<ActiveEquipmentSlot>(true);
            if (activeEquipmentSlots.Length > 0)
            {
                activeEquipmentSlotsProp.arraySize = activeEquipmentSlots.Length;
                for (int i = 0; i < activeEquipmentSlots.Length; i++)
                {
                    activeEquipmentSlotsProp.GetArrayElementAtIndex(i).objectReferenceValue = activeEquipmentSlots[i];
                }
                Debug.Log($"InventoryManager: Auto-assigned {activeEquipmentSlots.Length} ActiveEquipmentSlots.");
                hasChanges = true;
            }
            else
            {
                Debug.LogWarning("InventoryManager: Could not find any ActiveEquipmentSlot components in the scene.");
            }
        }

        // 6. Find InputActionReference
        SerializedProperty inventoryActionProp = serializedObject.FindProperty("inventoryAction");
        if (inventoryActionProp != null && inventoryActionProp.objectReferenceValue == null)
        {
            InputActionReference inventoryActionRef = null;
            
            // Try to find InputActionReference in the scene
            InputActionReference[] inputRefs = FindObjectsOfType<InputActionReference>(true);
            inventoryActionRef = inputRefs.FirstOrDefault(ir => 
                ir.name.Contains("Inventory") || 
                (ir.action != null && ir.action.name.Contains("Inventory")));
            
            // If not found in scene, try to find in project assets
            if (inventoryActionRef == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:InputActionReference");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    InputActionReference refAsset = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
                    if (refAsset != null && (refAsset.name.Contains("Inventory") || 
                        (refAsset.action != null && refAsset.action.name.Contains("Inventory"))))
                    {
                        inventoryActionRef = refAsset;
                        break;
                    }
                }
            }
            
            if (inventoryActionRef != null)
            {
                inventoryActionProp.objectReferenceValue = inventoryActionRef;
                Debug.Log($"InventoryManager: Auto-assigned inventoryAction: {inventoryActionRef.name}");
                hasChanges = true;
            }
            else
            {
                Debug.LogWarning("InventoryManager: Could not find InputActionReference for inventory. Please assign manually.");
            }
        }

        if (hasChanges)
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(inventoryManager);
            Debug.Log("InventoryManager: Auto-population complete! Check the console for details.");
        }
        else
        {
            Debug.Log("InventoryManager: All references are already assigned or could not be found automatically.");
        }
    }
}
