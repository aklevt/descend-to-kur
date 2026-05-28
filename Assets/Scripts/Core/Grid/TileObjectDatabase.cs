using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TileObjectDatabase", menuName = "Grid/Tile Entity Database")]
public class TileObjectDatabase : ScriptableObject
{
    public List<TileMapping> mappings = new();

    [System.Serializable]
    public struct TileMapping
    {
        public TileBase tile;
        public GameObject entityPrefab;
    }

    public GameObject GetPrefabForTile(TileBase tile)
    {
        if (!mappings.Exists(m => m.tile == tile))
        {
            return null;
        }

        var mapping = mappings.Find(m => m.tile == tile);
        return mapping.entityPrefab;
    }
}