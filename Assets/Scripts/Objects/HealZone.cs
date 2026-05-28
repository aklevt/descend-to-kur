using Entities;
using UnityEngine;

public class HealZone : MonoBehaviour, ITileObject
{
    [SerializeField] private int healAmount = 5;

    private bool wasCollected = false;
    private bool playerStandingOn = false;

    public bool BlocksMovement => false;
    public Vector3Int CellPosition { get; set; }

    public void OnEntityEnter(BaseEntity entity)
    {
        if (wasCollected)
            return;

        if (entity is PlayerMovement player)
        {
            if (!player.IsPhysicallyDead())
            {
                playerStandingOn = true;
                Debug.Log(
                    $"<color=yellow>[HealZone]</color> {player.name} стоит на хилке. Нажмите 'Завершить ход' чтобы подобрать (+{healAmount} HP). " +
                    $"❗ Это надо переместить в PopUp-канал сообщений");
            }
        }
    }

    public void OnEntityExit(BaseEntity entity)
    {
        if (entity is PlayerMovement)
        {
            playerStandingOn = false;
        }
    }

    public void OnPlayerEndTurn(PlayerMovement player)
    {
        if (wasCollected || !playerStandingOn)
            return;

        if (player.IsPhysicallyDead())
            return;

        var health = player.GetComponent<Health>();
        if (health != null && !health.IsDead)
        {
            health.Heal(healAmount);
            Debug.Log($"<color=green>[HealZone]</color> {player.name} подобрал хилку! +{healAmount} HP");

            wasCollected = true;
            playerStandingOn = false;

            GridManager.Instance.RemoveTileObject(CellPosition);
        }
    }

    public void OnEntityStay(BaseEntity entity)
    {
    }
}