using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace UI.Menu
{
    /// <summary>
    /// Простая панель интро без Input System
    /// </summary>
    public class SimpleIntroPanel : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI textDisplay;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button invisibleClickArea;
        
        [Header("Data")]
        [SerializeField] private IntroData introData;
        
        private int currentTextIndex = 0;
        private bool isTyping = false;
        private bool isActive = false;
        private Coroutine typeCoroutine;

        private void Awake()
        {
            if (panel != null)
                panel.SetActive(false);
                
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinue);
                
            if (invisibleClickArea != null)
                invisibleClickArea.onClick.AddListener(OnContinue);
        }

        public void StartIntro()
        {
            if (introData == null)
            {
                Debug.LogWarning("[SimpleIntroPanel] IntroData не назначен!");
                EndIntro();
                return;
            }

            if (introData.introTexts.Length == 0)
            {
                Debug.LogWarning("[SimpleIntroPanel] IntroData пустой!");
                EndIntro();
                return;
            }

            isActive = true;
            
            if (panel != null)
                panel.SetActive(true);
                
            currentTextIndex = 0;
            ShowCurrentText();
        }

        private void ShowCurrentText()
        {
            if (currentTextIndex >= introData.introTexts.Length)
            {
                EndIntro();
                return;
            }

            if (typeCoroutine != null)
                StopCoroutine(typeCoroutine);
                
            typeCoroutine = StartCoroutine(TypeText(introData.introTexts[currentTextIndex]));
        }

        private IEnumerator TypeText(string text)
        {
            isTyping = true;
            
            if (continueButton != null)
                continueButton.gameObject.SetActive(false);

            if (textDisplay != null)
            {
                textDisplay.text = "";
                
                for (int i = 0; i <= text.Length; i++)
                {
                    if (!isActive) yield break;
                    
                    textDisplay.text = text.Substring(0, i);
                    yield return new WaitForSeconds(1f / introData.typeSpeed);
                }
            }

            isTyping = false;
            
            if (continueButton != null)
                continueButton.gameObject.SetActive(true);
        }

        private void OnContinue()
        {
            if (!isActive) return;

            if (isTyping)
            {
                if (typeCoroutine != null)
                    StopCoroutine(typeCoroutine);
                    
                if (textDisplay != null && currentTextIndex < introData.introTexts.Length)
                    textDisplay.text = introData.introTexts[currentTextIndex];
                    
                isTyping = false;
                
                if (continueButton != null)
                    continueButton.gameObject.SetActive(true);
            }
            else
            {
                currentTextIndex++;
                ShowCurrentText();
            }
        }

        private void EndIntro()
        {
            isActive = false;
            
            if (typeCoroutine != null)
            {
                StopCoroutine(typeCoroutine);
                typeCoroutine = null;
            }
            
            if (panel != null)
                panel.SetActive(false);

            string sceneName = introData?.gameplaySceneName ?? "SampleScene";
            SceneManager.LoadScene(sceneName);
        }

        public void SkipIntro()
        {
            EndIntro();
        }

        private void OnDestroy()
        {
            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinue);
                
            if (invisibleClickArea != null)
                invisibleClickArea.onClick.RemoveListener(OnContinue);
        }
    }
}