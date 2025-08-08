using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class TilemapLevelEditor : EditorWindow
{
    public LevelAssetsDatabase assetsDatabase;
    public int gridWidth = 10;
    public int gridHeight = 10;
    public int currentLevel = 1;

    private TilemapType selectedTilemapType = TilemapType.Ground;
    private int selectedTileIndex = 0;
    private int selectedPrefabIndex = 0;

    private List<PlacedTile> placedTiles = new List<PlacedTile>();
    private List<PlacedPrefab> placedPrefabs = new List<PlacedPrefab>();
    private List<Vector2Int> patrolCells = new List<Vector2Int>();

    private Vector2 scrollPos;
    private bool isDragging = false;
    private float gridZoom = 100f;
    private bool eraseMode = false;

    private enum EditMode { None, Tile, Prefab, PatrolPoint, PlayerSpawn }
    private EditMode currentMode = EditMode.Tile;

    private Vector2Int? hoveredCell = null;
    private int currentRotation = 0;
    private bool flipX = false;
    private bool flipY = false;

    private int selectedBackgroundIndex = -1;
    private Vector2Int? playerSpawnCell = null;

    [MenuItem("Tools/Tilemap Level Editor")]
    public static void ShowWindow() => GetWindow<TilemapLevelEditor>("Tilemap Level Editor");

    private void OnGUI()
    {
        GUILayout.Label("\ud83e\udde9 Tilemap Level Editor", EditorStyles.boldLabel);
        assetsDatabase = (LevelAssetsDatabase)EditorGUILayout.ObjectField("Assets DB", assetsDatabase, typeof(LevelAssetsDatabase), false);
        if (assetsDatabase == null) return;

        gridWidth = EditorGUILayout.IntField("Grid Width", gridWidth);
        gridHeight = EditorGUILayout.IntField("Grid Height", gridHeight);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("\u25c0 Prev", GUILayout.Width(60))) { currentLevel = Mathf.Max(1, currentLevel - 1); LoadLevel(); }
        currentLevel = EditorGUILayout.IntField("Level", currentLevel);
        if (GUILayout.Button("Next \u25b6", GUILayout.Width(60))) { currentLevel++; LoadLevel(); }
        EditorGUILayout.EndHorizontal();

        gridZoom = EditorGUILayout.Slider("Grid Zoom %", gridZoom, 25f, 300f);


        GUILayout.Space(5);
        GUILayout.Label("\u270f\ufe0f Mode Selection", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        DrawModeButton(EditMode.Tile, "Tile Mode");
        DrawModeButton(EditMode.Prefab, "Prefab Mode");
        DrawModeButton(EditMode.PatrolPoint, "Patrol Point");
        DrawModeButton(EditMode.PlayerSpawn, "Player Spawn");
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);
        if (assetsDatabase.tilemapTypes != null && assetsDatabase.tilemapTypes.Length > 0)
        {
            string[] names = assetsDatabase.tilemapTypes.Select(t => t.displayName).ToArray();
            int currentIdx = System.Array.FindIndex(assetsDatabase.tilemapTypes, t => t.type == selectedTilemapType);
            int newIdx = EditorGUILayout.Popup("Tilemap Type", currentIdx, names);
            if (newIdx >= 0) selectedTilemapType = assetsDatabase.tilemapTypes[newIdx].type;
        }

        if (assetsDatabase.tiles != null && assetsDatabase.tiles.Length > 0)
        {
            GUILayout.Label($"Tiles (Rotation: {currentRotation}\u00b0)", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            flipX = GUILayout.Toggle(flipX, "Flip X");
            flipY = GUILayout.Toggle(flipY, "Flip Y");
            GUILayout.EndHorizontal();
            DrawAssetPalette(assetsDatabase.tiles, ref selectedTileIndex, 48f);
        }





        if (assetsDatabase.prefabs != null && assetsDatabase.prefabs.Length > 0)
        {
            GUILayout.Label("Prefabs", EditorStyles.boldLabel);
            DrawAssetPalette(assetsDatabase.prefabs, ref selectedPrefabIndex, 48f);
        }

        if (assetsDatabase.backgroundSprites != null && assetsDatabase.backgroundSprites.Length > 0)
        {
            GUILayout.Label("Background Sprites", EditorStyles.boldLabel);
            DrawSpritePalette(assetsDatabase.backgroundSprites, 48f);
        }

        GUILayout.Space(20);

        eraseMode = GUILayout.Toggle(eraseMode, "Erase Mode");

        GUILayout.Space(5);
        GUILayout.Label("\ud83e\uddf9 Clear Options", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Tiles")) placedTiles.Clear();
        if (GUILayout.Button("Prefabs")) placedPrefabs.Clear();
        if (GUILayout.Button("Patrols")) patrolCells.Clear();
        if (GUILayout.Button("Player")) playerSpawnCell = null;
        if (GUILayout.Button("All")) { placedTiles.Clear(); placedPrefabs.Clear(); patrolCells.Clear(); }
        EditorGUILayout.EndHorizontal();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
        DrawGrid();
        EditorGUILayout.EndScrollView();

        GUILayout.FlexibleSpace();

        GUILayout.Space(10);
        GUILayout.Label("\ud83d\udcbe Save / Load", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save")) SaveLevel();
        if (GUILayout.Button("Load")) LoadLevel();
        EditorGUILayout.EndHorizontal();

        HandleEvents();
    }

    private void DrawModeButton(EditMode mode, string label)
    {
        bool active = (currentMode == mode);
        GUI.backgroundColor = active ? Color.cyan : Color.white;
        if (GUILayout.Button(label))
            currentMode = active ? EditMode.None : mode;
        GUI.backgroundColor = Color.white;
    }

    private void DrawSpritePalette(Sprite[] sprites, float size)
    {
        int perRow = Mathf.Max(1, Mathf.FloorToInt((position.width - 40) / (size + 6)));

        for (int i = 0; i < sprites.Length; i += perRow)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < perRow && i + j < sprites.Length; j++)
            {
                int idx = i + j;
                var sprite = sprites[idx];
                Texture2D tex = sprite != null ? AssetPreview.GetAssetPreview(sprite) : Texture2D.grayTexture;

                GUI.backgroundColor = (selectedBackgroundIndex == idx) ? Color.green : Color.white;

                if (GUILayout.Button(tex, GUILayout.Width(size), GUILayout.Height(size)))
                    selectedBackgroundIndex = idx;

                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawAssetPalette(Object[] assets, ref int selectedIndex, float size)
    {
        int perRow = Mathf.Max(1, Mathf.FloorToInt((position.width - 40) / (size + 6)));
        for (int i = 0; i < assets.Length; i += perRow)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < perRow && i + j < assets.Length; j++)
            {
                int idx = i + j;
                var asset = assets[idx];
                Texture2D preview = null;

                if (asset is Tile tile && tile.sprite != null)
                    preview = tile.sprite.texture;
                else if (asset)
                    preview = AssetPreview.GetAssetPreview(asset) ?? AssetPreview.GetMiniThumbnail(asset);
                if (preview == null) preview = Texture2D.grayTexture;

                GUI.backgroundColor = (selectedIndex == idx) ? Color.cyan : Color.white;

                GUILayout.BeginVertical(GUILayout.Width(size + 4));
                if (GUILayout.Button(preview, GUILayout.Width(size), GUILayout.Height(size)))
                    selectedIndex = idx;

                if (asset is GameObject)
                {
                    GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
                    labelStyle.fontSize = 9;
                    labelStyle.alignment = TextAnchor.MiddleCenter;
                    labelStyle.wordWrap = true;

                    GUILayout.Label(asset.name, labelStyle, GUILayout.Width(size));
                }

                GUILayout.EndVertical();

                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawGrid()
    {
        float cellSize = 40f * (gridZoom / 100f);
        hoveredCell = null;

        for (int y = gridHeight - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(cellSize));
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Rect rect = GUILayoutUtility.GetRect(cellSize, cellSize);

                if (rect.Contains(Event.current.mousePosition))
                    hoveredCell = cell;

                bool isPatrol = patrolCells.Contains(cell);
                bool isPlayer = playerSpawnCell.HasValue && playerSpawnCell.Value == cell; // ✅

                GUI.color = isPlayer ? new Color(1f, 1f, 0f, 0.8f) : // ✅ Yellow highlight
                            isPatrol ? new Color(0, 0.5f, 0.8f, 0.8f) :
                            eraseMode ? new Color(1, 0.5f, 0.5f) : Color.white;

                if (GUI.Button(rect, GUIContent.none)) ToggleCellAt(x, y);
                GUI.color = Color.white;

                var prefab = placedPrefabs.FirstOrDefault(p => p.position == cell);
                if (prefab != null && prefab.prefabIndex < assetsDatabase.prefabs.Length)
                {
                    var p = assetsDatabase.prefabs[prefab.prefabIndex];
                    var tex = AssetPreview.GetAssetPreview(p) ?? AssetPreview.GetMiniThumbnail(p);
                    if (tex != null)
                        GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
                }
                else
                {
                    var tile = placedTiles.FirstOrDefault(t => t.position == cell);
                    if (tile != null && tile.tileIndex < assetsDatabase.tiles.Length)
                    {
                        var t = assetsDatabase.tiles[tile.tileIndex];
                        Texture2D tex = (t is Tile cast && cast.sprite != null) ? cast.sprite.texture : AssetPreview.GetMiniThumbnail(t);
                        if (tex != null)
                            DrawTextureRotated(rect, tex, tile.rotation, tile.flipX, tile.flipY);
                    }
                }

                if (hoveredCell == cell)
                {
                    Texture2D hover = null;
                    int angle = 0;
                    if (currentMode == EditMode.Tile && selectedTileIndex < assetsDatabase.tiles.Length)
                    {
                        var t = assetsDatabase.tiles[selectedTileIndex];
                        if (t is Tile cast && cast.sprite != null)
                            hover = cast.sprite.texture;
                        else hover = AssetPreview.GetMiniThumbnail(t);
                        angle = currentRotation;
                    }
                    else if (currentMode == EditMode.Prefab && selectedPrefabIndex < assetsDatabase.prefabs.Length)
                    {
                        var p = assetsDatabase.prefabs[selectedPrefabIndex];
                        hover = AssetPreview.GetAssetPreview(p) ?? AssetPreview.GetMiniThumbnail(p);
                    }

                    if (hover)
                    {
                        GUI.color = new Color(1, 1, 1, 0.5f);
                        DrawTextureRotated(rect, hover, angle, flipX, flipY);
                        GUI.color = Color.white;
                    }
                }

                if (isDragging && rect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDrag)
                    ToggleCellAt(x, y);
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawTextureRotated(Rect rect, Texture2D tex, int angle, bool flipX, bool flipY)
    {
        Matrix4x4 prevMatrix = GUI.matrix;

        Vector2 pivot = rect.center;

        Vector2 scale = new Vector2(flipX ? -1f : 1f, flipY ? -1f : 1f);
        GUIUtility.ScaleAroundPivot(scale, pivot);
        GUIUtility.RotateAroundPivot(angle, pivot);
        GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
        GUI.matrix = prevMatrix;
    }

    private void ToggleCellAt(int x, int y)
    {
        Vector2Int c = new Vector2Int(x, y);
        if (currentMode == EditMode.Prefab)
        {
            if (eraseMode) placedPrefabs.RemoveAll(p => p.position == c);
            else placedPrefabs.Add(new PlacedPrefab { position = c, prefabIndex = selectedPrefabIndex, size = Vector2Int.one });
        }
        else if (currentMode == EditMode.Tile)
        {
            if (eraseMode) placedTiles.RemoveAll(t => t.position == c && t.tilemapType == selectedTilemapType);
            else placedTiles.Add(new PlacedTile
            {
                position = c,
                tileIndex = selectedTileIndex,
                tilemapType = selectedTilemapType,
                rotation = currentRotation,
                flipX = flipX,
                flipY = flipY
            });
        }
        else if (currentMode == EditMode.PatrolPoint)
        {
            if (patrolCells.Contains(c)) patrolCells.Remove(c); else patrolCells.Add(c);
        }
        else if (currentMode == EditMode.PlayerSpawn) // ✅ Set or clear player spawn
        {
            if (playerSpawnCell == c) playerSpawnCell = null;
            else playerSpawnCell = c;
        }
    }

    private void SaveLevel()
    {
        var d = new LevelTileData
        {
            width = gridWidth,
            height = gridHeight,
            tiles = placedTiles,
            prefabs = placedPrefabs,
            patrolPoints = patrolCells,
            selectedBackgroundIndex = selectedBackgroundIndex,
            playerSpawn = playerSpawnCell.HasValue ? playerSpawnCell.Value : new Vector2Int(-1, -1) // ✅
        };

        File.WriteAllText(Application.dataPath + $"/Level{currentLevel}.json", JsonUtility.ToJson(d, true));
        AssetDatabase.Refresh();
        Debug.Log("Saved!");
    }

    private void LoadLevel()
    {
        string p = Application.dataPath + $"/Level{currentLevel}.json";
        if (!File.Exists(p)) { Debug.LogWarning("Not found"); return; }

        var d = JsonUtility.FromJson<LevelTileData>(File.ReadAllText(p));
        gridWidth = d.width;
        gridHeight = d.height;
        placedTiles = d.tiles;
        placedPrefabs = d.prefabs;
        patrolCells = d.patrolPoints;
        selectedBackgroundIndex = d.selectedBackgroundIndex;
        playerSpawnCell = (d.playerSpawn.x >= 0 && d.playerSpawn.y >= 0) ? (Vector2Int?)d.playerSpawn : null; // ✅
        Debug.Log("Loaded!");
    }

    private void HandleEvents()
    {
        var e = Event.current;

        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.R)
            {
                currentRotation = (currentRotation + 90) % 360;
                Repaint();
            }
            else if (e.keyCode == KeyCode.H)
            {
                flipX = !flipX;
                Repaint();
            }
            else if (e.keyCode == KeyCode.V)
            {
                flipY = !flipY;
                Repaint();
            }
        }

        if (e.type == EventType.MouseDown && e.button == 0) isDragging = true;
        if (e.type == EventType.MouseUp && e.button == 0) isDragging = false;
    }
}

[System.Serializable] public class PlacedTile { public Vector2Int position; public int tileIndex; public TilemapType tilemapType; public int rotation; public bool flipX; public bool flipY; }
[System.Serializable] public class PlacedPrefab { public Vector2Int position; public int prefabIndex; public Vector2Int size; }

[System.Serializable]
public class LevelTileData
{
    public int width, height;
    public List<PlacedTile> tiles;
    public List<PlacedPrefab> prefabs;
    public List<Vector2Int> patrolPoints;
    public int selectedBackgroundIndex = -1;

    public Vector2Int playerSpawn = new Vector2Int(-1, -1); // ✅ Add this
}
