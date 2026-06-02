
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Intro
{
    /// <summary>
    /// Показ картинки с подписью
    /// </summary>
    public class ImageDisplayUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private CanvasGroup container;
        [SerializeField] private Image displayImage;
        [SerializeField] private TextMeshProUGUI captionText;
        [SerializeField] private GameObject continueIndicator;
        [SerializeField] private Button clickArea;
        
        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float imageFadeDelay = 0.2f;

        public bool WaitingForInput { get; set; }

        private void Awake()
        {
            gameObject.SetActive(false);
            
            if (container != null)
                container.alpha = 0f;
            
            Clear();
        }

        public void SetClickListener(System.Action callback)
        {
            if (clickArea != null)
            {
                clickArea.onClick.RemoveAllListeners();
                clickArea.onClick.AddListener(() => callback?.Invoke());
            }
        }

        public IEnumerator Show(Sprite image, string caption = "")
        {
            gameObject.SetActive(true);
            WaitingForInput = false;
            
            if (continueIndicator != null)
                continueIndicator.SetActive(false);

            
            if (displayImage != null && image != null)
            {
                displayImage.sprite = image;
                displayImage.color = new Color(1f, 1f, 1f, 0f);
            }

            
            if (captionText != null)
            {
                captionText.text = caption ?? "";
                captionText.alpha = 0f;
            }

            
            if (container != null)
            {
                var elapsed = 0f;
                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.deltaTime;
                    container.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                    yield return null;
                }
                container.alpha = 1f;
            }

            yield return new WaitForSeconds(imageFadeDelay);

            
            if (displayImage != null)
            {
                var elapsed = 0f;
                var c = displayImage.color;
                
                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.deltaTime;
                    var alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                    displayImage.color = new Color(c.r, c.g, c.b, alpha);
                    yield return null;
                }
                
                displayImage.color = new Color(c.r, c.g, c.b, 1f);
            }

            
            if (captionText != null && !string.IsNullOrEmpty(caption))
            {
                var elapsed = 0f;
                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.deltaTime;
                    captionText.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                    yield return null;
                }
                captionText.alpha = 1f;
            }

            if (continueIndicator != null)
                continueIndicator.SetActive(true);
            
            WaitingForInput = true;
        }

        public IEnumerator Hide()
        {
            WaitingForInput = false;
            
            if (container != null)
            {
                var elapsed = 0f;
                var startAlpha = container.alpha;
                
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    container.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
                    yield return null;
                }
                container.alpha = 0f;
            }
            
            Clear();
            gameObject.SetActive(false);
        }

        private void Clear()
        {
            if (displayImage != null)
            {
                displayImage.sprite = null;
                displayImage.color = Color.white;
            }
            
            if (captionText != null)
            {
                captionText.text = "";
                captionText.alpha = 0f;
            }
            
            if (continueIndicator != null)
                continueIndicator.SetActive(false);
            
            WaitingForInput = false;
        }
    }
}