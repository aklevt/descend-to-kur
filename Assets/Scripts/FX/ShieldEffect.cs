using UnityEngine;

public class ShieldEffect : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float maxAlpha = 0.8f;
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }
    
    private void Update()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        
        if (spriteRenderer != null)
        {
            var alpha = Mathf.Lerp(minAlpha, maxAlpha, 
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            
            var color = originalColor;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
}