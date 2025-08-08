using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;


public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    public int levelToLoad = 2;
    public LevelAssetsDatabase assetsDatabase;

    [Serializable]
    public class TilemapBindingMap { public TilemapType type; public Tilemap tilemap; }
    public TilemapBindingMap[] tilemapBindings;

    private List<PlacedTile> loadedTiles;
    private List<PlacedPrefab> loadedPrefabs;
    private List<Vector2Int> enemySpawnPoints;
    public List<Vector2Int> patrolPoints;
    private List<PlacedBackground> loadedBackgrounds;

    public List<Vector2Int> emptyCells = new List<Vector2Int>();
    private int gridWidth, gridHeight;

    public List<GameObject> patrolObjects = new List<GameObject>();
    //  public List<Transform> patrolObjects = new List<Transform>();

    //public GameObject playerPrefab;  // ✅ Player prefab reference
    private Vector2Int playerSpawn;  // ✅ Loaded player spawn cell

    

    void Start()
    {
       
        LoadLevelData();
        SpawnTiles();
        SpawnBackgrounds();
        BuildEmptyGridCells();
        SpawnPrefabs();
        SpawnPatrolPoints();
        SpawnPlayer();  // ✅ Spawn player
    }
    private void Awake()
    {
        Instance = this;
    }

    void LoadLevelData()
    {

        string fileName = $"Level{levelToLoad}";
        TextAsset jsonFile = Resources.Load<TextAsset>(fileName);

        if (jsonFile != null)
        {
            string jsonText = jsonFile.text;
            var data = JsonUtility.FromJson<LevelTileData>(jsonText);
            gridWidth = data.width;
            gridHeight = data.height;
            loadedTiles = data.tiles ?? new List<PlacedTile>();
            loadedPrefabs = data.prefabs ?? new List<PlacedPrefab>();
            enemySpawnPoints = data.enemySpawns ?? new List<Vector2Int>();
            patrolPoints = data.patrolPoints ?? new List<Vector2Int>();
            playerSpawn = data.playerSpawn;
            loadedBackgrounds = new List<PlacedBackground>
    {
        new PlacedBackground
        {
            spriteIndex = data.selectedBackgroundIndex,
            rotation = 0,
            flipX = false,
            flipY = false
        }
    };

            Debug.Log($"Loaded BG index: {data.selectedBackgroundIndex}");
        }
        else
        {
            Debug.LogError("JSON file not found in Resources folder.");
        }
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

                float angle = tile.rotation;
                Vector3 scale = new Vector3(tile.flipX ? -1f : 1f, tile.flipY ? -1f : 1f, 1f);
                Quaternion rotation = Quaternion.Euler(0, 0, -angle);
                Matrix4x4 tileTransform = Matrix4x4.TRS(Vector3.zero, rotation, scale);
                binding.tilemap.SetTransformMatrix(cellPos, tileTransform);
            }
            else
            {
                Debug.LogWarning($"Skipped invalid tile at {tile.position}");
            }
        }
    }

    void SpawnBackgrounds()
    {
        if (loadedBackgrounds.Count == 0) return;

        var bg = loadedBackgrounds[0];
        if (bg.spriteIndex >= 0 && bg.spriteIndex < assetsDatabase.backgroundSprites.Length)
        {
            Sprite sprite = assetsDatabase.backgroundSprites[bg.spriteIndex];
            GameObject go = new GameObject("Background");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -100;
            Debug.Log("Background Name " + renderer.material.name);
            
            Debug.Log("Background Name " + renderer.material.name);
            float width = gridWidth;
            float height = gridHeight;
            Vector2 spriteSize = sprite.bounds.size;

            go.transform.localScale = new Vector3(
                width / spriteSize.x,
                height / spriteSize.y,
                1
            );

            go.transform.position = new Vector3(width / 2f, height / 2f, 0);

            go.transform.rotation = Quaternion.Euler(0, 0, bg.rotation);
            Vector3 scale = go.transform.localScale;
            scale.x *= bg.flipX ? -1 : 1;
            scale.y *= bg.flipY ? -1 : 1;
            go.transform.localScale = scale;

            
        }
    }

    void SpawnPrefabs()
    {
        if (assetsDatabase.prefabs == null || tilemapBindings == null || tilemapBindings.Length == 0)
        {
            Debug.LogWarning("Missing prefabs or tilemap bindings!");
            return;
        }

        var groundTilemap = tilemapBindings.FirstOrDefault(b => b.type == TilemapType.Walls)?.tilemap;
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
                    worldPos += groundTilemap.cellSize / 2;

                    var instance = Instantiate(prefab, worldPos, Quaternion.identity);
                    Debug.Log($"Spawned prefab '{prefab.name}' at grid cell {p.position} -> world pos {worldPos}");
                }
            }
        }
    }

    /* void SpawnPatrolPoints()
     {
         patrolObjects.Clear();

         var groundTilemap = tilemapBindings.FirstOrDefault(b => b.type == TilemapType.Walls)?.tilemap;
         if (groundTilemap == null)
         {
             Debug.LogWarning("No Ground tilemap found to convert patrol cell to world position.");
             return;
         }
         foreach (var pos in patrolPoints)
         {
             Vector3Int cellPos = new Vector3Int(pos.x, pos.y, 0);
             Vector3 worldPos = groundTilemap.CellToWorld(cellPos) + groundTilemap.cellSize / 2;

             GameObject go = new GameObject($"PatrolPoint_{pos.x}_{pos.y}");
             go.transform.position = worldPos;
             patrolObjects.Add(go);
             Game_Manager.Instance.PatrolPoints.Add(go);

         }


         Debug.Log($"Spawned {patrolObjects.Count} patrol point objects.");
     }*/


    void SpawnPatrolPoints()
    {
        patrolObjects.Clear();
        

        var groundTilemap = tilemapBindings.FirstOrDefault(b => b.type == TilemapType.Walls)?.tilemap;
        if (groundTilemap == null)
        {
            Debug.LogWarning("No Ground tilemap found to convert patrol cell to world position.");
            return;
        }

        foreach (var pos in patrolPoints)
        {
            Vector3Int cellPos = new Vector3Int(pos.x, pos.y, 0);
            Vector3 worldPos = groundTilemap.CellToWorld(cellPos) + groundTilemap.cellSize / 2;

            GameObject go = new GameObject($"PatrolPoint_{pos.x}_{pos.y}");
            go.transform.position = worldPos;

            patrolObjects.Add(go);                          // Local list
           
        }

        Debug.Log($"Spawned {patrolObjects.Count} patrol point objects.");
    }
    void SpawnPlayer()
    {
        /*if (playerPrefab == null)
        {
            Debug.LogWarning("Player prefab is not assigned!");
            return;
        }
*/
        var groundTilemap = tilemapBindings.FirstOrDefault(b => b.type == TilemapType.Walls)?.tilemap;
        if (groundTilemap == null)
        {
            Debug.LogWarning("No Ground tilemap found to convert player cell to world position.");
            return;
        }

        Vector3Int cellPos = new Vector3Int(playerSpawn.x, playerSpawn.y, 0);
        Vector3 worldPos = groundTilemap.CellToWorld(cellPos) + groundTilemap.cellSize / 2;

        /* Instantiate(playerPrefab, worldPos, Quaternion.identity);
         Debug.Log($"Player spawned at {playerSpawn}");*/
        

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
    public bool flipX;
    public bool flipY;
}

[Serializable]
public class PlacedPrefab
{
    public Vector2Int position;
    public int prefabIndex;
    public Vector2Int size;
}

[Serializable]
public class PlacedBackground
{
    public int spriteIndex;
    public int rotation;
    public bool flipX;
    public bool flipY;
}

[Serializable]
public class LevelTileData
{
    public int width, height;
    public List<PlacedTile> tiles;
    public List<PlacedPrefab> prefabs;
    public List<Vector2Int> enemySpawns;
    public List<Vector2Int> patrolPoints;
    public Vector2Int playerSpawn; // ✅ Added
    public int selectedBackgroundIndex = -1;
}
