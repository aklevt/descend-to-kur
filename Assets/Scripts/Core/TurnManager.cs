using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Room;
using Entities;
using Settings;

public class TurnManager : MonoBehaviour
{
    [ContextMenu("Ход противника")]
    public void DebugEnemyTurn() => SetState(TurnState.EnemyTurn);

    private List<EnemyBase> allEnemies = new();
    private IEnumerator EnemyTurnSequence()
    {
        var activeEnemies = new List<EnemyBase>();
    
        if (RoomController.Current != null)
        {
            activeEnemies = RoomController.Current.GetActiveEnemiesForTurn();
            // Debug.Log(activeEnemies.Count);
        }
    
        if (activeEnemies.Count == 0)
        {
            yield return ProcessEndOfRound();
            SetState(TurnState.PlayerTurn);
            CameraFollow.Instance?.ResetFocus();
            yield break;
        }
        
        var allFrozen = activeEnemies.Count > 0 && activeEnemies.All(e => e.IsFreeze);
        
        if (allFrozen)
        {
            Debug.Log("<color=cyan>[TurnManager]</color> Все враги заморожены");
            
            yield return new WaitForSeconds(SettingsManager.Instance.GetScaledDelay(1.0f));
            yield return ProcessEndOfRound();
            
            if (PlayerMovement.Instance == null || PlayerMovement.Instance.Stats.Health <= 0)
                yield break;
        
            SetState(TurnState.PlayerTurn);
            CameraFollow.Instance?.ResetFocus();
            yield break;
        }


        if (activeEnemies.Count > 0)
        {
            var center = activeEnemies
                             .Aggregate(Vector3.zero, (sum, e) => sum + e.transform.position)
                         / activeEnemies.Count;

            // Камера смещается к центру один раз на весь ход врагов
            CameraFollow.Instance?.ShiftTowards(center);
        }
        
        var sortedEnemies = GetEnemiesSortedByDistance(activeEnemies);

        foreach (var enemy in sortedEnemies)
        {
            if (enemy == null) continue;
            yield return enemy.DoTurn();
            
            if (PlayerMovement.Instance == null || PlayerMovement.Instance.Stats.Health <= 0)
            {
                Debug.Log("<color=red>[TurnManager]</color> Игрок погиб, последовательность ходов врагов прервана");
                yield break;
            }
        }

        yield return ProcessEndOfRound();


        // yield return null;

        if (PlayerMovement.Instance.Stats.Health <= 0)
        {
            yield break;
        }

        SetState(TurnState.PlayerTurn);

        CameraFollow.Instance?.ResetFocus();
    }
    
    /// <summary>
    /// Сортирует врагов по расстоянию до игрока (ближайшие первые)
    /// </summary>
    private List<EnemyBase> GetEnemiesSortedByDistance(List<EnemyBase> enemies)
    {
        if (PlayerMovement.Instance == null)
            return enemies;

        var playerCell = PlayerMovement.Instance.CurrentCell;

        return enemies
            .Where(e => e != null)
            .OrderBy(e => GridManager.Instance.GetPathDistance(
                e.CurrentCell, 
                playerCell, 
                e.gameObject))
            .ToList();
    }

    /// <summary>
    /// Обрабатывает все эффекты конца раунда (разморозка)
    /// </summary>
    private IEnumerator ProcessEndOfRound()
    {
        var allEntities = new List<BaseEntity>(allEnemies);
        if (PlayerMovement.Instance != null)
            allEntities.Add(PlayerMovement.Instance);

        foreach (var entity in allEntities)
        {
            if (entity != null)
                entity.OnRoundEnd();
        }

        foreach (var entity in allEntities)
        {
            if (entity != null && entity.Stats.Freeze == 0)
            {
                entity.UpdateVisualStatus();
                yield return new WaitForSeconds(SettingsManager.Instance.GetScaledDelay(0.02f));
            }
        }

        yield return new WaitForSeconds(SettingsManager.Instance.GetScaledDelay(0.1f));
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        if (!allEnemies.Contains(enemy))
        {
            allEnemies.Add(enemy);
        }
    }

    public static TurnManager Instance { get; private set; }

    public TurnState CurrentState { get; private set; }

    public event Action<TurnState> OnStateChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UnregisterEnemy(EnemyBase enemy)
    {
        if (allEnemies.Contains(enemy))
        {
            allEnemies.Remove(enemy);
            Debug.Log($"<color=red>[TurnManager]</color> Противник удален. Осталось: {allEnemies.Count}");
        }
    }

    private void SetState(TurnState newState)
    {
        CurrentState = newState;

        if (newState == TurnState.PlayerTurn && PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.Stats.RestoreEnergy(PlayerMovement.Instance.Stats.MaxEnergy);
            PlayerMovement.Instance.OnTurnStart();
        }

        Debug.Log($"<color=yellow>[TurnManager]</color> Ход сменился на: <b>{newState}</b>");

        OnStateChanged?.Invoke(newState);

        if (newState == TurnState.EnemyTurn)
        {
            StartCoroutine(EnemyTurnSequence());
        }
    }

    public void BeginLevel()
    {
        SetState(TurnState.PlayerTurn);
    }

    public void EndPlayerTurn()
    {
        if (CurrentState != TurnState.PlayerTurn)
            return;
    
        if (PlayerMovement.Instance != null)
        {
            var playerCell = PlayerMovement.Instance.CurrentCell;
            GridManager.Instance.TriggerTileObjectEndTurn(playerCell, PlayerMovement.Instance);
        }
    
        SetState(TurnState.EnemyTurn);
    }

    public void ResetEnemies()
    {
        allEnemies.Clear();
        StopAllCoroutines(); // ❗ Это временно, надо переписать ❗
    }
}