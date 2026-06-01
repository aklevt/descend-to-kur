using System.Collections;
using System.Collections.Generic;
using Entities;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Базовый класс для всех способностей в игре. 
    /// Отвечает за логику поиска целей, визуализацию и исполнение способности
    /// </summary>
    public abstract class AbilityData : ScriptableObject
    {
        public string abilityName;

        [Header("Info Panel")]
        [Tooltip("Минимальная дальность для отображения в UI. 0 = нет минимального радиуса")]
        public int displayMinRange = 0;
        
        [Tooltip("Дальность способности для отображения в UI. -1 = авто")]
        public int displayRange = -1;
        
        [Tooltip("Описание способности (коротко)")]
        [TextArea(2, 2)]
        public string description = "";

        [Header("Cost")] public int energyCost = 0;

        [Header("Damage Settings")] 
        [Tooltip("Сколько урона добавляется к базовой атаке (может быть отрицательным)")]
        public int bonusDamage = 0;
        public bool overrideBaseDamage = false;

        [Header("Colors")] public Color highlightColor = Color.white; //  Color.black;
        public Color effectColor = new Color(1f, 0.2f, 0.2f, 0.9f); //new Color(0f, 0f, 0f, 0.9f);
        
        [Header("Animation Settings")]
        [Tooltip("Использовать процедурную анимацию удара вместе с Animator'ом. Если false, будет проигрываться только анимация из Animator'а, и она должна быть универсальной для всех направлений атаки")]
        public bool forceProceduralPunch = false;

        [Tooltip("Использовать диагональную анимацию для вертикальных атак")]
        public bool useDiagonalPunch = false;
        
        [Header("Turn Limits")]
        [Tooltip("Можно ли использовать эту способность только один раз за ход?")]
        public bool limitOncePerTurn = false;

        /// <summary>
        /// Проверяет кастомные ограничения (лимит на использование за ход).
        /// </summary>
        public virtual bool IsTurnLimitExceeded()
        {
            return false;
        }
        
        /// <summary>
        /// Рассчитывает итоговый урон на основе статов атакующего
        /// </summary>
        public virtual int GetCalculatedDamage(BaseEntity actor)
        {
            if (overrideBaseDamage) return bonusDamage;
            return Mathf.Max(0, actor.Stats.AttackDamage + bonusDamage);
        }
        
        /// <summary>
        /// Возвращает отображаемый урон для UI
        /// </summary>
        public int GetDisplayDamage(BaseEntity actor)
        {
            if (overrideBaseDamage && bonusDamage == 0)
                return 0;
            return GetCalculatedDamage(actor);
        }

        /// <summary>
        /// Возвращает отображаемую дальность для UI.
        /// Если displayRange == -1, пытается посчитать автоматически.
        /// </summary>
        public int GetDisplayRange(BaseEntity actor)
        {
            if (displayRange >= 0) return displayRange;
            return ComputeAutoRange(actor);
        }
        
        protected virtual int ComputeAutoRange(BaseEntity actor)
        {
            var cells = GetTheoreticalCellsFrom(actor.CurrentCell, actor);
            var maxRange = 0;
            foreach (var cell in cells)
            {
                var dist = Mathf.Max(
                    Mathf.Abs(cell.x - actor.CurrentCell.x),
                    Mathf.Abs(cell.y - actor.CurrentCell.y)
                );
                if (dist > maxRange) maxRange = dist;
            }
            return maxRange;
        }

        /// <summary>
        /// Вычисляет доступные для выбора клетки по текущему положению исполнителя
        /// </summary>
        /// <param name="actor">Сущность, использующая способность</param>
        /// <returns>Список координат клеток, на которые можно нажать для активации способности</returns>
        public List<Vector3Int> GetTargetCells(BaseEntity actor)
            => GetTargetCellsFrom(actor.CurrentCell, actor);

        /// <summary>
        /// Вычисляет доступные клетки от заданной точки (нужен для превью)
        /// </summary>
        /// /// <param name="position">Точка отсчета</param>
        /// <param name="actor">Сущность, применяющая способность (учитывает её stats)</param>
        /// <returns>Список координат доступных клеток</returns>
        public abstract List<Vector3Int> GetTargetCellsFrom(Vector3Int position, BaseEntity actor);

        /// <summary>
        /// Определяет область непосредственного воздействия способности
        /// </summary>
        /// <param name="hoveredCell">Клетка, над которой находится курсор</param>
        /// <param name="actor">Сущность, применяющая способность</param>
        /// <returns>Список клеток, которые будут подсвечены как "зона поражения"</returns>
        public virtual List<Vector3Int> GetEffectCells(Vector3Int hoveredCell, BaseEntity actor)
            => new() { hoveredCell };

        /// <summary>
        /// Исполнение логики способности (анимации, нанесение урона, перемещение)
        /// </summary>
        /// <param name="actor">Сущность, применяющая способность</param>
        /// <param name="targetCell">Выбранная целевая клетка</param>
        /// <returns></returns>
        public abstract IEnumerator Execute(BaseEntity actor, Vector3Int targetCell);

        /// <summary>
        /// Проверка условий активации способности (наличие энергии, кулдаун, состояние actor).
        /// </summary>
        /// <param name="user">Сущность, для которой надо вызвать проверку</param>
        /// <returns>True, если можно использовать способность</returns>
        public virtual bool CanUse(BaseEntity user)
        {
            // Пусть враги не тратят энергию
            if (user is EnemyBase)
                return true;

            return user.Stats.HasEnergyForAction(energyCost);
        }

        /// <summary>
        /// Выбор цели через AI
        /// Если у врага будет несколько целей или игрок может быть на недостижимом расстоянии, способность может оценить, на какую клетку эффективно ее применять
        /// На случай, если логика усложнится. Условно враг может опросить свои способности (опять же, если у него их несколько) и принять финальное решение
        /// Пока что способность у каждого одна, а цель единственная - игрок, и до него всегда можно добраться
        /// Возможно, стоит перенести эту логику в базовый класс либо в контроллер противника
        /// </summary>
        /// <param name="actor">Сущность, которая пытается использовать эту способность</param>
        /// <returns>Координата цели или null</returns>
        public virtual Vector3Int? ChooseTarget(BaseEntity actor)
        {
            var cells = GetTargetCells(actor);
            return cells.Count > 0 ? cells[0] : null;
        }

        /// <summary>
        /// Проверяет, можно ли выполнить способность на этой клетке (переопределяется для обычной атаки)
        /// Если вернет false, клик на клетку будет проигнорирован
        /// </summary>
        /// <param name="targetCell">Координаты клетки, которую мы проверяем.</param>
        /// <param name="actor">Тот, кто пытается использовать способность.</param>
        /// <returns>True, если клетка подходит (например, там враг). False, если клик на клетку должен быть проигнорирован</returns>
        public virtual bool IsValidTarget(Vector3Int targetCell, BaseEntity caster)
        {
            return true;
        }
        
        /// <summary>
        /// Полный геометрический радиус способности без учёта препятствий/сущностей.
        /// Используется для слабой подсветки общего радиуса в UI.
        /// По-умолчанию совпадает с реально доступными клетками.
        /// Переопределяется в способностях, где есть смысл показать общий радиус.
        /// </summary>
        public virtual List<Vector3Int> GetTheoreticalCellsFrom(Vector3Int position, BaseEntity actor)
            => GetTargetCellsFrom(position, actor);
    }
}