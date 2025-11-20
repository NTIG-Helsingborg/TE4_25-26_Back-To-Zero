using UnityEngine;
using UnityEditor;

public class CheckPlayerPrefab : EditorWindow
{
    [MenuItem("Tools/Check Player Prefab Components")]
    public static void ShowWindow()
    {
        GetWindow<CheckPlayerPrefab>("Player Prefab Checker");
    }

    void OnGUI()
    {
        GUILayout.Label("Player Prefab Component Checker", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Check Player Prefab"))
        {
            CheckPrefab();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Find Player Prefab"))
        {
            string[] guids = AssetDatabase.FindAssets("Player t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Object prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log($"Found Player prefab at: {path}");
            }
            else
            {
                Debug.LogError("Player prefab not found!");
            }
        }
    }

    void CheckPrefab()
    {
        // Find the player prefab
        string[] guids = AssetDatabase.FindAssets("Player t:Prefab");
        if (guids.Length == 0)
        {
            Debug.LogError("Player prefab not found!");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        
        if (prefab == null)
        {
            Debug.LogError($"Could not load prefab from: {path}");
            return;
        }

        Debug.Log("=== PLAYER PREFAB COMPONENT CHECK ===");
        Debug.Log($"Checking prefab: {path}");
        
        // Required components based on code analysis
        System.Type[] requiredComponents = new System.Type[]
        {
            typeof(PlayerMove),
            typeof(Health),
            typeof(UnityEngine.InputSystem.PlayerInput),
            typeof(Rigidbody2D),
            typeof(PlayerHandler),
            typeof(InventoryManager),
            typeof(Healing),
            typeof(AbilitySetter), // This is likely missing!
            typeof(Aim),
            typeof(ExperienceManager),
            typeof(MeleeHitbox),
            typeof(Projectiles),
        };

        // Check each required component
        bool allFound = true;
        foreach (System.Type componentType in requiredComponents)
        {
            Component comp = prefab.GetComponent(componentType);
            if (comp == null)
            {
                Debug.LogWarning($"❌ MISSING: {componentType.Name}");
                allFound = false;
            }
            else
            {
                Debug.Log($"✓ Found: {componentType.Name}");
            }
        }

        // Check for AbilityHolder components
        AbilityHolder[] holders = prefab.GetComponents<AbilityHolder>();
        Debug.Log($"Found {holders.Length} AbilityHolder component(s)");

        // List all components
        Debug.Log("\n=== ALL COMPONENTS ON PLAYER PREFAB ===");
        Component[] allComponents = prefab.GetComponents<Component>();
        foreach (Component comp in allComponents)
        {
            if (comp != null)
            {
                Debug.Log($"  - {comp.GetType().Name}");
            }
        }

        if (allFound)
        {
            Debug.Log("\n✓ All required components found!");
        }
        else
        {
            Debug.LogWarning("\n⚠ Some required components are missing!");
        }
    }
}

