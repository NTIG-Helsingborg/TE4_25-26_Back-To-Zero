using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(IntermissionTriggerProxy))]
public class IntermissionTriggerProxyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        IntermissionTriggerProxy proxy = (IntermissionTriggerProxy)target;
        
        SerializedProperty managerProp = serializedObject.FindProperty("manager");
        SerializedProperty targetTagProp = serializedObject.FindProperty("targetTag");
        SerializedProperty entryIndexProp = serializedObject.FindProperty("entryIndex");
        
        EditorGUILayout.PropertyField(managerProp);
        
        // Show entry dropdown if manager is assigned
        IntermissionTextDisplay manager = (IntermissionTextDisplay)managerProp.objectReferenceValue;
        if (manager != null)
        {
            // We need to access the entries from the manager to build the list
            // Since we can't easily access the serialized property of another object here without creating a SerializedObject for it,
            // we'll use the reference directly for reading names. This is safe for display.
            
            SerializedObject managerSO = new SerializedObject(manager);
            SerializedProperty entriesProp = managerSO.FindProperty("intermissionEntries");
            
            if (entriesProp != null && entriesProp.isArray)
            {
                int arraySize = entriesProp.arraySize;
                string[] options = new string[arraySize];
                
                for (int i = 0; i < arraySize; i++)
                {
                    SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(i);
                    SerializedProperty textProp = entryProp.FindPropertyRelative("text");
                    string text = textProp.stringValue;
                    
                    if (string.IsNullOrEmpty(text))
                    {
                        text = "(Empty)";
                    }
                    else if (text.Length > 30)
                    {
                        text = text.Substring(0, 30) + "...";
                    }
                    
                    options[i] = $"{i}: {text}";
                }
                
                if (arraySize > 0)
                {
                    // Ensure index is valid
                    if (entryIndexProp.intValue >= arraySize)
                    {
                        entryIndexProp.intValue = arraySize - 1;
                    }
                    if (entryIndexProp.intValue < 0)
                    {
                        entryIndexProp.intValue = 0;
                    }
                    
                    entryIndexProp.intValue = EditorGUILayout.Popup("Intermission Entry", entryIndexProp.intValue, options);
                }
                else
                {
                    EditorGUILayout.HelpBox("No intermission entries found in the assigned manager.", MessageType.Warning);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Assign an IntermissionTextDisplay manager to select an entry.", MessageType.Info);
        }
        
        EditorGUILayout.PropertyField(targetTagProp);
        
        serializedObject.ApplyModifiedProperties();
    }
}
