using System;
using System.Collections.Generic;
using Entities;
using UnityEngine;

namespace Core.Room
{
    /// <summary>
    /// Интерфейс для проверки границ и ограничений комнаты.
    /// Прячет то, что комната разбита на секции
    /// </summary>
    public interface IRoomBoundaryProvider
    {
        /// <summary>
        /// Можно ли переместиться в указанную клетку
        /// </summary>
        bool CanMoveTo(Vector3Int cell);
    
        /// <summary>
        /// Можно ли использовать способность на указанную позицию
        /// </summary>
        bool CanTargetPosition(Vector3 worldPos);
    
        /// <summary>
        /// Активен ли враг (находится ли в активной секции)
        /// </summary>
        bool IsEnemyActive(EnemyBase enemy);
    
        /// <summary>
        /// Получить текущие границы камеры
        /// </summary>
        Bounds GetCameraBounds();
    
        /// <summary>
        /// Событие изменения границ камеры (при переходе между секциями)
        /// </summary>
        event Action<Bounds> OnCameraBoundsChanged;
        
        /// <summary>
        /// Возвращает список врагов, которые должны ходить в данный момент
        /// </summary>
        List<EnemyBase> GetActiveEnemiesForTurn();
    }
}