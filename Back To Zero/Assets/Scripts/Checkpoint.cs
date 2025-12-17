using UnityEngine;
using UnityEngine.Tilemaps;

public enum CheckpointMode
{
    GameObjectBased,  // Use GameObject position (original behavior)
    TileBased         // Use tilemap position and auto-detect center
}

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Mode")]
    [Tooltip("GameObjectBased: Uses this GameObject's position\nTileBased: Auto-finds position from tilemap")]
    [SerializeField] private CheckpointMode mode = CheckpointMode.GameObjectBased;
    
    [Header("Tile-Based Settings")]
    [Tooltip("The tilemap to search for tiles. If null, will search all tilemaps in scene.")]
    [SerializeField] private Tilemap targetTilemap;
    
    [Tooltip("The tile position (in tile coordinates, e.g., (10, 5, 0) for a 3x3 area starting at tile 10,5)")]
    [SerializeField] private Vector3Int tilePosition = Vector3Int.zero;
    
    [Tooltip("Size of the tile area (e.g., 3x3 for summoning circles). Center will be calculated automatically.")]
    [SerializeField] private Vector2Int tileSize = new Vector2Int(3, 3);
    
    [Tooltip("If true, validates that tiles exist at the specified position before setting checkpoint")]
    [SerializeField] private bool requireTilesToExist = true;
    
    [Tooltip("Button to auto-find and position checkpoint at tile center")]
    [SerializeField] private bool findTileCenter = false;
    
    [Header("Checkpoint Settings")]
    [Tooltip("The tag of the player object (default: 'Player')")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("Visual Settings")]
    [Tooltip("Sprite to display when checkpoint is active (currently set as spawn point)")]
    [SerializeField] private Sprite activeSprite;
    
    [Tooltip("Sprite to display when checkpoint is inactive (not set as spawn point)")]
    [SerializeField] private Sprite inactiveSprite;
    
    [Tooltip("SpriteRenderer component. If null, will try to find it on this GameObject or children.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("Debug")]
    [Tooltip("Shows if this checkpoint is currently active")]
    [SerializeField] private bool isActive = false;
    
    [Tooltip("Shows the calculated world position (for tile-based mode)")]
    [SerializeField] private Vector3 calculatedPosition;
    
    private PlayerHandler playerHandler;
    private static Checkpoint currentActiveCheckpoint;
    private Vector3 spawnPosition;
    
    private void Awake()
    {
        // Handle tile-based positioning
        if (mode == CheckpointMode.TileBased)
        {
            CalculateTilePosition();
        }
        else
        {
            spawnPosition = transform.position;
        }
        
        // Ensure collider is set as trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogWarning($"Checkpoint on {gameObject.name}: No Collider2D found! Adding BoxCollider2D.");
            col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }
        
        // Find SpriteRenderer if not assigned
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }
        
        // Initialize visual state
        UpdateVisualState();
    }
    
    private void Start()
    {
        // Find PlayerHandler in scene
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerHandler = player.GetComponent<PlayerHandler>();
            if (playerHandler == null)
            {
                Debug.LogWarning($"Checkpoint on {gameObject.name}: Player object found but no PlayerHandler component!");
            }
        }
        else
        {
            Debug.LogWarning($"Checkpoint on {gameObject.name}: No GameObject with tag '{playerTag}' found in scene!");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the colliding object is the player
        if (other.CompareTag(playerTag))
        {
            ActivateCheckpoint();
        }
    }
    
    /// <summary>
    /// Calculates the world position from tile coordinates and finds the center
    /// </summary>
    private void CalculateTilePosition()
    {
        Tilemap tilemap = GetTargetTilemap();
        
        if (tilemap == null)
        {
            Debug.LogError($"Checkpoint on {gameObject.name}: No tilemap found! Cannot calculate tile position.");
            spawnPosition = transform.position;
            return;
        }
        
        // Calculate center tile position
        Vector3Int centerTile = new Vector3Int(
            tilePosition.x + tileSize.x / 2,
            tilePosition.y + tileSize.y / 2,
            tilePosition.z
        );
        
        // Validate tiles exist if required
        if (requireTilesToExist)
        {
            bool hasTiles = false;
            for (int x = 0; x < tileSize.x; x++)
            {
                for (int y = 0; y < tileSize.y; y++)
                {
                    Vector3Int checkPos = new Vector3Int(
                        tilePosition.x + x,
                        tilePosition.y + y,
                        tilePosition.z
                    );
                    
                    if (tilemap.GetTile(checkPos) != null)
                    {
                        hasTiles = true;
                        break;
                    }
                }
                if (hasTiles) break;
            }
            
            if (!hasTiles)
            {
                Debug.LogWarning($"Checkpoint on {gameObject.name}: No tiles found at position {tilePosition} with size {tileSize}. Using GameObject position instead.");
                spawnPosition = transform.position;
                return;
            }
        }
        
        // Convert tile position to world position
        Vector3 worldPos = tilemap.CellToWorld(centerTile);
        
        // Adjust for tilemap cell size
        // Unity's CellToWorld returns the bottom-left corner, so we need to center it
        Vector3 cellSize = tilemap.cellSize;
        
        // Calculate the actual center of the tile
        worldPos += new Vector3(
            cellSize.x * 0.5f,
            cellSize.y * 0.5f,
            0
        );
        
        spawnPosition = worldPos;
        calculatedPosition = worldPos;
        
        // Optionally move the GameObject to this position
        if (findTileCenter)
        {
            transform.position = worldPos;
        }
        
        Debug.Log($"Checkpoint on {gameObject.name}: Calculated position from tile {centerTile} = {worldPos}");
    }
    
    /// <summary>
    /// Gets the target tilemap, searching if not assigned
    /// </summary>
    private Tilemap GetTargetTilemap()
    {
        if (targetTilemap != null)
        {
            return targetTilemap;
        }
        
        // Try to find on this GameObject or parent
        Tilemap found = GetComponentInParent<Tilemap>();
        if (found != null)
        {
            return found;
        }
        
        // Search all tilemaps in scene
        Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();
        if (allTilemaps.Length > 0)
        {
            // Prefer tilemaps that have tiles at our position
            foreach (Tilemap tm in allTilemaps)
            {
                if (tm.GetTile(tilePosition) != null)
                {
                    return tm;
                }
            }
            // If none have tiles at position, return first one
            return allTilemaps[0];
        }
        
        return null;
    }
    
    /// <summary>
    /// Activates this checkpoint and sets it as the player's spawn point
    /// </summary>
    public void ActivateCheckpoint()
    {
        if (playerHandler == null)
        {
            Debug.LogWarning($"Checkpoint on {gameObject.name}: Cannot activate - PlayerHandler not found!");
            return;
        }
        
        // Recalculate position if in tile-based mode (in case tiles changed)
        if (mode == CheckpointMode.TileBased)
        {
            CalculateTilePosition();
        }
        else
        {
            spawnPosition = transform.position;
        }
        
        // Deactivate previous checkpoint if one exists
        if (currentActiveCheckpoint != null && currentActiveCheckpoint != this)
        {
            currentActiveCheckpoint.DeactivateCheckpoint();
        }
        
        // Set this as the active checkpoint
        currentActiveCheckpoint = this;
        isActive = true;
        
        // Update player's spawn point
        playerHandler.SetSpawnPoint(spawnPosition);
        
        // Update visual state
        UpdateVisualState();
        
        Debug.Log($"Checkpoint activated at {spawnPosition}");
    }
    
    /// <summary>
    /// Deactivates this checkpoint (called when another checkpoint becomes active)
    /// </summary>
    public void DeactivateCheckpoint()
    {
        isActive = false;
        UpdateVisualState();
    }
    
    /// <summary>
    /// Updates the visual appearance based on active state
    /// </summary>
    private void UpdateVisualState()
    {
        if (spriteRenderer == null) return;
        
        if (isActive)
        {
            if (activeSprite != null)
            {
                spriteRenderer.sprite = activeSprite;
            }
        }
        else
        {
            if (inactiveSprite != null)
            {
                spriteRenderer.sprite = inactiveSprite;
            }
        }
    }
    
    /// <summary>
    /// Manually set the active sprite (useful for runtime changes)
    /// </summary>
    public void SetActiveSprite(Sprite sprite)
    {
        activeSprite = sprite;
        if (isActive)
        {
            UpdateVisualState();
        }
    }
    
    /// <summary>
    /// Manually set the inactive sprite (useful for runtime changes)
    /// </summary>
    public void SetInactiveSprite(Sprite sprite)
    {
        inactiveSprite = sprite;
        if (!isActive)
        {
            UpdateVisualState();
        }
    }
    
    /// <summary>
    /// Gets whether this checkpoint is currently active
    /// </summary>
    public bool IsActive => isActive;
    
    /// <summary>
    /// Gets the spawn position (calculated from tile or GameObject position)
    /// </summary>
    public Vector3 GetSpawnPosition()
    {
        if (mode == CheckpointMode.TileBased)
        {
            CalculateTilePosition();
        }
        return spawnPosition;
    }
    
    // Editor helper method - called when values change in inspector
    private void OnValidate()
    {
        // Recalculate position when tile settings change
        if (mode == CheckpointMode.TileBased && Application.isPlaying)
        {
            CalculateTilePosition();
        }
        
        // Update visual state in editor when sprites change
        if (Application.isPlaying)
        {
            UpdateVisualState();
        }
    }
}
