using System.Collections;
using UnityEngine;

public class ShieldEffect : MonoBehaviour
{
    [SerializeField] private float globalScale = 1f;
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(PlayShieldAnimation());
    }

    private IEnumerator PlayShieldAnimation()
    {
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        transform.localRotation = Quaternion.identity;
        
        var midScale = Vector3.one * globalScale;
        var peakScale = midScale * 1.3f;       
        var fakeMinScale = midScale * 1.25f;     
        var secondPeakScale = midScale * 1.1f;  
        var finalVisibleScale = midScale * 1.0f; 

        float elapsed;
        float duration;
        
        elapsed = 0f;
        duration = 0.35f; 
        var scaleUpThreshold = 0.15f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (elapsed < scaleUpThreshold)
            {
                transform.localScale = Vector3.Lerp(midScale, peakScale, elapsed / scaleUpThreshold);
            }
            else
            {
                var waveProgress = (elapsed - scaleUpThreshold) / (duration - scaleUpThreshold); 
        
                var wave = Mathf.Sin((elapsed - scaleUpThreshold) * 40f) * 0.1f * (1f - waveProgress);
        
                transform.localScale = peakScale + (Vector3.one * wave);
            }
            yield return null;
        }
        transform.localScale = peakScale; 

        elapsed = 0f;
        duration = 0.04f;
        transform.localRotation = Quaternion.identity; 
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.localScale = Vector3.Lerp(peakScale, fakeMinScale, t);
            yield return null;
        }

        elapsed = 0f;
        duration = 0.25f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / duration;
            transform.localScale = Vector3.Lerp(fakeMinScale, secondPeakScale, t);
            
            var zRotation = Mathf.Sin(Time.time * 80f) * 8f; 
            transform.localRotation = Quaternion.Euler(0, 0, zRotation);
            yield return null;
        }

        elapsed = 0f;
        duration = 0.2f;
        transform.localRotation = Quaternion.identity;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / duration;

            transform.localScale = Vector3.Lerp(secondPeakScale, finalVisibleScale, t);

            if (spriteRenderer != null)
            {
                var color = originalColor;
                color.a = Mathf.Lerp(originalColor.a, 0f, t);
                spriteRenderer.color = color;
            }

            yield return null;
        }

        gameObject.SetActive(false);
    }
}