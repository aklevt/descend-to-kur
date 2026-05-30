using System.Collections.Generic;
using Abilities;
using Entities;
using Stats;
using UI;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Отвечает за проверку условий для вызова способностей и показ предупреждений в UI
    /// </summary>
    public class AbilityValidator
    {
        /// <summary>
        /// Проверяет ресурсы способности и показывает предупреждения в UI
        /// </summary>
        public void ValidateAbilityAndWarn(AbilityData ability, int abilityIndex, AbilityBar abilityBar)
        {
            if (PlayerMovement.Instance == null) return;
            if (GameStateManager.Instance?.CurrentState != GameState.Gameplay) return;
            if (TurnManager.Instance?.CurrentState != TurnState.PlayerTurn) return;
            
            var stats = PlayerMovement.Instance.Stats;
            
            if (ShouldFlashAbility(ability, stats))
            {
                abilityBar?.TriggerWarningFlash(abilityIndex);
            }
            
            // ShowAbilityWarnings(ability, stats);
        }

        /// <summary>
        /// Проверяет возможность использования способности на указанной цели
        /// </summary>
        public bool CanUseAbilityOnTarget(AbilityData ability, Vector3Int targetCell, List<Vector3Int> availableCells)
        {
            var stats = PlayerMovement.Instance?.Stats;

            if (!ability.CanUse(PlayerMovement.Instance))
            {
                if (ability is MoveAbilityData)
                {
                    if (stats != null && stats.RemainingSteps <= 0)
                        UIController.Instance?.ShowStepsWarning();
                    else
                        UIController.Instance?.ShowEnergyWarning();
                }
                else
                {
                    UIController.Instance?.ShowEnergyWarning();
                }
                return false;
            }

            if (!availableCells.Contains(targetCell)) return false;

            if (!ability.IsValidTarget(targetCell, PlayerMovement.Instance))
                return false;
                
            return true;
        }

        /// <summary>
        /// Проверяет состояние игрока для выполнения действия
        /// </summary>
        public bool IsPlayerReadyForAction()
        {
            if (PlayerMovement.Instance.IsFreeze)
            {
                UnityEngine.Debug.Log("Игрок заморожен, пропуск хода");
                return false;
            }
            
            return !PlayerMovement.Instance.IsMoving;
        }

        private bool ShouldFlashAbility(AbilityData ability, EntityRuntimeStats stats)
        {
            if (ability is MoveAbilityData)
            {
                return stats.RemainingSteps <= 0 || stats.Energy <= 0;
            }
            else
            {
                return !stats.HasEnergyForAction(ability.energyCost);
            }
        }

        private void ShowAbilityWarnings(AbilityData ability, EntityRuntimeStats stats)
        {
            if (GameStateManager.Instance?.CurrentState != GameState.Gameplay) return;
            if (TurnManager.Instance?.CurrentState != TurnState.PlayerTurn)
                return;
            
            if (ability is MoveAbilityData && stats.RemainingSteps <= 0)
            {
                UIController.Instance?.ShowStepsWarning();
            }
            
            if (!ability.CanUse(PlayerMovement.Instance))
            {
                UIController.Instance?.ShowEnergyWarning();
            }
        }
    }
}