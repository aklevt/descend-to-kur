using System.Collections;
using System.Collections.Generic;
using Entities;
using UnityEngine;

namespace Abilities
{
    [CreateAssetMenu(fileName = "PowerfulPunchAbility", menuName = "Abilities/PowerfulPunch")]
    public class PowerfulPunchAD : AbilityData
    {
        [Header("Freeze Effect")] [SerializeField]
        private int freezePower = 3;

        public override List<Vector3Int> GetTargetCellsFrom(Vector3Int origin, BaseEntity caster)
        {
            return GridManager.Instance.GetAttackableCellsInRadius(origin, 1);
        }

        public override List<Vector3Int> GetEffectCells(Vector3Int hoveredCell, BaseEntity actor)
        {
            var effectCells = new List<Vector3Int>();
            Vector3Int rayVector;
            Vector3Int deltaVector;
            if (hoveredCell - actor.CurrentCell == Vector3Int.up)
            {
                rayVector = Vector3Int.up;
                deltaVector = Vector3Int.right;
            }
            else if (hoveredCell - actor.CurrentCell == Vector3Int.down)
            {
                rayVector = Vector3Int.down;
                deltaVector = Vector3Int.right;
            }
            else if (hoveredCell - actor.CurrentCell == Vector3Int.right)
            {
                rayVector = Vector3Int.right;
                deltaVector = Vector3Int.up;
            }
            else if (hoveredCell - actor.CurrentCell == Vector3Int.left)
            {
                rayVector = Vector3Int.left;
                deltaVector = Vector3Int.up;
            }
            else
            {
                return effectCells;
            }

            for (var j = 2; j > 0; j--)
            {
                for (var i = -1; i <= 1; i++)
                {
                    effectCells.Add(actor.CurrentCell + j * rayVector + i * deltaVector);
                }
            }

            return effectCells;
        }

        public override bool IsValidTarget(Vector3Int targetCell, BaseEntity caster)
        {
            foreach (var cell in GetEffectCells(targetCell, caster))
            {
                var target = GridManager.Instance.GetEntityAt(cell);
                if (target != null)
                    return true;
            }

            return false;
        }

        public override IEnumerator Execute(BaseEntity actor, Vector3Int targetCell)
        {
            var targetPos = GridManager.Instance.GetCellCenterWorld(targetCell);
            actor.FlipToTarget(targetPos);
            
            CameraFollow.Instance?.ShakeMedium();
            var knockbackVector = targetCell - actor.CurrentCell;
            var targets = GetEffectCells(targetCell, actor);

            foreach (var cell in targets)
            {
                var targetObj = GridManager.Instance.GetEntityAt(cell);
                if (targetObj != null)
                {
                    var entity = targetObj.GetComponent<BaseEntity>();
                    var health = targetObj.GetComponent<Health>();

                    entity.Freeze(freezePower);
                    
                    var damage = GetCalculatedDamage(actor);
                    
                    health?.TakeDamage(damage);
                    KnobackEnemy(knockbackVector, entity);
                }
            }

            yield return new WaitForSeconds(actor.GetScaledTime(0.1f));
        }

        private void KnobackEnemy(Vector3Int direction, BaseEntity entity)
        {
            var cell1 = entity.CurrentCell + direction;
            var cell2 = entity.CurrentCell + 2 * direction;
    
            if (!GridManager.Instance.IsCellKnockbackable(cell1))
            {
                return;
            }
    
            if (GridManager.Instance.HasBlockingTileObject(cell1))
            {
                entity.MoveDirectly(cell1);
                return;
            }
    
            if (!GridManager.Instance.IsCellKnockbackable(cell2))
            {
                entity.MoveDirectly(cell1);
                return;
            }
    
            if (GridManager.Instance.HasBlockingTileObject(cell2))
            {
                entity.MoveDirectly(cell2);
                return;
            }
    
            entity.MoveDirectly(cell2);
        }
    }
}