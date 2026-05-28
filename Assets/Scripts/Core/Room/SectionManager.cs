using System;
using System.Collections.Generic;
using Entities;
using UnityEngine;

namespace Core.Room
{
    /// <summary>
    /// Управляет секциями внутри комнаты
    /// </summary>
    public class SectionManager : MonoBehaviour
    {
        private readonly List<RoomSection> sections = new();
        private int currentSectionIndex = 0;

        public RoomSection CurrentSection => sections.Count > 0 && currentSectionIndex < sections.Count
            ? sections[currentSectionIndex]
            : null;

        public event Action<Bounds> OnBoundsChanged;
        public event Action<int> OnSectionEntered;

        #region Lifecycle

        private void Awake() => CollectSections();
        
        private void OnDestroy()
        {
            UnsubscribeFromPlayer();
        }

        /// <summary>
        /// Автоматический сбор всех дочерних объектов с компонентом RoomSection.
        /// </summary>
        private void CollectSections()
        {
            sections.Clear();

            foreach (Transform child in transform)
            {
                if (child.TryGetComponent<RoomSection>(out var section))
                {
                    section.SectionIndex = sections.Count;
                    sections.Add(section);
                }
            }

            Debug.Log($"<color=cyan>[SectionManager]</color> Найдено секций: {sections.Count}");
        }

        /// <summary>
        /// Полный сброс и первичная настройка всех секций при старте комнаты.
        /// </summary>
        public void Initialize()
        {
            currentSectionIndex = 0;

            foreach (var section in sections)
                section.Initialize();
            
            if (sections.Count > 0)
                ActivateSection(0);
        }

        /// <summary>
        /// Принудительный сброс системы (для перезапуска).
        /// </summary>
        public void ForceReset()
        {
            StopAllCoroutines();
            currentSectionIndex = 0;
            foreach (var section in sections)
            {
                section.Initialize();
                CleanupSectionEnemies(section);
            }
        }

        #endregion
        
        #region Player Event Handling
        
        /// <summary>
        /// Прямая подписка на событие изменения клетки игрока. Вызывается из RoomController.
        /// </summary>
        public void LinkPlayer(PlayerMovement player)
        {
            UnsubscribeFromPlayer();

            if (player != null)
            {
                player.OnCellChanged += HandlePlayerCellChanged;
                Debug.Log("<color=green>[SectionManager]</color> Успешная подписка на игрока из секции");
            }
            else
            {
                Debug.LogError("<color=red>[SectionManager]</color> Null вместо игрока");
            }
        }

        /// <summary>
        /// Подписаться на событие изменения клетки игрока
        /// </summary>
        private void SubscribeToPlayer()
        {
            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.OnCellChanged += HandlePlayerCellChanged;
            }
            else
            {
                StartCoroutine(WaitForPlayerAndSubscribe());
            }
        }

        /// <summary>
        /// Отписаться от событий игрока
        /// </summary>
        private void UnsubscribeFromPlayer()
        {
            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.OnCellChanged -= HandlePlayerCellChanged;
            }
        }

        /// <summary>
        /// Корутина для случая, если игрок еще не создан
        /// </summary>
        private System.Collections.IEnumerator WaitForPlayerAndSubscribe()
        {
            var timeout = 5f;
            var elapsed = 0f;

            while (PlayerMovement.Instance == null && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.OnCellChanged += HandlePlayerCellChanged;
                Debug.LogError("<color=green>[SectionManager]</color> Произошла подписка на игрока в корутине ❗");
            }
            else
            {
                Debug.LogError("<color=red>[SectionManager]</color> Не удалось найти игрока для подписки");
            }
        }

        /// <summary>
        /// Обработчик события изменения клетки игрока
        /// </summary>
        private void HandlePlayerCellChanged(Vector3Int newCell)
        {
            if (PlayerMovement.Instance == null) return;
            
            var worldPos = GridManager.Instance.GetCellCenterWorld(newCell);
            CheckPosition(worldPos);
        }

        #endregion

        #region Section Control

        /// <summary>
        /// Проверяет позицию игрока и инициирует переход в следующую секцию, если граница пересечена.
        /// </summary>
        public void CheckPosition(Vector3 playerWorldPos)
        {
            if (CurrentSection == null || CurrentSection.rightBoundary == null) return;

            if (playerWorldPos.x >= CurrentSection.RightX && CurrentSection.IsCleared)
            {
                TryMoveToNextSection();
            }
        }

        private void TryMoveToNextSection()
        {
            if (currentSectionIndex >= sections.Count - 1) return;

            var previousSection = CurrentSection;
            currentSectionIndex++;

            // Если игрок каким-то образом покидает секцию - враги на ней погибают
            if (previousSection != null && !previousSection.IsCleared)
                CleanupSectionEnemies(previousSection);

            ActivateSection(currentSectionIndex);
        }

        /// <summary>
        /// Включает указанную секцию и уведомляет системы об изменении границ.
        /// </summary>
        private void ActivateSection(int index)
        {
            sections[index].SetActive(true);
            
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.BeginLevel();
                Debug.Log("<color=lime>[SectionManager]</color> Ход игрока перезапущен через BeginLevel");
            }
            
            NotifyBoundsChanged();
            OnSectionEntered?.Invoke(index);

            Debug.Log($"<color=green>[SectionManager]</color> Секция {index} активирована");
        }

        /// <summary>
        /// Полное удаление врагов из указанной секции.
        /// </summary>
        private void CleanupSectionEnemies(RoomSection section)
        {
            for (var i = section.Enemies.Count - 1; i >= 0; i--)
            {
                var enemy = section.Enemies[i];
                if (enemy == null) continue;

                TurnManager.Instance?.UnregisterEnemy(enemy);
                Destroy(enemy.gameObject);
            }

            section.Enemies.Clear();
        }

        #endregion

        #region Bounds & API

        /// <summary>
        /// Обновляет параметры камеры и вызывает внешнее событие с новыми границами.
        /// </summary>
        private void NotifyBoundsChanged()
        {
            var bounds = GetCurrentCameraBounds();
            OnBoundsChanged?.Invoke(bounds);

            if (CameraFollow.Instance != null && CurrentSection != null)
            {
                var roomBounds = CameraFollow.Instance.GetRoomBounds();
                if (roomBounds.size.magnitude < 0.1f)
                {
                    Debug.LogWarning("<color=yellow>[SectionManager]</color> Базовые границы комнаты еще не установлены!");
                    return;
                }
                
                CameraFollow.Instance.SetDynamicBoundaries(
                    CurrentSection.LeftX,
                    CurrentSection.RightX,
                    CurrentSection.LeftCameraOffset, 
                    CurrentSection.RightCameraOffset
                );
                CameraFollow.Instance.SetCameraBounds(bounds);
            }
        }

        /// <summary>
        /// Проверяет, находится ли позиция за левой границей текущей секции (движение назад).
        /// </summary>
        public bool IsPositionBehindBoundary(Vector3 worldPos) =>
            CurrentSection != null && worldPos.x < CurrentSection.LeftX;

        /// <summary>
        /// Проверяет, находится ли позиция за правой границей текущей секции (движение вперед).
        /// </summary>
        public bool IsPositionBeyondForwardBoundary(Vector3 worldPos) =>
            CurrentSection != null && worldPos.x >= CurrentSection.RightX;

        /// <summary>
        /// Проверяет принадлежность врага к текущей активной секции.
        /// </summary>
        public bool IsEnemyInActiveSection(EnemyBase enemy) =>
            CurrentSection != null && CurrentSection.ContainsEnemy(enemy);

        /// <summary>
        /// Вызывается при смерти врага для проверки условий зачистки текущей секции.
        /// </summary>
        public void OnEnemyDeath() => CurrentSection?.CheckCleared();

        /// <summary>
        /// Возвращает актуальные границы камеры для текущей секции.
        /// </summary>
        public Bounds GetCurrentCameraBounds() =>
            CurrentSection != null ? CurrentSection.GetCameraBounds() : new Bounds(Vector3.zero, Vector3.one * 20f);

        /// <summary>
        /// Статус прохождения текущей зоны.
        /// </summary>
        public bool IsCurrentSectionCleared() => CurrentSection == null || CurrentSection.IsCleared;

        #endregion
        
        #region Editor

        /// <summary>
        /// Принудительный переход (для отладки в инспекторе).
        /// </summary>
        public void ForceNextSection() => TryMoveToNextSection();

        /// <summary>
        /// Возвращает индекс текущей секции и общее количество.
        /// </summary>
        public (int current, int total) GetSectionInfo() => (currentSectionIndex, sections.Count);

        #endregion
        
        /// <summary>
        /// Проверяет, можно ли двигаться в указанную клетку с учетом границ секции
        /// </summary>
        public bool IsWithinMovementBoundary(Vector3Int cell)
        {
            if (CurrentSection == null)
                return true;

            var worldPos = GridManager.Instance.GetCellCenterWorld(cell);
            
            if (worldPos.x < CurrentSection.LeftX - 0.01f)
                return false;
    
            // Если секция зачищена, разрешено зайти на одну клетку дальше границы, чтобы перейти на след. секцию
            var allowedMaxX = CurrentSection.IsCleared 
                ? CurrentSection.RightX + 1f 
                : CurrentSection.RightX;

            return worldPos.x <= allowedMaxX;
        }
    }
}