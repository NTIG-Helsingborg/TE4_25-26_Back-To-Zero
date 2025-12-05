using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[CustomEditor(typeof(TilemapColliderManager))]
public class TilemapColliderManagerEditor : Editor
{
    private void ShowControlledAutoDetectWindow(TilemapColliderManager manager)
    {
        ControlledAutoDetectWindow.ShowWindow(manager);
    }

    // Visual Editor State
    private bool showVisualEditor = false;
    private int selectedLayerIndex = 0;
    private string[] availableLayers;
    private string[] displayLayers; // Includes "All" option

    private void OnEnable()
    {
        // Ensure we have the tool enabled if it was saved
        showVisualEditor = EditorPrefs.GetBool("TilemapColliderManager_ShowVisualEditor", false);
        selectedLayerIndex = EditorPrefs.GetInt("TilemapColliderManager_SelectedLayerIndex", 0);
    }

    private void OnDisable()
    {
        EditorPrefs.SetBool("TilemapColliderManager_ShowVisualEditor", showVisualEditor);
        EditorPrefs.SetInt("TilemapColliderManager_SelectedLayerIndex", selectedLayerIndex);
    }

    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Collider Management", EditorStyles.boldLabel);
        
        TilemapColliderManager manager = (TilemapColliderManager)target;
        
        // Visual Editor Toggle
        GUI.backgroundColor = showVisualEditor ? Color.green : Color.white;
        if (GUILayout.Button(showVisualEditor ? "Disable Visual Editor" : "Enable Visual Editor", GUILayout.Height(30)))
        {
            showVisualEditor = !showVisualEditor;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        if (showVisualEditor)
        {
            // Layer Filter Dropdown
            UpdateLayerList(manager);
            
            if (displayLayers != null && displayLayers.Length > 0)
            {
                EditorGUI.BeginChangeCheck();
                selectedLayerIndex = EditorGUILayout.Popup("Filter Layer", selectedLayerIndex, displayLayers);
                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.HelpBox(
                "VISUAL EDITOR ENABLED\n\n" +
                "• Red boxes appear over all generated colliders in the Scene View.\n" +
                "• CLICK a red box to delete that collider.\n" +
                "• Hold SHIFT to see the boxes but disable clicking (if they get in the way).", 
                MessageType.Warning);
        }
        
        EditorGUILayout.Space();

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

        // Remove Unwanted Colliders button
        if (GUILayout.Button("Remove Unwanted Colliders", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog(
                "Remove Unwanted Colliders",
                "This will remove any colliders that do not match the current collider data rules.\n\n" +
                "Use this if you have changed tiles to non-colliding ones and want to clean up old colliders without regenerating everything.",
                "Yes", "No"))
            {
                manager.RemoveUnwantedColliders();
                EditorUtility.SetDirty(manager);
                UnityEditor.SceneView.RepaintAll();
            }
        }
        
        // Rebuild All Colliders button (The "Direct Approach")
        GUI.backgroundColor = new Color(1f, 0.8f, 0.8f); // Light red warning color
        if (GUILayout.Button("Rebuild All Colliders", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "Rebuild All Colliders",
                "This will DELETE ALL generated colliders and regenerate them from scratch.\n\n" +
                "This is the safest way to ensure everything is correct, but might be slower on huge maps.",
                "Rebuild", "Cancel"))
            {
                manager.RebuildColliders();
                EditorUtility.SetDirty(manager);
                UnityEditor.SceneView.RepaintAll();
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space();
        
        // Bake Colliders button
        GUI.backgroundColor = new Color(1f, 0.7f, 0.3f); // Orange
        if (GUILayout.Button("🔥 Bake Colliders for Git", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "Bake Colliders for Git?",
                "This will remove all individual collider components and keep only the CompositeCollider2D.\n\n" +
                "✅ File size will shrink dramatically (great for Git)\n" +
                "❌ Visual Editor won't work until you Rebuild\n\n" +
                "You can always regenerate individual colliders by clicking 'Rebuild All Colliders'.",
                "Bake It!",
                "Cancel"))
            {
                manager.BakeColliders();
                EditorUtility.SetDirty(manager);
                
                // Mark the scene as dirty so Unity saves it
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                
                // Force save the scene
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                
                UnityEditor.SceneView.RepaintAll();
                
                EditorUtility.DisplayDialog("Bake Complete!", 
                    "Colliders baked successfully!\n\nScene has been saved. Check the file size now - it should be much smaller!", 
                    "OK");
            }
        }
        GUI.backgroundColor = Color.white;
        
        // Log Configuration button
        if (GUILayout.Button("Log Configuration to Console", GUILayout.Height(25)))
        {
            manager.LogConfiguration();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Auto-Detect by Sprite (All): Scans all Tilemaps and automatically creates TileColliderData entries for each unique sprite.\n" +
            "• Groups tiles with the same sprite together (even if they're different TileBase assets)\n" +
            "• Perfect for tiles that look the same but have different names\n" +
            "• Deleted tiles/sprites are tracked and won't be re-added\n\n" +
            "Controlled Auto Detect: Select specific tiles from Sprites and Village Assets folders to add.\n" +
            "• Choose which tiles to add manually\n" +
            "• Shows previews of tiles\n" +
            "• Searches both Sprites and Village Assets folders\n" +
            "• Respects exclusion list\n\n" +
            "Generate Colliders: Creates colliders for all tiles matching the configured TileColliderData.\n" +
            "• Automatically prevents duplicates if clicked multiple times\n" +
            "• Organizes colliders in folders (Tilemap Colliders > Colliders_[TilemapName])\n\n" +
            "Clear Colliders: Removes all generated colliders.\n\n" +
            "Refresh Colliders: Regenerates colliders (clears and generates again).\n\n" +
            "Remove Unwanted Colliders: Removes colliders that no longer match any valid rule.\n\n" +
            "Rebuild All Colliders: Completely deletes and regenerates all colliders (safest option).",
            MessageType.Info);
    }

    private void UpdateLayerList(TilemapColliderManager manager)
    {
        Transform container = manager.CollidersContainer;
        if (container == null)
        {
            availableLayers = new string[0];
            displayLayers = new string[] { "All Layers" };
            return;
        }

        int childCount = container.childCount;
        if (availableLayers == null || availableLayers.Length != childCount)
        {
            availableLayers = new string[childCount];
            displayLayers = new string[childCount + 1];
            displayLayers[0] = "All Layers";
            
            for (int i = 0; i < childCount; i++)
            {
                string name = container.GetChild(i).name.Replace("Colliders_", "");
                availableLayers[i] = name;
                displayLayers[i + 1] = name;
            }
        }
    }

    private void OnSceneGUI()
    {
        if (!showVisualEditor) return;

        TilemapColliderManager manager = (TilemapColliderManager)target;
        Transform container = manager.CollidersContainer;

        if (container == null)
        {
             // Try to find it again
             manager.EnsureCollidersContainer();
             container = manager.CollidersContainer;
             if (container == null) 
             {
                 Debug.LogWarning("VisualEditor: Container is null!");
                 return;
             }
        }

        // Update layers to ensure index is valid
        UpdateLayerList(manager);
        if (selectedLayerIndex >= displayLayers.Length) selectedLayerIndex = 0;

        // Iterate through all colliders
        // We do this manually to avoid GC allocs if possible, but for Editor tools it's less critical
        // However, modifying the list while iterating is tricky, so we'll collect actions
        
        // GameObject colliderToDelete = null; // No longer used
        Event currentEvent = Event.current;
        bool isClick = currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.shift;
        int colliderCount = 0;

        // Ensure handles are drawn on top of everything
        UnityEngine.Rendering.CompareFunction originalZTest = Handles.zTest;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        // Iterate subfolders
        for (int i = 0; i < container.childCount; i++)
        {
            Transform tilemapContainer = container.GetChild(i);
            if (tilemapContainer == null) continue;

            // FILTER: Check if this layer should be shown
            // selectedLayerIndex 0 is "All", 1 is first layer (index 0 in container)
            if (selectedLayerIndex != 0 && (selectedLayerIndex - 1) != i) continue;
            
            // Iterate colliders (Components now)
            Collider2D[] colliders = tilemapContainer.GetComponents<Collider2D>();
            
            foreach (Collider2D col in colliders)
            {
                if (col == null) continue;
                if (col is CompositeCollider2D) continue; // Skip the composite itself

                colliderCount++;
                Bounds bounds = col.bounds;
                Vector3 center = bounds.center;
                Vector3 size = bounds.size;

                // Draw handle - Make it more visible (Solid Red)
                Handles.color = new Color(1f, 0f, 0f, 0.5f); // 50% transparent red
                Handles.DrawSolidRectangleWithOutline(
                    new Rect(center.x - size.x/2, center.y - size.y/2, size.x, size.y), 
                    new Color(1f, 0f, 0f, 0.4f), 
                    Color.yellow); // Yellow outline for better contrast

                // Check for click
                Handles.color = Color.clear; // Invisible button over the area
                if (Handles.Button(center, Quaternion.identity, size.x, size.y, Handles.RectangleHandleCap))
                {
                    if (!currentEvent.shift) // Allow Shift to bypass deletion (select underneath)
                    {
                        Undo.RegisterCompleteObjectUndo(manager, "Delete Collider");
                        manager.RemoveCollider(col);
                        EditorUtility.SetDirty(manager);
                        
                        // Consume event
                        currentEvent.Use();
                    }
                }
            }
        }

        // Restore ZTest
        Handles.zTest = originalZTest;

        // Debug Log to confirm it ran
        if (Event.current.type == EventType.Repaint)
        {
            // Only log occasionally to avoid spam, or just once per interaction
            // For now, let's print if count is 0
            if (colliderCount == 0)
            {
                 Debug.LogWarning($"VisualEditor: Found 0 colliders to draw. Container children: {container.childCount}");
            }
        }

        // Show feedback if no colliders found
        if (colliderCount == 0)
        {
            Handles.BeginGUI();
            GUI.color = Color.yellow;
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label("VISUAL EDITOR: No colliders found.\nTry 'Generate Colliders' first.", EditorStyles.boldLabel);
            GUILayout.EndArea();
            Handles.EndGUI();
        }
        else
        {
            // Show instructions in Scene View
            Handles.BeginGUI();
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label($"VISUAL EDITOR: {colliderCount} colliders shown.\nClick Red Box to Delete.\nHold Shift to Select.", EditorStyles.boldLabel);
            GUILayout.EndArea();
            Handles.EndGUI();
        }
        
        // Force repaint to show updates
        if (Event.current.type == EventType.MouseMove)
        {
            SceneView.RepaintAll();
        }
    }
}

// Window for controlled auto-detect - selecting tiles/sprites from Sprites and Village Assets folders
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
        string[] spritesGuids = AssetDatabase.FindAssets("t:TileBase", new[] { "Assets/Sprites" });
        
        // Find all TileBase assets in Village Assets folder and its subfolders
        // AssetDatabase.FindAssets searches recursively, so it will find tiles in:
        // - Assets/Village Assets/A Hard Day's Work/
        // - Assets/Village Assets/Pixel Lands Village/
        string[] villageGuids = AssetDatabase.FindAssets("t:TileBase", new[] { "Assets/Village Assets" });
        
        // Combine both arrays
        string[] allGuids = new string[spritesGuids.Length + villageGuids.Length];
        System.Array.Copy(spritesGuids, 0, allGuids, 0, spritesGuids.Length);
        System.Array.Copy(villageGuids, 0, allGuids, spritesGuids.Length, villageGuids.Length);
        
        // Track tiles by folder for better logging
        int spritesCount = 0;
        int villageCount = 0;
        int hardDaysWorkCount = 0;
        int pixelLandsCount = 0;
        
        foreach (string guid in allGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            
            if (tile != null)
            {
                availableTiles.Add(tile);
                tileSelection[tile] = false;
                
                // Count by folder for logging
                if (path.Contains("Assets/Sprites"))
                {
                    spritesCount++;
                }
                else if (path.Contains("Assets/Village Assets"))
                {
                    villageCount++;
                    if (path.Contains("A Hard Day's Work"))
                    {
                        hardDaysWorkCount++;
                    }
                    else if (path.Contains("Pixel Lands Village"))
                    {
                        pixelLandsCount++;
                    }
                }
            }
        }
        
        Debug.Log($"ControlledAutoDetect: Found {availableTiles.Count} total tile(s):\n" +
                  $"  - Sprites folder: {spritesCount}\n" +
                  $"  - Village Assets folder: {villageCount}\n" +
                  $"    • A Hard Day's Work: {hardDaysWorkCount}\n" +
                  $"    • Pixel Lands Village: {pixelLandsCount}");
    }
    
    private void OnGUI()
    {
        if (manager == null)
        {
            EditorGUILayout.HelpBox("Manager not available.", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.LabelField("Controlled Auto Detect", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select tiles from the Sprites and Village Assets folders to add as TileColliderData entries.", MessageType.Info);
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

