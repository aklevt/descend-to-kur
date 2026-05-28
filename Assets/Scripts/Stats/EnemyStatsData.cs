using UnityEngine;

namespace Stats
{
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "Stats/Enemy Stats")]
    public class EnemyStatsData : EntityStatsBase
    {
        [Header("Enemy stats")]
        [Tooltip("Предпочитаемая дистанция атаки")]
        public int preferredAttackRange = 1;
        
        [Tooltip("Минимальная дистанция до игрока (0 = не отступать)")]
        public int minimumRange = 0;
        
        [Tooltip("Радиус обнаружения игрока (0 = бесконечный)")]
        public int detectionRadius = 0;
    }
}