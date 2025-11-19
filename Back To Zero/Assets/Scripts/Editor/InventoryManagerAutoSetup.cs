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
        
        if (GUILayout.Button("Auto-Populate All References", GUILayout.Height(30)))
        {
            AutoPopulateReferences(inventoryManager);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Click the button above to automatically find and assign all required references for InventoryManager.", MessageType.Info);
    }

    private void AutoPopulateReferences(InventoryManager inventoryManager)
    {
        serializedObject.Update();
        Undo.RecordObject(inventoryManager, "Auto-populate InventoryManager references");
        bool hasChanges = false;

        // 1. Find and assign ItemSOs
        SerializedProperty itemSOsProp = serializedObject.FindProperty("itemSOs");
        if (itemSOsProp != null && (itemSOsProp.arraySize == 0 || itemSOsProp.GetArrayElementAtIndex(0).objectReferenceValue == null))
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemSO");
            ItemSO[] itemSOs = guids.Select(guid => AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
            
            if (itemSOs.Length > 0)
            {
                itemSOsProp.arraySize = itemSOs.Length;
                for (int i = 0; i < itemSOs.Length; i++)
                {
                    itemSOsProp.GetArrayElementAtIndex(i).objectReferenceValue = itemSOs[i];
                }
                Debug.Log($"InventoryManager: Auto-assigned {itemSOs.Length} ItemSOs.");
                hasChanges = true;
            }
            else
            {
                Debug.LogWarning("InventoryManager: No ItemSO ScriptableObjects found in project.");
            }
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
