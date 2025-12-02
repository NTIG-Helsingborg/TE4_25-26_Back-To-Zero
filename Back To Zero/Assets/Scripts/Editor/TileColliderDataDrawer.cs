using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

[CustomPropertyDrawer(typeof(TileColliderData))]
public class TileColliderDataDrawer : PropertyDrawer
{
    private const float PreviewSize = 48f;
    private const float MiniPreviewSize = 16f;
    private const float Spacing = 5f;
    
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
        
        // Draw foldout with mini preview on the right
        Rect foldoutRect = new Rect(position.x, yPos, width - MiniPreviewSize - Spacing, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
        
        // Draw mini preview next to foldout (always visible)
        if (previewSprite != null)
        {
            Rect miniPreviewRect = new Rect(position.x + width - MiniPreviewSize, yPos, MiniPreviewSize, MiniPreviewSize);
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
}
