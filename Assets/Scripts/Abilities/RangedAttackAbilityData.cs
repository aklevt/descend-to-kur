using System.Collections;
using System.Collections.Generic;
using Entities;
using FX;
using UnityEngine;

namespace Abilities
{
    [CreateAssetMenu(fileName = "RangedAttackAbility", menuName = "Abilities/RangedAttack")]
    public class RangedAttackAbilityData : AbilityData
    {
        [Header("Range")] public int minRange = 2;
        public int maxRange = 4;

        [Header("Projectile")] [SerializeField]
        private GameObject projectilePrefab;

        [Header("Charge Effect")] [SerializeField]
        private GameObject chargeEffectPrefab;

        [SerializeField] private float chargeTime = 0.3f;

        public override List<Vector3Int> GetTargetCellsFrom(Vector3Int origin, BaseEntity actor)
        {
            var allCells = GridManager.Instance.GetAttackableCellsInRadius(origin, maxRange, minRange);
            var shootableCells = new List<Vector3Int>();

            foreach (var cell in allCells)
            {
                // Клетка простреливаема
                if (!GridManager.Instance.IsCellShootable(cell))
                    continue;

                // Есть линия видимости
                if (!GridManager.Instance.HasLineOfSight(origin, cell))
                    continue;

                shootableCells.Add(cell);
            }

            return shootableCells;
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
        /// Требует наличие цели на клетке, так как это не aoe-атака
        /// </summary>
        public override bool IsValidTarget(Vector3Int targetCell, BaseEntity actor)
        {
            var target = GridManager.Instance.GetEntityAt(targetCell);
            return target != null;
        }
        
        public override List<Vector3Int> GetTheoreticalCellsFrom(Vector3Int origin, BaseEntity actor)
        {
            var result = new List<Vector3Int>();

            for (var dx = -maxRange; dx <= maxRange; dx++)
            for (var dy = -maxRange; dy <= maxRange; dy++)
            {
                var manh = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (manh < minRange || manh > maxRange) continue;

                var cell = new Vector3Int(origin.x + dx, origin.y + dy, origin.z);
                if (!GridManager.Instance.HasFloor(cell)) continue;

                result.Add(cell);
            }

            return result;
        }

        public override IEnumerator Execute(BaseEntity actor, Vector3Int targetCell)
        {
            var targetObj = GridManager.Instance.GetEntityAt(targetCell);
            if (targetObj == null) yield break;

            var targetPos = targetObj.transform.position + new Vector3(0, 0.5f, 0);
            var damage = GetCalculatedDamage(actor);

            actor.FlipToTarget(targetPos);

            yield return actor.StartCoroutine(actor.PerformCast("RangedAttack", null));

            var spawnPos = actor.GetProjectileSpawnPosition();
            yield return PlayChargeEffect(spawnPos, actor);
    
            if (projectilePrefab != null)
            {
                yield return LaunchProjectile(spawnPos, targetPos, targetObj, damage, actor);
            }
            else
            {
                ApplyDamage(targetObj, damage);
            }
        }

        private IEnumerator PlayChargeEffect(Vector3 spawnPos, BaseEntity actor)
        {
            if (chargeEffectPrefab == null) yield break;

            var chargeEffect = Instantiate(chargeEffectPrefab, spawnPos, Quaternion.identity);
            yield return new WaitForSeconds(actor.GetScaledTime(chargeTime));
            Destroy(chargeEffect);
        }

        private IEnumerator LaunchProjectile(Vector3 spawnPos, Vector3 targetPos, GameObject targetObj, int damage,
            BaseEntity actor)
        {
            var projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var projectile = projectileObj.GetComponent<Projectile>();

            if (projectile != null)
            {
                var projectileHit = false;
                var speedMultiplier = actor.GetAnimationSpeedMultiplier();

                projectile.Launch(spawnPos, targetPos, speedMultiplier, () =>
                {
                    ApplyDamage(targetObj, damage);
                    projectileHit = true;
                });

                while (!projectileHit && projectileObj != null)
                {
                    yield return null;
                }
            }
            else
            {
                Debug.LogWarning("[RangedAttackAbility] Нет компонента Projectile");
                ApplyDamage(targetObj, damage);
            }
        }

        private void ApplyDamage(GameObject targetObj, int damage)
        {
            targetObj.GetComponent<Health>()?.TakeDamage(damage);
            CameraFollow.Instance?.ShakeLight();
        }
    }
}