using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(IntermissionTextDisplay.IntermissionEntry))]
public class IntermissionEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Get the parent array to know how many entries exist
        SerializedProperty arrayProperty = property.serializedObject.FindProperty("intermissionEntries");
        int arraySize = arrayProperty != null ? arrayProperty.arraySize : 0;
        
        // Calculate current index
        int currentIndex = -1;
        if (arrayProperty != null)
        {
            string propertyPath = property.propertyPath;
            string indexString = propertyPath.Substring(propertyPath.IndexOf('[') + 1);
            indexString = indexString.Substring(0, indexString.IndexOf(']'));
            int.TryParse(indexString, out currentIndex);
        }
        
        // Get all properties
        SerializedProperty textProp = property.FindPropertyRelative("text");
        SerializedProperty triggerTypeProp = property.FindPropertyRelative("triggerType");
        SerializedProperty layerProp = property.FindPropertyRelative("triggerLayer");
        SerializedProperty specificColliderProp = property.FindPropertyRelative("specificColliderObject");
        SerializedProperty delayProp = property.FindPropertyRelative("triggerDelay");
        SerializedProperty triggerOnceProp = property.FindPropertyRelative("triggerOnce");
        SerializedProperty forceStartProp = property.FindPropertyRelative("forceStart");
        SerializedProperty startDarkenedProp = property.FindPropertyRelative("startDarkened");
        SerializedProperty playAfterProp = property.FindPropertyRelative("playAfterEntry");
        SerializedProperty playAfterIndexProp = property.FindPropertyRelative("playAfterEntryIndex");
        
        // Timing properties
        SerializedProperty customDurationProp = property.FindPropertyRelative("customDisplayDuration");
        SerializedProperty waitForInputProp = property.FindPropertyRelative("waitForInput");
        SerializedProperty dismissKeyProp = property.FindPropertyRelative("dismissKey");
        
        // Display properties
        SerializedProperty enableTextProp = property.FindPropertyRelative("enableText");
        SerializedProperty enableOverlayProp = property.FindPropertyRelative("enableOverlay");
        SerializedProperty enableDarkeningProp = property.FindPropertyRelative("enableDarkening");
        SerializedProperty overlayOpacityProp = property.FindPropertyRelative("overlayOpacity");
        SerializedProperty snappyCanvasProp = property.FindPropertyRelative("snappyCanvas");
        
        // Start drawing
        Rect rect = position;
        rect.height = EditorGUIUtility.singleLineHeight;
        
        // Foldout for the entry
        property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }
        
        EditorGUI.indentLevel++;
        
        // ===== TEXT =====
        EditorGUI.LabelField(rect, "Text", EditorStyles.boldLabel);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        rect.height = EditorGUIUtility.singleLineHeight * 3;
        textProp.stringValue = EditorGUI.TextArea(rect, textProp.stringValue);
        rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
        rect.height = EditorGUIUtility.singleLineHeight;
        
        // ===== TRIGGER SETTINGS =====
        EditorGUI.LabelField(rect, "Trigger Settings", EditorStyles.boldLabel);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        EditorGUI.PropertyField(rect, triggerTypeProp);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        IntermissionTextDisplay.TriggerType triggerType = (IntermissionTextDisplay.TriggerType)triggerTypeProp.enumValueIndex;
        
        if (triggerType != IntermissionTextDisplay.TriggerType.None && triggerType != IntermissionTextDisplay.TriggerType.AfterEntry)
        {
            EditorGUI.PropertyField(rect, layerProp);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
        
        if (triggerType == IntermissionTextDisplay.TriggerType.OnCollisionEnter || 
            triggerType == IntermissionTextDisplay.TriggerType.OnCollisionExit ||
            triggerType == IntermissionTextDisplay.TriggerType.OnTriggerEnter ||
            triggerType == IntermissionTextDisplay.TriggerType.OnTriggerExit)
        {
            EditorGUI.PropertyField(rect, specificColliderProp);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
        
        EditorGUI.PropertyField(rect, delayProp);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        EditorGUI.PropertyField(rect, triggerOnceProp);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        EditorGUI.PropertyField(rect, forceStartProp);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        if (triggerType == IntermissionTextDisplay.TriggerType.OnStart)
        {
            EditorGUI.PropertyField(rect, startDarkenedProp);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
        
        // ===== TIMING SETTINGS =====
        EditorGUI.LabelField(rect, "Timing Settings", EditorStyles.boldLabel);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        EditorGUI.PropertyField(rect, customDurationProp, new GUIContent("Display Duration (-1 = Global)"));
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        EditorGUI.PropertyField(rect, waitForInputProp, new GUIContent("Wait For Input"));
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        if (waitForInputProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUI.PropertyField(rect, dismissKeyProp, new GUIContent("Dismiss Key"));
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.indentLevel--;
        }
        
        // ===== DISPLAY OPTIONS =====
        EditorGUI.LabelField(rect, "Display Options", EditorStyles.boldLabel);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        EditorGUI.PropertyField(rect, enableTextProp);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        EditorGUI.PropertyField(rect, enableOverlayProp);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        EditorGUI.PropertyField(rect, enableDarkeningProp);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        if (enableDarkeningProp.boolValue)
        {
            EditorGUI.indentLevel++;
            overlayOpacityProp.floatValue = EditorGUI.Slider(rect, "Overlay Opacity", overlayOpacityProp.floatValue, 0f, 1f);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.PropertyField(rect, snappyCanvasProp, new GUIContent("Snappy Canvas"));
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // ===== PLAY AFTER ENTRY =====
        EditorGUI.LabelField(rect, "Play After Entry", EditorStyles.boldLabel);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        EditorGUI.PropertyField(rect, playAfterProp);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        if (playAfterProp.boolValue)
        {
            EditorGUI.indentLevel++;
            
            string[] options = new string[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                if (i == currentIndex)
                {
                    options[i] = $"Entry {i} (This Entry)";
                }
                else
                {
                    SerializedProperty entryProp = arrayProperty.GetArrayElementAtIndex(i);
                    SerializedProperty entryTextProp = entryProp.FindPropertyRelative("text");
                    string textPreview = entryTextProp.stringValue;
                    if (string.IsNullOrEmpty(textPreview))
                    {
                        textPreview = "(Empty)";
                    }
                    else if (textPreview.Length > 30)
                    {
                        textPreview = textPreview.Substring(0, 30) + "...";
                    }
                    options[i] = $"Entry {i}: {textPreview}";
                }
            }
            
            int currentValue = playAfterIndexProp.intValue;
            if (currentValue < 0 || currentValue >= arraySize || currentValue == currentIndex)
            {
                currentValue = 0;
                for (int i = 0; i < arraySize; i++)
                {
                    if (i != currentIndex)
                    {
                        currentValue = i;
                        break;
                    }
                }
                if (currentValue >= 0 && currentValue < arraySize)
                {
                    playAfterIndexProp.intValue = currentValue;
                }
            }
            
            int selectedIndex = EditorGUI.Popup(rect, "Entry", currentValue, options);
            if (selectedIndex != currentValue && selectedIndex >= 0 && selectedIndex < arraySize && selectedIndex != currentIndex)
            {
                playAfterIndexProp.intValue = selectedIndex;
            }
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }
        
        SerializedProperty triggerTypeProp = property.FindPropertyRelative("triggerType");
        IntermissionTextDisplay.TriggerType triggerType = (IntermissionTextDisplay.TriggerType)triggerTypeProp.enumValueIndex;
        
        SerializedProperty playAfterProp = property.FindPropertyRelative("playAfterEntry");
        SerializedProperty enableDarkeningProp = property.FindPropertyRelative("enableDarkening");
        SerializedProperty waitForInputProp = property.FindPropertyRelative("waitForInput");
        
        float height = EditorGUIUtility.singleLineHeight; // Foldout
        
        // Text section
        height += EditorGUIUtility.singleLineHeight; // Header
        height += EditorGUIUtility.singleLineHeight * 3; // TextArea
        height += EditorGUIUtility.standardVerticalSpacing * 2;
        
        // Trigger Settings section
        height += EditorGUIUtility.singleLineHeight; // Header
        height += EditorGUIUtility.singleLineHeight; // Trigger Type
        height += EditorGUIUtility.singleLineHeight; // Trigger Delay
        height += EditorGUIUtility.singleLineHeight; // Trigger Once
        height += EditorGUIUtility.singleLineHeight; // Force Start
        height += EditorGUIUtility.standardVerticalSpacing * 5;
        
        if (triggerType != IntermissionTextDisplay.TriggerType.None && triggerType != IntermissionTextDisplay.TriggerType.AfterEntry)
        {
            height += EditorGUIUtility.singleLineHeight; // Layer
            height += EditorGUIUtility.standardVerticalSpacing;
        }
        
        if (triggerType == IntermissionTextDisplay.TriggerType.OnCollisionEnter || 
            triggerType == IntermissionTextDisplay.TriggerType.OnCollisionExit ||
            triggerType == IntermissionTextDisplay.TriggerType.OnTriggerEnter ||
            triggerType == IntermissionTextDisplay.TriggerType.OnTriggerExit)
        {
            height += EditorGUIUtility.singleLineHeight; // Collider field
            height += EditorGUIUtility.standardVerticalSpacing;
        }
        
        if (triggerType == IntermissionTextDisplay.TriggerType.OnStart)
        {
            height += EditorGUIUtility.singleLineHeight; // Start Darkened
            height += EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Timing Settings section
        height += EditorGUIUtility.singleLineHeight; // Header
        height += EditorGUIUtility.singleLineHeight; // Custom Duration
        height += EditorGUIUtility.singleLineHeight; // Wait For Input
        height += EditorGUIUtility.standardVerticalSpacing * 3;
        
        if (waitForInputProp.boolValue)
        {
            height += EditorGUIUtility.singleLineHeight; // Dismiss Key
            height += EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Display Options section
        height += EditorGUIUtility.singleLineHeight; // Header
        height += EditorGUIUtility.singleLineHeight; // Enable Text
        height += EditorGUIUtility.singleLineHeight; // Enable Overlay
        height += EditorGUIUtility.singleLineHeight; // Enable Darkening
        height += EditorGUIUtility.singleLineHeight; // Snappy Canvas
        height += EditorGUIUtility.standardVerticalSpacing * 5;
        
        if (enableDarkeningProp.boolValue)
        {
            height += EditorGUIUtility.singleLineHeight; // Overlay Opacity
            height += EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Play After Entry section
        height += EditorGUIUtility.singleLineHeight; // Header
        height += EditorGUIUtility.singleLineHeight; // Play After checkbox
        height += EditorGUIUtility.standardVerticalSpacing * 2;
        
        if (playAfterProp.boolValue)
        {
            height += EditorGUIUtility.singleLineHeight; // Entry dropdown
            height += EditorGUIUtility.standardVerticalSpacing;
        }
        
        return height;
    }
}
