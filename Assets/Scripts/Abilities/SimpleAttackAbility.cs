using System.Collections;
using System.Collections.Generic;
using Entities;
using UnityEngine;

namespace Abilities
{
    [CreateAssetMenu(fileName = "SimpleAttackAbility", menuName = "Abilities/SimpleAttack")]
    public class SimpleAttackAbilityData : AbilityData
    {
        public override List<Vector3Int> GetTargetCellsFrom(Vector3Int origin, BaseEntity actor)
        {
            return GridManager.Instance.GetAttackableCellsInRadius(origin, 1);
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
            return target != null && target != actor.gameObject;
        }

        public override IEnumerator Execute(BaseEntity actor, Vector3Int targetCell)
        {
            var target = GridManager.Instance.GetEntityAt(targetCell);
            var targetHealth = target.GetComponent<Health>();

            var damage = GetCalculatedDamage(actor);

            yield return actor.StartCoroutine(actor.PerformAttack(
                target.transform.position,
                "SimpleAttack",
                () =>
                {
                    targetHealth?.TakeDamage(damage);
                    CameraFollow.Instance?.ShakeMedium();
                },
                forceProceduralPunch,
                useDiagonalPunch
            ));
        }
    }
}