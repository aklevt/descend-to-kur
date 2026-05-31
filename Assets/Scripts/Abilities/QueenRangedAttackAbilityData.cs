// --- FILE: Assets/Scripts/Abilities/QueenRangedAttackAbilityData.cs ---

using System.Collections;
using System.Collections.Generic;
using Entities;
using FX;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Дальняя атака ферзем
    /// </summary>
    [CreateAssetMenu(fileName = "QueenRangedAttackAbility",
        menuName = "Abilities/QueenRangedAttack")]
    public class QueenRangedAttackAbilityData : AbilityData
    {
        [Header("Range")] public int minRange = 2;
        public int maxRange = 3;
        public int maxDiagonalRange = 2;

        [Header("Projectile")] [SerializeField]
        private GameObject projectilePrefab;

        [Header("Charge Effect")] [SerializeField]
        private GameObject chargeEffectPrefab;

        [SerializeField] private float chargeTime = 0.3f;

        private static bool IsDiagonal(Vector3Int dir) => dir.x != 0 && dir.y != 0;

        private static readonly Vector3Int[] QueenDirections =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down,
            new Vector3Int(1, 1, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, -1, 0),
        };


        public override List<Vector3Int> GetTargetCellsFrom(Vector3Int origin, BaseEntity actor)
        {
            var result = new List<Vector3Int>();

            foreach (var dir in QueenDirections)
            {
                var rangeLimit = IsDiagonal(dir) ? maxDiagonalRange : maxRange;
                for (var step = 1; step <= rangeLimit; step++)
                {
                    var cell = origin + dir * step;

                    if (!GridManager.Instance.IsCellShootable(cell))
                        break; 

                    if (step >= minRange)
                        result.Add(cell);
                    
                    // Может стрелять сквозь сущности
                    // if (GridManager.Instance.GetEntityAt(cell) != null)
                    //     break;
                }
            }

            return result;
        }

        public override List<Vector3Int> GetEffectCells(Vector3Int hoveredCell, BaseEntity actor)
            => new List<Vector3Int> { hoveredCell };

        public override Vector3Int? ChooseTarget(BaseEntity actor)
        {
            var playerCell = PlayerMovement.Instance?.CurrentCell;
            if (playerCell == null) return null;

            var available = GetTargetCells(actor);
            return available.Contains(playerCell.Value) ? playerCell : null;
        }

        /// <summary>
        /// Атака не AoE — требует сущность на клетке
        /// </summary>
        public override bool IsValidTarget(Vector3Int targetCell, BaseEntity actor)
            => GridManager.Instance.GetEntityAt(targetCell) != null;

        public override IEnumerator Execute(BaseEntity actor, Vector3Int targetCell)
        {
            var targetObj = GridManager.Instance.GetEntityAt(targetCell);
            if (targetObj == null) yield break;

            var targetPos = targetObj.transform.position + new Vector3(0, 0.5f, 0);
            var damage = GetCalculatedDamage(actor);

            actor.FlipToTarget(targetPos);
            yield return new WaitForSeconds(actor.GetScaledTime(0.1f));

            var spawnPos = actor.GetProjectileSpawnPosition();
            yield return PlayChargeEffect(spawnPos, actor);

            if (projectilePrefab != null)
                yield return LaunchProjectile(spawnPos, targetPos, targetObj, damage, actor);
            else
            {
                targetObj.GetComponent<Health>()?.TakeDamage(damage);
                CameraFollow.Instance?.ShakeMedium();
                yield return new WaitForSeconds(actor.GetScaledTime(0.05f));
            }
        }


        private IEnumerator PlayChargeEffect(Vector3 spawnPos, BaseEntity actor)
        {
            if (chargeEffectPrefab == null) yield break;

            var fx = Instantiate(chargeEffectPrefab, spawnPos, Quaternion.identity);
            yield return new WaitForSeconds(actor.GetScaledTime(chargeTime));
            Destroy(fx);
        }

        private IEnumerator LaunchProjectile(Vector3 spawnPos, Vector3 targetPos,
            GameObject targetObj, int damage, BaseEntity actor)
        {
            var projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var projectile = projObj.GetComponent<Projectile>();

            if (projectile != null)
            {
                var hit = false;
                var speedMultiplier = actor.GetAnimationSpeedMultiplier();

                projectile.Launch(spawnPos, targetPos, speedMultiplier, () =>
                {
                    ApplyDamage(targetObj, damage);
                    hit = true;
                });

                while (!hit && projObj != null)
                    yield return null;
            }
            else
            {
                Debug.LogWarning("[QueenRangedAttack] Нет компонента Projectile");
                ApplyDamage(targetObj, damage);
            }
        }

        /// <summary>
        /// Полный геометрический радиус атаки без учёта препятствий и сущностей.
        /// Нужен для показа теоретической зоны досягаемости в превью врага
        /// </summary>
        public override List<Vector3Int> GetTheoreticalCellsFrom(Vector3Int origin, BaseEntity actor)
        {
            var result = new List<Vector3Int>();

            foreach (var dir in QueenDirections)
            {
                var rangeLimit = IsDiagonal(dir) ? maxDiagonalRange : maxRange;
                for (var step = 1; step <= rangeLimit; step++)
                {
                    var cell = origin + dir * step;

                    if (!GridManager.Instance.HasFloor(cell))
                        continue;

                    if (step >= minRange)
                        result.Add(cell);
                }
            }

            return result;
        }


        private void ApplyDamage(GameObject targetObj, int damage)
        {
            targetObj.GetComponent<Health>()?.TakeDamage(damage);
            CameraFollow.Instance?.ShakeLight();
        }
    }
}