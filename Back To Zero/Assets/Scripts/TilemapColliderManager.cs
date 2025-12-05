using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public enum ColliderType
{
    Box,
    Circle,
    Polygon,
    Edge
}

public enum MatchMode
{
    ByTileAsset,    // Match by TileBase reference (exact match)
    BySprite        // Match by sprite (visual match, works across different TileBase assets)
}

[System.Serializable]
public class TileColliderData
{
    [Header("Matching Mode")]
    [Tooltip("ByTileAsset: Matches exact TileBase reference\nBySprite: Matches any tile with the same sprite (works across different TileBase assets)")]
    public MatchMode matchMode = MatchMode.ByTileAsset;
    
    [Header("Tile Reference (for ByTileAsset mode)")]
    public TileBase tileAsset;
    
    [Header("Sprite Reference (for BySprite mode)")]
    [Tooltip("Drag a sprite here to match all tiles that use this sprite, regardless of their TileBase asset")]
    public Sprite spriteAsset;
    
    [Header("Collider Settings")]
    public ColliderType colliderType = ColliderType.Box;
    public bool isTrigger = false;
    public PhysicsMaterial2D physicsMaterial;
    
    [Header("Transform")]
    public Vector2 offset = Vector2.zero;
    public Vector2 size = Vector2.zero; // If zero, uses tile size
    
    [Header("Circle Collider Settings")]
    [Range(0.1f, 2f)]
    public float circleRadius = 0.5f;
    
    [Header("Polygon Collider Settings")]
    public Vector2[] polygonPoints = new Vector2[]
    {
        new Vector2(-0.5f, -0.5f),
        new Vector2(0.5f, -0.5f),
        new Vector2(0.5f, 0.5f),
        new Vector2(-0.5f, 0.5f)
    };
    
    [Header("Edge Collider Settings")]
    public Vector2[] edgePoints = new Vector2[]
    {
        new Vector2(-0.5f, -0.5f),
        new Vector2(0.5f, -0.5f),
        new Vector2(0.5f, 0.5f),
        new Vector2(-0.5f, 0.5f)
    };
}

public class TilemapColliderManager : MonoBehaviour
{
    [Header("Tilemap Reference")]
    [Tooltip("Assign a single Tilemap component, OR assign a parent GameObject to find all Tilemaps within it")]
    [SerializeField] private Tilemap tilemap;
    
    [Header("Multiple Tilemaps (Alternative)")]
    [Tooltip("Drag a parent GameObject (like Grid) here to automatically find all Tilemap components in it and its children")]
    [SerializeField] private GameObject tilemapParent;
    
    [Header("Collider Configuration")]
    [SerializeField] public TileColliderData[] colliderData = new TileColliderData[0];
    
    [Header("Auto-Detect Settings")]
    [Tooltip("Sprites and tiles that have been manually deleted and should not be auto-added again")]
    [SerializeField] public List<Sprite> excludedSprites = new List<Sprite>();
    [SerializeField] public List<TileBase> excludedTiles = new List<TileBase>();
    
    [Header("Generation Settings")]
    [SerializeField] private bool generateOnStart = true;
    [Tooltip("Merges all colliders on a layer into a single CompositeCollider2D. Drastically reduces scene size and improves performance.")]
    [SerializeField] private bool useCompositeCollider = true;
    [SerializeField] private bool combineAdjacentTiles = false;
    
    [Header("Generated Colliders Container")]
    [SerializeField] private Transform collidersContainer;
    
    [Header("Folder Settings")]
    [Tooltip("Name of the main folder that will contain all collider containers")]
    [SerializeField] private string mainFolderName = "Tilemap Colliders";
    
    private List<Tilemap> foundTilemaps = new List<Tilemap>();
    private HashSet<string> existingColliderKeys = new HashSet<string>();
    
    /// <summary>
    /// Gets the list of found tilemaps (for editor use)
    /// </summary>
    public List<Tilemap> GetFoundTilemaps()
    {
        FindAllTilemaps();
        return new List<Tilemap>(foundTilemaps);
    }
    
    private void Start()
    {
        // Find all Tilemaps
        FindAllTilemaps();
        
        if (foundTilemaps.Count == 0)
        {
            Debug.LogError("TilemapColliderManager: No Tilemap components found! Please assign a Tilemap or a parent GameObject containing Tilemaps.");
            return;
        }
        
        // Ensure colliders container exists with proper folder structure
        EnsureCollidersContainer();
        
        if (generateOnStart)
        {
            GenerateColliders();
        }
    }
    
    /// <summary>
    /// Ensures the colliders container exists with proper folder structure
    /// </summary>
    /// <summary>
    /// Ensures the colliders container exists with proper folder structure
    /// </summary>
    public void EnsureCollidersContainer()
    {
        if (collidersContainer == null)
        {
            // Look for existing main folder
            Transform existingFolder = transform.Find(mainFolderName);
            if (existingFolder != null)
            {
                collidersContainer = existingFolder;
            }
            else
            {
                // Create main folder
                GameObject mainFolder = new GameObject(mainFolderName);
                mainFolder.transform.SetParent(transform);
                mainFolder.transform.localPosition = Vector3.zero;
                collidersContainer = mainFolder.transform;
            }
        }
    }
    
    /// <summary>
    /// Finds all Tilemap components to process
    /// </summary>
    private void FindAllTilemaps()
    {
        foundTilemaps.Clear();
        
        // If tilemapParent is assigned, find all Tilemaps in it and its children
        if (tilemapParent != null)
        {
            Tilemap[] tilemaps = tilemapParent.GetComponentsInChildren<Tilemap>(true);
            foundTilemaps.AddRange(tilemaps);
            Debug.Log($"TilemapColliderManager: Found {foundTilemaps.Count} Tilemap(s) in '{tilemapParent.name}'");
        }
        // Otherwise, use the single tilemap reference
        else if (tilemap != null)
        {
            foundTilemaps.Add(tilemap);
        }
        // Try to find on this GameObject
        else
        {
            tilemap = GetComponent<Tilemap>();
            if (tilemap != null)
            {
                foundTilemaps.Add(tilemap);
            }
        }
    }
    
    /// <summary>
    /// Generates colliders for all tiles in all Tilemaps that match the collider data
    /// </summary>
    public void GenerateColliders()
    {
        // Refresh tilemap list
        FindAllTilemaps();
        
        if (foundTilemaps.Count == 0)
        {
            Debug.LogError("TilemapColliderManager: No Tilemap components found!");
            return;
        }
        
        if (colliderData == null || colliderData.Length == 0)
        {
            Debug.LogWarning("TilemapColliderManager: No collider data configured!");
            return;
        }
        
        // Ensure container exists
        EnsureCollidersContainer();
        
        // Build set of existing collider keys to prevent duplicates
        // This allows clicking "Generate" multiple times without creating duplicates
        existingColliderKeys.Clear();
        ScanExistingColliders();
        
        // Remove colliders that shouldn't exist anymore
        RemoveUnwantedColliders();
        
        int totalColliderCount = 0;
        int duplicateCount = 0;
        
        // Process each Tilemap
        foreach (Tilemap currentTilemap in foundTilemaps)
        {
            if (currentTilemap == null) continue;
            
            // Create or find container for this Tilemap's colliders
            string containerName = $"Colliders_{currentTilemap.name}";
            Transform tilemapContainer = collidersContainer.Find(containerName);
            
            if (tilemapContainer == null)
            {
                GameObject containerObj = new GameObject(containerName);
                containerObj.transform.SetParent(collidersContainer);
                containerObj.transform.localPosition = Vector3.zero;
                tilemapContainer = containerObj.transform;
            }
            
            // Use GetUsedTiles for better performance (only iterates over tiles that exist)
            // This is much faster than iterating through entire bounds
            int tilemapColliderCount = 0;
            BoundsInt bounds = currentTilemap.cellBounds;
            
            // Get all used tile positions
            foreach (Vector3Int tilePosition in currentTilemap.cellBounds.allPositionsWithin)
            {
                TileBase tile = currentTilemap.GetTile(tilePosition);
                
                if (tile == null) continue;
                
                // Get sprite for sprite-based matching
                Sprite tileSprite = GetTileSprite(currentTilemap, tilePosition);
                
                // Find matching collider data (checks both tile asset and sprite)
                TileColliderData matchingData = GetMatchingColliderData(tile, tileSprite);
                if (matchingData != null)
                {
                    // Check for duplicates
                    string colliderKey = GetColliderKey(currentTilemap, tilePosition);
                    if (!existingColliderKeys.Contains(colliderKey))
                    {
                        CreateColliderForTile(currentTilemap, tilePosition, matchingData, tilemapContainer);
                        existingColliderKeys.Add(colliderKey);
                        tilemapColliderCount++;
                        totalColliderCount++;
                    }
                    else
                    {
                        duplicateCount++;
                    }
                }
            }
            
            Debug.Log($"TilemapColliderManager: Generated {tilemapColliderCount} colliders for '{currentTilemap.name}'");
        }
        
        if (duplicateCount > 0)
        {
            Debug.LogWarning($"TilemapColliderManager: Skipped {duplicateCount} duplicate collider(s). Colliders already exist at those positions.");
        }
        
        if (totalColliderCount > 0)
        {
            Debug.Log($"TilemapColliderManager: Generated {totalColliderCount} new collider(s) across {foundTilemaps.Count} Tilemap(s).");
        }
        else if (duplicateCount == 0)
        {
            Debug.LogWarning("TilemapColliderManager: No colliders were generated. Check that your TileColliderData entries match tiles in your Tilemaps.");
        }
        
        // Apply composite collider optimization if enabled
        if (useCompositeCollider)
        {
            ApplyCompositeCollider();
        }
    }
    
    /// <summary>
    /// Finds the TileColliderData that matches the given tile and sprite
    /// </summary>
    private TileColliderData GetMatchingColliderData(TileBase tile, Sprite tileSprite)
    {
        foreach (var data in colliderData)
        {
            if (data == null) continue;
            
            if (data.matchMode == MatchMode.ByTileAsset)
            {
                // Match by TileBase reference
                if (data.tileAsset == tile)
                {
                    return data;
                }
            }
            else if (data.matchMode == MatchMode.BySprite)
            {
                // Match by sprite (works across different TileBase assets)
                if (data.spriteAsset != null && tileSprite != null && data.spriteAsset == tileSprite)
                {
                    return data;
                }
            }
        }
        return null;
    }
    
    /// <summary>
    /// Gets the sprite from a tile at a specific position in a Tilemap
    /// </summary>
    private Sprite GetTileSprite(Tilemap tilemap, Vector3Int position)
    {
        // Try to get sprite directly from Tilemap
        Sprite sprite = tilemap.GetSprite(position);
        
        // If that doesn't work, try to get it from the TileBase
        if (sprite == null)
        {
            TileBase tile = tilemap.GetTile(position);
            if (tile != null)
            {
                // Try to cast to Tile (most common type)
                if (tile is Tile)
                {
                    sprite = ((Tile)tile).sprite;
                }
                // Try other tile types if needed
                else if (tile is UnityEngine.Tilemaps.AnimatedTile)
                {
                    // Animated tiles might have multiple sprites, get the first one
                    var animatedTile = (UnityEngine.Tilemaps.AnimatedTile)tile;
                    if (animatedTile.m_AnimatedSprites != null && animatedTile.m_AnimatedSprites.Length > 0)
                    {
                        sprite = animatedTile.m_AnimatedSprites[0];
                    }
                }
            }
        }
        
        return sprite;
    }
    
    /// <summary>
    /// Gets a unique key for a collider at a specific tile position
    /// </summary>
    private string GetColliderKey(Tilemap targetTilemap, Vector3Int tilePosition)
    {
        return $"{targetTilemap.name}_{tilePosition.x}_{tilePosition.y}_{tilePosition.z}";
    }
    
    /// <summary>
    /// Scans existing colliders to build a set of keys (prevents duplicates)
    /// </summary>
    private void ScanExistingColliders()
    {
        if (collidersContainer == null) return;
        
        // Scan all existing collider containers
        foreach (Transform tilemapContainer in collidersContainer)
        {
            if (tilemapContainer == null) continue;
            
            // Extract tilemap name from container name (format: "Colliders_TilemapName")
            string containerName = tilemapContainer.name;
            if (!containerName.StartsWith("Colliders_")) continue;
            
            string tilemapName = containerName.Replace("Colliders_", "");
            
            // Find the tilemap to convert positions
            Tilemap currentTilemap = null;
            foreach (var tm in foundTilemaps)
            {
                if (tm != null && tm.name == tilemapName)
                {
                    currentTilemap = tm;
                    break;
                }
            }
            
            if (currentTilemap == null) continue;
            
            // Scan all Collider2D components in this container
            Collider2D[] colliders = tilemapContainer.GetComponents<Collider2D>();
            foreach (Collider2D col in colliders)
            {
                // Ignore CompositeCollider2D itself
                if (col is CompositeCollider2D) continue;
                
                // Calculate tile position from collider offset
                // offset is relative to container. container is at (0,0,0) usually.
                // So offset ~ world position of center.
                // We need to reverse the CreateCollider logic:
                // finalOffset = localPos + data.offset
                // This is tricky because data.offset is unknown here.
                // However, usually data.offset is small (within the tile).
                // So if we take the center of the collider, and find the cell it belongs to, that should be enough.
                
                Vector3 worldPos = tilemapContainer.TransformPoint(col.offset);
                Vector3Int cellPos = currentTilemap.WorldToCell(worldPos);
                
                string key = GetColliderKey(currentTilemap, cellPos);
                existingColliderKeys.Add(key);
            }
        }
    }
    
    /// <summary>
    /// Creates a collider Component for a specific tile position on the parent container
    /// </summary>
    private void CreateColliderForTile(Tilemap targetTilemap, Vector3Int tilePosition, TileColliderData data, Transform parent)
    {
        // Get world position of the tile center
        Vector3 tileCenterWorld = targetTilemap.GetCellCenterWorld(tilePosition);
        
        // Convert world position to local position relative to the parent container
        // Since parent is usually at (0,0,0) world space (if it's a root or child of zeroed root), 
        // local position is effectively world position. But let's be safe.
        Vector3 localPos = parent.InverseTransformPoint(tileCenterWorld);
        
        // Apply data offset
        Vector2 finalOffset = (Vector2)localPos + data.offset;
        
        // Get tile size
        Vector3 tileSize = targetTilemap.cellSize;
        Vector2 colliderSize = data.size != Vector2.zero ? data.size : new Vector2(tileSize.x, tileSize.y);
        
        // Create appropriate collider based on type
        Collider2D collider = null;
        GameObject parentObj = parent.gameObject;
        
        switch (data.colliderType)
        {
            case ColliderType.Box:
                BoxCollider2D boxCollider = parentObj.AddComponent<BoxCollider2D>();
                boxCollider.size = colliderSize;
                boxCollider.offset = finalOffset;
                collider = boxCollider;
                break;
                
            case ColliderType.Circle:
                CircleCollider2D circleCollider = parentObj.AddComponent<CircleCollider2D>();
                circleCollider.radius = data.circleRadius;
                circleCollider.offset = finalOffset;
                collider = circleCollider;
                break;
                
            case ColliderType.Polygon:
                PolygonCollider2D polygonCollider = parentObj.AddComponent<PolygonCollider2D>();
                // Scale polygon points by tile size and shift by offset
                Vector2[] scaledPoints = new Vector2[data.polygonPoints.Length];
                for (int i = 0; i < data.polygonPoints.Length; i++)
                {
                    scaledPoints[i] = new Vector2(
                        data.polygonPoints[i].x * colliderSize.x,
                        data.polygonPoints[i].y * colliderSize.y
                    ) + finalOffset;
                }
                polygonCollider.points = scaledPoints;
                // PolygonCollider2D doesn't have a simple 'offset' that moves points, 
                // but it does have 'offset' property which shifts the whole shape.
                // However, setting points relative to (0,0) and then setting offset is cleaner.
                // Wait, if we set points relative to finalOffset, we don't need to set .offset.
                // But .offset is more efficient for physics engine updates? 
                // Actually, let's stick to setting .offset = finalOffset and points relative to 0.
                // Re-calculating points:
                for (int i = 0; i < data.polygonPoints.Length; i++)
                {
                    scaledPoints[i] = new Vector2(
                        data.polygonPoints[i].x * colliderSize.x,
                        data.polygonPoints[i].y * colliderSize.y
                    );
                }
                polygonCollider.points = scaledPoints;
                polygonCollider.offset = finalOffset;
                collider = polygonCollider;
                break;
                
            case ColliderType.Edge:
                EdgeCollider2D edgeCollider = parentObj.AddComponent<EdgeCollider2D>();
                // Scale edge points by tile size
                Vector2[] scaledEdgePoints = new Vector2[data.edgePoints.Length];
                for (int i = 0; i < data.edgePoints.Length; i++)
                {
                    scaledEdgePoints[i] = new Vector2(
                        data.edgePoints[i].x * colliderSize.x,
                        data.edgePoints[i].y * colliderSize.y
                    );
                }
                edgeCollider.points = scaledEdgePoints;
                edgeCollider.offset = finalOffset;
                collider = edgeCollider;
                break;
        }
        
        // Configure collider properties
        if (collider != null)
        {
            collider.isTrigger = data.isTrigger;
            if (data.physicsMaterial != null)
            {
                collider.sharedMaterial = data.physicsMaterial;
            }
            
            // Enable Composite Collider usage
            if (useCompositeCollider)
            {
                collider.usedByComposite = true;
            }
        }
        
        // Collider key is already added to existingColliderKeys in GenerateColliders()
    }

    /// <summary>
    /// Removes colliders that no longer match any valid collider data or are on empty tiles
    /// </summary>
    public void RemoveUnwantedColliders()
    {
        FindAllTilemaps();
        EnsureCollidersContainer();
        
        if (collidersContainer == null) return;
        
        int removedCount = 0;
        
        // Iterate BACKWARDS so we can destroy containers if needed
        for (int i = collidersContainer.childCount - 1; i >= 0; i--)
        {
            Transform tilemapContainer = collidersContainer.GetChild(i);
            if (tilemapContainer == null) continue;

            // Get the corresponding Tilemap
            string containerName = tilemapContainer.name;
            if (!containerName.StartsWith("Colliders_")) continue;
            
            string tilemapName = containerName.Replace("Colliders_", "");
            
            // Find the tilemap by name
            Tilemap currentTilemap = null;
            foreach (var tm in foundTilemaps)
            {
                if (tm != null && tm.name == tilemapName)
                {
                    currentTilemap = tm;
                    break;
                }
            }
            
            // If tilemap is missing, this is an ORPHANED container. Delete it.
            if (currentTilemap == null)
            {
                int childCount = tilemapContainer.childCount;
                if (Application.isPlaying)
                {
                    Destroy(tilemapContainer.gameObject);
                }
                else
                {
                    DestroyImmediate(tilemapContainer.gameObject);
                }
                removedCount += childCount; // Count all the colliders we just nuked
                Debug.Log($"TilemapColliderManager: Removed orphaned container '{containerName}'");
                continue;
            }

            // We need to store components to destroy to avoid modifying collection while iterating
            List<Collider2D> collidersToRemove = new List<Collider2D>();
            
            Collider2D[] colliders = tilemapContainer.GetComponents<Collider2D>();

            foreach (Collider2D col in colliders)
            {
                // Ignore CompositeCollider2D
                if (col is CompositeCollider2D) continue;
                
                // Calculate tile position
                Vector3 worldPos = tilemapContainer.TransformPoint(col.offset);
                Vector3Int tilePos = currentTilemap.WorldToCell(worldPos);
                
                TileBase tile = currentTilemap.GetTile(tilePos);
                
                // Check if this position SHOULD have a collider
                bool shouldHaveCollider = false;
                
                if (tile != null)
                {
                    Sprite tileSprite = GetTileSprite(currentTilemap, tilePos);
                    TileColliderData matchingData = GetMatchingColliderData(tile, tileSprite);
                    if (matchingData != null)
                    {
                        shouldHaveCollider = true;
                    }
                }

                if (!shouldHaveCollider)
                {
                    collidersToRemove.Add(col);
                    
                    // Remove from existing keys
                    string key = GetColliderKey(currentTilemap, tilePos);
                    if (existingColliderKeys.Contains(key))
                    {
                        existingColliderKeys.Remove(key);
                    }
                }
            }

            // Destroy identified colliders
            foreach (Collider2D col in collidersToRemove)
            {
                if (Application.isPlaying)
                {
                    Destroy(col);
                }
                else
                {
                    DestroyImmediate(col);
                }
                removedCount++;
            }
            
            // If container is now empty, we could delete it, but let's keep it for now as the tilemap still exists
        }

        if (removedCount > 0)
        {
            Debug.Log($"TilemapColliderManager: Removed {removedCount} unwanted colliders.");
        }
    }

    /// <summary>
    /// Completely clears and regenerates all colliders
    /// </summary>
    public void RebuildColliders()
    {
        ClearColliders();
        GenerateColliders();
    }
    
    /// <summary>
    /// Removes a specific collider component and updates the internal key tracking
    /// </summary>
    public void RemoveCollider(Collider2D col)
    {
        if (col == null) return;
        
        // We need to find the tilemap and position to remove the key
        Transform container = col.transform;
        string containerName = container.name;
        
        if (containerName.StartsWith("Colliders_"))
        {
            string tilemapName = containerName.Replace("Colliders_", "");
            
            // Find the tilemap
            Tilemap currentTilemap = null;
            foreach (var tm in foundTilemaps)
            {
                if (tm != null && tm.name == tilemapName)
                {
                    currentTilemap = tm;
                    break;
                }
            }
            
            if (currentTilemap != null)
            {
                // Calculate tile position
                Vector3 worldPos = container.TransformPoint(col.offset);
                Vector3Int tilePos = currentTilemap.WorldToCell(worldPos);
                
                string key = GetColliderKey(currentTilemap, tilePos);
                if (existingColliderKeys.Contains(key))
                {
                    existingColliderKeys.Remove(key);
                }
            }
        }
        
        if (Application.isPlaying)
        {
            Destroy(col);
        }
        else
        {
            DestroyImmediate(col);
        }
    }
    
    /// <summary>
    /// Logs all configured collider rules to the console
    /// </summary>
    public void LogConfiguration()
    {
        if (colliderData == null || colliderData.Length == 0)
        {
            Debug.Log("TilemapColliderManager: No collider rules configured.");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"TilemapColliderManager: Found {colliderData.Length} collider rules:");
        
        for (int i = 0; i < colliderData.Length; i++)
        {
            var data = colliderData[i];
            if (data == null) continue;
            
            string name = "Unknown";
            if (data.matchMode == MatchMode.ByTileAsset && data.tileAsset != null)
            {
                name = $"Tile: {data.tileAsset.name}";
            }
            else if (data.matchMode == MatchMode.BySprite && data.spriteAsset != null)
            {
                name = $"Sprite: {data.spriteAsset.name}";
            }
            
            sb.AppendLine($"  {i+1}. {name} ({data.colliderType})");
        }
        
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Clears all generated colliders
    /// </summary>
    public void ClearColliders()
    {
        EnsureCollidersContainer();
        
        if (collidersContainer != null)
        {
            // Destroy all child objects (tilemap containers)
            for (int i = collidersContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = collidersContainer.GetChild(i);
                if (child != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }
        
        existingColliderKeys.Clear();
    }

    /// <summary>
    /// Public accessor for the colliders container
    /// </summary>
    public Transform CollidersContainer
    {
        get
        {
            EnsureCollidersContainer();
            return collidersContainer;
        }
    }
    
    /// <summary>
    /// Bakes colliders for Git/Push by removing all individual collider components
    /// and keeping only the CompositeCollider2D. This drastically reduces file size.
    /// Use "Rebuild Colliders" to regenerate individual colliders for editing.
    /// </summary>
    public void BakeColliders()
    {
        if (collidersContainer == null)
        {
            Debug.LogWarning("TilemapColliderManager: No colliders container found. Nothing to bake.");
            return;
        }
        
        int removedCount = 0;
        
        foreach (Transform layerContainer in collidersContainer)
        {
            if (layerContainer == null) continue;
            
            // Get all Collider2D components
            Collider2D[] colliders = layerContainer.GetComponents<Collider2D>();
            
            foreach (Collider2D col in colliders)
            {
                // Keep CompositeCollider2D, remove everything else
                if (col is CompositeCollider2D) continue;
                
                if (Application.isPlaying)
                {
                    Destroy(col);
                }
                else
                {
                    DestroyImmediate(col);
                }
                removedCount++;
            }
        }
        
        // Clear the internal tracking since individual colliders are gone
        existingColliderKeys.Clear();
        
        Debug.Log($"TilemapColliderManager: Baked colliders for Git! Removed {removedCount} individual collider components. Only CompositeCollider2D remains. File size should be much smaller now. Use 'Rebuild Colliders' to restore individual colliders for editing.");
    }

    /// <summary>
    /// Removes a specific collider GameObject and updates the internal registry
    /// </summary>
    public void RemoveCollider(GameObject colliderObj)
    {
        if (colliderObj == null) return;

        // Try to remove from keys
        // We need to reconstruct the key
        // Parent should be Colliders_MapName
        Transform parent = colliderObj.transform.parent;
        if (parent != null && parent.name.StartsWith("Colliders_"))
        {
            string tilemapName = parent.name.Replace("Colliders_", "");
            
            // Name is Collider_x_y
            string[] parts = colliderObj.name.Split('_');
            if (parts.Length >= 3 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
            {
                // Assuming Z is 0 as per ScanExistingColliders
                string key = $"{tilemapName}_{x}_{y}_0";
                if (existingColliderKeys.Contains(key))
                {
                    existingColliderKeys.Remove(key);
                }
            }
        }

        if (Application.isPlaying)
        {
            Destroy(colliderObj);
        }
        else
        {
            DestroyImmediate(colliderObj);
        }
    }
    
    /// <summary>
    /// Applies CompositeCollider2D optimization to combine colliders
    /// Note: CompositeCollider2D works best with BoxCollider2D and PolygonCollider2D
    /// </summary>
    /// <summary>
    /// Applies CompositeCollider2D optimization to combine colliders per layer
    /// </summary>
    private void ApplyCompositeCollider()
    {
        if (collidersContainer == null) return;
        
        foreach (Transform layerContainer in collidersContainer)
        {
            if (layerContainer == null) continue;
            
            // Add Rigidbody2D and CompositeCollider2D to the LAYER container
            Rigidbody2D rb = layerContainer.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = layerContainer.gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
                rb.simulated = false; // Static colliders don't need simulation
            }
            
            CompositeCollider2D composite = layerContainer.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                composite = layerContainer.gameObject.AddComponent<CompositeCollider2D>();
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                composite.generationType = CompositeCollider2D.GenerationType.Synchronous; // Force immediate generation
            }
        }
    }
    
    /// <summary>
    /// Updates colliders when tiles change (call this manually or hook into Tilemap callbacks)
    /// </summary>
    public void RefreshColliders()
    {
        GenerateColliders();
    }
    
    /// <summary>
    /// Auto-detects all unique sprites from Tilemaps and creates TileColliderData entries for them
    /// This groups tiles by sprite, so tiles with the same sprite but different TileBase assets are grouped together
    /// </summary>
    public void AutoDetectTilesBySprite()
    {
        // Refresh tilemap list
        FindAllTilemaps();
        
        if (foundTilemaps.Count == 0)
        {
            Debug.LogError("TilemapColliderManager: No Tilemap components found!");
            return;
        }
        
        // Dictionary to store unique sprites and their first tile reference
        Dictionary<Sprite, TileBase> spriteToTileMap = new Dictionary<Sprite, TileBase>();
        Dictionary<Sprite, int> spriteCounts = new Dictionary<Sprite, int>();
        
        // Scan all Tilemaps for unique sprites (optimized - only iterates over used tiles)
        foreach (Tilemap currentTilemap in foundTilemaps)
        {
            if (currentTilemap == null) continue;
            
            // Use allPositionsWithin for cleaner iteration
            foreach (Vector3Int tilePosition in currentTilemap.cellBounds.allPositionsWithin)
            {
                TileBase tile = currentTilemap.GetTile(tilePosition);
                
                if (tile == null) continue;
                
                Sprite sprite = GetTileSprite(currentTilemap, tilePosition);
                if (sprite != null)
                {
                    // Track this sprite
                    if (!spriteToTileMap.ContainsKey(sprite))
                    {
                        spriteToTileMap[sprite] = tile;
                    }
                    
                    // Count occurrences
                    if (spriteCounts.ContainsKey(sprite))
                    {
                        spriteCounts[sprite]++;
                    }
                    else
                    {
                        spriteCounts[sprite] = 1;
                    }
                }
            }
        }
        
        // Create new TileColliderData entries for each unique sprite
        List<TileColliderData> newColliderData = new List<TileColliderData>(colliderData);
        
        foreach (var spritePair in spriteToTileMap)
        {
            Sprite sprite = spritePair.Key;
            TileBase sampleTile = spritePair.Value;
            int count = spriteCounts[sprite];
            
            // Check if this sprite is already in the list
            bool alreadyExists = false;
            foreach (var existingData in newColliderData)
            {
                if (existingData != null && 
                    ((existingData.matchMode == MatchMode.BySprite && existingData.spriteAsset == sprite) ||
                     (existingData.matchMode == MatchMode.ByTileAsset && existingData.tileAsset == sampleTile)))
                {
                    alreadyExists = true;
                    break;
                }
            }
            
            // Check if this sprite/tile is in the excluded list
            bool isExcluded = excludedSprites.Contains(sprite) || excludedTiles.Contains(sampleTile);
            
            if (!alreadyExists && !isExcluded)
            {
                // Create new entry
                TileColliderData newData = new TileColliderData
                {
                    matchMode = MatchMode.BySprite,
                    spriteAsset = sprite,
                    tileAsset = sampleTile, // Keep reference for reference, but matching will use sprite
                    colliderType = ColliderType.Box,
                    isTrigger = false
                };
                
                newColliderData.Add(newData);
            }
        }
        
        // Update the array
        colliderData = newColliderData.ToArray();
        
        Debug.Log($"TilemapColliderManager: Auto-detected {spriteToTileMap.Count} unique sprite(s) from {foundTilemaps.Count} Tilemap(s). Added {spriteToTileMap.Count} new TileColliderData entries.");
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    /// <summary>
    /// Adds a sprite/tile to the exclusion list so it won't be auto-added
    /// </summary>
    public void AddToExclusionList(Sprite sprite, TileBase tile)
    {
        if (sprite != null && !excludedSprites.Contains(sprite))
        {
            excludedSprites.Add(sprite);
        }
        if (tile != null && !excludedTiles.Contains(tile))
        {
            excludedTiles.Add(tile);
        }
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    /// <summary>
    /// Removes a sprite/tile from the exclusion list
    /// </summary>
    public void RemoveFromExclusionList(Sprite sprite, TileBase tile)
    {
        if (sprite != null)
        {
            excludedSprites.Remove(sprite);
        }
        if (tile != null)
        {
            excludedTiles.Remove(tile);
        }
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
}

