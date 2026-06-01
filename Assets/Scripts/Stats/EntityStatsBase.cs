using UnityEngine;

namespace Stats
{
    /// <summary>
    /// Базовый класс для всех статов
    /// </summary>
    public abstract class EntityStatsBase : ScriptableObject
    {
        [Header("Base Info")]
        [Tooltip("Отображаемое имя сущности")]
        public string entityName = "Сущность";

        [Header("Base Stats")]
        [Tooltip("Максимальное здоровье")]
        public int maxHealth = 20;
        
        [Tooltip("Дальность передвижения за один ход")]
        public int moveRange = 1;
        
        [Tooltip("Базовый урон атаки сущности")]
        public int baseAttackDamage = 5;
    }
}