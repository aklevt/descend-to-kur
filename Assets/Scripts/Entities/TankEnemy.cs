using System.Collections;
using UnityEngine;

namespace Entities
{
    /// <summary>
    /// Тяжелый ближник. Стоит на месте, охраняет проход. Когда игрок подходит в радиус обнаружения
    /// медленно сближается и бьёт мощным ударом с отбрасыванием.
    /// </summary>
    public class TankEnemy : EnemyBase
    {
        [Header("Tank Behavior")]
        [SerializeField] private bool isStationary = false;
        [SerializeField] private bool returnToPost = true;

        private Vector3Int postCell;
        private bool postInitialized;

        protected override void Start()
        {
            base.Start();
            postCell = CurrentCell;
            postInitialized = true;
        }

        protected override Vector3Int? GetBestMove()
        {
            // Без движения
            if (isStationary)
                return CurrentCell;

            var bestMove = base.GetBestMove();
            
            // Возврат на точку своего спауна
            if (!bestMove.HasValue && returnToPost && !IsOnPost())
                return GetReturnToPostMove();
            
            return bestMove ?? CurrentCell;
        }

        protected override IEnumerator ExecuteAction()
        {
            if (PlayerMovement.Instance != null)
            {
                var playerCell = PlayerMovement.Instance.CurrentCell;
                if (!Stats.IsInDetectionRange(CurrentCell, playerCell))
                {
                    Debug.Log($"[{gameObject.name}] Игрок вне зоны обнаружения");
                    yield break;
                }
            }

            yield return TryUseAbility(0);
        }

        private bool IsOnPost() => postInitialized && CurrentCell == postCell;

        private Vector3Int? GetReturnToPostMove()
        {
            if (CurrentCell == postCell)
                return CurrentCell;
            
            var possibleMoves = GridManager.Instance.GetWalkableCellsInRange(
                CurrentCell, Stats.MoveRange, gameObject
            );

            if (possibleMoves.Count == 0)
                return CurrentCell;

            var best = CurrentCell;
            var bestDist = ManhattanDistance(CurrentCell, postCell);

            foreach (var move in possibleMoves)
            {
                var dist = ManhattanDistance(move, postCell);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = move;
                }
            }

            if (best != CurrentCell)
                Debug.Log($"[{gameObject.name}] Возвращение на пост: {CurrentCell} -> {best}");

            return best;
        }

        private int ManhattanDistance(Vector3Int a, Vector3Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}