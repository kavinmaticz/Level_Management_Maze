using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

public class LevelManager : MonoBehaviour
{
    public int levelToLoad = 1;
    public LevelAssetsDatabase assetsDatabase;

    [Serializable]
    public class TilemapBindingMap { public TilemapType type; public Tilemap tilemap; }
    public TilemapBindingMap[] tilemapBindings;

    private List<PlacedTile> loadedTiles;
    private List<PlacedPrefab> loadedPrefabs;
    private List<Vector2Int> enemySpawnPoints;
    public List<Vector2Int> patrolPoints;

    public List<Vector2Int> emptyCells = new List<Vector2Int>();
    private int gridWidth, gridHeight;

    void Start()
    {
        LoadLevelData();
        BuildEmptyGridCells();
        SpawnTiles();
        SpawnPrefabs();
        SpawnEnemies();
    }

    void LoadLevelData()
    {
        string path = Application.dataPath + $"/Level{levelToLoad}.json";
        if (!File.Exists(path)) { Debug.LogWarning("Level not found at: " + path); return; }

        var data = JsonUtility.FromJson<LevelTileData>(File.ReadAllText(path));
        gridWidth = data.width;
        gridHeight = data.height;
        loadedTiles = data.tiles ?? new List<PlacedTile>();
        loadedPrefabs = data.prefabs ?? new List<PlacedPrefab>();
        enemySpawnPoints = data.enemySpawns ?? new List<Vector2Int>();
        patrolPoints = data.patrolPoints ?? new List<Vector2Int>();

        Debug.Log($"Loaded level {levelToLoad}: tiles={loadedTiles.Count}, prefabs={loadedPrefabs.Count}, spawns={enemySpawnPoints.Count}");
    }

    void SpawnTiles()
    {
        foreach (var tile in loadedTiles)
        {
            var binding = tilemapBindings.FirstOrDefault(b => b.type == tile.tilemapType);
            if (binding != null && binding.tilemap != null && tile.tileIndex >= 0 && tile.tileIndex < assetsDatabase.tiles.Length)
            {
                var cellPos = new Vector3Int(tile.position.x, tile.position.y, 0);
                binding.tilemap.SetTile(cellPos, assetsDatabase.tiles[tile.tileIndex]);

                // Apply rotation
                var tileTransform = Matrix4x4.Rotate(Quaternion.Euler(0, 0, -tile.rotation));
                binding.tilemap.SetTransformMatrix(cellPos, tileTransform);
            }
            else
            {
                Debug.LogWarning($"Skipped invalid tile at {tile.position}");
            }
        }
    }

    void SpawnPrefabs()
    {
        if (assetsDatabase.prefabs == null || tilemapBindings == null || tilemapBindings.Length == 0)
        {
            Debug.LogWarning("Missing prefabs or tilemap bindings!");
            return;
        }

        // Find any tilemap (e.g., Ground) to get grid position
        var groundTilemap = tilemapBindings.FirstOrDefault(b => b.type == TilemapType.Ground)?.tilemap;
        if (groundTilemap == null)
        {
            Debug.LogWarning("No Ground tilemap to align prefab positions!");
            return;
        }

        foreach (var p in loadedPrefabs)
        {
            if (p.prefabIndex >= 0 && p.prefabIndex < assetsDatabase.prefabs.Length)
            {
                var prefab = assetsDatabase.prefabs[p.prefabIndex];
                if (prefab != null)
                {
                    Vector3Int cellPos = new Vector3Int(p.position.x, p.position.y, 0);
                    Vector3 worldPos = groundTilemap.CellToWorld(cellPos);

                    // Align to tile center if needed
                    worldPos += groundTilemap.cellSize / 2;

                    var instance = Instantiate(prefab, worldPos, Quaternion.identity);
                    Debug.Log($"Spawned prefab '{prefab.name}' at grid cell {p.position} -> world pos {worldPos}");
                }
            }
        }
    }


    void SpawnEnemies()
    {
        if (assetsDatabase.enemyPrefab == null)
        {
            Debug.LogWarning("No enemy prefab in assetsDatabase");
            return;
        }

        foreach (var pos in enemySpawnPoints)
        {
            Vector3 spawnPos = new Vector3(pos.x, pos.y, 0);
            Instantiate(assetsDatabase.enemyPrefab, spawnPos, Quaternion.identity);
            Debug.Log($"Spawned enemy at {spawnPos}");
        }
    }

    void BuildEmptyGridCells()
    {
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

        foreach (var t in loadedTiles)
            occupied.Add(t.position);

        foreach (var p in loadedPrefabs)
            for (int dx = 0; dx < p.size.x; dx++)
                for (int dy = 0; dy < p.size.y; dy++)
                    occupied.Add(new Vector2Int(p.position.x + dx, p.position.y + dy));

        emptyCells.Clear();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!occupied.Contains(cell))
                    emptyCells.Add(cell);
            }
        }

        Debug.Log($"Empty cells found: {emptyCells.Count}");
    }
}

[Serializable]
public enum TilemapType
{
    Walls,
    Ground,
    Obstacles
}

[Serializable]
public class PlacedTile
{
    public Vector2Int position;
    public int tileIndex;
    public TilemapType tilemapType;
    public int rotation;
}

[Serializable]
public class PlacedPrefab
{
    public Vector2Int position;
    public int prefabIndex;
    public Vector2Int size;
}

[Serializable]
public class LevelTileData
{
    public int width, height;
    public List<PlacedTile> tiles;
    public List<PlacedPrefab> prefabs;
    public List<Vector2Int> enemySpawns;
    public List<Vector2Int> patrolPoints;
}
