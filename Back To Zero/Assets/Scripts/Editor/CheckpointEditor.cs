using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

[CustomEditor(typeof(Checkpoint))]
public class CheckpointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        Checkpoint checkpoint = (Checkpoint)target;
        
        // Draw default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // Tile-based mode helper buttons
        SerializedProperty modeProp = serializedObject.FindProperty("mode");
        
        // Always show helpers section, but highlight when in tile mode
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Helper Tools", EditorStyles.boldLabel);
        
        if (modeProp.enumValueIndex == (int)CheckpointMode.TileBased)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tile-Based Helpers", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "For a 3x3 summoning circle:\n" +
                "1. Set Tile Position to the top-left corner of the circle\n" +
                "2. Set Tile Size to (3, 3)\n" +
                "3. Click 'Find Tile Center' to auto-position the checkpoint",
                MessageType.Info
            );
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Get Tile Position From GameObject", GUILayout.Height(30)))
            {
                GetTilePositionFromGameObject(checkpoint);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Find Tile Center", GUILayout.Height(30)))
            {
                FindTileCenter(checkpoint);
            }
            
            if (GUILayout.Button("Validate Tile Position", GUILayout.Height(30)))
            {
                ValidateTilePosition(checkpoint);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Show calculated position
            SerializedProperty calcPosProp = serializedObject.FindProperty("calculatedPosition");
            if (calcPosProp != null)
            {
                EditorGUILayout.LabelField("Calculated Position:", calcPosProp.vector3Value.ToString("F2"));
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Switch to 'Tile Based' mode to use tile position helpers.",
                MessageType.Info
            );
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void FindTileCenter(Checkpoint checkpoint)
    {
        SerializedObject so = new SerializedObject(checkpoint);
        SerializedProperty tilePosProp = so.FindProperty("tilePosition");
        SerializedProperty tileSizeProp = so.FindProperty("tileSize");
        SerializedProperty targetTilemapProp = so.FindProperty("targetTilemap");
        SerializedProperty findTileCenterProp = so.FindProperty("findTileCenter");
        
        Vector3Int tilePos = tilePosProp.vector3IntValue;
        Vector2Int tileSize = tileSizeProp.vector2IntValue;
        Tilemap tilemap = targetTilemapProp.objectReferenceValue as Tilemap;
        
        // Find tilemap if not assigned
        if (tilemap == null)
        {
            tilemap = checkpoint.GetComponentInParent<Tilemap>();
            if (tilemap == null)
            {
                Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();
                if (allTilemaps.Length > 0)
                {
                    tilemap = allTilemaps[0];
                    targetTilemapProp.objectReferenceValue = tilemap;
                }
            }
        }
        
        if (tilemap == null)
        {
            EditorUtility.DisplayDialog("Error", "No Tilemap found! Please assign a Tilemap in the Target Tilemap field.", "OK");
            so.ApplyModifiedProperties();
            return;
        }
        
        // Calculate center tile
        Vector3Int centerTile = new Vector3Int(
            tilePos.x + tileSize.x / 2,
            tilePos.y + tileSize.y / 2,
            tilePos.z
        );
        
        // Convert to world position
        Vector3 worldPos = tilemap.CellToWorld(centerTile);
        Vector3 cellSize = tilemap.cellSize;
        
        // Calculate the center of the tile
        worldPos += new Vector3(
            cellSize.x * 0.5f,
            cellSize.y * 0.5f,
            0
        );
        
        // Move the GameObject
        Undo.RecordObject(checkpoint.transform, "Find Tile Center");
        checkpoint.transform.position = worldPos;
        
        // Enable findTileCenter flag
        findTileCenterProp.boolValue = true;
        
        so.ApplyModifiedProperties();
        
        EditorUtility.DisplayDialog("Success", 
            $"Checkpoint positioned at tile center:\n" +
            $"Tile: {centerTile}\n" +
            $"World Position: {worldPos}\n\n" +
            $"The GameObject has been moved to this position.",
            "OK");
    }
    
    private void ValidateTilePosition(Checkpoint checkpoint)
    {
        SerializedObject so = new SerializedObject(checkpoint);
        SerializedProperty tilePosProp = so.FindProperty("tilePosition");
        SerializedProperty tileSizeProp = so.FindProperty("tileSize");
        SerializedProperty targetTilemapProp = so.FindProperty("targetTilemap");
        
        Vector3Int tilePos = tilePosProp.vector3IntValue;
        Vector2Int tileSize = tileSizeProp.vector2IntValue;
        Tilemap tilemap = targetTilemapProp.objectReferenceValue as Tilemap;
        
        // Find tilemap if not assigned
        if (tilemap == null)
        {
            tilemap = checkpoint.GetComponentInParent<Tilemap>();
            if (tilemap == null)
            {
                Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();
                if (allTilemaps.Length > 0)
                {
                    tilemap = allTilemaps[0];
                }
            }
        }
        
        if (tilemap == null)
        {
            EditorUtility.DisplayDialog("Validation Failed", "No Tilemap found!", "OK");
            return;
        }
        
        // Check tiles in the area
        int tilesFound = 0;
        string tileInfo = "";
        
        for (int x = 0; x < tileSize.x; x++)
        {
            for (int y = 0; y < tileSize.y; y++)
            {
                Vector3Int checkPos = new Vector3Int(
                    tilePos.x + x,
                    tilePos.y + y,
                    tilePos.z
                );
                
                TileBase tile = tilemap.GetTile(checkPos);
                if (tile != null)
                {
                    tilesFound++;
                    if (tilesFound <= 5) // Show first 5 tiles
                    {
                        tileInfo += $"  Tile at ({checkPos.x}, {checkPos.y}): {tile.name}\n";
                    }
                }
            }
        }
        
        if (tilesFound > 5)
        {
            tileInfo += $"  ... and {tilesFound - 5} more tiles\n";
        }
        
        string message = $"Validation Results:\n\n" +
                        $"Tilemap: {tilemap.name}\n" +
                        $"Area: {tileSize.x}x{tileSize.y} starting at ({tilePos.x}, {tilePos.y})\n" +
                        $"Tiles Found: {tilesFound} / {tileSize.x * tileSize.y}\n\n" +
                        (tilesFound > 0 ? $"Found Tiles:\n{tileInfo}" : "No tiles found in this area!");
        
        EditorUtility.DisplayDialog("Tile Validation", message, "OK");
    }
    
    private void GetTilePositionFromGameObject(Checkpoint checkpoint)
    {
        SerializedObject so = new SerializedObject(checkpoint);
        SerializedProperty tilePosProp = so.FindProperty("tilePosition");
        SerializedProperty targetTilemapProp = so.FindProperty("targetTilemap");
        
        Tilemap tilemap = targetTilemapProp.objectReferenceValue as Tilemap;
        
        // Get world position once
        Vector3 worldPos = checkpoint.transform.position;
        
        // Find tilemap if not assigned
        if (tilemap == null)
        {
            tilemap = checkpoint.GetComponentInParent<Tilemap>();
            if (tilemap == null)
            {
                Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();
                if (allTilemaps.Length > 0)
                {
                    // Try to find one that has tiles near the GameObject position
                    foreach (Tilemap tm in allTilemaps)
                    {
                        Vector3Int cellPos = tm.WorldToCell(worldPos);
                        if (tm.GetTile(cellPos) != null)
                        {
                            tilemap = tm;
                            break;
                        }
                    }
                    // If none found, use first one
                    if (tilemap == null)
                    {
                        tilemap = allTilemaps[0];
                    }
                    targetTilemapProp.objectReferenceValue = tilemap;
                }
            }
        }
        
        if (tilemap == null)
        {
            EditorUtility.DisplayDialog("Error", "No Tilemap found! Please assign a Tilemap in the Target Tilemap field.", "OK");
            so.ApplyModifiedProperties();
            return;
        }
        
        // Convert GameObject's world position to tile coordinates
        Vector3Int tilePos = tilemap.WorldToCell(worldPos);
        
        // Update the tile position
        tilePosProp.vector3IntValue = tilePos;
        
        so.ApplyModifiedProperties();
        
        // Show info
        TileBase tile = tilemap.GetTile(tilePos);
        string tileName = tile != null ? tile.name : "None";
        
        EditorUtility.DisplayDialog("Tile Position Found", 
            $"Converted GameObject position to tile coordinates:\n\n" +
            $"World Position: {worldPos}\n" +
            $"Tile Position: ({tilePos.x}, {tilePos.y}, {tilePos.z})\n" +
            $"Tile at this position: {tileName}\n\n" +
            $"The Tile Position field has been updated.\n\n" +
            $"For a 3x3 area, this is the top-left corner.\n" +
            $"Set Tile Size to (3, 3) for a 3x3 summoning circle.",
            "OK");
    }
}
