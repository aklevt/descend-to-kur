using System.Collections;
using System.Collections.Generic;
using Core;
using Core.Room;
using Entities;
using UnityEngine;

namespace Abilities
{
    [CreateAssetMenu(fileName = "MoveAbility", menuName = "Abilities/Move")]
    public class MoveAbilityData : AbilityData
    {
        public override List<Vector3Int> GetTargetCellsFrom(Vector3Int position, BaseEntity actor)
        {
            int maxAvailableDistance;
            
            if (actor is PlayerMovement player)
            {
                maxAvailableDistance = Mathf.Min(player.Stats.RemainingSteps, player.Stats.Energy);
            }
            else if (actor is EnemyBase)
            {
                maxAvailableDistance = actor.Stats.MoveRange;
            }
            else
            {
                maxAvailableDistance = 0;
            }

            if (maxAvailableDistance <= 0) return new List<Vector3Int>();
            
            var boundaryCheck = RoomController.Current?.GetMovementBoundaryCheck();
            
            return GridManager.Instance.GetWalkableCellsInRange(
                position,
                maxAvailableDistance,
                actor.gameObject,
                boundaryCheck
            );
        }

        public override IEnumerator Execute(BaseEntity actor, Vector3Int targetCell)
        {
            var boundaryCheck = RoomController.Current?.GetMovementBoundaryCheck();
            
            var path = GridManager.Instance.GetPath(actor.CurrentCell, targetCell, actor.gameObject, boundaryCheck);
            var distance = path.Count > 0 ? path.Count - 1 : 0; // (path включает стартовую клетку)

            if (actor is PlayerMovement player)
            {
                var isSectionCleared = RoomController.Current != null && RoomController.Current.IsCurrentSectionCleared();
                
                if (isSectionCleared)
                {
                    Debug.Log($"<color=lime>[MoveAbility]</color> Секция зачищена. Неограниченное перемещение на {distance} клеток");
                }
                else
                {
                    if (!player.Stats.CanMove(distance) || !player.Stats.HasEnergyForAction(distance))
                    {
                        if (!player.Stats.HasEnergyForAction(distance))
                            UI.UIController.Instance?.ShowEnergyWarning();
                        else
                            UI.UIController.Instance?.ShowStepsWarning();

                        yield break;
                    }

                    player.Stats.SpendEnergy(distance);
                    player.Stats.SpendSteps(distance);

                    Debug.Log(
                        $"<color=cyan>[MoveAbility]</color> Потрачено: {distance}. Осталось энергии: {player.Stats.Energy}, шагов: {player.Stats.RemainingSteps}");
                }            }
            
            actor.MoveToCell(targetCell);
            while (actor.IsMoving) yield return null;
            
            if (actor is PlayerMovement player2)
            {
                var isSectionCleared = RoomController.Current != null && RoomController.Current.IsCurrentSectionCleared();

                if (!isSectionCleared)
                {
                    var hasStepsNow = player2.Stats.RemainingSteps > 0;
                    var hasEnergyNow = player2.Stats.Energy > 0;

                    if (!hasStepsNow && hasEnergyNow)
                    {
                        Debug.Log("<color=yellow>[MoveAbility]</color> Доступные шаги закончились");

                        yield return new WaitForSeconds(player2.GetScaledTime(0.1f));

                        UI.UIController.Instance?.ShowStepsEndedWarning();
                    }
                }
            }
        }
        
        public override List<Vector3Int> GetTheoreticalCellsFrom(Vector3Int position, BaseEntity actor)
        {
            int maxRange;

            if (actor is PlayerMovement player)
                maxRange = player.Stats.MaxStepsPerRound;
            else if (actor is EnemyBase)
                maxRange = actor.Stats.MoveRange;
            else
                maxRange = 0;

            if (maxRange <= 0) return new List<Vector3Int>();

            var cells = new List<Vector3Int>();
            for (var dx = -maxRange; dx <= maxRange; dx++)
            for (var dy = -maxRange; dy <= maxRange; dy++)
            {
                var manh = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (manh == 0 || manh > maxRange) continue;

                var cell = new Vector3Int(position.x + dx, position.y + dy, position.z);
                if (!GridManager.Instance.HasFloor(cell)) 
                    continue;

                cells.Add(cell);
            }
            return cells;
        }
    }
}