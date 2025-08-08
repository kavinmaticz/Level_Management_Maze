/*using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SpriteAssigner : MonoBehaviour
{
    public static SpriteAssigner Instance;
    public LevelAssetsDatabase assets;
    public List<Sprite> extractImages = new List<Sprite>();
    public Tile[] tile;


    private void Awake()
    {
        Instance = this;
    }


    public void AssignData()
    {
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>("MySprites/Tile");
        extractImages.Clear();
        extractImages.AddRange(loadedSprites);

        Debug.Log($"Loaded {extractImages.Count} sprites");

        //targetTilemap.ClearAllTiles();

        for (int i = 0; i < extractImages.Count; i++)
        {
            tile[i].sprite = extractImages[i];
        }

        LevelManager.Instance.StartData();
    }
}
*/

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SpriteAssigner : MonoBehaviour
{
    public static SpriteAssigner Instance;
    public LevelAssetsDatabase assets;
    public List<Sprite> extractImages = new List<Sprite>();
    public Tile[] tile;

#if UNITY_ANDROID && !UNITY_EDITOR
    private string spriteFolderPath;
#endif

    private void Awake()
    {
        Instance = this;

#if UNITY_ANDROID && !UNITY_EDITOR
        spriteFolderPath = Path.Combine(Application.persistentDataPath, "MySprites/Tile");
#endif
    }

    public void AssignData()
    {
        extractImages.Clear();

#if UNITY_EDITOR
        // Load from Resources in Editor
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>("MySprites/Tile");
        extractImages.AddRange(loadedSprites);
        Debug.Log($"[Editor] Loaded {extractImages.Count} sprites from Resources/MySprites/Tile");
#else
        // Load from persistent data path on Android
        if (!Directory.Exists(spriteFolderPath))
        {
            Debug.LogError("Sprite folder not found: " + spriteFolderPath);
            return;
        }

        string[] files = Directory.GetFiles(spriteFolderPath, "*.png");
        Debug.Log($"[Android] Found {files.Length} sprites in {spriteFolderPath}");

        for (int i = 0; i < files.Length; i++)
        {
            byte[] fileData = File.ReadAllBytes(files[i]);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );

            extractImages.Add(sprite);
        }
#endif

        // Assign sprites to tiles
        for (int i = 0; i < extractImages.Count && i < tile.Length; i++)
        {
            tile[i].sprite = extractImages[i];
        }

        LevelManager.Instance.StartData();
    }
}
