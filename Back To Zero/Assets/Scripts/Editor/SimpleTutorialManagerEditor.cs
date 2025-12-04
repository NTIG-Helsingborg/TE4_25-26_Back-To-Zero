using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(SimpleTutorialManager))]
public class SimpleTutorialManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SimpleTutorialManager manager = (SimpleTutorialManager)target;
        
        // Draw default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Setup Tools", EditorStyles.boldLabel);
        
        // Get the tutorial steps array property
        SerializedProperty stepsProp = serializedObject.FindProperty("tutorialSteps");
        
        if (stepsProp != null && stepsProp.arraySize > 0)
        {
            // Show buttons for each step
            for (int i = 0; i < stepsProp.arraySize; i++)
            {
                SerializedProperty stepProp = stepsProp.GetArrayElementAtIndex(i);
                SerializedProperty entryIndexProp = stepProp.FindPropertyRelative("intermissionEntryIndex");
                SerializedProperty showProp = stepProp.FindPropertyRelative("uiToShow");
                SerializedProperty hideProp = stepProp.FindPropertyRelative("uiToHide");
                SerializedProperty highlightProp = stepProp.FindPropertyRelative("uiToHighlight");
                
                int entryIndex = entryIndexProp.intValue;
                string stepName = $"Entry {entryIndex}";
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{stepName}:", GUILayout.Width(100));
                
                if (GUILayout.Button("Hide Everything Else", GUILayout.Height(25)))
                {
                    HideEverythingElse(stepProp, showProp, highlightProp, hideProp);
                }
                
                if (GUILayout.Button("Clear Hide List", GUILayout.Height(25)))
                {
                    hideProp.ClearArray();
                    serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Add tutorial steps above to see quick setup buttons.", MessageType.Info);
        }
    }
    
    private void HideEverythingElse(SerializedProperty stepProp, SerializedProperty showProp, SerializedProperty highlightProp, SerializedProperty hideProp)
    {
        // Get all GameObjects from uiToShow and uiToHighlight (these should stay visible)
        HashSet<GameObject> objectsToKeepVisible = new HashSet<GameObject>();
        
        for (int i = 0; i < showProp.arraySize; i++)
        {
            GameObject obj = showProp.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (obj != null)
            {
                objectsToKeepVisible.Add(obj);
            }
        }
        
        for (int i = 0; i < highlightProp.arraySize; i++)
        {
            GameObject obj = highlightProp.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (obj != null)
            {
                objectsToKeepVisible.Add(obj);
            }
        }
        
        // Find all Canvas objects in the scene
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        List<GameObject> uiToHideList = new List<GameObject>();
        
        foreach (Canvas canvas in allCanvases)
        {
            // Get all direct children of the canvas (top-level UI elements)
            foreach (Transform child in canvas.transform)
            {
                GameObject childObj = child.gameObject;
                
                // Skip if this is in the "show" or "highlight" list
                if (objectsToKeepVisible.Contains(childObj))
                    continue;
                
                // Skip if it's already in the hide list
                bool alreadyInHideList = false;
                for (int i = 0; i < hideProp.arraySize; i++)
                {
                    if (hideProp.GetArrayElementAtIndex(i).objectReferenceValue == childObj)
                    {
                        alreadyInHideList = true;
                        break;
                    }
                }
                
                if (!alreadyInHideList)
                {
                    uiToHideList.Add(childObj);
                }
            }
        }
        
        // Add to hide list
        Undo.RecordObject(target, "Hide Everything Else");
        
        int startSize = hideProp.arraySize;
        hideProp.arraySize = startSize + uiToHideList.Count;
        
        for (int i = 0; i < uiToHideList.Count; i++)
        {
            hideProp.GetArrayElementAtIndex(startSize + i).objectReferenceValue = uiToHideList[i];
        }
        
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        
        Debug.Log($"SimpleTutorialManager: Added {uiToHideList.Count} UI elements to hide list. " +
                  $"(Found {allCanvases.Length} canvases, skipped {objectsToKeepVisible.Count} elements in 'show/highlight' lists)");
    }
}

// Custom property drawer for TutorialStep to show dropdown
[CustomPropertyDrawer(typeof(SimpleTutorialManager.TutorialStep))]
public class TutorialStepDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Foldout
        Rect rect = position;
        rect.height = EditorGUIUtility.singleLineHeight;
        property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Get IntermissionTextDisplay from SimpleTutorialManager
            SimpleTutorialManager manager = property.serializedObject.targetObject as SimpleTutorialManager;
            SerializedProperty intermissionDisplayProp = property.serializedObject.FindProperty("intermissionDisplay");
            IntermissionTextDisplay intermissionDisplay = intermissionDisplayProp.objectReferenceValue as IntermissionTextDisplay;
            
            SerializedProperty entryIndexProp = property.FindPropertyRelative("intermissionEntryIndex");
            
            // Build entry names dropdown
            if (intermissionDisplay != null)
            {
                var entriesField = typeof(IntermissionTextDisplay).GetField("intermissionEntries", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (entriesField != null)
                {
                    var entries = entriesField.GetValue(intermissionDisplay) as System.Array;
                    if (entries != null && entries.Length > 0)
                    {
                        string[] entryNames = new string[entries.Length];
                        for (int i = 0; i < entries.Length; i++)
                        {
                            var entry = entries.GetValue(i);
                            var textField = entry.GetType().GetField("text");
                            string text = textField != null ? (string)textField.GetValue(entry) : "";
                            
                            if (string.IsNullOrEmpty(text))
                                text = "(Empty)";
                            else if (text.Length > 30)
                                text = text.Substring(0, 30) + "...";
                            
                            entryNames[i] = $"Entry {i}: {text}";
                        }
                        
                        int currentIndex = entryIndexProp.intValue;
                        if (currentIndex < 0 || currentIndex >= entries.Length) currentIndex = 0;
                        
                        int newIndex = EditorGUI.Popup(rect, "Intermission Entry", currentIndex, entryNames);
                        entryIndexProp.intValue = newIndex;
                        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                    else
                    {
                        EditorGUI.LabelField(rect, "No entries in IntermissionTextDisplay!");
                        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
            }
            else
            {
                EditorGUI.PropertyField(rect, entryIndexProp);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            
            // Draw other properties
            SerializedProperty showProp = property.FindPropertyRelative("uiToShow");
            float showHeight = EditorGUI.GetPropertyHeight(showProp, true);
            rect.height = showHeight;
            EditorGUI.PropertyField(rect, showProp, true);
            rect.y += showHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty hideProp = property.FindPropertyRelative("uiToHide");
            float hideHeight = EditorGUI.GetPropertyHeight(hideProp, true);
            rect.height = hideHeight;
            EditorGUI.PropertyField(rect, hideProp, true);
            rect.y += hideHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty highlightProp = property.FindPropertyRelative("uiToHighlight");
            float highlightHeight = EditorGUI.GetPropertyHeight(highlightProp, true);
            rect.height = highlightHeight;
            EditorGUI.PropertyField(rect, highlightProp, true);
            rect.y += highlightHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty openInvProp = property.FindPropertyRelative("openInventory");
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, openInvProp);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        
        float height = EditorGUIUtility.singleLineHeight; // Foldout
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Entry dropdown
        
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("uiToShow"), true) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("uiToHide"), true) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("uiToHighlight"), true) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Open inventory
        
        return height;
    }
}
