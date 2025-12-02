using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TilemapColliderManager))]
public class TilemapColliderManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Collider Management", EditorStyles.boldLabel);
        
        TilemapColliderManager manager = (TilemapColliderManager)target;
        
        // Bulk delete selected elements
        SerializedProperty colliderDataProp = serializedObject.FindProperty("colliderData");
        if (colliderDataProp != null && colliderDataProp.isArray)
        {
            int selectedCount = TileColliderDataSelection.GetSelectedCount(manager.GetInstanceID());
            if (selectedCount > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"{selectedCount} element(s) selected", MessageType.Info);
                if (GUILayout.Button($"Delete Selected ({selectedCount})", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Delete Selected Elements",
                        $"Are you sure you want to delete {selectedCount} selected element(s)?",
                        "Yes", "No"))
                    {
                        TileColliderDataDrawer.DeleteSelectedElements(colliderDataProp);
                        serializedObject.ApplyModifiedProperties();
                    }
                }
                if (GUILayout.Button("Clear Selection", GUILayout.Height(25), GUILayout.Width(120)))
                {
                    TileColliderDataSelection.ClearSelection(manager.GetInstanceID());
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
            }
        }
        
        // Show folder structure info
        Transform collidersFolder = manager.transform.Find("Tilemap Colliders");
        if (collidersFolder != null && collidersFolder.childCount > 0)
        {
            EditorGUILayout.HelpBox(
                $"All colliders are organized in the 'Tilemap Colliders' folder with {collidersFolder.childCount} Tilemap subfolder(s).",
                MessageType.Info);
        }
        
        // Auto-Detect by Sprite button
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Auto-Detect Tiles by Sprite", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "Auto-Detect Tiles by Sprite",
                "This will scan all Tilemaps and create TileColliderData entries for each unique sprite found.\n\n" +
                "Tiles with the same sprite (but different TileBase assets) will be grouped together.\n\n" +
                "Continue?",
                "Yes", "No"))
            {
                manager.AutoDetectTilesBySprite();
                EditorUtility.SetDirty(manager);
                UnityEditor.SceneView.RepaintAll();
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Generate Colliders button
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Colliders", GUILayout.Height(30)))
        {
            manager.GenerateColliders();
            EditorUtility.SetDirty(manager);
            // Refresh the scene view
            UnityEditor.SceneView.RepaintAll();
        }
        
        // Clear Colliders button
        if (GUILayout.Button("Clear Colliders", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "Clear Colliders",
                "Are you sure you want to clear all generated colliders?",
                "Yes", "No"))
            {
                manager.ClearColliders();
                EditorUtility.SetDirty(manager);
                UnityEditor.SceneView.RepaintAll();
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // Refresh Colliders button
        if (GUILayout.Button("Refresh Colliders", GUILayout.Height(25)))
        {
            manager.RefreshColliders();
            EditorUtility.SetDirty(manager);
            UnityEditor.SceneView.RepaintAll();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Auto-Detect by Sprite: Scans all Tilemaps and automatically creates TileColliderData entries for each unique sprite.\n" +
            "• Groups tiles with the same sprite together (even if they're different TileBase assets)\n" +
            "• Perfect for tiles that look the same but have different names\n\n" +
            "Generate Colliders: Creates colliders for all tiles matching the configured TileColliderData.\n" +
            "• Automatically prevents duplicates if clicked multiple times\n" +
            "• Organizes colliders in folders (Tilemap Colliders > Colliders_[TilemapName])\n\n" +
            "Clear Colliders: Removes all generated colliders.\n\n" +
            "Refresh Colliders: Regenerates colliders (clears and generates again).",
            MessageType.Info);
    }
}

