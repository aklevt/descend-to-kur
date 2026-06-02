using System.Collections.Generic;
using Abilities;
using UnityEngine;

namespace Entities.AI
{
    public class TankEnemyAI : EnemyAIBase
    {
        public override Vector3Int? CalculateBestMove(EnemyBase enemy)
        {
            if (PlayerMovement.Instance == null) return null;

            var playerCell = PlayerMovement.Instance.CurrentCell;
            
            if (!IsPlayerDetected(enemy, playerCell))
            {
                Debug.Log($"[{enemy.name}] Игрок вне радиуса обнаружения");
                return null;
            }

            var possibleMoves = GetPossibleMoves(enemy);
            if (possibleMoves.Count == 0)
                return enemy.CurrentCell;

            
            var abilities = enemy.Abilities;
            var tankPunch = (abilities != null && abilities.Count > 0) 
                ? abilities[0] as TankPunchAbilityData 
                : null;

            
            if (tankPunch != null)
            {
                var currentTargetCells = tankPunch.GetTargetCellsFrom(enemy.CurrentCell, enemy);
                if (currentTargetCells.Contains(playerCell))
                {
                    return enemy.CurrentCell;
                }
            }

            var bestMove = enemy.CurrentCell;
            var bestScore = float.MinValue;

            foreach (var move in possibleMoves)
            {
                var dx = Mathf.Abs(playerCell.x - move.x);
                var dy = Mathf.Abs(playerCell.y - move.y);

                
                var euclideanDistance = Mathf.Sqrt(dx * dx + dy * dy);
                var score = -euclideanDistance;

                
                var canAttackFromThisMove = false;
                if (tankPunch != null)
                {
                    var futureTargetCells = tankPunch.GetTargetCellsFrom(move, enemy);
                    if (futureTargetCells.Contains(playerCell))
                    {
                        canAttackFromThisMove = true;
                    }
                }

                
                if (canAttackFromThisMove)
                {
                    
                    if (dx == 0 || dy == 0)
                    {
                        score += 2.0f; 
                    }
                    
                    else if (dx == 1 && dy == 1)
                    {
                        score += 1.5f;
                    }
                }

                
                if (dx == 0 && dy == 0) 
                    score -= 5.0f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }
    }
}