using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class GridHighlighter : MonoBehaviour
{
    public static GridHighlighter Instance { get; private set; }

    [SerializeField] private Tilemap selectionTilemap;
    [SerializeField] private Tilemap effectTilemap;
    [SerializeField] private Tilemap previewTilemap;
    [SerializeField] private Color highlightColor = new(1, 1, 1, 0.8f);
    [SerializeField] private TileBase highlightTile;
    [SerializeField] private TileBase previewTile;
    [SerializeField] private TileBase defaultEffectTile;
    
    [Header("Dimming")]
    [SerializeField, Range(0f, 1f)] private float dimmedAlphaFactor = 0.15f;

    private const float FadedAlphaFactor = 0.30f;

    private bool hasSavedAlpha;
    private float savedSelectionAlpha;
    private float savedEffectAlpha;
    
    private void Awake()
    {
        Instance = this;
    }

    public void UpdateTilemaps(Tilemap selection, Tilemap preview, Tilemap effect)
    {
        if (selectionTilemap != null) selectionTilemap.ClearAllTiles();
        if (effectTilemap != null) effectTilemap.ClearAllTiles();
        if (previewTilemap != null) previewTilemap.ClearAllTiles();

        selectionTilemap = selection;
        previewTilemap = preview;
        effectTilemap = effect;
    }
    
    
    #region Selection (player)

    /// <summary>
    /// Подсветка доступных клеток способности игрока
    /// </summary>
    public void HighlightCells(List<Vector3Int> cells, Color? color = null, TileBase customTile = null)
    {
        if (selectionTilemap == null) return;

        selectionTilemap.ClearAllTiles();
        var baseColor = color ?? highlightColor;
        var tileToSet = customTile != null ? customTile : highlightTile;

        foreach (var cell in cells)
        {
            if (!HasFloor(cell)) continue;
            SetTile(selectionTilemap, cell, tileToSet, baseColor);
        }
    }

    /// <summary>
    /// Подсветка: теоретическая и достигаемая
    /// </summary>
    public void HighlightCellsTwoLayers(
        List<Vector3Int> fadedCells,
        List<Vector3Int> reachableCells,
        Color baseColor,
        TileBase customTile = null)
    {
        if (selectionTilemap == null) return;

        selectionTilemap.ClearAllTiles();
        var tile = customTile != null ? customTile : highlightTile;

        var fadedColor = new Color(baseColor.r, baseColor.g, baseColor.b,
            baseColor.a * FadedAlphaFactor);

        foreach (var cell in fadedCells)
        {
            if (!HasFloor(cell)) continue;
            SetTile(selectionTilemap, cell, tile, fadedColor);
        }

        foreach (var cell in reachableCells)
        {
            if (!HasFloor(cell)) continue;
            SetTile(selectionTilemap, cell, tile, baseColor);
        }
    }

    #endregion
    
    #region Effect

    public void HighlightEffect(List<Vector3Int> cells, Color? color = null, TileBase customTile = null)
    {
        if (effectTilemap == null) return;

        effectTilemap.ClearAllTiles();
        var baseColor = color ?? highlightColor;
        var tileToSet = customTile != null ? customTile : defaultEffectTile;

        foreach (var cell in cells)
        {
            if (!HasFloor(cell)) continue;
            SetTile(effectTilemap, cell, tileToSet, baseColor);
        }
    }

    public void ClearEffect() => effectTilemap?.ClearAllTiles();

    #endregion

    #region Preview (enemy)

    public void HighlightPreview(List<Vector3Int> cells, Color color, TileBase customTile = null)
    {
        if (previewTilemap == null) return;
        previewTilemap.ClearAllTiles();

        var tileToSet = customTile != null ? customTile : previewTile;
        foreach (var cell in cells)
        {
            if (!HasFloor(cell)) continue;
            SetTile(previewTilemap, cell, tileToSet, color);
        }
    }

    public void HighlightPreviewTwoLayers(
        List<Vector3Int> fadedCells,
        List<Vector3Int> reachableCells,
        Color reachableColor,
        TileBase customTile = null)
    {
        if (previewTilemap == null) return;
        previewTilemap.ClearAllTiles();

        var tile = customTile != null ? customTile : previewTile;

        var fadedColor = new Color(reachableColor.r, reachableColor.g, reachableColor.b,
                                   reachableColor.a * FadedAlphaFactor);

        foreach (var cell in fadedCells)
        {
            if (!HasFloor(cell)) continue;
            SetTile(previewTilemap, cell, tile, fadedColor);
        }

        foreach (var cell in reachableCells)
        {
            if (!HasFloor(cell)) continue;
            SetTile(previewTilemap, cell, tile, reachableColor);
        }
    }

    public void ClearPreview()
    {
        previewTilemap?.ClearAllTiles();
    }

    #endregion

    #region Dim

    public void DimPlayerHighlights()
    {
        if (!hasSavedAlpha)
        {
            if (selectionTilemap != null) savedSelectionAlpha = selectionTilemap.color.a;
            if (effectTilemap    != null) savedEffectAlpha    = effectTilemap.color.a;
            hasSavedAlpha = true;
        }

        ApplyTilemapAlpha(selectionTilemap, savedSelectionAlpha * dimmedAlphaFactor);
        ApplyTilemapAlpha(effectTilemap,    savedEffectAlpha    * dimmedAlphaFactor);
    }

    public void ResetDim()
    {
        if (!hasSavedAlpha) return;

        ApplyTilemapAlpha(selectionTilemap, savedSelectionAlpha);
        ApplyTilemapAlpha(effectTilemap,    savedEffectAlpha);

        hasSavedAlpha = false;
    }

    private static void ApplyTilemapAlpha(Tilemap tilemap, float alpha)
    {
        if (tilemap == null) return;
        var c = tilemap.color;
        tilemap.color = new Color(c.r, c.g, c.b, alpha);
    }

    #endregion

    public void Clear()
    {
        if (selectionTilemap != null)
            selectionTilemap.ClearAllTiles();

        if (effectTilemap != null)
            effectTilemap.ClearAllTiles();
    }

    private void SetTile(Tilemap tilemap, Vector3Int cell, TileBase tile, Color color)
    {
        tilemap.SetTile(cell, tile);
        tilemap.SetTileFlags(cell, TileFlags.None);
        tilemap.SetColor(cell, color);
    }
    
    private static bool HasFloor(Vector3Int cell)
    {
        return GridManager.Instance != null && GridManager.Instance.HasFloor(cell);
    }
}