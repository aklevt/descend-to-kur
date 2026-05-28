using System.Collections.Generic;
using Core;
using Core.Room;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FogSystemManager : MonoBehaviour
{
    public static FogSystemManager Instance { get; private set; }
    
    [SerializeField] private List<FogLayerController> fogLayers = new();
    
    [Header("Auto Scaling")]
    [SerializeField] private bool autoScaleToMap = true;
    [SerializeField] private float padding = 2f;
    [SerializeField] private Vector2 minSize = new Vector2(10f, 10f);
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    private void Start()
    {
        if (autoScaleToMap)
        {
            ScaleToCurrentRoom();
        }
    }
    
    /// <summary>
    /// Масштабирует туман под размер текущей комнаты
    /// </summary>
    public void ScaleToCurrentRoom()
    {
        var roomController = FindFirstObjectByType<RoomController>();
        if (roomController?.wallsTilemap != null)
        {
            ScaleToTilemap(roomController.wallsTilemap);
        }
        else
        {
            var tilemap = FindFirstObjectByType<Tilemap>();
            if (tilemap != null)
            {
                ScaleToTilemap(tilemap);
            }
        }
    }
    
    /// <summary>
    /// Масштабирует туман под конкретный Tilemap
    /// </summary>
    public void ScaleToTilemap(Tilemap tilemap)
    {
        if (tilemap == null) return;
        
        var bounds = tilemap.localBounds;
        
        bounds.Expand(padding * 2f);
        
        if (bounds.size.x < minSize.x) bounds.size = new Vector3(minSize.x, bounds.size.y, bounds.size.z);
        if (bounds.size.y < minSize.y) bounds.size = new Vector3(bounds.size.x, minSize.y, bounds.size.z);
        
        ScaleToSize(bounds);
    }
    
    /// <summary>
    /// Масштабирует туман под указанные границы
    /// </summary>
    public void ScaleToSize(Bounds targetBounds)
    {
        foreach (var layer in fogLayers)
        {
            if (layer == null) continue;
            
            ScaleFogLayer(layer, targetBounds);
        }
    }
    
    /// <summary>
    /// Масштабирует конкретный слой тумана
    /// </summary>
    private void ScaleFogLayer(FogLayerController layer, Bounds targetBounds)
    {
        var spriteRenderer = layer.GetComponent<SpriteRenderer>();
        if (spriteRenderer?.sprite == null) return;
        
        var spriteSize = spriteRenderer.sprite.bounds.size;
        var scaleX = targetBounds.size.x / spriteSize.x;
        var scaleY = targetBounds.size.y / spriteSize.y;
        
        layer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        
        layer.transform.position = new Vector3(targetBounds.center.x, targetBounds.center.y, layer.transform.position.z);
    }
    
    /// <summary>
    /// Вызывается при смене комнаты
    /// </summary>
    public void OnRoomChanged()
    {
        if (autoScaleToMap)
        {
            ScaleToCurrentRoom();
        }
    }
    
    public void ClearFog(float duration = 2f)
    {
        foreach (var layer in fogLayers)
        {
            if (layer != null)
                layer.FadeTo(0f, duration);
        }
    }
    
    public void IntensifyFog(float duration = 1f)
    {
        foreach (var layer in fogLayers)
        {
            if (layer != null)
                layer.FadeTo(0.9f, duration);
        }
    }
    
    /// <summary>
    /// Для дебага
    /// </summary>
    [ContextMenu("Debug: Scale to Current Room")]
    public void DebugScaleToRoom()
    {
        ScaleToCurrentRoom();
    }
}