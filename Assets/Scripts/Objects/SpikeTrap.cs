using Entities;
using UnityEngine;

public class SpikeTrap : MonoBehaviour, ITileObject
{
    [SerializeField] private int damage = 5;
    
    public bool BlocksMovement => true;
    public Vector3Int CellPosition { get; set; }

    public void OnEntityEnter(BaseEntity entity)
    {
        if (entity.IsPhysicallyDead())
            return;
        
        var health = entity.GetComponent<Health>();
        if (health != null && !health.IsDead)
        {
            health.TakeDamage(damage);
            Debug.Log($"<color=red>[SpikeTrap]</color> {entity.name} попал на шипы в {CellPosition}! -{damage} HP");
            CameraFollow.Instance?.ShakeLight();
        }
    }

    public void OnEntityStay(BaseEntity entity) { }
    public void OnEntityExit(BaseEntity entity) { }
    
    public void OnPlayerEndTurn(PlayerMovement player) { }
}