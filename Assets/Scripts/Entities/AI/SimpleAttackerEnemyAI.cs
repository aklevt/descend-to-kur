using UnityEngine;

namespace Entities.AI
{
    public class SimpleAttackerEnemyAI : EnemyAIBase
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
                Debug.Log($"[{enemy.name}] Уже на дистанции атаки");
                return enemy.CurrentCell;
            }

            var possibleMoves = GetPossibleMoves(enemy);
            if (possibleMoves.Count == 0)
                return enemy.CurrentCell;

            return tooClose
                ? CalculateRetreatMove(enemy, playerCell, possibleMoves)
                : CalculateAdvanceMove(enemy, playerCell, possibleMoves);
        }
    }
}