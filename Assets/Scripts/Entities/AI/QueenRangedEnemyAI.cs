using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Entities.AI
{
    public class QueenRangedEnemyAI : EnemyAIBase
    {
        public int maxDiagonalRange = 5; // ❗ По-хорошему, вынести в Runtime Stats
        
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

            if (CanQueenAttackFrom(enemy.CurrentCell, playerCell, enemy))
            {
                Debug.Log($"[{enemy.name}] Уже на позиции для атаки ферзем");
                return enemy.CurrentCell;
            }

            var possibleMoves = GetPossibleMoves(enemy);
            if (possibleMoves.Count == 0)
                return enemy.CurrentCell;

            var validShootPositions = possibleMoves
                .Where(pos => CanQueenAttackFrom(pos, playerCell, enemy))
                .ToList();

            if (validShootPositions.Count > 0)
            {
                possibleMoves = validShootPositions;
                Debug.Log($"[{enemy.name}] Найдено {possibleMoves.Count} позиций для атаки ферзем");
            }
            else
            {
                Debug.Log($"[{enemy.name}] Нет позиций для атаки ферзем, перемещение ближе");
            }

            return tooClose
                ? CalculateRetreatMove(enemy, playerCell, possibleMoves)
                : CalculateAdvanceMove(enemy, playerCell, possibleMoves);
        }

        private bool CanQueenAttackFrom(Vector3Int from, Vector3Int target, EnemyBase enemy)
        {
            if (!GridManager.Instance.IsCellShootable(target))
                return false;

            var dx = target.x - from.x;
            var dy = target.y - from.y;

            var absX = Mathf.Abs(dx);
            var absY = Mathf.Abs(dy);

            var isStraight = dx == 0 || dy == 0;
            var isDiagonal = absX == absY;

            if (!isStraight && !isDiagonal)
                return false;

            var steps = Mathf.Max(absX, absY);

            var maxRange = isDiagonal ? maxDiagonalRange : enemy.Stats.PreferredAttackRange;

            if (steps < enemy.Stats.MinimumRange || steps > maxRange)
                return false;

            return IsQueenLineClear(from, target);
        }

        private bool IsQueenLineClear(Vector3Int from, Vector3Int target)
        {
            var dx = target.x - from.x;
            var dy = target.y - from.y;

            var stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
            var stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

            var step = new Vector3Int(stepX, stepY, 0);
            var current = from + step;

            while (current != target)
            {
                if (!GridManager.Instance.IsCellShootable(current))
                    return false;

                // Может стрелять
                // if (GridManager.Instance.GetEntityAt(current) != null)
                //     return false;

                current += new Vector3Int(stepX, stepY, 0);
            }

            return true;
        }
    }
}