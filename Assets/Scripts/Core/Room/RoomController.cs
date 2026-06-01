using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Abilities;
using Entities;
using UI.Dialogue;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Core.Room
{
    /// <summary>
    /// Управляет комнатой и предоставляет API для проверки границ/ограничений
    /// Включает логику секций
    /// </summary>
    public class RoomController : MonoBehaviour, IRoomBoundaryProvider
    {
        #region Properties

        [Header("Room Setup")] // ❗ Не используется
        [Header("Grid Layers")]
        public Tilemap floorTilemap; // Определяет где можно ходить

        public Tilemap wallsTilemap;
        public Tilemap wallsInnerTilemap; // Внутренние стены (блокируют всё)
        public Tilemap obstaclesTilemap; // Блокируют ходьбу, но не стрельбу
        public Tilemap objectsTilemap; // Ловушки + хилки
        public Tilemap decorTilemap;

        [Header("Highlights")] public Tilemap highlightTilemap;
        public Tilemap previewTilemap;
        public Tilemap effectHighlightTilemap;

        [Header("Spawn")] public Transform playerSpawnPoint;

        [Header("Objective")] [SerializeField]
        private RoomObjectiveType objectiveType = RoomObjectiveType.KillAllEnemies;

        [SerializeField] private string victoryMessage = "Комната пройдена";

        public RoomObjectiveType ObjectiveType => objectiveType;
        public string VictoryMessage => victoryMessage;

        public static RoomController Current { get; private set; }

        public event Action OnRoomCleared;
        public event Action<Bounds> OnCameraBoundsChanged;

        private List<EnemyBase> enemiesInRoom = new();
        private List<RoomDialogueTrigger> dialogueTriggers = new();
        private SectionManager sectionManager;
        private bool isCleared;
        private bool wasSectionCleared;
        private readonly RoomBoundarySystem boundarySystem = new RoomBoundarySystem();

        #endregion

        #region Initialization

        private void Awake()
        {
            Current = this;

            if (floorTilemap == null)
                floorTilemap = transform.Find("Grid/Floor_Tilemap")?.GetComponent<Tilemap>();

            if (wallsTilemap == null)
                wallsTilemap = transform.Find("Grid/Walls_Tilemap")?.GetComponent<Tilemap>();

            if (wallsInnerTilemap == null)
                wallsInnerTilemap = transform.Find("Grid/Walls_Inner_Tilemap")?.GetComponent<Tilemap>();

            if (obstaclesTilemap == null)
                obstaclesTilemap = transform.Find("Grid/Obstacles_Tilemap")?.GetComponent<Tilemap>();

            if (objectsTilemap == null)
                objectsTilemap = transform.Find("Grid/Objects_Tilemap")?.GetComponent<Tilemap>();

            if (decorTilemap == null)
                decorTilemap = transform.Find("Grid/Decor_Tilemap")?.GetComponent<Tilemap>();

            if (highlightTilemap == null)
                highlightTilemap = transform.Find("Grid/Highlight_Tilemap")?.GetComponent<Tilemap>();
            
            if (previewTilemap == null)
                previewTilemap = transform.Find("Grid/Preview_Tilemap")?.GetComponent<Tilemap>();

            if (effectHighlightTilemap == null)
                effectHighlightTilemap = transform.Find("Grid/EffectHighlightTilemap")?.GetComponent<Tilemap>();

            sectionManager = GetComponentInChildren<SectionManager>();
            boundarySystem.Initialize(sectionManager);

            enemiesInRoom = new List<EnemyBase>(GetComponentsInChildren<EnemyBase>());
            dialogueTriggers = new List<RoomDialogueTrigger>(GetComponentsInChildren<RoomDialogueTrigger>());
        }


        private void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        public void Initialize()
        {
            isCleared = false;
            wasSectionCleared = false;

            GridManager.Instance?.UpdateRoomData(
                floorTilemap,
                wallsTilemap,
                wallsInnerTilemap,
                obstaclesTilemap,
                objectsTilemap
            );

            // Настройка подсветки
            GridHighlighter.Instance?.UpdateTilemaps(highlightTilemap, previewTilemap, effectHighlightTilemap);

            // Инициализация врагов
            foreach (var enemy in enemiesInRoom)
            {
                if (enemy != null)
                {
                    var health = enemy.GetComponent<Health>();
                    if (health != null)
                    {
                        health.OnDeath += HandleEnemyDeath;
                    }
                }
            }

            // Инициализация диалогов
            foreach (var trigger in dialogueTriggers)
            {
                if (trigger != null)
                {
                    trigger.Initialize();
                }
            }

            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.ResetZoom();
            }

            if (CameraFollow.Instance != null && wallsTilemap != null)
            {
                var roomBounds = wallsTilemap.GetComponent<Renderer>().bounds;
                CameraFollow.Instance.SetCameraBounds(roomBounds);
                Debug.Log($"<color=cyan>[RoomController]</color> Базовые границы комнаты установлены: {roomBounds}");
            }

            // Инициализация секций (если есть)
            if (sectionManager != null)
            {
                sectionManager.OnBoundsChanged += HandleSectionBoundsChanged;
                sectionManager.Initialize();
            }

            if (FogSystemManager.Instance != null)
            {
                FogSystemManager.Instance.OnRoomChanged();
            }

            Debug.Log(
                $"<color=cyan>[RoomController]</color> Комната инициализирована. Врагов: {enemiesInRoom.Count}, Секций: {(sectionManager != null ? "есть" : "нет")}");
        }

        /// <summary>
        /// Передает ссылку на созданного игрока внутренним системам комнаты
        /// </summary>
        public void LinkPlayerToRoom(PlayerMovement player)
        {
            if (sectionManager != null)
            {
                sectionManager.LinkPlayer(player);
            }
        }

        public void Cleanup()
        {
            foreach (var enemy in enemiesInRoom)
            {
                if (enemy != null)
                {
                    var health = enemy.GetComponent<Health>();
                    if (health != null)
                    {
                        health.OnDeath -= HandleEnemyDeath;
                    }
                }
            }

            foreach (var trigger in dialogueTriggers)
            {
                if (trigger != null)
                {
                    trigger.Cleanup();
                }
            }

            if (sectionManager != null)
            {
                sectionManager.OnBoundsChanged -= HandleSectionBoundsChanged;
                sectionManager.ForceReset();
            }

            if (GridHighlighter.Instance != null)
            {
                GridHighlighter.Instance.Clear();
            }

            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.ClearBounds();
                CameraFollow.Instance.ClearDynamicBoundaries();
            }
        }

        #endregion

        #region Room Boundaries

        /// <summary>
        /// Проверяет базовую возможность перемещения в клетку.
        /// </summary>
        public bool CanMoveTo(Vector3Int cell)
        {
            return boundarySystem.ValidateAction(cell, null).Success;
        }

        /// <summary>
        /// Проверяет, находится ли клетка внутри допустимой игровой зоны.
        /// </summary>
        public bool IsCellInsideActiveArea(Vector3Int cell)
        {
            return boundarySystem.ValidateAction(cell, null).Success;
        }

        /// <summary>
        /// Выполняет полную проверку действия (движение или способность) с возвратом текста ошибки
        /// </summary>
        public ValidationResult ValidateActionInRoom(Vector3Int cell, AbilityData ability)
        {
            return boundarySystem.ValidateAction(cell, ability);
        }

        public bool CanTargetPosition(Vector3 worldPos)
        {
            var cell = GridManager.Instance.WorldToCell(worldPos);
            return boundarySystem.ValidateAction(cell, null).Success;
        }

        /// <summary>
        /// Проверяет, находится ли враг в активной зоне игрока.
        /// </summary>
        public bool IsEnemyActive(EnemyBase enemy) => boundarySystem.IsEnemyActive(enemy);

        /// <summary>
        /// Возвращает актуальные границы камеры для текущей секции.
        /// </summary>
        public Bounds GetCameraBounds() => boundarySystem.GetCurrentBounds();

        private void HandleSectionBoundsChanged(Bounds newBounds)
        {
            OnCameraBoundsChanged?.Invoke(newBounds);
        }

        /// <summary>
        /// Проверяет, зачищена ли текущая секция (для бесплатного перемещения)
        /// Возвращает false для комнат БЕЗ секций (в них всегда нужно тратить ресурсы)
        /// </summary>
        public bool IsCurrentSectionCleared()
        {
            if (sectionManager == null)
                return false;

            return sectionManager.IsCurrentSectionCleared();
        }

        #endregion

        #region Entities Management

        private void HandleEnemyDeath(GameObject enemyObj)
        {
            sectionManager?.OnEnemyDeath();

            if (sectionManager != null)
            {
                var isNowCleared = sectionManager.IsCurrentSectionCleared();

                if (isNowCleared && !wasSectionCleared)
                {
                    wasSectionCleared = true;
                    OnSectionJustCleared();
                }
            }

            CheckObjective();
        }

        private void CheckObjective()
        {
            if (isCleared) return;

            switch (objectiveType)
            {
                case RoomObjectiveType.KillAllEnemies:
                    if (AreAllEnemiesDead())
                        CompleteRoom();
                    break;

                case RoomObjectiveType.BossFight:
                    if (AreAllEnemiesDead())
                        CompleteRoom();
                    break;
            }
        }

        private void OnSectionJustCleared()
        {
            var player = PlayerMovement.Instance;
            if (player == null) return;

            AbilityController.Instance
                .SelectAbilityByIndex(0); // ❗ Сейчас сбрасывается энергия в конце секции, возможно, так не должно быть (если введем частичное восстановление)
            
            player.Stats.ResetStepsTo(player.Stats.MaxStepsPerRound);
            player.Stats.RestoreEnergy(player.Stats.MaxEnergy);

            AbilityController.Instance?.RefreshAbilityOverlay();
        }

        private bool AreAllEnemiesDead()
        {
            if (sectionManager != null)
            {
                var info = sectionManager.GetSectionInfo();
                // Проверка, зачищена ли последняя секция
                return info.current == info.total - 1 && sectionManager.IsCurrentSectionCleared();
            }

            // Если комната без секций
            if (enemiesInRoom.Count == 0) return true;
            return enemiesInRoom.TrueForAll(e => e == null || e.Stats == null || e.Stats.IsDead);
        }

        private void CompleteRoom()
        {
            if (isCleared) return;
            isCleared = true;
            Debug.Log($"<color=green>[RoomController]</color> Цели комнаты выполнены");
    
            StartCoroutine(CompleteRoomSequence());
        }
        
        /// <summary>
        /// Последовательность завершения комнаты: диалоги, событие победы
        /// </summary>
        private IEnumerator CompleteRoomSequence()
        {
            yield return TriggerDialoguesOfTypeSequential(DialogueTriggerType.OnRoomCleared);
    
            Debug.Log($"<color=green>[RoomController]</color> Диалоги завершены, переход к экрану победы");
    
            OnRoomCleared?.Invoke();
        }


        #endregion

        #region Dialogs

        public IEnumerator TriggerDialoguesOfTypeSequential(DialogueTriggerType type)
        {
            var dialoguesOfType = dialogueTriggers.FindAll(t => t != null && t.TriggerType == type);

            Debug.Log($"<color=yellow>[RoomController]</color> Найдено диалогов типа {type}: {dialoguesOfType.Count}");

            foreach (var trigger in dialoguesOfType)
            {
                Debug.Log($"<color=yellow>[RoomController]</color> Запуск диалога: {trigger.DialogueData?.name}");
                trigger.TriggerDialogue();

                yield return new WaitUntil(() =>
                    DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive);
            }

            Debug.Log($"<color=yellow>[RoomController]</color> Все диалоги типа {type} завершены");
        }

        #endregion

        public Vector3Int GetPlayerSpawnCell()
        {
            if (playerSpawnPoint == null)
            {
                Debug.LogWarning("[RoomController] Точка спауна игрока не задана");
                return Vector3Int.zero;
            }

            return GridManager.Instance.WorldToCell(playerSpawnPoint.position);
        }

        /// <summary>
        /// Возвращает делегат проверки границы движения для текущей секции
        /// </summary>
        public Func<Vector3Int, bool> GetMovementBoundaryCheck()
        {
            return sectionManager != null
                ? sectionManager.IsWithinMovementBoundary
                : null;
        }
        
        public List<EnemyBase> GetActiveEnemiesForTurn()
        {
            if (sectionManager != null && sectionManager.CurrentSection != null)
            {
                return sectionManager.CurrentSection.Enemies
                    .Where(e => e != null && !e.IsPhysicallyDead())
                    .ToList();
            }

            return enemiesInRoom
                .Where(e => e != null && e.Stats != null && !e.Stats.IsDead)
                .ToList();
        }
        
        /// <summary>
        /// Принудительное завершение комнаты (для пустых секций)
        /// </summary>
        public void ForceComplete()
        {
            if (isCleared) return;
    
            Debug.Log("<color=green>[RoomController]</color> Принудительное завершение комнаты (пустая секция)");
            CompleteRoom();
        }
    }

    public enum RoomObjectiveType
    {
        KillAllEnemies,
        BossFight
    }
}