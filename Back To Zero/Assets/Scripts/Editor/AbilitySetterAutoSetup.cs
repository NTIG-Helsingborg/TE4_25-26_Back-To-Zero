using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(AbilitySetter))]
public class AbilitySetterAutoSetup : Editor
{
    public override void OnInspectorGUI()
    {
        AbilitySetter abilitySetter = (AbilitySetter)target;

        // Draw default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Auto Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Auto-Load Abilities from Ability's Folder", GUILayout.Height(30)))
        {
            AutoLoadAbilities(abilitySetter);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Click the button above to automatically find and assign all Ability ScriptableObjects from the Ability's folder.", MessageType.Info);
    }

    private void AutoLoadAbilities(AbilitySetter abilitySetter)
    {
        serializedObject.Update();
        Undo.RecordObject(abilitySetter, "Auto-load abilities");
        bool hasChanges = false;

        // Find all Ability ScriptableObjects in the project
        string[] guids = AssetDatabase.FindAssets("t:Ability");
        Ability[] abilities = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<Ability>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(ability => ability != null)
            .ToArray();
        
        // Filter to only abilities from the Ability's folder
        Ability[] abilitiesFromFolder = abilities
            .Where(ability => 
            {
                string path = AssetDatabase.GetAssetPath(ability);
                return path.Contains("Ability's") || path.Contains("Abilities");
            })
            .ToArray();
        
        if (abilitiesFromFolder.Length > 0)
        {
            SerializedProperty allAbilitiesProp = serializedObject.FindProperty("allAbilities");
            if (allAbilitiesProp != null)
            {
                allAbilitiesProp.arraySize = abilitiesFromFolder.Length;
                for (int i = 0; i < abilitiesFromFolder.Length; i++)
                {
                    allAbilitiesProp.GetArrayElementAtIndex(i).objectReferenceValue = abilitiesFromFolder[i];
                }
                Debug.Log($"AbilitySetter: Auto-loaded {abilitiesFromFolder.Length} abilities from Ability's folder.");
                hasChanges = true;
            }
        }
        else if (abilities.Length > 0)
        {
            // Fallback: use all abilities if none found in Ability's folder
            SerializedProperty allAbilitiesProp = serializedObject.FindProperty("allAbilities");
            if (allAbilitiesProp != null)
            {
                allAbilitiesProp.arraySize = abilities.Length;
                for (int i = 0; i < abilities.Length; i++)
                {
                    allAbilitiesProp.GetArrayElementAtIndex(i).objectReferenceValue = abilities[i];
                }
                Debug.Log($"AbilitySetter: Auto-loaded {abilities.Length} abilities (could not filter by folder, using all found abilities).");
                hasChanges = true;
            }
        }
        else
        {
            Debug.LogWarning("AbilitySetter: No Ability ScriptableObjects found in project.");
        }

        if (hasChanges)
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(abilitySetter);
            Debug.Log("AbilitySetter: Auto-load complete! Check the console for details.");
        }
        else
        {
            Debug.Log("AbilitySetter: Abilities are already assigned or could not be found automatically.");
        }
    }
}

