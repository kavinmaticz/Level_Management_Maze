using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "LevelAssetsDatabase", menuName = "Custom/Level Assets Database")]
public class LevelAssetsDatabase : ScriptableObject
{
    public TileBase[] tiles;           // list of tiles to paint
    public GameObject[] prefabs;       // list of placeable prefabs
    public GameObject enemyPrefab;     // single enemy prefab

    [System.Serializable]
    public class TilemapBinding
    {
        public TilemapType type;
        public string displayName;     // e.g. "Ground Layer"
    }

    public TilemapBinding[] tilemapTypes; // define available tilemap types
}
