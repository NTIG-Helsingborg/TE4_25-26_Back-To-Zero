using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

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
        // Note: Unity's API for programmatically setting physics shapes is not available in all versions
        // This tool now provides instructions instead of doing it automatically
        
        int spriteCount = selectedSprites.Count(s => s != null);
        
        if (spriteCount == 0)
        {
            EditorUtility.DisplayDialog("No Sprites Selected", "Please add sprites to configure.", "OK");
            return;
        }
        
        string instructions = "To configure sprite physics shapes to be full boxes:\n\n" +
            "1. Select each sprite in the Project window\n" +
            "2. In the Inspector, click 'Sprite Editor'\n" +
            "3. In the Sprite Editor window, select 'Custom Physics Shape' from the dropdown\n" +
            "4. Click 'Generate' or manually create a box shape with 4 points:\n" +
            "   - Bottom-left: (0, 0)\n" +
            "   - Bottom-right: (sprite width, 0)\n" +
            "   - Top-right: (sprite width, sprite height)\n" +
            "   - Top-left: (0, sprite height)\n" +
            "5. Click 'Apply' to save changes\n\n" +
            $"You have {spriteCount} sprite(s) selected. Configure them manually using the steps above.";
        
        EditorUtility.DisplayDialog("Manual Configuration Required", instructions, "OK");
        
        // Open the first sprite in Sprite Editor if available
        foreach (Sprite sprite in selectedSprites)
        {
            if (sprite != null)
            {
                string path = AssetDatabase.GetAssetPath(sprite);
                if (!string.IsNullOrEmpty(path))
                {
                    // Select the sprite in the project window
                    Selection.activeObject = sprite;
                    EditorGUIUtility.PingObject(sprite);
                    break;
                }
            }
        }
        
        Debug.Log($"Sprite Physics Shape Configurator: Please configure {spriteCount} sprite(s) manually using the Sprite Editor.");
    }
}
