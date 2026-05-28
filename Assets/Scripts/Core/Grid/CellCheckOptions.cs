using UnityEngine;
using System;

/// <summary>
/// Параметры проверки клетки при поиске
/// </summary>
public struct CellCheckOptions
{
    public bool checkEntities; // Учитывать ли сущности при проверке проходимости
    public bool useBFS; // Использовать BFS (true) или простой перебор без учета препятствий (false)
    public GameObject currentEntity; // Сущность, для которой выполняется проверка

    // Дополнительная проверка границ, чтобы BFS не заходил дальше определенной границы
    public System.Func<Vector3Int, bool> boundaryCheck;

    public static CellCheckOptions ForMovement(GameObject entity = null, Func<Vector3Int, bool> boundaryCheck = null)
    {
        return new CellCheckOptions
        {
            checkEntities = true,
            useBFS = true,
            currentEntity = entity,
            boundaryCheck = boundaryCheck
        };
    }

    public static CellCheckOptions ForAttack()
    {
        return new CellCheckOptions
        {
            checkEntities = false,
            useBFS = false,
            currentEntity = null,
            boundaryCheck = null
        };
    }
}