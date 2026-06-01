using System.Collections.Generic;
using Core;
using Core.Room;
using Entities;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    #region Core

    public static GridManager Instance { get; private set; }

    [SerializeField] private TileObjectDatabase tileObjectDatabase;

    private Tilemap floorTilemap; // Определяет игровое поле (где можно ходить)
    private Tilemap wallsTilemap; // Блокируют стрельбу (но не ходьбу)
    private Tilemap wallsInnerTilemap; // Внутренние стены (блокируют всё)
    private Tilemap obstaclesTilemap; // Блокируют ходьбу, но не стрельбу (полустенки)
    private Tilemap objectsTilemap; // Ловушки + хилки

    private readonly Dictionary<Vector3Int, GameObject> entitiesOnGrid = new();
    private readonly Dictionary<Vector3Int, ITileObject> tileObjectsOnGrid = new();

    private GridPathfinder pathfinder;

    #endregion

    #region Initialization

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            pathfinder = new GridPathfinder(this);
        }
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Обновляет данные комнаты: все слои тайлмапов
    /// </summary>
    public void UpdateRoomData(Tilemap floor, Tilemap walls, Tilemap wallsInner, Tilemap obstacles, Tilemap objects)
    {
        ClearTileObjects();
        entitiesOnGrid.Clear();

        floorTilemap = floor;
        wallsTilemap = walls;
        wallsInnerTilemap = wallsInner;
        obstaclesTilemap = obstacles;
        objectsTilemap = objects;

        if (tileObjectDatabase != null && objectsTilemap != null)
        {
            InitializeTileObjects();
        }
    }

    /// <summary>
    /// Автоматически создаёт объекты (ловушки/хилки) из тайлмапа
    /// </summary>
    private void InitializeTileObjects()
    {
        if (objectsTilemap == null || tileObjectDatabase == null) return;

        foreach (var pos in objectsTilemap.cellBounds.allPositionsWithin)
        {
            var tile = objectsTilemap.GetTile(pos);
            if (tile == null) continue;

            var prefab = tileObjectDatabase.GetPrefabForTile(tile);
            if (prefab != null)
            {
                var obj = Instantiate(prefab, transform);
                obj.name = $"{tile.name}_Logic_{pos}";

                var tileObject = obj.GetComponent<ITileObject>();
                if (tileObject != null)
                {
                    tileObject.CellPosition = pos;
                    tileObjectsOnGrid[pos] = tileObject;
                }
            }
        }

        Debug.Log($"<color=green>[GridManager]</color> Инициализировано {tileObjectsOnGrid.Count} tile objects");
    }

    private void ClearTileObjects()
    {
        foreach (var obj in tileObjectsOnGrid.Values)
        {
            if (obj is MonoBehaviour mb && mb != null)
                Destroy(mb.gameObject);
        }

        tileObjectsOnGrid.Clear();
    }

    /// <summary>
    /// Обновляет ссылку на тайлмап препятствий и очищает данные о сущностях на сетке (для перехода в новую комнату)
    /// </summary>
    /// <param name="newMap">Тайлпам стен из префаба новой комнаты</param>
    public void UpdateObstacles(Tilemap newMap)
    {
        obstaclesTilemap = newMap;
        entitiesOnGrid.Clear();
    }

    #endregion

    #region Entity Management

    public void MoveEntity(Vector3Int from, Vector3Int to, GameObject entity)
    {
        if (entitiesOnGrid.ContainsKey(from) && entitiesOnGrid[from] == entity)
        {
            entitiesOnGrid.Remove(from);
        }

        entitiesOnGrid[to] = entity;
    }

    public void RegisterFixedEntity(Vector3Int pos, GameObject entity)
    {
        entitiesOnGrid[pos] = entity;
    }

    public void UnregisterEntity(Vector3Int pos)
    {
        entitiesOnGrid.Remove(pos);
    }

    /// <summary>
    /// Возвращает сущность, находящаяся на указанной клетке, если она там есть
    /// </summary>
    /// <returns>GameObject сущности или null</returns>
    public GameObject GetEntityAt(Vector3Int cellPos)
    {
        if (entitiesOnGrid.TryGetValue(cellPos, out var entity))
        {
            if (entity == null)
            {
                entitiesOnGrid.Remove(cellPos);
                return null;
            }

            var health = entity.GetComponent<Health>();
            if (health != null && health.IsDead)
            {
                return null;
            }

            return entity;
        }

        return null;
    }
    
    /// <summary>
    /// Вызывает OnPlayerEndTurn для TileObject на которой стоит игрок
    /// </summary>
    public void TriggerTileObjectEndTurn(Vector3Int pos, PlayerMovement player)
    {
        if (tileObjectsOnGrid.TryGetValue(pos, out var tileObject))
        {
            tileObject.OnPlayerEndTurn(player);
        }
    }

    #endregion

    #region Cell Validation

    /// <summary>
    /// Проверяет, может ли клетка быть пройдена с учетом указанных опций
    /// Базовый метод для всех алгоритмов поиска
    /// </summary>
    /// <param name="cellPos">Координаты проверяемой клетки</param>
    /// <param name="options">Опции проверки (учитывать ли сущности, какую сущность игнорировать)</param>
    /// <returns>true если клетка проходима, false если заблокирована</returns>
    public bool IsCellPassable(Vector3Int cellPos, CellCheckOptions options)
    {
        // Должен быть пол
        if (floorTilemap == null || !floorTilemap.HasTile(cellPos))
            return false;
        
        // Внутренние стены блокируют всё
        if (wallsInnerTilemap != null && wallsInnerTilemap.HasTile(cellPos))
            return false;
        
        // Блокируют ходьбу (полустенки)
        if (obstaclesTilemap != null && obstaclesTilemap.HasTile(cellPos))
            return false;

        // Tile Objects могут блокировать
        if (tileObjectsOnGrid.TryGetValue(cellPos, out var tileObject))
        {
            if (tileObject.BlocksMovement)
                return false;
        }

        // Проверка логических границ комнаты/секции
        // Не нужно, т.к. вынесено в способность передвижения, пока что, и надо подумать,
        // можно ли в результате толчка вылетать за пределы секции
        if (RoomController.Current != null && !RoomController.Current.IsCellInsideActiveArea(cellPos))
            return false;

        if (!options.checkEntities) return true;

        // Проверка сущностей
        if (entitiesOnGrid.TryGetValue(cellPos, out var entityInCell))
        {
            if (entityInCell == null)
            {
                entitiesOnGrid.Remove(cellPos);
                return true;
            }

            if (entityInCell != options.currentEntity)
                return false;

            var health = entityInCell.GetComponent<Health>();
            if (health != null && health.IsDead)
            {
                return true;
            }
        }

        return true;
    }
    
    /// <summary>
    /// Проверяет, можно ли стрелять в эту клетку
    /// </summary>
    public bool IsCellShootable(Vector3Int cellPos)
    {
        // Должен быть пол (нельзя стрелять в пустоту)
        if (floorTilemap == null || !floorTilemap.HasTile(cellPos))
            return false;
        
        // Внутренние стены блокируют
        if (wallsInnerTilemap != null && wallsInnerTilemap.HasTile(cellPos))
            return false;
        
        // Стены блокируют стрельбу
        // if (wallsTilemap != null && wallsTilemap.HasTile(cellPos))
        //     return false;
        
        // Obstacles не блокируют стрельбу
        // Tile Objects не блокируют стрельбу
        return true;
    }
    
    /// <summary>
    /// Проверка линии видимости для дальних атак
    /// </summary>
    public bool HasLineOfSight(Vector3Int from, Vector3Int to)
    {
        var line = GetLine(from, to);
        
        foreach (var cell in line)
        {
            if (cell == from || cell == to) continue;
            
            if (floorTilemap == null || !floorTilemap.HasTile(cell))
                return false;
            
            // Внутренние стены блокируют
            if (wallsInnerTilemap != null && wallsInnerTilemap.HasTile(cell))
                return false;
            
            // if (wallsTilemap != null && wallsTilemap.HasTile(cell))
            //     return false;
        }
        
        return true;
    }

    /// <summary>
    /// Алгоритм Брезенхема для построения линии между двумя точками
    /// "алгоритм, определяющий, какие точки двумерного растра нужно закрасить,
    /// чтобы получить близкое приближение прямой линии между двумя заданными точками"
    /// </summary>
    private List<Vector3Int> GetLine(Vector3Int from, Vector3Int to)
    {
        var points = new List<Vector3Int>();
        int x = from.x, y = from.y;
        var dx = Mathf.Abs(to.x - from.x);
        var dy = Mathf.Abs(to.y - from.y);
        var sx = from.x < to.x ? 1 : -1;
        var sy = from.y < to.y ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            points.Add(new Vector3Int(x, y, 0));
            
            if (x == to.x && y == to.y) break;
            
            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
        
        return points;
    }
    
    /// <summary>
    /// Проверяет, есть ли на клетке блокирующий TileObject (ловушка)
    /// </summary>
    public bool HasBlockingTileObject(Vector3Int cellPos)
    {
        if (tileObjectsOnGrid.TryGetValue(cellPos, out var tileObject))
        {
            return tileObject.BlocksMovement;
        }
        return false;
    }
    
    /// <summary>
    /// Проверяет можно ли атаковать эту клетку (игнорирует TileObjects, но проверяет стены)
    /// </summary>
    public bool IsCellTargetable(Vector3Int cellPos)
    {
        if (floorTilemap == null || !floorTilemap.HasTile(cellPos))
            return false;
    
        if (wallsInnerTilemap != null && wallsInnerTilemap.HasTile(cellPos))
            return false;
    
        if (RoomController.Current != null && !RoomController.Current.IsCellInsideActiveArea(cellPos))
            return false;
    
        return true;
    }
    
    public bool HasFloor(Vector3Int cellPos)
    {
        return floorTilemap != null && floorTilemap.HasTile(cellPos);
    }

    #endregion
    
    #region Tile Object Triggers

    /// <summary>
    /// Вызывается когда BaseEntity входит в клетку с объектом
    /// </summary>
    public void TriggerTileObjectEnter(Vector3Int pos, BaseEntity entity)
    {
        if (tileObjectsOnGrid.TryGetValue(pos, out var tileObject))
        {
            tileObject.OnEntityEnter(entity);
        }
    }

    /// <summary>
    /// Вызывается в начале хода, если BaseEntity стоит на объекте
    /// </summary>
    public void TriggerTileObjectStay(Vector3Int pos, BaseEntity entity)
    {
        if (tileObjectsOnGrid.TryGetValue(pos, out var tileObject))
        {
            tileObject.OnEntityStay(entity);
        }
    }

    /// <summary>
    /// Вызывается когда BaseEntity покидает клетку с объектом
    /// </summary>
    public void TriggerTileObjectExit(Vector3Int pos, BaseEntity entity)
    {
        if (tileObjectsOnGrid.TryGetValue(pos, out var tileObject))
        {
            tileObject.OnEntityExit(entity);
        }
    }

    #endregion

    #region Tile Object Helpers (добавить в конец класса)

    /// <summary>
    /// Возвращает TileObject на указанной позиции, если он есть
    /// </summary>
    public ITileObject GetTileObjectAt(Vector3Int cellPos)
    {
        if (tileObjectsOnGrid.TryGetValue(cellPos, out var tileObject))
        {
            return tileObject;
        }

        return null;
    }

    public int GetHealAmountAt(Vector3Int cellPos)
    {
        var tileObject = GetTileObjectAt(cellPos);

        if (tileObject is HealZone healZone)
        {
            return healZone.HealAmount;
        }

        return 0;
    }


    /// <summary>
    /// Проверяет, является ли TileObject хилкой
    /// </summary>
    public bool IsHealingItemAt(Vector3Int cellPos)
    {
        return GetTileObjectAt(cellPos) is HealZone;
    }

    /// <summary>
    /// Проверяет, является ли TileObject ловушкой
    /// </summary>
    /// <param name="cellPos">Позиция для проверки</param>
    /// <returns>True, если это ловушка</returns>
    public bool IsTrapAt(Vector3Int cellPos)
    {
        return GetTileObjectAt(cellPos) is SpikeTrap;
    }

    #endregion


    #region Knockback Support

    /// <summary>
    /// Проверяет можно ли толкнуть сущность в эту клетку (игнорирует BlocksMovement)
    /// </summary>
    public bool IsCellKnockbackable(Vector3Int cellPos)
    {
        // Должен быть пол
        if (floorTilemap == null || !floorTilemap.HasTile(cellPos))
            return false;

        // Внутренние стены блокируют
        if (wallsInnerTilemap != null && wallsInnerTilemap.HasTile(cellPos))
            return false;

        // Obstacles блокируют толчок
        if (obstaclesTilemap != null && obstaclesTilemap.HasTile(cellPos))
            return false;

        // Границы секций блокируют
        if (RoomController.Current != null && !RoomController.Current.IsCellInsideActiveArea(cellPos))
            return false;

        // Другие сущности блокируют
        if (entitiesOnGrid.ContainsKey(cellPos))
            return false;

        return true;
    }

    /// <summary>
    /// Удаляет tile object из словаря и визуально (для подбираемых хилок)
    /// </summary>
    public void RemoveTileObject(Vector3Int pos)
    {
        if (tileObjectsOnGrid.TryGetValue(pos, out var obj))
        {
            tileObjectsOnGrid.Remove(pos);

            if (obj is MonoBehaviour mb && mb != null)
                Destroy(mb.gameObject);

            if (objectsTilemap != null)
                objectsTilemap.SetTile(pos, null);

            Debug.Log($"<color=yellow>[GridManager]</color> Tile object удален в {pos}");
        }
    }

    #endregion


    #region Search API

    /// <summary>
    /// Ищет все доступные клетки в радиусе с настраиваемыми параметрами
    /// Универсальный метод для движения и атак
    /// </summary>
    /// <param name="center">Центральная клетка</param>
    /// <param name="maxRange">Максимальный радиус</param>
    /// <param name="minRange">Минимальный радиус</param>
    /// <param name="options">Опции поиска (алгоритм, учет сущностей)</param>
    /// <returns>Список доступных клеток</returns>
    public List<Vector3Int> GetCellsInRange(Vector3Int center, int maxRange, int minRange, CellCheckOptions options)
    {
        return pathfinder.GetCellsInRange(center, maxRange, minRange, options);
    }

    /// <summary>
    /// Вычисляет кратчайшее расстояние между клетками через проходимые пути (BFS)
    /// </summary>
    /// <param name="start">Начальная клетка</param>
    /// <param name="target">Целевая клетка</param>
    /// <param name="currentEntity">Сущность для которой считается путь (может проходить через свою клетку)</param>
    /// <returns>Расстояние в клетках или int.MaxValue, если путь не найден</returns>
    public int GetPathDistance(Vector3Int start, Vector3Int target, GameObject currentEntity = null)
    {
        return pathfinder.GetPathDistance(start, target, currentEntity);
    }

    /// <summary>
    /// Строит путь между двумя клетками через проходимые клетки
    /// </summary>
    /// <param name="start">Начальная клетка</param>
    /// <param name="target">Целевая клетка</param>
    /// <param name="currentEntity">Сущность для которой строится путь</param>
    /// <returns>Список клеток пути или пустой список, если путь не найден</returns>
    public List<Vector3Int> GetPath(Vector3Int start, Vector3Int target, GameObject currentEntity = null,
        System.Func<Vector3Int, bool> boundaryCheck = null)
    {
        return pathfinder.GetPath(start, target, CellCheckOptions.ForMovement(currentEntity, boundaryCheck));
    }


    public bool IsCellWalkable(Vector3Int cellPos, GameObject currentEntity = null)
    {
        if (!IsCellPassable(cellPos, CellCheckOptions.ForMovement(currentEntity)))
            return false;

        return true;
    }

    public List<Vector3Int> GetWalkableCellsInRange(Vector3Int startPos, int range, GameObject currentEntity = null,
        System.Func<Vector3Int, bool> boundaryCheck = null)
        => GetCellsInRange(startPos, range, 1, CellCheckOptions.ForMovement(currentEntity, boundaryCheck));


    public List<Vector3Int> GetAttackableCellsInRadius(Vector3Int center, int maxRange, int minRange = 1)
        => GetCellsInRange(center, maxRange, minRange, CellCheckOptions.ForAttack());

    /// <summary>
    /// Возвращает клетки, с которых можно выстрелить в указанную цель
    /// </summary>
    /// <param name="targetCell">Целевая клетка</param>
    /// <param name="minRange">Минимальная дистанция</param>
    /// <param name="maxRange">Максимальная дистанция</param>
    /// <param name="currentEntity">Сущность, для которой выполняется поиск позиции</param>
    /// <returns>Список клеток, с которых можно стрелять</returns>
    public List<Vector3Int> GetShootablePositionsTo(Vector3Int sourcePos, Vector3Int targetCell, int minRange,
        int maxRange, GameObject currentEntity = null)
    {
        var result = new List<Vector3Int>();

        var walkableCells = GetWalkableCellsInRange(sourcePos, maxRange, currentEntity);

        foreach (var cell in walkableCells)
        {
            var distance = Mathf.Abs(cell.x - targetCell.x) + Mathf.Abs(cell.y - targetCell.y);

            if (distance < minRange || distance > maxRange)
                continue;

            if (!HasLineOfSight(cell, targetCell))
                continue;

            if (!IsCellShootable(targetCell))
                continue;

            result.Add(cell);
        }

        return result;
    }

    #endregion

    #region Coordinate calculations

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        if (floorTilemap == null)
            return Vector3Int.zero;

        return floorTilemap.WorldToCell(worldPos);
    }

    public Vector3 GetCellCenterWorld(Vector3Int cellPos) => floorTilemap.GetCellCenterWorld(cellPos);

    #endregion
}