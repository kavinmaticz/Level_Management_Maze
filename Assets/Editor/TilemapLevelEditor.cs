using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class TilemapLevelEditor : EditorWindow
{
    public LevelAssetsDatabase assetsDatabase;   // assign in inspector
    public int gridWidth = 10;
    public int gridHeight = 10;
    public int currentLevel = 1;

    private TilemapType selectedTilemapType = TilemapType.Ground;
    private int selectedTileIndex = 0;
    private int selectedPrefabIndex = 0;

    private List<PlacedTile> placedTiles = new List<PlacedTile>();
    private List<PlacedPrefab> placedPrefabs = new List<PlacedPrefab>();
    private List<Vector2Int> enemySpawnCells = new List<Vector2Int>();
    private List<Vector2Int> patrolCells = new List<Vector2Int>();

    private Vector2 scrollPos;
    private bool isDragging = false;
    private float gridZoom = 100f;
    private bool eraseMode = false;
    private bool prefabMode = false;
    private bool spawnPointMode = false;
    private bool patrolPointMode = false;

    [MenuItem("Tools/Tilemap Level Editor")]
    public static void ShowWindow() => GetWindow<TilemapLevelEditor>("Tilemap Level Editor");

    private void OnGUI()
    {
        GUILayout.Label("🧩 Tilemap Level Editor", EditorStyles.boldLabel);
        assetsDatabase = (LevelAssetsDatabase)EditorGUILayout.ObjectField("Assets DB", assetsDatabase, typeof(LevelAssetsDatabase), false);
        if (assetsDatabase == null) return;

        gridWidth = EditorGUILayout.IntField("Grid Width", gridWidth);
        gridHeight = EditorGUILayout.IntField("Grid Height", gridHeight);
        currentLevel = EditorGUILayout.IntField("Current Level", currentLevel);
        gridZoom = EditorGUILayout.Slider("Grid Zoom %", gridZoom, 25f, 300f);

        // Select TilemapType
        if (assetsDatabase.tilemapTypes != null && assetsDatabase.tilemapTypes.Length > 0)
        {
            string[] names = assetsDatabase.tilemapTypes.Select(t => t.displayName).ToArray();
            int currentIdx = System.Array.FindIndex(assetsDatabase.tilemapTypes, t => t.type == selectedTilemapType);
            int newIdx = EditorGUILayout.Popup("Tilemap Type", currentIdx, names);
            if (newIdx >= 0 && newIdx < assetsDatabase.tilemapTypes.Length)
                selectedTilemapType = assetsDatabase.tilemapTypes[newIdx].type;
        }

        // Tile selection
        if (assetsDatabase.tiles != null && assetsDatabase.tiles.Length > 0)
        {
            string[] tileNames = assetsDatabase.tiles.Select(t => t != null ? t.name : "Null").ToArray();
            selectedTileIndex = EditorGUILayout.Popup("Tile", selectedTileIndex, tileNames);
        }

        prefabMode = GUILayout.Toggle(prefabMode, "Prefab Mode");
        spawnPointMode = GUILayout.Toggle(spawnPointMode, "Spawn Point Mode");
        patrolPointMode = GUILayout.Toggle(patrolPointMode, "Patrol Point Mode");
        eraseMode = GUILayout.Toggle(eraseMode, "Erase Mode");

        // Prefab selection
        if (assetsDatabase.prefabs != null && assetsDatabase.prefabs.Length > 0)
        {
            string[] prefabNames = assetsDatabase.prefabs.Select(p => p != null ? p.name : "Null").ToArray();
            selectedPrefabIndex = EditorGUILayout.Popup("Prefab", selectedPrefabIndex, prefabNames);
        }

        GUILayout.Space(5);
        GUILayout.Label("🧹 Clear Options", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Tiles")) placedTiles.Clear();
        if (GUILayout.Button("Prefabs")) placedPrefabs.Clear();
        if (GUILayout.Button("Spawns")) enemySpawnCells.Clear();
        if (GUILayout.Button("Patrols")) patrolCells.Clear();
        if (GUILayout.Button("All")) { placedTiles.Clear(); placedPrefabs.Clear(); enemySpawnCells.Clear(); patrolCells.Clear(); }
        EditorGUILayout.EndHorizontal();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(500));
        DrawGrid();
        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);
        GUILayout.Label("💾 Save / Load", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save")) SaveLevel();
        if (GUILayout.Button("Load")) LoadLevel();
        EditorGUILayout.EndHorizontal();

        HandleEvents();
    }

    private void DrawGrid()
    {
        float cellSize = 40f * (gridZoom / 100f);
        GUIStyle style = new GUIStyle(GUI.skin.button) { margin = new RectOffset(0, 0, 0, 0), padding = new RectOffset(0, 0, 0, 0) };

        for (int y = gridHeight - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(cellSize));
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Texture2D preview = null;

                var tile = placedTiles.FirstOrDefault(t => t.position == cell);
                if (tile != null && tile.tileIndex >= 0 && tile.tileIndex < assetsDatabase.tiles.Length)
                    preview = AssetPreview.GetAssetPreview(assetsDatabase.tiles[tile.tileIndex]);

                var prefab = placedPrefabs.FirstOrDefault(p => p.position == cell);
                if (prefab != null && prefab.prefabIndex >= 0 && prefab.prefabIndex < assetsDatabase.prefabs.Length)
                    preview = AssetPreview.GetAssetPreview(assetsDatabase.prefabs[prefab.prefabIndex]);

                bool isSpawnCell = enemySpawnCells.Contains(cell);
                bool isPatrolCell = patrolCells.Contains(cell);

                GUI.color = isPatrolCell ? new Color(0, 0.5f, 0.8f, 0.8f) :
                          isSpawnCell ? new Color(0.7f, 0, 0, 0.8f) :
                          prefabMode ? new Color(0.7f, 0.85f, 1f) :
                          eraseMode ? new Color(1f, 0.5f, 0.5f) : Color.white;

                if (GUILayout.Button(preview != null ? new GUIContent(preview) : GUIContent.none, style, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                    ToggleCellAt(x, y);
                GUI.color = Color.white;

                var rect = GUILayoutUtility.GetLastRect();
                if (isDragging && rect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDrag)
                    ToggleCellAt(x, y);
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void ToggleCellAt(int x, int y)
    {
        Vector2Int cell = new Vector2Int(x, y);

        if (patrolPointMode)
        {
            if (patrolCells.Contains(cell)) patrolCells.Remove(cell);
            else patrolCells.Add(cell);
        }
        else if (spawnPointMode)
        {
            if (enemySpawnCells.Contains(cell)) enemySpawnCells.Remove(cell);
            else enemySpawnCells.Add(cell);
        }
        else if (prefabMode)
        {
            if (eraseMode) placedPrefabs.RemoveAll(p => p.position == cell);
            else placedPrefabs.Add(new PlacedPrefab { position = cell, prefabIndex = selectedPrefabIndex, size = Vector2Int.one });
        }
        else
        {
            if (eraseMode) placedTiles.RemoveAll(t => t.position == cell && t.tilemapType == selectedTilemapType);
            else placedTiles.Add(new PlacedTile { position = cell, tileIndex = selectedTileIndex, tilemapType = selectedTilemapType });
        }
    }

    private void SaveLevel()
    {
        var data = new LevelTileData
        {
            width = gridWidth,
            height = gridHeight,
            tiles = placedTiles,
            prefabs = placedPrefabs,
            enemySpawns = enemySpawnCells,
            patrolPoints = patrolCells
        };
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(Application.dataPath + $"/Level{currentLevel}.json", json);
        AssetDatabase.Refresh();
        Debug.Log("Saved!");
    }

    private void LoadLevel()
    {
        string path = Application.dataPath + $"/Level{currentLevel}.json";
        if (!File.Exists(path)) { Debug.LogWarning("File not found"); return; }
        var data = JsonUtility.FromJson<LevelTileData>(File.ReadAllText(path));
        gridWidth = data.width; gridHeight = data.height;
        placedTiles = data.tiles ?? new List<PlacedTile>();
        placedPrefabs = data.prefabs ?? new List<PlacedPrefab>();
        enemySpawnCells = data.enemySpawns ?? new List<Vector2Int>();
        patrolCells = data.patrolPoints ?? new List<Vector2Int>();
        Debug.Log("Loaded!");
    }

    private void HandleEvents()
    {
        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0) isDragging = true;
        if (e.type == EventType.MouseUp && e.button == 0) isDragging = false;
    }
}

// Data classes
[System.Serializable] public class PlacedTile { public Vector2Int position; public int tileIndex; public TilemapType tilemapType; }
[System.Serializable] public class PlacedPrefab { public Vector2Int position; public int prefabIndex; public Vector2Int size; }
[System.Serializable]
public class LevelTileData
{
    public int width, height;
    public List<PlacedTile> tiles;
    public List<PlacedPrefab> prefabs;
    public List<Vector2Int> enemySpawns;
    public List<Vector2Int> patrolPoints;
}
