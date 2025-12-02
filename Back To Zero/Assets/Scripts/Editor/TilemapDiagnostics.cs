using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public class TilemapDiagnostics : EditorWindow
{
    [MenuItem("Tools/Tilemap Diagnostics")]
    public static void ShowWindow()
    {
        GetWindow<TilemapDiagnostics>("Tilemap Diagnostics");
    }

    void OnGUI()
    {
        GUILayout.Label("Tilemap Size Analysis", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Analyze All Tilemaps", GUILayout.Height(30)))
        {
            AnalyzeTilemaps();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This tool shows:\n" +
            "• Total cells in bounds (including empty)\n" +
            "• Actually used cells\n" +
            "• Wasted space (empty cells tracked in undo)\n" +
            "• Recommendations for optimization",
            MessageType.Info
        );
    }

    void AnalyzeTilemaps()
    {
        Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();

        if (tilemaps.Length == 0)
        {
            Debug.LogWarning("No Tilemaps found in scene!");
            return;
        }

        Debug.Log("=== TILEMAP DIAGNOSTICS ===");
        Debug.Log($"Found {tilemaps.Length} Tilemap(s)\n");

        long totalCells = 0;
        long totalUsedCells = 0;
        long totalWastedCells = 0;

        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap == null) continue;

            BoundsInt bounds = tilemap.cellBounds;
            long cellsInBounds = (long)bounds.size.x * bounds.size.y * bounds.size.z;
            
            // Count actually used cells
            int usedCells = 0;
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                if (tilemap.GetTile(pos) != null)
                {
                    usedCells++;
                }
            }

            long wastedCells = cellsInBounds - usedCells;
            float wastePercentage = cellsInBounds > 0 ? (wastedCells / (float)cellsInBounds) * 100f : 0f;

            totalCells += cellsInBounds;
            totalUsedCells += usedCells;
            totalWastedCells += wastedCells;

            Debug.Log($"Tilemap: {tilemap.name}");
            Debug.Log($"  Bounds: {bounds.min} to {bounds.max}");
            Debug.Log($"  Size: {bounds.size.x} x {bounds.size.y} x {bounds.size.z}");
            Debug.Log($"  Total Cells: {cellsInBounds:N0}");
            Debug.Log($"  Used Cells: {usedCells:N0}");
            Debug.Log($"  Wasted Cells: {wastedCells:N0} ({wastePercentage:F1}%)");
            
            // Recommendations
            if (cellsInBounds > 100000)
            {
                Debug.LogWarning($"  ⚠️  HUGE TILEMAP! Consider splitting this into smaller chunks.");
            }
            if (wastePercentage > 90f)
            {
                Debug.LogWarning($"  ⚠️  {wastePercentage:F1}% empty space! Compress bounds or split tilemap.");
            }
            if (cellsInBounds > 10000 && usedCells < 100)
            {
                Debug.LogWarning($"  ⚠️  Very sparse tilemap! Only {usedCells} tiles in {cellsInBounds:N0} cells.");
            }
            
            Debug.Log("");
        }

        Debug.Log("=== SUMMARY ===");
        Debug.Log($"Total Cells (All Tilemaps): {totalCells:N0}");
        Debug.Log($"Total Used Cells: {totalUsedCells:N0}");
        Debug.Log($"Total Wasted Cells: {totalWastedCells:N0}");
        Debug.Log($"Waste Percentage: {(totalWastedCells / (float)totalCells) * 100f:F1}%");

        if (totalCells > 1000000)
        {
            Debug.LogError($"⚠️  CRITICAL: {totalCells:N0} total cells tracked in undo!");
            Debug.LogError("This will cause severe undo lag. Split your tilemaps!");
        }
        else if (totalCells > 100000)
        {
            Debug.LogWarning($"⚠️  WARNING: {totalCells:N0} total cells tracked in undo.");
            Debug.LogWarning("Consider splitting tilemaps to improve undo performance.");
        }
        else
        {
            Debug.Log($"✓ Tilemap size looks reasonable ({totalCells:N0} cells)");
        }
    }
}

