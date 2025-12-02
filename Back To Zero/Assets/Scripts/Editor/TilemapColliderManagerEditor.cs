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
        
        // Show folder structure info
        Transform collidersFolder = manager.transform.Find("Tilemap Colliders");
        if (collidersFolder != null && collidersFolder.childCount > 0)
        {
            EditorGUILayout.HelpBox(
                $"All colliders are organized in the 'Tilemap Colliders' folder with {collidersFolder.childCount} Tilemap subfolder(s).",
                MessageType.Info);
        }
        
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
            "Generate Colliders: Creates colliders for all tiles matching the configured TileColliderData.\n" +
            "• Automatically prevents duplicates if clicked multiple times\n" +
            "• Organizes colliders in folders (Tilemap Colliders > Colliders_[TilemapName])\n\n" +
            "Clear Colliders: Removes all generated colliders.\n\n" +
            "Refresh Colliders: Regenerates colliders (clears and generates again).",
            MessageType.Info);
    }
}

