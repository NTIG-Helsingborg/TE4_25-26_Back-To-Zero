using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

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
        if (GUILayout.Button("Auto-Detect Tiles by Sprite (All)", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "Auto-Detect Tiles by Sprite",
                "This will scan all Tilemaps and create TileColliderData entries for each unique sprite found.\n\n" +
                "Tiles with the same sprite (but different TileBase assets) will be grouped together.\n\n" +
                "Deleted tiles/sprites will not be re-added.\n\n" +
                "Continue?",
                "Yes", "No"))
            {
                manager.AutoDetectTilesBySprite();
                EditorUtility.SetDirty(manager);
                UnityEditor.SceneView.RepaintAll();
            }
        }
        
        // Controlled Auto Detect button
        GUI.backgroundColor = new Color(0.7f, 0.9f, 1f); // Light blue background
        if (GUILayout.Button("Controlled Auto Detect", GUILayout.Height(30)))
        {
            ShowControlledAutoDetectWindow(manager);
        }
        GUI.backgroundColor = Color.white; // Reset
        
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
            "Auto-Detect by Sprite (All): Scans all Tilemaps and automatically creates TileColliderData entries for each unique sprite.\n" +
            "• Groups tiles with the same sprite together (even if they're different TileBase assets)\n" +
            "• Perfect for tiles that look the same but have different names\n" +
            "• Deleted tiles/sprites are tracked and won't be re-added\n\n" +
            "Controlled Auto Detect: Select specific tiles from the Sprites folder to add.\n" +
            "• Choose which tiles to add manually\n" +
            "• Shows previews of tiles\n" +
            "• Respects exclusion list\n\n" +
            "Generate Colliders: Creates colliders for all tiles matching the configured TileColliderData.\n" +
            "• Automatically prevents duplicates if clicked multiple times\n" +
            "• Organizes colliders in folders (Tilemap Colliders > Colliders_[TilemapName])\n\n" +
            "Clear Colliders: Removes all generated colliders.\n\n" +
            "Refresh Colliders: Regenerates colliders (clears and generates again).",
            MessageType.Info);
    }
    
    private void ShowControlledAutoDetectWindow(TilemapColliderManager manager)
    {
        ControlledAutoDetectWindow.ShowWindow(manager);
    }
}

// Window for controlled auto-detect - selecting tiles/sprites from Sprites folder
public class ControlledAutoDetectWindow : EditorWindow
{
    private TilemapColliderManager manager;
    private List<TileBase> availableTiles = new List<TileBase>();
    private Dictionary<TileBase, bool> tileSelection = new Dictionary<TileBase, bool>();
    private Vector2 scrollPosition;
    private string searchFilter = "";
    
    public static void ShowWindow(TilemapColliderManager manager)
    {
        ControlledAutoDetectWindow window = GetWindow<ControlledAutoDetectWindow>("Controlled Auto Detect");
        window.manager = manager;
        window.LoadTilesFromSpritesFolder();
        window.Show();
    }
    
    private void LoadTilesFromSpritesFolder()
    {
        availableTiles.Clear();
        tileSelection.Clear();
        
        // Find all TileBase assets in the Sprites folder
        string[] guids = AssetDatabase.FindAssets("t:TileBase", new[] { "Assets/Sprites" });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            
            if (tile != null)
            {
                availableTiles.Add(tile);
                tileSelection[tile] = false;
            }
        }
        
        Debug.Log($"ControlledAutoDetect: Found {availableTiles.Count} tile(s) in Sprites folder.");
    }
    
    private void OnGUI()
    {
        if (manager == null)
        {
            EditorGUILayout.HelpBox("Manager not available.", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.LabelField("Controlled Auto Detect", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select tiles from the Sprites folder to add as TileColliderData entries.", MessageType.Info);
        EditorGUILayout.Space();
        
        // Search filter
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Select All / Deselect All buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All"))
        {
            foreach (var tile in availableTiles)
            {
                if (tile != null && MatchesFilter(tile))
                    tileSelection[tile] = true;
            }
        }
        if (GUILayout.Button("Deselect All"))
        {
            foreach (var tile in availableTiles)
            {
                if (tile != null)
                    tileSelection[tile] = false;
            }
        }
        if (GUILayout.Button("Refresh List"))
        {
            LoadTilesFromSpritesFolder();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Scrollable list of tiles
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        int visibleCount = 0;
        foreach (var tile in availableTiles)
        {
            if (tile == null) continue;
            
            if (!MatchesFilter(tile))
                continue;
            
            visibleCount++;
            
            if (!tileSelection.ContainsKey(tile))
                tileSelection[tile] = false;
            
            EditorGUILayout.BeginHorizontal();
            
            // Show tile preview if it's a Tile with a sprite
            Sprite previewSprite = null;
            if (tile is Tile)
            {
                previewSprite = ((Tile)tile).sprite;
            }
            
            if (previewSprite != null)
            {
                Rect previewRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
                DrawSpritePreview(previewRect, previewSprite);
            }
            else
            {
                GUILayout.Space(20);
            }
            
            // Tile name and checkbox
            string tileName = tile.name;
            tileSelection[tile] = EditorGUILayout.ToggleLeft(tileName, tileSelection[tile]);
            
            EditorGUILayout.EndHorizontal();
        }
        
        if (visibleCount == 0)
        {
            EditorGUILayout.HelpBox("No tiles found matching the search filter.", MessageType.Info);
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        
        // Show selection count
        int selectedCount = 0;
        foreach (var kvp in tileSelection)
        {
            if (kvp.Value && MatchesFilter(kvp.Key))
                selectedCount++;
        }
        
        EditorGUILayout.LabelField($"Selected: {selectedCount} tile(s)", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        // Action buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Selected Tiles", GUILayout.Height(30)))
        {
            List<TileBase> selectedTiles = new List<TileBase>();
            foreach (var kvp in tileSelection)
            {
                if (kvp.Value && kvp.Key != null && MatchesFilter(kvp.Key))
                {
                    selectedTiles.Add(kvp.Key);
                }
            }
            
            if (selectedTiles.Count == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select at least one tile.", "OK");
                return;
            }
            
            AddSelectedTiles(selectedTiles);
            
            EditorUtility.DisplayDialog(
                "Tiles Added",
                $"Added {selectedTiles.Count} tile(s) as TileColliderData entries.\n\n" +
                "Deleted tiles/sprites were not re-added.",
                "OK");
            
            Close();
        }
        
        if (GUILayout.Button("Cancel", GUILayout.Height(30), GUILayout.Width(100)))
        {
            Close();
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private bool MatchesFilter(TileBase tile)
    {
        if (string.IsNullOrEmpty(searchFilter))
            return true;
        
        return tile.name.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
    
    private void DrawSpritePreview(Rect rect, Sprite sprite)
    {
        if (sprite != null && sprite.texture != null)
        {
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
    
    private void AddSelectedTiles(List<TileBase> selectedTiles)
    {
        if (manager == null || selectedTiles == null || selectedTiles.Count == 0)
            return;
        
        // Get current collider data
        List<TileColliderData> newColliderData = new List<TileColliderData>(manager.colliderData);
        int addedCount = 0;
        
        foreach (TileBase tile in selectedTiles)
        {
            if (tile == null) continue;
            
            // Check if already exists
            bool alreadyExists = false;
            foreach (var existingData in newColliderData)
            {
                if (existingData != null && existingData.tileAsset == tile)
                {
                    alreadyExists = true;
                    break;
                }
            }
            
            // Check if excluded
            bool isExcluded = manager.excludedTiles.Contains(tile);
            
            if (!alreadyExists && !isExcluded)
            {
                // Get sprite from tile
                Sprite sprite = null;
                if (tile is Tile)
                {
                    sprite = ((Tile)tile).sprite;
                }
                
                // Check if sprite is excluded
                if (sprite != null && manager.excludedSprites.Contains(sprite))
                {
                    continue;
                }
                
                // Create new entry
                TileColliderData newData = new TileColliderData
                {
                    matchMode = MatchMode.ByTileAsset,
                    tileAsset = tile,
                    spriteAsset = sprite,
                    colliderType = ColliderType.Box,
                    isTrigger = false
                };
                
                newColliderData.Add(newData);
                addedCount++;
            }
        }
        
        // Update the array
        manager.colliderData = newColliderData.ToArray();
        EditorUtility.SetDirty(manager);
    }
}

