using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Collections.Generic;

public class TilemapSplitter : EditorWindow
{
    private Tilemap sourceTilemap;
    private SplitMode splitMode = SplitMode.ByRegion;
    private int regionSizeX = 100;
    private int regionSizeY = 100;
    private bool copyTiles = true;
    private bool deleteOriginal = false;
    
    private enum SplitMode
    {
        ByRegion,      // Split into grid regions
        ByFunction,    // Split by tile type/function (manual)
        ByBounds       // Split by actual used bounds
    }

    [MenuItem("Tools/Tilemap Splitter")]
    public static void ShowWindow()
    {
        GetWindow<TilemapSplitter>("Tilemap Splitter");
    }

    void OnGUI()
    {
        GUILayout.Label("Split Large Tilemap", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Source tilemap selection
        sourceTilemap = (Tilemap)EditorGUILayout.ObjectField(
            "Source Tilemap",
            sourceTilemap,
            typeof(Tilemap),
            true
        );

        if (sourceTilemap == null)
        {
            EditorGUILayout.HelpBox("Select a Tilemap to split", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();

        // Split mode selection
        splitMode = (SplitMode)EditorGUILayout.EnumPopup("Split Mode", splitMode);

        EditorGUILayout.Space();

        // Mode-specific options
        switch (splitMode)
        {
            case SplitMode.ByRegion:
                EditorGUILayout.LabelField("Region Size (cells):", EditorStyles.boldLabel);
                regionSizeX = EditorGUILayout.IntField("Width", regionSizeX);
                regionSizeY = EditorGUILayout.IntField("Height", regionSizeY);
                EditorGUILayout.HelpBox(
                    $"Will split into regions of {regionSizeX}x{regionSizeY} cells.\n" +
                    "Each region becomes a separate tilemap.",
                    MessageType.Info
                );
                break;

            case SplitMode.ByFunction:
                EditorGUILayout.HelpBox(
                    "This mode requires manual setup.\n" +
                    "Create separate tilemaps for different functions:\n" +
                    "- Ground_Tilemap\n" +
                    "- Walls_Tilemap\n" +
                    "- Decorations_Tilemap\n" +
                    "Then use 'Copy Selected Tiles' to move tiles.",
                    MessageType.Info
                );
                break;

            case SplitMode.ByBounds:
                EditorGUILayout.HelpBox(
                    "Will split by compressing bounds.\n" +
                    "Creates separate tilemaps for disconnected areas.",
                    MessageType.Info
                );
                break;
        }

        EditorGUILayout.Space();

        // Options
        copyTiles = EditorGUILayout.Toggle("Copy Tiles to New Tilemaps", copyTiles);
        deleteOriginal = EditorGUILayout.Toggle("Delete Original After Split", deleteOriginal);

        if (deleteOriginal)
        {
            EditorGUILayout.HelpBox("WARNING: This will delete the original tilemap!", MessageType.Warning);
        }

        EditorGUILayout.Space();

        // Action buttons
        EditorGUI.BeginDisabledGroup(sourceTilemap == null);
        
        if (splitMode == SplitMode.ByRegion)
        {
            if (GUILayout.Button("Split by Region", GUILayout.Height(30)))
            {
                SplitByRegion();
            }
        }
        else if (splitMode == SplitMode.ByBounds)
        {
            if (GUILayout.Button("Split by Bounds", GUILayout.Height(30)))
            {
                SplitByBounds();
            }
        }
        else
        {
            if (GUILayout.Button("Copy Selected Tiles", GUILayout.Height(30)))
            {
                CopySelectedTiles();
            }
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "TIP: Use 'Tilemap Diagnostics' tool first to see your tilemap size.\n" +
            "Then split into manageable chunks (recommended: < 10,000 cells per tilemap).",
            MessageType.Info
        );
    }

    void SplitByRegion()
    {
        if (sourceTilemap == null) return;

        BoundsInt bounds = sourceTilemap.cellBounds;
        int totalRegionsX = Mathf.CeilToInt((float)bounds.size.x / regionSizeX);
        int totalRegionsY = Mathf.CeilToInt((float)bounds.size.y / regionSizeY);

        Debug.Log($"Splitting tilemap '{sourceTilemap.name}' into {totalRegionsX}x{totalRegionsY} = {totalRegionsX * totalRegionsY} regions");

        Grid grid = sourceTilemap.GetComponentInParent<Grid>();
        if (grid == null)
        {
            Debug.LogError("No Grid found! Tilemaps must be children of a Grid.");
            return;
        }

        List<Tilemap> createdTilemaps = new List<Tilemap>();

        // Create tilemaps for each region
        for (int regionY = 0; regionY < totalRegionsY; regionY++)
        {
            for (int regionX = 0; regionX < totalRegionsX; regionX++)
            {
                int startX = bounds.xMin + (regionX * regionSizeX);
                int startY = bounds.yMin + (regionY * regionSizeY);
                int endX = Mathf.Min(startX + regionSizeX, bounds.xMax);
                int endY = Mathf.Min(startY + regionSizeY, bounds.yMax);

                // Create new tilemap for this region
                GameObject tilemapObj = new GameObject($"{sourceTilemap.name}_Region_{regionX}_{regionY}");
                tilemapObj.transform.SetParent(grid.transform);
                tilemapObj.transform.localPosition = Vector3.zero;

                Tilemap newTilemap = tilemapObj.AddComponent<Tilemap>();
                TilemapRenderer renderer = tilemapObj.AddComponent<TilemapRenderer>();
                
                // Copy renderer settings from original
                TilemapRenderer originalRenderer = sourceTilemap.GetComponent<TilemapRenderer>();
                if (originalRenderer != null)
                {
                    renderer.sortingOrder = originalRenderer.sortingOrder;
                    renderer.sortingLayerID = originalRenderer.sortingLayerID;
                    renderer.mode = originalRenderer.mode;
                }

                // Copy tiles in this region
                if (copyTiles)
                {
                    int tilesCopied = 0;
                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            Vector3Int pos = new Vector3Int(x, y, 0);
                            TileBase tile = sourceTilemap.GetTile(pos);
                            if (tile != null)
                            {
                                newTilemap.SetTile(pos, tile);
                                tilesCopied++;
                            }
                        }
                    }
                    Debug.Log($"Region {regionX},{regionY}: Copied {tilesCopied} tiles");
                }

                createdTilemaps.Add(newTilemap);
            }
        }

        Debug.Log($"Created {createdTilemaps.Count} new tilemaps");

        // Delete original if requested
        if (deleteOriginal)
        {
            Undo.DestroyObjectImmediate(sourceTilemap.gameObject);
            Debug.Log("Original tilemap deleted");
        }
        else
        {
            Debug.Log("Original tilemap preserved. You can delete it manually if needed.");
        }

        // Select first new tilemap
        if (createdTilemaps.Count > 0)
        {
            Selection.activeGameObject = createdTilemaps[0].gameObject;
        }
    }

    void SplitByBounds()
    {
        if (sourceTilemap == null) return;

        // Find all used tiles and group them into connected regions
        BoundsInt bounds = sourceTilemap.cellBounds;
        HashSet<Vector3Int> usedPositions = new HashSet<Vector3Int>();

        // Collect all used positions
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (sourceTilemap.GetTile(pos) != null)
            {
                usedPositions.Add(pos);
            }
        }

        if (usedPositions.Count == 0)
        {
            Debug.LogWarning("No tiles found in tilemap!");
            return;
        }

        // Find bounding boxes of connected regions
        List<BoundsInt> regions = FindConnectedRegions(usedPositions, bounds);

        Debug.Log($"Found {regions.Count} separate regions");

        Grid grid = sourceTilemap.GetComponentInParent<Grid>();
        if (grid == null)
        {
            Debug.LogError("No Grid found!");
            return;
        }

        List<Tilemap> createdTilemaps = new List<Tilemap>();

        // Create a tilemap for each region
        for (int i = 0; i < regions.Count; i++)
        {
            BoundsInt regionBounds = regions[i];

            GameObject tilemapObj = new GameObject($"{sourceTilemap.name}_Region_{i}");
            tilemapObj.transform.SetParent(grid.transform);
            tilemapObj.transform.localPosition = Vector3.zero;

            Tilemap newTilemap = tilemapObj.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObj.AddComponent<TilemapRenderer>();

            // Copy renderer settings
            TilemapRenderer originalRenderer = sourceTilemap.GetComponent<TilemapRenderer>();
            if (originalRenderer != null)
            {
                renderer.sortingOrder = originalRenderer.sortingOrder;
                renderer.sortingLayerID = originalRenderer.sortingLayerID;
                renderer.mode = originalRenderer.mode;
            }

            // Copy tiles in this region
            if (copyTiles)
            {
                int tilesCopied = 0;
                foreach (Vector3Int pos in regionBounds.allPositionsWithin)
                {
                    TileBase tile = sourceTilemap.GetTile(pos);
                    if (tile != null)
                    {
                        newTilemap.SetTile(pos, tile);
                        tilesCopied++;
                    }
                }
                Debug.Log($"Region {i}: {regionBounds.size.x}x{regionBounds.size.y} cells, {tilesCopied} tiles");
            }

            createdTilemaps.Add(newTilemap);
        }

        // Delete original if requested
        if (deleteOriginal)
        {
            Undo.DestroyObjectImmediate(sourceTilemap.gameObject);
        }

        if (createdTilemaps.Count > 0)
        {
            Selection.activeGameObject = createdTilemaps[0].gameObject;
        }
    }

    List<BoundsInt> FindConnectedRegions(HashSet<Vector3Int> usedPositions, BoundsInt bounds)
    {
        List<BoundsInt> regions = new List<BoundsInt>();
        HashSet<Vector3Int> processed = new HashSet<Vector3Int>();

        foreach (Vector3Int startPos in usedPositions)
        {
            if (processed.Contains(startPos)) continue;

            // Flood fill to find connected region
            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            HashSet<Vector3Int> region = new HashSet<Vector3Int>();
            
            queue.Enqueue(startPos);
            region.Add(startPos);

            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();
                
                // Check 4 neighbors
                Vector3Int[] neighbors = new Vector3Int[]
                {
                    current + Vector3Int.right,
                    current + Vector3Int.left,
                    current + Vector3Int.up,
                    current + Vector3Int.down
                };

                foreach (Vector3Int neighbor in neighbors)
                {
                    if (usedPositions.Contains(neighbor) && !region.Contains(neighbor))
                    {
                        region.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // Calculate bounds for this region
            if (region.Count > 0)
            {
                int minX = int.MaxValue, minY = int.MaxValue;
                int maxX = int.MinValue, maxY = int.MinValue;

                foreach (Vector3Int pos in region)
                {
                    minX = Mathf.Min(minX, pos.x);
                    minY = Mathf.Min(minY, pos.y);
                    maxX = Mathf.Max(maxX, pos.x);
                    maxY = Mathf.Max(maxY, pos.y);
                    processed.Add(pos);
                }

                BoundsInt regionBounds = new BoundsInt(
                    new Vector3Int(minX, minY, 0),
                    new Vector3Int(maxX - minX + 1, maxY - minY + 1, 1)
                );
                regions.Add(regionBounds);
            }
        }

        return regions;
    }

    void CopySelectedTiles()
    {
        if (sourceTilemap == null)
        {
            EditorUtility.DisplayDialog("Error", "No source tilemap selected!", "OK");
            return;
        }

        // Get target tilemap from selection
        Tilemap targetTilemap = Selection.activeGameObject?.GetComponent<Tilemap>();
        if (targetTilemap == null)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Please select a target Tilemap GameObject first!\n\n" +
                "1. Create a new Tilemap\n" +
                "2. Select it\n" +
                "3. Click 'Copy Selected Tiles'",
                "OK"
            );
            return;
        }

        if (targetTilemap == sourceTilemap)
        {
            EditorUtility.DisplayDialog("Error", "Source and target cannot be the same!", "OK");
            return;
        }

        // Copy all tiles from source to target
        BoundsInt bounds = sourceTilemap.cellBounds;
        int tilesCopied = 0;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            TileBase tile = sourceTilemap.GetTile(pos);
            if (tile != null)
            {
                targetTilemap.SetTile(pos, tile);
                tilesCopied++;
            }
        }

        Debug.Log($"Copied {tilesCopied} tiles from '{sourceTilemap.name}' to '{targetTilemap.name}'");
        EditorUtility.DisplayDialog("Success", $"Copied {tilesCopied} tiles!", "OK");
    }
}

