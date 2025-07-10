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
        SpawnPrefabs();
        SpawnEnemies();
    }

    void LoadLevelData()
    {
        string path = Application.dataPath + $"/Level{levelToLoad}.json";
        if (!File.Exists(path)) { Debug.LogWarning("Level not found"); return; }

        var data = JsonUtility.FromJson<LevelTileData>(File.ReadAllText(path));
        gridWidth = data.width;
        gridHeight = data.height;
        loadedTiles = data.tiles ?? new List<PlacedTile>();
        loadedPrefabs = data.prefabs ?? new List<PlacedPrefab>();
        enemySpawnPoints = data.enemySpawns ?? new List<Vector2Int>();
        patrolPoints = data.patrolPoints ?? new List<Vector2Int>();

        foreach (var tile in loadedTiles)
        {
            var bind = tilemapBindings.FirstOrDefault(b => b.type == tile.tilemapType);
            if (bind != null && bind.tilemap != null && tile.tileIndex >= 0 && tile.tileIndex < assetsDatabase.tiles.Length)
            {
                bind.tilemap.SetTile(new Vector3Int(tile.position.x, tile.position.y, 0), assetsDatabase.tiles[tile.tileIndex]);
            }
            else
            {
                Debug.LogWarning($"Skipped invalid tile at {tile.position}");
            }
        }
    }

    void SpawnPrefabs()
    {
        foreach (var p in loadedPrefabs)
        {
            if (p.prefabIndex >= 0 && p.prefabIndex < assetsDatabase.prefabs.Length)
            {
                var prefab = assetsDatabase.prefabs[p.prefabIndex];
                if (prefab != null)
                {
                    Vector3 pos = new Vector3(p.position.x + p.size.x / 2f, p.position.y + p.size.y / 2f, 0);
                    p.instance = Instantiate(prefab, pos, Quaternion.identity);
                }
            }
        }
    }

    void SpawnEnemies()
    {
        if (assetsDatabase.enemyPrefab == null) { Debug.LogWarning("No enemy prefab"); return; }

        foreach (var pos in enemySpawnPoints)
        {
            Vector3 spawnPos = new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0); // center
            var enemy = Instantiate(assetsDatabase.enemyPrefab, spawnPos, Quaternion.identity);

            // If your enemy has AI script, assign patrol points
           /* var ai = enemy.GetComponent<EnemyAI>();
            if (ai != null && patrolPoints != null)
            {
                ai.patrolPoints = patrolPoints.Select(p => new Vector3(p.x + 0.5f, p.y + 0.5f, 0)).ToList();
            }*/
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

        // enemySpawnPoints and patrolPoints do NOT mark as occupied

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
}

[Serializable]
public class PlacedPrefab
{
    public Vector2Int position;
    public int prefabIndex;
    public Vector2Int size;
    [NonSerialized] public GameObject instance;
}

[Serializable]
public class LevelTileData
{
    public int width, height;
    public List<PlacedTile> tiles;
    public List<PlacedPrefab> prefabs;
    public List<Vector2Int> enemySpawns;
    public List<Vector2Int> patrolPoints; // new
}
