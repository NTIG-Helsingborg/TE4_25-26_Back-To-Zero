using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool to configure sprite physics shapes to be full boxes.
/// This ensures TilemapCollider2D generates full-tile box colliders instead of sprite-shaped colliders.
/// </summary>
public class SpritePhysicsShapeConfigurator : EditorWindow
{
    private List<Sprite> selectedSprites = new List<Sprite>();
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Sprite Physics Shape Configurator")]
    public static void ShowWindow()
    {
        GetWindow<SpritePhysicsShapeConfigurator>("Sprite Physics Shape Configurator");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Sprite Physics Shape Configurator", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "This tool configures sprite physics shapes to be full boxes. " +
            "When used with TilemapCollider2D, this ensures colliders fill the entire tile square.",
            MessageType.Info
        );
        EditorGUILayout.Space();
        
        // Sprite selection
        EditorGUILayout.LabelField("Selected Sprites:", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < selectedSprites.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            selectedSprites[i] = (Sprite)EditorGUILayout.ObjectField(
                selectedSprites[i],
                typeof(Sprite),
                false
            );
            
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                selectedSprites.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        if (GUILayout.Button("Add Sprite"))
        {
            selectedSprites.Add(null);
        }
        
        EditorGUILayout.Space();
        
        // Configure button
        GUI.enabled = selectedSprites.Count > 0 && selectedSprites.Exists(s => s != null);
        
        if (GUILayout.Button("Configure Selected Sprites to Full Box", GUILayout.Height(40)))
        {
            ConfigureSpritesToFullBox();
        }
        
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        // Instructions
        EditorGUILayout.HelpBox(
            "How to use:\n" +
            "1. Add sprites you want to configure\n" +
            "2. Click 'Configure Selected Sprites to Full Box'\n" +
            "3. This will set the physics shape of each sprite to a full box\n" +
            "4. The sprite import settings will be modified and the sprites will be reimported\n\n" +
            "Note: This modifies the sprite import settings. Make sure you have version control!",
            MessageType.Info
        );
    }
    
    private void ConfigureSpritesToFullBox()
    {
        int configuredCount = 0;
        int failedCount = 0;
        HashSet<string> modifiedPaths = new HashSet<string>();
        
        foreach (Sprite sprite in selectedSprites)
        {
            if (sprite == null) continue;
            
            string path = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(path)) continue;
            
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                failedCount++;
                Debug.LogWarning($"Could not get TextureImporter for sprite '{sprite.name}' at '{path}'");
                continue;
            }
            
            // Get sprite data - works for both single and multiple sprites
            SpriteMetaData[] spriteMetaData = importer.spritesheet;
            
            if (spriteMetaData == null || spriteMetaData.Length == 0)
            {
                failedCount++;
                Debug.LogWarning($"No sprite data found for '{sprite.name}' at '{path}'. Make sure the texture is imported as a Sprite.");
                continue;
            }
            
            // Find the sprite in the spritesheet (for single sprites, this will be index 0)
            int spriteIndex = -1;
            for (int i = 0; i < spriteMetaData.Length; i++)
            {
                if (spriteMetaData[i].name == sprite.name)
                {
                    spriteIndex = i;
                    break;
                }
            }
            
            if (spriteIndex == -1)
            {
                failedCount++;
                Debug.LogWarning($"Could not find sprite '{sprite.name}' in spritesheet at '{path}'");
                continue;
            }
            
            // Get the sprite's rect
            Rect spriteRect = spriteMetaData[spriteIndex].rect;
            
            // Calculate full box physics shape (in sprite pixels)
            // The physics shape is in local sprite coordinates (0,0 is bottom-left of sprite)
            Vector2[] physicsShape = new Vector2[]
            {
                new Vector2(0, 0),                                    // Bottom-left
                new Vector2(spriteRect.width, 0),                    // Bottom-right
                new Vector2(spriteRect.width, spriteRect.height),    // Top-right
                new Vector2(0, spriteRect.height)                     // Top-left
            };
            
            // Set physics shape
            List<Vector2[]> physicsShapes = new List<Vector2[]>();
            physicsShapes.Add(physicsShape);
            spriteMetaData[spriteIndex].physicsShape = physicsShapes.ToArray();
            
            // Update spritesheet (works for both single and multiple sprites)
            importer.spritesheet = spriteMetaData;
            
            // Mark for reimport
            EditorUtility.SetDirty(importer);
            modifiedPaths.Add(path);
            configuredCount++;
        }
        
        // Reimport all modified assets
        foreach (string path in modifiedPaths)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
        
        AssetDatabase.Refresh();
        
        string message = $"Configured {configuredCount} sprite(s) to use full-box physics shapes.";
        if (failedCount > 0)
        {
            message += $"\n\n{failedCount} sprite(s) could not be configured (see console for details).";
        }
        message += "\n\nThe sprites have been reimported. Your TilemapCollider2D should now generate full-tile box colliders.";
        
        EditorUtility.DisplayDialog("Configuration Complete", message, "OK");
        
        Debug.Log($"Configured {configuredCount} sprite(s) to use full-box physics shapes.");
        if (failedCount > 0)
        {
            Debug.LogWarning($"Failed to configure {failedCount} sprite(s).");
        }
    }
}
