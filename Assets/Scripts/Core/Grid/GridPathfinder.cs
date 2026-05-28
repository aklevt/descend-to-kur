using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Отвечает за все алгоритмы поиска клеток на сетке
/// </summary>
public class GridPathfinder
{
    #region Constants

    // Константы для защиты от зависания
    private const int MaxPathfindingIterations = 50000;
    private const int MaxRangeSearchIterations = 50000;
    private const int MaxAttackRangeLimit = 50;

    private static readonly Vector3Int[] AdjacentDirections =
    {
        Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right
    };

    #endregion

    private readonly GridManager gridManager;

    public GridPathfinder(GridManager gridManager)
    {
        this.gridManager = gridManager;
    }

    /// <summary>
    /// Возвращает кратчайшее расстояние между двумя клетками с учётом препятствий (BFS)
    /// </summary>
    public int GetPathDistance(Vector3Int start, Vector3Int target, GameObject currentEntity = null)
    {
        if (start == target) return 0;

        var options = CellCheckOptions.ForMovement(currentEntity);
        var queue = new Queue<(Vector3Int pos, int dist)>();
        var visited = new HashSet<Vector3Int> { start };
        queue.Enqueue((start, 0));

        var iterations = 0;

        while (queue.Count > 0 && iterations < MaxPathfindingIterations)
        {
            iterations++;
            var (current, dist) = queue.Dequeue();

            foreach (var dir in AdjacentDirections)
            {
                var next = current + dir;
                if (next == target) return dist + 1;

                if (visited.Contains(next) || !gridManager.IsCellPassable(next, options))
                    continue;

                visited.Add(next);
                queue.Enqueue((next, dist + 1));
            }
        }

        if (iterations >= MaxPathfindingIterations)
            LogIterationLimitExceeded("GetPathDistance", $"{start} -> {target}");
        return int.MaxValue;
    }

    /// <summary>
    /// Ищет все доступные клетки в указанном радиусе, используя выбранный алгоритм
    /// </summary>
    /// <param name="center">Центральная клетка для поиска</param>
    /// <param name="maxRange">Максимальный радиус поиска</param>
    /// <param name="minRange">Минимальный радиус (для исключения ближних клеток)</param>
    /// <param name="options">Параметры проверки клеток и выбор алгоритма</param>
    /// <returns>Список доступных клеток</returns>
    public List<Vector3Int> GetCellsInRange(Vector3Int center, int maxRange, int minRange, CellCheckOptions options)
    {
        maxRange = Mathf.Min(maxRange, MaxAttackRangeLimit);

        if (options.useBFS)
        {
            return GetCellsBFS(center, maxRange, options);
        }
        else
        {
            return GetCellsManhattanRange(center, maxRange, minRange, options);
        }
    }

    /// <summary>
    /// Поиск клеток через BFS - учитывает реальные пути через проходимые клетки
    /// Используется для движения персонажей
    /// </summary>
    private List<Vector3Int> GetCellsBFS(Vector3Int startPos, int range, CellCheckOptions options)
    {
        if (range <= 0) return new List<Vector3Int>();

        var result = new HashSet<Vector3Int>();
        var visited = new HashSet<Vector3Int> { startPos };
        var queue = new Queue<(Vector3Int pos, int steps)>();
        queue.Enqueue((startPos, 0));

        var iterations = 0;

        while (queue.Count > 0 && iterations < MaxRangeSearchIterations)
        {
            iterations++;
            var (pos, steps) = queue.Dequeue();

            if (steps > 0 && gridManager.IsCellPassable(pos, options))
            {
                result.Add(pos);
            }

            if (steps >= range) continue;

            foreach (var dir in AdjacentDirections)
            {
                var next = pos + dir;

                if (visited.Contains(next) || !gridManager.IsCellPassable(next, options))
                    continue;
                
                if (options.boundaryCheck != null && !options.boundaryCheck(next))
                    continue;

                visited.Add(next);
                queue.Enqueue((next, steps + 1));
            }
        }

        if (iterations >= MaxRangeSearchIterations)
        {
            LogIterationLimitExceeded("GetCellsBFS", $"{startPos}, range={range}");
        }

        return new List<Vector3Int>(result);
    }

    /// <summary>
    /// Простой перебор всех клеток в заданном радиусе. Не учитывает препятствия между центром и целью
    /// </summary>
    private List<Vector3Int> GetCellsManhattanRange(Vector3Int center, int maxRange, int minRange,
        CellCheckOptions options)
    {
        var cells = new List<Vector3Int>();

        for (var x = -maxRange; x <= maxRange; x++)
        {
            for (var y = -maxRange; y <= maxRange; y++)
            {
                var distance = Mathf.Abs(x) + Mathf.Abs(y);
                if (distance > maxRange || distance < minRange) continue;

                var cellPos = new Vector3Int(center.x + x, center.y + y, center.z);

                if (options.useBFS 
                        ? gridManager.IsCellPassable(cellPos, options)
                        : gridManager.IsCellTargetable(cellPos))
                {
                    cells.Add(cellPos);
                }
            }
        }

        return cells;
    }

    /// <summary>
    /// Строит путь от начальной до целевой клетки через проходимые клетки (BFS)
    /// </summary>
    /// <param name="start">Начальная клетка</param>
    /// <param name="target">Целевая клетка</param>
    /// <param name="options">Набор опций (например, сущность, для которой строится путь)</param>
    /// <returns>Список клеток пути (включая start и target) или пустой список если путь не найден</returns>
    public List<Vector3Int> GetPath(Vector3Int start, Vector3Int target, CellCheckOptions options)
    {
        if (start == target) return new List<Vector3Int> { start };

        var queue = new Queue<Vector3Int>();
        var visited = new HashSet<Vector3Int> { start };
        var parent = new Dictionary<Vector3Int, Vector3Int>();

        queue.Enqueue(start);
        var iterations = 0;

        while (queue.Count > 0 && iterations < MaxPathfindingIterations)
        {
            iterations++;
            var current = queue.Dequeue();

            foreach (var dir in AdjacentDirections)
            {
                var next = current + dir;

                if (next == target)
                {
                    parent[next] = current;
                    return ReconstructPath(parent, start, target);
                }

                if (visited.Contains(next) || !gridManager.IsCellPassable(next, options))
                    continue;
                
                if (options.boundaryCheck != null && !options.boundaryCheck(next))
                    continue;

                visited.Add(next);
                parent[next] = current;
                queue.Enqueue(next);
            }
        }

        if (iterations >= MaxPathfindingIterations)
            LogIterationLimitExceeded("GetPath", $"{start} -> {target}");

        return new List<Vector3Int>();
    }

    private List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> parent, Vector3Int start,
        Vector3Int target)
    {
        var path = new List<Vector3Int>();
        var current = target;

        while (current != start)
        {
            path.Add(current);
            current = parent[current];
        }

        path.Add(start);
        path.Reverse();
        return path;
    }

    private static void LogIterationLimitExceeded(string operation, string details)
    {
        Debug.LogWarning($"[GridPathfinder] Превышен лимит итераций в {operation}: {details}");
    }
}