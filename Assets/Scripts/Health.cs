using System;
using System.Collections;
using Entities;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.5f;

    private SpriteRenderer spriteRenderer;
    public bool IsDead => entity != null && entity.Stats.IsDead;
    
    private bool isDying = false;

    private BaseEntity entity;
    
    public event Action<GameObject> OnDeath;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        entity = GetComponent<BaseEntity>();
    }

    public void TakeDamage(int damage)
    {
        if (isDying || IsDead || !entity) return;
        
        if (entity.Stats.HasActiveShield)
        {
            Debug.Log($"<color=yellow>[Health]</color> Урон {damage} поглощен щитом для {gameObject.name}");
            StartCoroutine(FlashGold());
            entity.Stats.ShieldTurns = 0;
            return;
        }

        entity.Stats.ApplyDamage(damage);
        
        Debug.Log($"{gameObject.name} получил урон: {damage}. ХП: {entity.Stats.Health}/{entity.Stats.MaxHealth}");

        if (entity.Stats.IsDead)
        {
            StartCoroutine(FlashRed());
            Die();
        }
        else
        {
            StartCoroutine(FlashRed());
        }
    }
    
    public void Heal(int amount)
    {
        if (isDying || IsDead || !entity) return;

        entity.Stats.ApplyHeal(amount);
    
        Debug.Log($"{gameObject.name} получил исцеление: {amount}. ХП: {entity.Stats.Health}/{entity.Stats.MaxHealth}");

        StartCoroutine(FlashGreen());
    }

    private IEnumerator FlashGreen()
    {
        if (spriteRenderer == null) yield break;
    
        spriteRenderer.color = Color.green;
        yield return new WaitForSeconds(0.1f);
    
        if (entity != null)
        {
            entity.UpdateVisualStatus();
        }
    }
    
    private IEnumerator FlashGold()
    {
        if (spriteRenderer == null) yield break;
    
        spriteRenderer.color = Color.yellow;
        yield return new WaitForSeconds(0.15f);
    
        if (entity != null)
        {
            entity.UpdateVisualStatus();
        }
    }

    private void Die()
    {
        if (isDying) return;
        isDying = true;
        
        CameraFollow.Instance?.ShakeMedium();
        
        if (entity != null)
        {
            GridManager.Instance.UnregisterEntity(entity.CurrentCell);
        }
        
        OnDeath?.Invoke(gameObject);

        // var cell = entity.CurrentCell;
        // GridManager.Instance.UnregisterEntity(cell);

        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;

        if (entity is PlayerMovement)
        {
            StartCoroutine(FadeOutPlayer());
        }
        else
        {
            StartCoroutine(FadeOutAndDestroy());
        }
    }

    private IEnumerator FlashRed()
    {
        if (spriteRenderer == null) yield break;
        
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (entity != null)
        {
            entity.UpdateVisualStatus();
        }
    }
    
    private IEnumerator FadeOutPlayer()
    {
        var elapsed = 0f;
        var color = spriteRenderer.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            spriteRenderer.color = color;
            yield return null;
        }
        
        // Игрок просто скрывается, не уничтожается
        gameObject.SetActive(false);
    }
    
    private IEnumerator FadeOutAndDestroy()
    {
        var elapsed = 0f;
        var color = spriteRenderer.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            
            color.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            spriteRenderer.color = color;

            yield return null;
        }
        
        // if (entity != null)
        // {
        //     GridManager.Instance.UnregisterEntity(entity.CurrentCell);
        // }

        Destroy(gameObject);
    }
    
    public void ResetDeathState()
    {
        isDying = false;
    
        if (spriteRenderer != null)
        {
            var color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }
    
        if (TryGetComponent<Collider2D>(out var col))
        {
            col.enabled = true;
        }
    
        gameObject.SetActive(true);
    
        Debug.Log($"<color=green>[Health]</color> {gameObject.name} восстановлен");
    }
}