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

[System.Serializable]
public class TileColliderData
{
    [Header("Tile Reference")]
    public TileBase tileAsset;
    
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
    [SerializeField] private TileColliderData[] colliderData = new TileColliderData[0];
    
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
            
            // Get all tile positions
            BoundsInt bounds = currentTilemap.cellBounds;
            int tilemapColliderCount = 0;
            
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int tilePosition = new Vector3Int(x, y, 0);
                    TileBase tile = currentTilemap.GetTile(tilePosition);
                    
                    if (tile == null) continue;
                    
                    // Find matching collider data
                    TileColliderData matchingData = GetMatchingColliderData(tile);
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
    /// Finds the TileColliderData that matches the given tile
    /// </summary>
    private TileColliderData GetMatchingColliderData(TileBase tile)
    {
        foreach (var data in colliderData)
        {
            if (data != null && data.tileAsset == tile)
            {
                return data;
            }
        }
        return null;
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
}

