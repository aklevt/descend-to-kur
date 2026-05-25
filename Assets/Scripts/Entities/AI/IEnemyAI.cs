using UnityEngine;

namespace Entities.AI
{
    public interface IEnemyAI
    {
        Vector3Int? CalculateBestMove(EnemyBase enemy);
    }
}