
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Intro
{
    /// <summary>
    /// Простой полноэкранный текст для повествования
    /// </summary>
    public class NarrativeUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private CanvasGroup container;
        [SerializeField] private TextMeshProUGUI narrativeText;
        [SerializeField] private GameObject continueIndicator;
        [SerializeField] private Button clickArea;
        
        [Header("Visual Style")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image decorativeFrame; 
        
        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        public bool IsTyping { get; private set; }
        public bool WaitingForInput { get; set; }

        private bool shouldCompleteInstantly;

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

        public IEnumerator FadeIn()
        {
            gameObject.SetActive(true);
            
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
        }

        public IEnumerator FadeOut()
        {
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

        public IEnumerator ShowText(string text, float typeSpeed)
        {
            IsTyping = true;
            shouldCompleteInstantly = false;
            WaitingForInput = false;
            
            if (continueIndicator != null)
                continueIndicator.SetActive(false);

            if (narrativeText != null)
            {
                narrativeText.text = text;
                narrativeText.ForceMeshUpdate();
                
                var totalChars = narrativeText.textInfo.characterCount;
                narrativeText.maxVisibleCharacters = 0;

                for (var i = 0; i <= totalChars; i++)
                {
                    if (shouldCompleteInstantly)
                    {
                        narrativeText.maxVisibleCharacters = totalChars;
                        break;
                    }

                    narrativeText.maxVisibleCharacters = i;
                    
                    var delay = 1f / typeSpeed;
                    
                    if (i < totalChars)
                    {
                        var c = narrativeText.textInfo.characterInfo[i].character;
                        delay = GetCharacterDelay(c, delay);
                    }
                    
                    yield return new WaitForSeconds(delay);
                }
            }

            IsTyping = false;
            
            if (continueIndicator != null)
                continueIndicator.SetActive(true);
            
            WaitingForInput = true;
        }

        public void CompleteText()
        {
            shouldCompleteInstantly = true;
            
            if (narrativeText != null && narrativeText.textInfo != null)
            {
                narrativeText.maxVisibleCharacters = narrativeText.textInfo.characterCount;
            }
            
            IsTyping = false;
            
            if (continueIndicator != null)
                continueIndicator.SetActive(true);
        }

        private void Clear()
        {
            if (narrativeText != null)
            {
                narrativeText.text = "";
                narrativeText.maxVisibleCharacters = 0;
            }
            
            if (continueIndicator != null)
                continueIndicator.SetActive(false);
            
            WaitingForInput = false;
        }

        private float GetCharacterDelay(char character, float baseDelay)
        {
            return character switch
            {
                '.' or '!' or '?' => baseDelay * 3f,
                ',' or ';' or ':' => baseDelay * 2f,
                '…' => baseDelay * 4f,
                _ when char.IsWhiteSpace(character) => 0f,
                _ => baseDelay
            };
        }
    }
}