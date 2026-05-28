using Entities;
using UnityEngine;

/// <summary>
/// Интерфейс для статичных объектов на клетках (ловушки, хилки)
/// </summary>
public interface ITileObject
{
    /// <summary>Блокирует ли этот объект движение</summary>
    bool BlocksMovement { get; }
    Vector3Int CellPosition { get; set; } 
    
    /// <summary>Вызывается когда BaseEntity входит в клетку</summary>
    void OnEntityEnter(BaseEntity entity);
    
    /// <summary>Вызывается в начале хода, если BaseEntity стоит на клетке</summary>
    void OnEntityStay(BaseEntity entity);
    
    /// <summary>Вызывается когда BaseEntity покидает клетку</summary>
    void OnEntityExit(BaseEntity entity);
    
    /// <summary> Вызывается когда игрок завершает ход стоя на этой клетке </summary>
    void OnPlayerEndTurn(PlayerMovement player);
}