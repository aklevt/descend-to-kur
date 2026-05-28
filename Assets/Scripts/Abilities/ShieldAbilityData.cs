using System.Collections;
using System.Collections.Generic;
using Entities;
using UnityEngine;

namespace Abilities
{
    [CreateAssetMenu(fileName = "ShieldAbility", menuName = "Abilities/Shield")]
    public class ShieldAbilityData : AbilityData
    {
        [Header("Shield Settings")]
        [SerializeField] private int shieldDuration = 3;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject shieldEffectPrefab;
        [SerializeField] private float effectDuration = 0.5f;
        
        [Header("Energy Freeze")] // ❗ Заготовка
        [SerializeField] private int freezeEnergyCost = 3; 

        public override List<Vector3Int> GetTargetCellsFrom(Vector3Int origin, BaseEntity actor)
        {
            return new List<Vector3Int> { origin };
        }

        public override List<Vector3Int> GetEffectCells(Vector3Int hoveredCell, BaseEntity actor)
        {
            return new List<Vector3Int> { actor.CurrentCell };
        }

        public override bool IsValidTarget(Vector3Int targetCell, BaseEntity caster)
        {
            return targetCell == caster.CurrentCell;
        }

        public override IEnumerator Execute(BaseEntity actor, Vector3Int targetCell)
        {
            if (targetCell != actor.CurrentCell)
            {
                yield break;
            }

            actor.Stats.ApplyShield(shieldDuration);
            actor.UpdateVisualStatus();

            yield return PlayShieldEffect(actor);
        }

        private IEnumerator PlayShieldEffect(BaseEntity actor)
        {
            if (shieldEffectPrefab != null)
            {
                var effectPos = actor.transform.position + new Vector3(0, 0.4f, 0);
                var effect = Instantiate(shieldEffectPrefab, effectPos, Quaternion.identity);
                
                yield return new WaitForSeconds(actor.GetScaledTime(effectDuration));
                
                if (effect != null)
                    Destroy(effect);
            }
            else
            {
                yield return new WaitForSeconds(actor.GetScaledTime(0.1f));
            }
        }

        public override Vector3Int? ChooseTarget(BaseEntity actor)
        {
            return actor.CurrentCell;
        }
    }
}