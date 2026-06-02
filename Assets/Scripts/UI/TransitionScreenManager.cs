using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TransitionScreenManager : MonoBehaviour
    {
        public static TransitionScreenManager Instance { get; private set; }

        [Header("Victory Screen")] [SerializeField]
        private GameObject victoryPanel;

        [SerializeField] private TextMeshProUGUI victoryTitle;
        [SerializeField] private Button victoryNextButton;
        
        [SerializeField] private CanvasGroup victoryContainerGroup;
        [SerializeField] private RectTransform victoryLineRect;
        [SerializeField] private float targetLineWidth = 400f; 
        [SerializeField] private float elementsFadeDuration = 0.5f;
        [SerializeField] private AnimationCurve lineBounceCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Defeat Screen")] [SerializeField]
        private GameObject defeatPanel;

        [SerializeField] private TextMeshProUGUI defeatTitle;
        [SerializeField] private Button defeatRetryButton;

        [Header("Black Overlay")] [SerializeField]
        private CanvasGroup blackOverlayGroup;

        [Header("Vignette Settings")] [SerializeField]
        private Image vignetteImage;

        [SerializeField] private Material vignetteMaterial;
        [SerializeField] private float colorTransitionDuration = 0.5f;

        [Header("Vignette Colors")] [SerializeField]
        private Color victoryVignetteColor = Color.white;

        [SerializeField] private Color defeatVignetteColor = new Color(0.67f, 0f, 0f, 1f);
        
        private bool canSkip = false;
        private bool skipRequested = false;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            
            if (vignetteMaterial == null && vignetteImage != null)
            {
                vignetteMaterial = vignetteImage.material;
            }
            
            ResetVignette();
            
            HideAll();
        }
        
        public void ResetVignette()
        {
            if (vignetteMaterial != null)
            {
                vignetteMaterial.SetColor("_MainColor", Color.black);
            }
        }
        
        /// <summary>
        /// Пропустить текущий экран (по ESC)
        /// </summary>
        public void SkipScreen()
        {
            if (canSkip)
            {
                skipRequested = true;
            }
        }

        public IEnumerator ShowVictoryScreen(string title = "ПОБЕДА!", bool isLastRoom = false)
        {
            if (victoryPanel == null)
            {
                yield break;
            }
    
            Core.GameStateManager.Instance?.SetState(Core.GameState.GameOver);
            
            if (victoryContainerGroup != null)
            {
                victoryContainerGroup.alpha = 0f;
            }
            if (victoryLineRect != null)
            {
                victoryLineRect.sizeDelta = new Vector2(0f, victoryLineRect.sizeDelta.y);
            }

            victoryPanel.SetActive(true);
            if (victoryTitle != null)
            {
                victoryTitle.text = title;
                var color = victoryTitle.color;
                color.a = 0f;
                victoryTitle.color = color;
            }

            if (victoryNextButton != null)
            {
                var buttonText = victoryNextButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = isLastRoom ? "В главное меню" : "Продолжить";
                }
            }

            var elementsAnimRoutine = StartCoroutine(AnimateVictoryElements());

            yield return StartCoroutine(ChangeVignetteColorWithTextFade(victoryVignetteColor, victoryTitle));

            yield return StartCoroutine(WaitForVictoryButton());

            if (elementsAnimRoutine != null) StopCoroutine(elementsAnimRoutine);

            yield return StartCoroutine(ChangeVignetteColorWithTextFade(Color.black, victoryTitle, false));

            HideVictoryScreen();
    
            Core.GameStateManager.Instance?.SetState(Core.GameState.Transition);
        }

        private IEnumerator AnimateVictoryElements()
        {
            yield return new WaitForSeconds(0.3f);

            var elapsed = 0f;
            var initialSize = victoryLineRect != null ? victoryLineRect.sizeDelta : Vector2.zero;

            while (elapsed < elementsFadeDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / elementsFadeDuration;

                if (victoryContainerGroup != null)
                {
                    victoryContainerGroup.alpha = t;
                }

                if (victoryLineRect != null)
                {
                    var curveValue = lineBounceCurve.Evaluate(t);
                    var currentWidth = curveValue * targetLineWidth;
                    
                    victoryLineRect.sizeDelta = new Vector2(currentWidth, initialSize.y);
                }

                yield return null;
            }

            if (victoryContainerGroup != null) victoryContainerGroup.alpha = 1f;
            if (victoryLineRect != null) victoryLineRect.sizeDelta = new Vector2(targetLineWidth, initialSize.y);
        }
        
        public IEnumerator ShowDefeatScreen(string title = "ПОРАЖЕНИЕ!")
        {
            if (defeatPanel == null)
            {
                yield break;
            }
            
            Core.GameStateManager.Instance?.SetState(Core.GameState.GameOver);

            defeatPanel.SetActive(true);
            if (defeatTitle != null)
            {
                defeatTitle.text = title;
                var color = defeatTitle.color;
                color.a = 0f;
                defeatTitle.color = color;
            }

            yield return StartCoroutine(ChangeVignetteColorWithTextFade(defeatVignetteColor, defeatTitle));

            yield return StartCoroutine(WaitForDefeatButton());

            yield return StartCoroutine(ChangeVignetteColorWithTextFade(Color.black, defeatTitle, false));

            HideDefeatScreen();
            
            Core.GameStateManager.Instance?.SetState(Core.GameState.Transition);
        }

        private IEnumerator ChangeVignetteColorWithTextFade(Color targetColor, TextMeshProUGUI textToFade,
            bool fadeIn = true)
        {
            if (vignetteMaterial == null) yield break;

            var startColor = vignetteMaterial.GetColor("_MainColor");
            var elapsed = 0f;

            while (elapsed < colorTransitionDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / colorTransitionDuration;

                var newColor = Color.Lerp(startColor, targetColor, t);
                vignetteMaterial.SetColor("_MainColor", newColor);

                if (textToFade != null)
                {
                    var textColor = textToFade.color;
                    textColor.a = fadeIn ? t : (1f - t);
                    textToFade.color = textColor;
                }

                yield return null;
            }

            vignetteMaterial.SetColor("_MainColor", targetColor);

            if (textToFade != null)
            {
                var finalColor = textToFade.color;
                finalColor.a = fadeIn ? 1f : 0f;
                textToFade.color = finalColor;
            }
        }

        private IEnumerator WaitForVictoryButton()
        {
            var buttonPressed = false;

            UnityEngine.Events.UnityAction listener = () =>
            {
                buttonPressed = true;
            };
            victoryNextButton.onClick.AddListener(listener);

            while (!buttonPressed && !skipRequested)
            {
                yield return null;
            }

            victoryNextButton.onClick.RemoveListener(listener);
        }

        private IEnumerator WaitForDefeatButton()
        {
            var buttonPressed = false;

            UnityEngine.Events.UnityAction listener = () =>
            {
                buttonPressed = true;
            };
            defeatRetryButton.onClick.AddListener(listener);

            while (!buttonPressed && !skipRequested)
            {
                yield return null;
            }

            defeatRetryButton.onClick.RemoveListener(listener);
        }

        public IEnumerator FadeFromBlack()
        {
            if (blackOverlayGroup == null)
            {
                yield break;
            }

            var elapsed = 0f;
            
            while (elapsed < colorTransitionDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / colorTransitionDuration;

                blackOverlayGroup.alpha = Mathf.Lerp(1f, 0f, t);
                
                yield return null;
            }

            blackOverlayGroup.alpha = 0f;

            blackOverlayGroup.gameObject.SetActive(false);
        }

        public IEnumerator FadeToBlack(Action duringFade = null)
        {
            if (blackOverlayGroup == null) yield break;

            blackOverlayGroup.gameObject.SetActive(true);
            blackOverlayGroup.alpha = 0f;

            var elapsed = 0f;
            
            while (elapsed < colorTransitionDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / colorTransitionDuration;

                blackOverlayGroup.alpha = Mathf.Lerp(0f, 1f, t);

                yield return null;
            }

            blackOverlayGroup.alpha = 1f;

            duringFade?.Invoke();

            yield return new WaitForSeconds(0.2f);
        }


        public void HideVictoryScreen()
        {
            if (victoryPanel != null)
                victoryPanel.SetActive(false);
        }

        public void HideDefeatScreen()
        {
            if (defeatPanel != null)
                defeatPanel.SetActive(false);
        }

        public void HideAll()
        {
            HideVictoryScreen();
            HideDefeatScreen();
            if (blackOverlayGroup != null)
            {
                blackOverlayGroup.alpha = 0f;
                blackOverlayGroup.gameObject.SetActive(false);
            }
        }
    }
}