using System.Collections;
using System.Collections.Generic;
using Entities;
using FX;
using UnityEngine;

namespace Abilities
{
    [CreateAssetMenu(fileName = "StunAbility", menuName = "Abilities/Stun")]
    public class StunAbilityData : AbilityData
    {
        [Header("Range Settings")]
        public int minRange = 2;
        public int maxRange = 4;
        
        [Header("Stun Settings")]
        [SerializeField] private int stunDuration = 2;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject stunEffectPrefab;
        [SerializeField] private float effectDuration = 0.3f;

        public override List<Vector3Int> GetTargetCellsFrom(Vector3Int origin, BaseEntity actor)
        {
            var allCells = GridManager.Instance.GetAttackableCellsInRadius(origin, maxRange, minRange);
            var targetableCells = new List<Vector3Int>();

            foreach (var cell in allCells)
            {
                if (!GridManager.Instance.IsCellShootable(cell))
                    continue;

                if (!GridManager.Instance.HasLineOfSight(origin, cell))
                    continue;

                targetableCells.Add(cell);
            }

            return targetableCells;
        }

        public override List<Vector3Int> GetEffectCells(Vector3Int hoveredCell, BaseEntity actor)
        {
            return new List<Vector3Int> { hoveredCell };
        }

        public override bool IsValidTarget(Vector3Int targetCell, BaseEntity caster)
        {
            var target = GridManager.Instance.GetEntityAt(targetCell);
            return target != null && target != caster.gameObject;
        }

        public override Vector3Int? ChooseTarget(BaseEntity actor)
        {
            var playerCell = PlayerMovement.Instance?.CurrentCell;
            if (playerCell == null) return null;

            var available = GetTargetCells(actor);
            return available.Contains(playerCell.Value) ? playerCell : null;
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
            if (targetObj == null) 
            {
                yield break;
            }

            var targetEntity = targetObj.GetComponent<BaseEntity>();
            if (targetEntity == null)
            {
                yield break;
            }

            var targetPos = targetObj.transform.position + new Vector3(0, 0.5f, 0);
            actor.FlipToTarget(targetPos);

            yield return new WaitForSeconds(actor.GetScaledTime(0.1f));

            targetEntity.Freeze(stunDuration);
            
            yield return PlayStunEffect(targetPos, actor);
            
            Debug.Log($"<color=purple>[StunAbility]</color> {targetEntity.name} оглушен на {stunDuration} ходов");
        }

        private IEnumerator PlayStunEffect(Vector3 targetPos, BaseEntity actor)
        {
            if (stunEffectPrefab != null)
            {
                var effect = Instantiate(stunEffectPrefab, targetPos, Quaternion.identity);
                yield return new WaitForSeconds(actor.GetScaledTime(effectDuration));
                if (effect != null)
                    Destroy(effect);
            }
            else
            {
                yield return new WaitForSeconds(actor.GetScaledTime(0.1f));
            }
        }
    }
}