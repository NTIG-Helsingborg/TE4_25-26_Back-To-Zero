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
        SerializedProperty playAfterProp = property.FindPropertyRelative("playAfterEntry");
        SerializedProperty playAfterIndexProp = property.FindPropertyRelative("playAfterEntryIndex");
        
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
        
        // Header: Text
        EditorGUI.LabelField(rect, "Text", EditorStyles.boldLabel);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // Text field (TextArea)
        rect.height = EditorGUIUtility.singleLineHeight * 3;
        textProp.stringValue = EditorGUI.TextArea(rect, textProp.stringValue);
        rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
        rect.height = EditorGUIUtility.singleLineHeight;
        
        // Header: Trigger Settings
        EditorGUI.LabelField(rect, "Trigger Settings", EditorStyles.boldLabel);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // Trigger Type
        EditorGUI.LabelField(new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height), "Trigger Type");
        Rect triggerTypeRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, rect.width - EditorGUIUtility.labelWidth, rect.height);
        triggerTypeProp.enumValueIndex = EditorGUI.Popup(triggerTypeRect, triggerTypeProp.enumValueIndex, triggerTypeProp.enumDisplayNames);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // Only show trigger settings if not "None" or "AfterEntry"
        IntermissionTextDisplay.TriggerType triggerType = (IntermissionTextDisplay.TriggerType)triggerTypeProp.enumValueIndex;
        
        if (triggerType != IntermissionTextDisplay.TriggerType.None && triggerType != IntermissionTextDisplay.TriggerType.AfterEntry)
        {
            // Trigger Layer
            EditorGUI.PropertyField(rect, layerProp, new GUIContent("Trigger Layer"));
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Show specific collider GameObject field for OnCollisionEnter/Exit and OnTriggerEnter/Exit
        if (triggerType == IntermissionTextDisplay.TriggerType.OnCollisionEnter || 
            triggerType == IntermissionTextDisplay.TriggerType.OnCollisionExit ||
            triggerType == IntermissionTextDisplay.TriggerType.OnTriggerEnter ||
            triggerType == IntermissionTextDisplay.TriggerType.OnTriggerExit)
        {
            EditorGUI.PropertyField(rect, specificColliderProp, new GUIContent("Collider Object"));
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Trigger Delay
        EditorGUI.LabelField(new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height), "Trigger Delay");
        Rect delayRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, rect.width - EditorGUIUtility.labelWidth, rect.height);
        delayProp.floatValue = EditorGUI.FloatField(delayRect, delayProp.floatValue);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // Trigger Once
        EditorGUI.LabelField(new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height), "Trigger Once");
        Rect triggerOnceRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, rect.width - EditorGUIUtility.labelWidth, rect.height);
        triggerOnceProp.boolValue = EditorGUI.Toggle(triggerOnceRect, triggerOnceProp.boolValue);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // Start Darkened (only show if trigger type is OnStart)
        if (triggerType == IntermissionTextDisplay.TriggerType.OnStart)
        {
            SerializedProperty startDarkenedProp = property.FindPropertyRelative("startDarkened");
            EditorGUI.LabelField(new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height), "Start Darkened");
            Rect startDarkenedRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, rect.width - EditorGUIUtility.labelWidth, rect.height);
            startDarkenedProp.boolValue = EditorGUI.Toggle(startDarkenedRect, startDarkenedProp.boolValue);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Header: Display Options
        EditorGUI.LabelField(rect, "Display Options", EditorStyles.boldLabel);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // Enable Text
        SerializedProperty enableTextProp = property.FindPropertyRelative("enableText");
        EditorGUI.LabelField(new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height), "Enable Text");
        Rect enableTextRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, rect.width - EditorGUIUtility.labelWidth, rect.height);
        enableTextProp.boolValue = EditorGUI.Toggle(enableTextRect, enableTextProp.boolValue);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // Enable Overlay
        SerializedProperty enableOverlayProp = property.FindPropertyRelative("enableOverlay");
        EditorGUI.LabelField(new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height), "Enable Overlay");
        Rect enableOverlayRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, rect.width - EditorGUIUtility.labelWidth, rect.height);
        enableOverlayProp.boolValue = EditorGUI.Toggle(enableOverlayRect, enableOverlayProp.boolValue);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // Enable Darkening
        SerializedProperty enableDarkeningProp = property.FindPropertyRelative("enableDarkening");
        EditorGUI.LabelField(new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height), "Enable Darkening");
        Rect enableDarkeningRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, rect.width - EditorGUIUtility.labelWidth, rect.height);
        enableDarkeningProp.boolValue = EditorGUI.Toggle(enableDarkeningRect, enableDarkeningProp.boolValue);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // Header: Play After Entry
        EditorGUI.LabelField(rect, "Play After Entry", EditorStyles.boldLabel);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // Play After Entry checkbox
        playAfterProp.boolValue = EditorGUI.Toggle(rect, new GUIContent("Play After Entry"), playAfterProp.boolValue);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        if (playAfterProp.boolValue)
        {
            EditorGUI.indentLevel++;
            
            // Create dropdown options
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
            
            // Clamp the index to valid range
            int currentValue = playAfterIndexProp.intValue;
            if (currentValue < 0 || currentValue >= arraySize || currentValue == currentIndex)
            {
                // Find first valid index
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
            
            // Show dropdown
            int selectedIndex = EditorGUI.Popup(rect, "  Entry", currentValue, options);
            if (selectedIndex != currentValue)
            {
                if (selectedIndex == currentIndex)
                {
                    // Prevent selecting self
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.HelpBox(rect, "Cannot play after itself!", MessageType.Warning);
                }
                else if (selectedIndex >= 0 && selectedIndex < arraySize)
                {
                    playAfterIndexProp.intValue = selectedIndex;
                }
            }
            
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
        SerializedProperty playAfterIndexProp = property.FindPropertyRelative("playAfterEntryIndex");
        
        float height = EditorGUIUtility.singleLineHeight; // Foldout
        height += EditorGUIUtility.singleLineHeight; // "Text" header
        height += EditorGUIUtility.singleLineHeight * 3; // TextArea (3 lines)
        height += EditorGUIUtility.singleLineHeight; // "Trigger Settings" header
        height += EditorGUIUtility.singleLineHeight; // Trigger Type
        height += EditorGUIUtility.singleLineHeight; // Trigger Delay
        height += EditorGUIUtility.singleLineHeight; // Trigger Once
        height += EditorGUIUtility.standardVerticalSpacing * 7;
        
        // Add height for Start Darkened if trigger type is OnStart
        if (triggerType == IntermissionTextDisplay.TriggerType.OnStart)
        {
            height += EditorGUIUtility.singleLineHeight; // Start Darkened
            height += EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Add height for specific collider if trigger type is OnCollisionEnter/Exit or OnTriggerEnter/Exit
        if (triggerType == IntermissionTextDisplay.TriggerType.OnCollisionEnter || 
            triggerType == IntermissionTextDisplay.TriggerType.OnCollisionExit ||
            triggerType == IntermissionTextDisplay.TriggerType.OnTriggerEnter ||
            triggerType == IntermissionTextDisplay.TriggerType.OnTriggerExit)
        {
            height += EditorGUIUtility.singleLineHeight; // Collider field
            height += EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Add height for Display Options section
        height += EditorGUIUtility.singleLineHeight; // "Display Options" header
        height += EditorGUIUtility.singleLineHeight; // Enable Text
        height += EditorGUIUtility.singleLineHeight; // Enable Overlay
        height += EditorGUIUtility.singleLineHeight; // Enable Darkening
        height += EditorGUIUtility.standardVerticalSpacing * 4;
        
        height += EditorGUIUtility.singleLineHeight; // "Play After Entry" header
        height += EditorGUIUtility.singleLineHeight; // Play After Entry checkbox
        height += EditorGUIUtility.standardVerticalSpacing * 2;
        
        // Add height for trigger settings if needed
        if (triggerType != IntermissionTextDisplay.TriggerType.None && triggerType != IntermissionTextDisplay.TriggerType.AfterEntry)
        {
            height += EditorGUIUtility.singleLineHeight; // layer
            height += EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Add height for play after dropdown if enabled
        if (playAfterProp.boolValue)
        {
            height += EditorGUIUtility.singleLineHeight; // dropdown
            height += EditorGUIUtility.standardVerticalSpacing;
            
            // Add height for warning if selecting self
            SerializedProperty arrayProperty = property.serializedObject.FindProperty("intermissionEntries");
            int currentIndex = -1;
            if (arrayProperty != null)
            {
                string propertyPath = property.propertyPath;
                string indexString = propertyPath.Substring(propertyPath.IndexOf('[') + 1);
                indexString = indexString.Substring(0, indexString.IndexOf(']'));
                int.TryParse(indexString, out currentIndex);
            }
            
            if (playAfterIndexProp.intValue == currentIndex)
            {
                height += EditorGUIUtility.singleLineHeight; // warning box
                height += EditorGUIUtility.standardVerticalSpacing;
            }
        }
        
        return height;
    }
}

