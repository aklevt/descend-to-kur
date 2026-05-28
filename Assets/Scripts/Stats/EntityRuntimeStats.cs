using UnityEngine;

namespace Stats
{
    [System.Serializable]
    public class EntityRuntimeStats
    {
        [Header("⚙️ Configuration")]
        [Tooltip("Если true, статы были изменены вручную в инспекторе")]
        public bool isCustomized;

        [Header("Common")]
        public int Health;
        public int MaxHealth;
        public int MoveRange;
        public int AttackDamage;
        public int Freeze;
        public int ShieldTurns;

        [Header("Player Only")]
        public int Energy;
        public int MaxEnergy;
        public int FrozenEnergy;
        public int RemainingSteps;
        public int MaxStepsPerRound;

        [Header("Enemies only")]
        [Tooltip("Предпочитаемая дистанция атаки")]
        public int PreferredAttackRange;
        [Tooltip("Минимальная дистанция до игрока (0 = не отступать)")]
        public int MinimumRange;
        [Tooltip("Радиус обнаружения игрока (0 = бесконечный)")]
        public int DetectionRadius;

        public EntityRuntimeStats()
        {
        }

        public void Initialize(EntityStatsBase baseStats)
        {
            if (baseStats == null)
            {
                Debug.LogWarning("[EntityRuntimeStats]");
                return;
            }

            // Базовые статы
            MaxHealth = baseStats.maxHealth;
            Health = MaxHealth;
            MoveRange = baseStats.moveRange;
            AttackDamage = baseStats.baseAttackDamage;
            Freeze = 0;

            // Статы игрока
            if (baseStats is PlayerStatsData playerData)
            {
                MaxEnergy = playerData.maxEnergy;
                Energy = MaxEnergy;
                MaxStepsPerRound = playerData.maxStepsPerRound;
                RemainingSteps = MaxStepsPerRound;
            }

            // Статы врагов
            if (baseStats is EnemyStatsData enemyData)
            {
                PreferredAttackRange = enemyData.preferredAttackRange;
                MinimumRange = enemyData.minimumRange;
                DetectionRadius = enemyData.detectionRadius;
            }
        }

        public bool IsDead => Health <= 0;
        
        /// <summary>
        /// Проверяет, находится ли цель в радиусе обнаружения (0 = бесконечный радиус)
        /// </summary>
        public bool IsInDetectionRange(Vector3Int from, Vector3Int target)
        {
            if (DetectionRadius <= 0) return true;
            var distance = Mathf.Abs(from.x - target.x) + Mathf.Abs(from.y - target.y);
            return distance <= DetectionRadius;
        }

        public bool HasEnergyForAction(int cost) => Energy >= cost;
        public void SpendEnergy(int amount) => Energy = Mathf.Max(0, Energy - amount);
        public void RestoreEnergy(int amount) 
        {
            var accessibleMax = MaxEnergy - FrozenEnergy;
            Energy = Mathf.Min(accessibleMax, Energy + amount);
        }
        
        public void ResetSteps() => RemainingSteps = Mathf.Min(MaxStepsPerRound, Energy);
        public void ResetStepsTo(int maxSteps) => RemainingSteps = maxSteps;
        public bool CanMove(int distance) => RemainingSteps >= distance;
        public void SpendSteps(int distance) => RemainingSteps = Mathf.Max(0, RemainingSteps - distance);

        public void ApplyDamage(int amount)
        {
            Health = Mathf.Max(0, Health - amount);
        }

        public void ApplyHeal(int amount)
        {
            Health = Mathf.Min(MaxHealth, Health + amount);
        }
        
        public bool HasActiveShield => ShieldTurns > 0;
        
        public void ProcessShieldEffect()
        {
            if (ShieldTurns > 0)
            {
                ShieldTurns--;
                if (ShieldTurns == 0)
                {
                    Debug.Log($"Щит всё");
                }
            }
        }
        
        public void ApplyShield(int turns)
        {
            ShieldTurns = Mathf.Max(ShieldTurns, turns);
            Debug.Log($"Щит активирован на {ShieldTurns} ходов");
        }
    }
}