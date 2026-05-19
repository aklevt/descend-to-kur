using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Entities.AI
{
    public abstract class EnemyAIBase : IEnemyAI
    {
        public abstract Vector3Int? CalculateBestMove(EnemyBase enemy);

        protected List<Vector3Int> GetPossibleMoves(EnemyBase enemy)
        {
            return GridManager.Instance.GetWalkableCellsInRange(
                enemy.CurrentCell,
                enemy.Stats.MoveRange,
                enemy.gameObject
            );
        }

        protected int ManhattanDistance(Vector3Int a, Vector3Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        protected float GetDirectionPriority(Vector3Int candidatePos, Vector3Int currentPos, Vector3Int target, bool retreat)
        {
            var direction = candidatePos - currentPos;
            var toTarget = retreat ? (currentPos - target) : (target - currentPos);

            var directionNorm = new Vector3(Mathf.Sign(direction.x), Mathf.Sign(direction.y), 0f);
            var targetNorm = new Vector3(Mathf.Sign(toTarget.x), Mathf.Sign(toTarget.y), 0f);

            return Vector3.Dot(directionNorm, targetNorm);
        }

        protected Vector3Int CalculateAdvanceMove(EnemyBase enemy, Vector3Int playerCell, List<Vector3Int> possibleMoves)
        {
            return possibleMoves
                .OrderBy(pos => Mathf.Abs(ManhattanDistance(pos, playerCell) - enemy.Stats.PreferredAttackRange))
                .ThenBy(pos => ManhattanDistance(pos, playerCell))
                .ThenByDescending(pos => GetDirectionPriority(pos, enemy.CurrentCell, playerCell, retreat: false))
                .First();
        }

        protected Vector3Int CalculateRetreatMove(EnemyBase enemy, Vector3Int playerCell, List<Vector3Int> possibleMoves)
        {
            var retreatMoves = possibleMoves
                .Where(pos => ManhattanDistance(pos, playerCell) >= enemy.Stats.MinimumRange)
                .ToList();

            if (retreatMoves.Count > 0)
            {
                return retreatMoves
                    .OrderBy(pos => Mathf.Abs(ManhattanDistance(pos, playerCell) - enemy.Stats.PreferredAttackRange))
                    .ThenByDescending(pos => GetDirectionPriority(pos, enemy.CurrentCell, playerCell, retreat: true))
                    .First();
            }

            return possibleMoves
                .OrderByDescending(pos => ManhattanDistance(pos, playerCell))
                .First();
        }

        protected bool IsPlayerDetected(EnemyBase enemy, Vector3Int playerCell)
        {
            return enemy.Stats.IsInDetectionRange(enemy.CurrentCell, playerCell);
        }
    }
}