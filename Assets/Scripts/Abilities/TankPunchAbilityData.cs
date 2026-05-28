using System.Collections;
using System.Collections.Generic;
using Entities;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Бьёт на 1 клетку, наносит большой урон и отбрасывает цель.
    /// </summary>
    [CreateAssetMenu(fileName = "TankPunchAbility", menuName = "Abilities/TankPunch")]
    public class TankPunchAbilityData : AbilityData
    {
        [Header("Knockback")] [Tooltip("Максимальная дистанция отброса")] [SerializeField]
        private int knockbackDistance = 2;

        [Header("Visual")] [Tooltip("Задержка перед")] [SerializeField]
        private float impactPause = 0.1f;

        public override List<Vector3Int> GetTargetCellsFrom(Vector3Int origin, BaseEntity actor)
        {
            return GridManager.Instance.GetAttackableCellsInRadius(origin, 1);
        }

        public override List<Vector3Int> GetEffectCells(Vector3Int hoveredCell, BaseEntity actor)
        {
            return new List<Vector3Int> { hoveredCell };
        }

        public override Vector3Int? ChooseTarget(BaseEntity actor)
        {
            var playerCell = PlayerMovement.Instance?.CurrentCell;
            if (playerCell == null) return null;

            var available = GetTargetCells(actor);
            return available.Contains(playerCell.Value) ? playerCell : null;
        }

        public override bool IsValidTarget(Vector3Int targetCell, BaseEntity caster)
        {
            var target = GridManager.Instance.GetEntityAt(targetCell);
            return target != null && target != caster.gameObject;
        }

        public override IEnumerator Execute(BaseEntity actor, Vector3Int targetCell)
        {
            var targetObj = GridManager.Instance.GetEntityAt(targetCell);
            if (targetObj == null) yield break;

            var targetEntity = targetObj.GetComponent<BaseEntity>();
            var targetHealth = targetObj.GetComponent<Health>();

            if (targetEntity == null) yield break;

            var damage = GetCalculatedDamage(actor);
            var knockbackDir = targetCell - actor.CurrentCell;

            // Анимация удара
            yield return actor.StartCoroutine(actor.PerformAttack(
                targetObj.transform.position,
                "TankPunch",
                () =>
                {
                    targetHealth?.TakeDamage(damage);
                    CameraFollow.Instance?.ShakeHeavy();
                },
                forceProceduralPunch
            ));


            yield return new WaitForSeconds(actor.GetScaledTime(impactPause));

            ApplyKnockback(targetEntity, knockbackDir);

            while (targetEntity != null && targetEntity.IsMoving)
            {
                yield return null;
            }
        }

        private void ApplyKnockback(BaseEntity target, Vector3Int direction)
        {
            if (target == null) return;

            var finalCell = target.CurrentCell;

            for (var i = 1; i <= knockbackDistance; i++)
            {
                var checkCell = target.CurrentCell + direction * i;

                if (!GridManager.Instance.IsCellKnockbackable(checkCell))
                {
                    break;
                }

                if (GridManager.Instance.HasBlockingTileObject(checkCell))
                {
                    finalCell = checkCell;
                    break;
                }

                finalCell = checkCell;
            }

            if (finalCell != target.CurrentCell)
            {
                Debug.Log($"[TankPunch] Отброс {target.gameObject.name}: {target.CurrentCell} -> {finalCell}");
                target.MoveDirectly(finalCell);
            }
        }
    }
}