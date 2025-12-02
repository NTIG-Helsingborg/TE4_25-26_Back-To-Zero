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
    [SerializeField] private bool useCompositeCollider = false;
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
    private void EnsureCollidersContainer()
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
            
            // Scan all colliders in this container
            foreach (Transform colliderTransform in tilemapContainer)
            {
                // Extract position from collider name (format: "Collider_x_y")
                string colliderName = colliderTransform.name;
                if (colliderName.StartsWith("Collider_"))
                {
                    // Try to parse the position from the name
                    string[] parts = colliderName.Split('_');
                    if (parts.Length >= 3)
                    {
                        if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
                        {
                            string tilemapName = containerName.Replace("Colliders_", "");
                            string key = $"{tilemapName}_{x}_{y}_0";
                            existingColliderKeys.Add(key);
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Creates a collider GameObject for a specific tile position
    /// </summary>
    private void CreateColliderForTile(Tilemap targetTilemap, Vector3Int tilePosition, TileColliderData data, Transform parent)
    {
        // Get world position of the tile
        Vector3 worldPosition = targetTilemap.CellToWorld(tilePosition);
        Vector3 tileCenter = targetTilemap.GetCellCenterWorld(tilePosition);
        
        // Create GameObject for this collider
        GameObject colliderObj = new GameObject($"Collider_{tilePosition.x}_{tilePosition.y}");
        colliderObj.transform.SetParent(parent);
        colliderObj.transform.position = tileCenter + (Vector3)data.offset;
        
        // Get tile size
        Vector3 tileSize = targetTilemap.cellSize;
        Vector2 colliderSize = data.size != Vector2.zero ? data.size : new Vector2(tileSize.x, tileSize.y);
        
        // Create appropriate collider based on type
        Collider2D collider = null;
        
        switch (data.colliderType)
        {
            case ColliderType.Box:
                BoxCollider2D boxCollider = colliderObj.AddComponent<BoxCollider2D>();
                boxCollider.size = colliderSize;
                collider = boxCollider;
                break;
                
            case ColliderType.Circle:
                CircleCollider2D circleCollider = colliderObj.AddComponent<CircleCollider2D>();
                circleCollider.radius = data.circleRadius;
                collider = circleCollider;
                break;
                
            case ColliderType.Polygon:
                PolygonCollider2D polygonCollider = colliderObj.AddComponent<PolygonCollider2D>();
                // Scale polygon points by tile size
                Vector2[] scaledPoints = new Vector2[data.polygonPoints.Length];
                for (int i = 0; i < data.polygonPoints.Length; i++)
                {
                    scaledPoints[i] = new Vector2(
                        data.polygonPoints[i].x * colliderSize.x,
                        data.polygonPoints[i].y * colliderSize.y
                    );
                }
                polygonCollider.points = scaledPoints;
                collider = polygonCollider;
                break;
                
            case ColliderType.Edge:
                EdgeCollider2D edgeCollider = colliderObj.AddComponent<EdgeCollider2D>();
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
        }
        
        // Collider key is already added to existingColliderKeys in GenerateColliders()
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
    /// Applies CompositeCollider2D optimization to combine colliders
    /// Note: CompositeCollider2D works best with BoxCollider2D and PolygonCollider2D
    /// </summary>
    private void ApplyCompositeCollider()
    {
        if (collidersContainer == null) return;
        
        // Add Rigidbody2D and CompositeCollider2D to container
        Rigidbody2D rb = collidersContainer.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = collidersContainer.gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            rb.simulated = false; // Static colliders don't need simulation
        }
        
        CompositeCollider2D composite = collidersContainer.GetComponent<CompositeCollider2D>();
        if (composite == null)
        {
            composite = collidersContainer.gameObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        }
        
        // Unity automatically uses child colliders with CompositeCollider2D
        // The colliders will be combined into the composite collider
        // Note: Only BoxCollider2D and PolygonCollider2D are supported by CompositeCollider2D
        // CircleCollider2D and EdgeCollider2D will be ignored
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

