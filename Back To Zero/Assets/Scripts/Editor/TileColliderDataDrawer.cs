using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// Static class to track selected indices for bulk deletion
public static class TileColliderDataSelection
{
    private static Dictionary<int, HashSet<int>> selectedIndices = new Dictionary<int, HashSet<int>>();
    
    public static bool IsSelected(int objectInstanceID, int index)
    {
        if (!selectedIndices.ContainsKey(objectInstanceID))
            return false;
        return selectedIndices[objectInstanceID].Contains(index);
    }
    
    public static void ToggleSelection(int objectInstanceID, int index)
    {
        if (!selectedIndices.ContainsKey(objectInstanceID))
            selectedIndices[objectInstanceID] = new HashSet<int>();
        
        if (selectedIndices[objectInstanceID].Contains(index))
            selectedIndices[objectInstanceID].Remove(index);
        else
            selectedIndices[objectInstanceID].Add(index);
    }
    
    public static void ClearSelection(int objectInstanceID)
    {
        if (selectedIndices.ContainsKey(objectInstanceID))
            selectedIndices[objectInstanceID].Clear();
    }
    
    public static HashSet<int> GetSelectedIndices(int objectInstanceID)
    {
        if (!selectedIndices.ContainsKey(objectInstanceID))
            return new HashSet<int>();
        return new HashSet<int>(selectedIndices[objectInstanceID]);
    }
    
    public static int GetSelectedCount(int objectInstanceID)
    {
        if (!selectedIndices.ContainsKey(objectInstanceID))
            return 0;
        return selectedIndices[objectInstanceID].Count;
    }
}

[CustomPropertyDrawer(typeof(TileColliderData))]
public class TileColliderDataDrawer : PropertyDrawer
{
    private const float PreviewSize = 48f;
    private const float MiniPreviewSize = 16f;
    private const float Spacing = 5f;
    private const float ButtonWidth = 20f;
    private const float CheckboxWidth = 18f;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        float yPos = position.y;
        float width = position.width;
        
        // Get properties for preview
        SerializedProperty matchModeProp = property.FindPropertyRelative("matchMode");
        SerializedProperty tileAssetProp = property.FindPropertyRelative("tileAsset");
        SerializedProperty spriteAssetProp = property.FindPropertyRelative("spriteAsset");
        Sprite previewSprite = null;
        
        if (matchModeProp != null && tileAssetProp != null && spriteAssetProp != null)
        {
            previewSprite = GetPreviewSprite(matchModeProp, tileAssetProp, spriteAssetProp);
        }
        
        // Get element index for selection tracking
        int elementIndex = GetElementIndex(property);
        int objectInstanceID = property.serializedObject.targetObject.GetInstanceID();
        bool isSelected = TileColliderDataSelection.IsSelected(objectInstanceID, elementIndex);
        
        // Calculate layout: checkbox | foldout | remove button | preview
        float checkboxX = position.x;
        float foldoutX = checkboxX + CheckboxWidth + Spacing;
        float buttonX = position.x + width - ButtonWidth - MiniPreviewSize - Spacing;
        float previewX = position.x + width - MiniPreviewSize;
        float foldoutWidth = buttonX - foldoutX - Spacing;
        
        // Draw checkbox for multi-select
        Rect checkboxRect = new Rect(checkboxX, yPos, CheckboxWidth, EditorGUIUtility.singleLineHeight);
        bool newSelection = EditorGUI.Toggle(checkboxRect, isSelected);
        if (newSelection != isSelected)
        {
            TileColliderDataSelection.ToggleSelection(objectInstanceID, elementIndex);
        }
        
        // Draw foldout
        Rect foldoutRect = new Rect(foldoutX, yPos, foldoutWidth, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
        
        // Draw remove button
        Rect buttonRect = new Rect(buttonX, yPos, ButtonWidth, EditorGUIUtility.singleLineHeight);
        if (GUI.Button(buttonRect, "×", EditorStyles.miniButton))
        {
            RemoveArrayElement(property);
            return;
        }
        
        // Draw mini preview next to button (always visible)
        if (previewSprite != null)
        {
            Rect miniPreviewRect = new Rect(previewX, yPos, MiniPreviewSize, MiniPreviewSize);
            DrawSpritePreview(miniPreviewRect, previewSprite);
        }
        
        yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }
        
        EditorGUI.indentLevel++;
        
        // Draw large preview when expanded
        if (previewSprite != null)
        {
            Rect previewRect = new Rect(position.x, yPos, PreviewSize, PreviewSize);
            DrawSpritePreview(previewRect, previewSprite);
            yPos += PreviewSize + EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Draw all child properties using Unity's default drawer
        SerializedProperty iterator = property.Copy();
        SerializedProperty endProperty = property.GetEndProperty();
        bool enterChildren = true;
        
        while (iterator.NextVisible(enterChildren))
        {
            if (SerializedProperty.EqualContents(iterator, endProperty))
                break;
            
            enterChildren = false;
            float propHeight = EditorGUI.GetPropertyHeight(iterator, true);
            Rect propRect = new Rect(position.x, yPos, width, propHeight);
            EditorGUI.PropertyField(propRect, iterator, true);
            yPos += propHeight + EditorGUIUtility.standardVerticalSpacing;
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
        
        float height = EditorGUIUtility.singleLineHeight; // Foldout
        height += EditorGUIUtility.standardVerticalSpacing;
        
        // Add preview height if there's a sprite
        SerializedProperty matchModeProp = property.FindPropertyRelative("matchMode");
        SerializedProperty tileAssetProp = property.FindPropertyRelative("tileAsset");
        SerializedProperty spriteAssetProp = property.FindPropertyRelative("spriteAsset");
        Sprite previewSprite = GetPreviewSprite(matchModeProp, tileAssetProp, spriteAssetProp);
        if (previewSprite != null)
        {
            height += PreviewSize + EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Add height for all child properties
        SerializedProperty iterator = property.Copy();
        SerializedProperty endProperty = property.GetEndProperty();
        bool enterChildren = true;
        
        while (iterator.NextVisible(enterChildren))
        {
            if (SerializedProperty.EqualContents(iterator, endProperty))
                break;
            
            enterChildren = false;
            height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
        }
        
        return height;
    }
    
    private Sprite GetPreviewSprite(SerializedProperty matchModeProp, SerializedProperty tileAssetProp, SerializedProperty spriteAssetProp)
    {
        MatchMode matchMode = (MatchMode)matchModeProp.enumValueIndex;
        if (matchMode == MatchMode.BySprite)
        {
            return spriteAssetProp.objectReferenceValue as Sprite;
        }
        else
        {
            TileBase tile = tileAssetProp.objectReferenceValue as TileBase;
            if (tile is Tile)
            {
                return ((Tile)tile).sprite;
            }
            else if (tile is UnityEngine.Tilemaps.AnimatedTile)
            {
                var animatedTile = (UnityEngine.Tilemaps.AnimatedTile)tile;
                if (animatedTile.m_AnimatedSprites != null && animatedTile.m_AnimatedSprites.Length > 0)
                {
                    return animatedTile.m_AnimatedSprites[0];
                }
            }
        }
        return null;
    }
    
    private void DrawSpritePreview(Rect rect, Sprite sprite)
    {
        if (sprite != null && sprite.texture != null)
        {
            // Draw border (only for larger previews)
            if (rect.height > 20f)
            {
                EditorGUI.DrawRect(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.height + 2), new Color(0.3f, 0.3f, 0.3f, 1f));
            }
            
            // Draw sprite
            Rect spriteRect = sprite.textureRect;
            Rect uvRect = new Rect(
                spriteRect.x / sprite.texture.width,
                spriteRect.y / sprite.texture.height,
                spriteRect.width / sprite.texture.width,
                spriteRect.height / sprite.texture.height
            );
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uvRect);
        }
    }
    
    private void RemoveArrayElement(SerializedProperty elementProperty)
    {
        // Parse the property path to find array and index
        // Path format: "colliderData.Array.data[5]"
        string path = elementProperty.propertyPath;
        
        // Find the array property path (everything before ".Array.data[")
        int arrayDataIndex = path.IndexOf(".Array.data[");
        if (arrayDataIndex == -1)
            return;
        
        string arrayPath = path.Substring(0, arrayDataIndex);
        
        // Extract the index
        int indexStart = arrayDataIndex + ".Array.data[".Length;
        int indexEnd = path.IndexOf(']', indexStart);
        if (indexEnd == -1)
            return;
        
        if (!int.TryParse(path.Substring(indexStart, indexEnd - indexStart), out int index))
            return;
        
        // Get the array property
        SerializedProperty arrayProperty = elementProperty.serializedObject.FindProperty(arrayPath);
        if (arrayProperty == null || !arrayProperty.isArray)
            return;
        
        if (index < 0 || index >= arrayProperty.arraySize)
            return;
        
        // Get the manager to track deletions
        TilemapColliderManager manager = elementProperty.serializedObject.targetObject as TilemapColliderManager;
        
        // Track deleted sprites/tiles before deleting
        if (manager != null)
        {
            SerializedProperty spriteProp = elementProperty.FindPropertyRelative("spriteAsset");
            SerializedProperty tileProp = elementProperty.FindPropertyRelative("tileAsset");
            
            if (spriteProp != null && spriteProp.objectReferenceValue != null)
            {
                manager.AddToExclusionList(spriteProp.objectReferenceValue as Sprite, null);
            }
            if (tileProp != null && tileProp.objectReferenceValue != null)
            {
                manager.AddToExclusionList(null, tileProp.objectReferenceValue as TileBase);
            }
        }
        
        // Record undo
        Undo.RecordObject(elementProperty.serializedObject.targetObject, "Remove Array Element");
        
        // Delete the array element
        arrayProperty.DeleteArrayElementAtIndex(index);
        
        // Mark object as dirty - Unity will handle serialization automatically (much faster)
        EditorUtility.SetDirty(elementProperty.serializedObject.targetObject);
    }
    
    private int GetElementIndex(SerializedProperty elementProperty)
    {
        // Parse the property path to find index
        string path = elementProperty.propertyPath;
        int arrayDataIndex = path.IndexOf(".Array.data[");
        if (arrayDataIndex == -1)
            return -1;
        
        int indexStart = arrayDataIndex + ".Array.data[".Length;
        int indexEnd = path.IndexOf(']', indexStart);
        if (indexEnd == -1)
            return -1;
        
        if (int.TryParse(path.Substring(indexStart, indexEnd - indexStart), out int index))
            return index;
        
        return -1;
    }
    
    public static void DeleteSelectedElements(SerializedProperty arrayProperty)
    {
        if (arrayProperty == null || !arrayProperty.isArray)
            return;
        
        int objectInstanceID = arrayProperty.serializedObject.targetObject.GetInstanceID();
        HashSet<int> selectedIndices = TileColliderDataSelection.GetSelectedIndices(objectInstanceID);
        
        if (selectedIndices.Count == 0)
            return;
        
        // Get the manager to track deletions
        TilemapColliderManager manager = arrayProperty.serializedObject.targetObject as TilemapColliderManager;
        
        // Record undo
        Undo.RecordObject(arrayProperty.serializedObject.targetObject, "Delete Selected Elements");
        
        // Delete from highest index to lowest to avoid index shifting issues
        List<int> sortedIndices = new List<int>(selectedIndices);
        sortedIndices.Sort();
        sortedIndices.Reverse();
        
        foreach (int index in sortedIndices)
        {
            if (index >= 0 && index < arrayProperty.arraySize)
            {
                // Track deleted sprites/tiles before deleting
                if (manager != null)
                {
                    SerializedProperty elementProp = arrayProperty.GetArrayElementAtIndex(index);
                    SerializedProperty spriteProp = elementProp.FindPropertyRelative("spriteAsset");
                    SerializedProperty tileProp = elementProp.FindPropertyRelative("tileAsset");
                    
                    if (spriteProp != null && spriteProp.objectReferenceValue != null)
                    {
                        manager.AddToExclusionList(spriteProp.objectReferenceValue as Sprite, null);
                    }
                    if (tileProp != null && tileProp.objectReferenceValue != null)
                    {
                        manager.AddToExclusionList(null, tileProp.objectReferenceValue as TileBase);
                    }
                }
                
                arrayProperty.DeleteArrayElementAtIndex(index);
            }
        }
        
        // Clear selection
        TileColliderDataSelection.ClearSelection(objectInstanceID);
        
        // Mark object as dirty
        EditorUtility.SetDirty(arrayProperty.serializedObject.targetObject);
    }
}
