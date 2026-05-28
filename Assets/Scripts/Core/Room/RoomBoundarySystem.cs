using System;
using Abilities;
using UnityEngine;

namespace Core.Room
{
    /// <summary>
    /// Отвечает исключительно за логику границ и валидацию перемещений/действий.
    /// </summary>
    [Serializable]
    public class RoomBoundarySystem
    {
        private SectionManager sectionManager;

        public void Initialize(SectionManager manager)
        {
            sectionManager = manager;
        }

        /// <summary>
        /// Универсальная проверка любого действия игрока (движение или способность)
        /// </summary>
        public ValidationResult ValidateAction(Vector3Int cell, AbilityData ability)
        {
            if (sectionManager == null || GridManager.Instance == null)
                return ValidationResult.Ok();

            var worldPos = GridManager.Instance.GetCellCenterWorld(cell);
            var isCleared = sectionManager.IsCurrentSectionCleared();

            // Назад ходить/атаковать нельзя никогда
            if (sectionManager.IsPositionBehindBoundary(worldPos))
            {
                return ValidationResult.Fail("Вы не можете ходить назад!");
            }

            // При движении вперед проверяем правую границу, если секция не зачищена
            if (!isCleared && sectionManager.IsPositionBeyondForwardBoundary(worldPos))
            {
                if (ability is MoveAbilityData || ability == null) // Обработка вызова из IsCellPassable
                    return ValidationResult.Fail("Путь закрыт! Сначала зачистите текущую область.");

                return ValidationResult.Fail("Цель недоступна! Сначала зачистите текущую область.");
            }

            return ValidationResult.Ok();
        }

        public bool IsEnemyActive(Entities.EnemyBase enemy) =>
            sectionManager == null || sectionManager.IsEnemyInActiveSection(enemy);

        public Bounds GetCurrentBounds() =>
            sectionManager != null
                ? sectionManager.GetCurrentCameraBounds()
                : new Bounds(Vector3.zero, Vector3.one * 20f);
    }

    /// <summary>
    /// Простой контейнер для результата валидации
    /// Смысл - вывод сообщений красивых как hint
    /// Основная логика валидации (ограничений клеток) вшита в GridManager
    /// </summary>
    public struct ValidationResult
    {
        public bool Success;
        public string ErrorMessage;

        public static ValidationResult Ok() => new() { Success = true };
        public static ValidationResult Fail(string msg) => new() { Success = false, ErrorMessage = msg };
    }
}