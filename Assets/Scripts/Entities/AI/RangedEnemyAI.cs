using System.Linq;
using UnityEngine;

namespace Entities.AI
{
    public class RangedEnemyAI : EnemyAIBase
    {
        public override Vector3Int? CalculateBestMove(EnemyBase enemy)
        {
            if (PlayerMovement.Instance == null) return null;

            var playerCell = PlayerMovement.Instance.CurrentCell;
            var currentDistance = ManhattanDistance(enemy.CurrentCell, playerCell);

            if (!IsPlayerDetected(enemy, playerCell))
            {
                Debug.Log($"[{enemy.name}] Игрок вне радиуса обнаружения");
                return null;
            }

            var tooClose = enemy.Stats.MinimumRange > 0 && currentDistance < enemy.Stats.MinimumRange;
            var inAttackRange = !tooClose && currentDistance <= enemy.Stats.PreferredAttackRange;

            if (inAttackRange)
            {
                var canShootFromHere =
                    GridManager.Instance.HasLineOfSight(enemy.CurrentCell, playerCell) &&
                    GridManager.Instance.IsCellShootable(playerCell);

                if (canShootFromHere)
                {
                    Debug.Log($"[{enemy.name}] Уже на позиции для обычной дальней атаки");
                    return enemy.CurrentCell;
                }
            }

            var possibleMoves = GetPossibleMoves(enemy);
            if (possibleMoves.Count == 0)
                return enemy.CurrentCell;

            var shootablePositions = GridManager.Instance.GetShootablePositionsTo(
                enemy.CurrentCell,
                playerCell,
                enemy.Stats.MinimumRange,
                enemy.Stats.PreferredAttackRange,
                enemy.gameObject
            );

            if (shootablePositions.Count > 0)
            {
                var validMoves = possibleMoves.Where(shootablePositions.Contains).ToList();
                if (validMoves.Count > 0)
                    possibleMoves = validMoves;
            }

            return tooClose
                ? CalculateRetreatMove(enemy, playerCell, possibleMoves)
                : CalculateAdvanceMove(enemy, playerCell, possibleMoves);
        }
    }
}