#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Custom editor for RewardManager to easily load all buffs from the Buffs folder
/// </summary>
[CustomEditor(typeof(RewardManager))]
public class RewardManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();

        RewardManager rewardManager = (RewardManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        // Button to load all buffs from Buffs folder
        if (GUILayout.Button("Load All Buffs from Player/Buffs Folder", GUILayout.Height(30)))
        {
            LoadBuffsIntoRewardManager(rewardManager);
        }

        // Button to refresh/reload all rewards
        if (GUILayout.Button("Refresh All Rewards (Auto-Load)", GUILayout.Height(25)))
        {
            rewardManager.LoadAllRewards();
            EditorUtility.SetDirty(rewardManager);
            Debug.Log("RewardManager: Refreshed all rewards!");
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Click 'Load All Buffs' to automatically populate the All Rewards array with all buffs from the Player/Buffs folder. " +
            "This makes it easy to 'drop in' all your buffs. The randomization and rarity system will work with all loaded rewards.",
            MessageType.Info
        );
    }

    /// <summary>
    /// Load all buffs from Player/Buffs folder and assign them to the RewardManager
    /// </summary>
    void LoadBuffsIntoRewardManager(RewardManager rewardManager)
    {
        // Find all RewardSO assets in Player/Buffs folder
        string[] guids = AssetDatabase.FindAssets("t:RewardSO", new[] { "Assets/Player/Buffs" });
        
        if (guids.Length == 0)
        {
            // Try alternative path
            guids = AssetDatabase.FindAssets("t:RewardSO", new[] { "Player/Buffs" });
        }

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No Buffs Found",
                "No RewardSO assets found in Player/Buffs folder. Make sure your buffs are RewardSO ScriptableObjects in that folder.",
                "OK"
            );
            return;
        }

        // Load all buffs
        RewardSO[] buffs = new RewardSO[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            buffs[i] = AssetDatabase.LoadAssetAtPath<RewardSO>(path);
        }

        // Remove null entries
        buffs = buffs.Where(b => b != null).ToArray();

        if (buffs.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Failed to load buffs. Make sure they are RewardSO ScriptableObjects.", "OK");
            return;
        }

        // Get current rewards array using SerializedProperty to properly modify it
        SerializedProperty allRewardsProp = serializedObject.FindProperty("allRewards");
        allRewardsProp.arraySize = buffs.Length;

        for (int i = 0; i < buffs.Length; i++)
        {
            allRewardsProp.GetArrayElementAtIndex(i).objectReferenceValue = buffs[i];
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(rewardManager);

        Debug.Log($"RewardManager: Loaded {buffs.Length} buffs from Player/Buffs folder into All Rewards array!");
        EditorUtility.DisplayDialog(
            "Success",
            $"Successfully loaded {buffs.Length} buffs into the All Rewards array!\n\n" +
            "These buffs will now be included in the reward pool and randomized based on their rarities.",
            "OK"
        );
    }
}
#endif

