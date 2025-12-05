using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Editor utility to fix the Ranged Small Undead prefab by removing duplicate EnemyChase script.
/// This script should only be run once in the editor.
/// </summary>
#if UNITY_EDITOR
public class FixRangedEnemyPrefab : MonoBehaviour
{
    [MenuItem("Tools/Fix Ranged Enemy Prefab")]
    public static void FixRangedEnemy()
    {
        // Load the prefab
        string prefabPath = "Assets/Prefabs/Enemies/Ranged Small Undead.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"Could not find prefab at {prefabPath}");
            return;
        }
        
        // Get all EnemyChase components
        EnemyChase[] chaseComponents = prefab.GetComponents<EnemyChase>();
        
        if (chaseComponents.Length > 0)
        {
            Debug.Log($"Found {chaseComponents.Length} EnemyChase component(s) on Ranged Small Undead");
            Debug.Log("Ranged Small Undead should use RangedSmallUndead script, not EnemyChase!");
            Debug.Log("Please manually remove the EnemyChase component from the prefab:");
            Debug.Log("1. Select 'Ranged Small Undead' prefab in Assets/Prefabs/Enemies/");
            Debug.Log("2. In the Inspector, find the 'EnemyChase' component");
            Debug.Log("3. Click the three dots (...) on the component");
            Debug.Log("4. Select 'Remove Component'");
            Debug.Log("5. Save the prefab");
            
            // Select the prefab so user can see it
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
        else
        {
            Debug.Log("Ranged Small Undead prefab looks good - no EnemyChase component found!");
        }
        
        // Check if RangedSmallUndead exists
        RangedSmallUndead rangedScript = prefab.GetComponent<RangedSmallUndead>();
        if (rangedScript == null)
        {
            Debug.LogError("Ranged Small Undead is missing the RangedSmallUndead script!");
        }
        else
        {
            Debug.Log("✅ RangedSmallUndead script is present");
        }
    }
}
#endif

