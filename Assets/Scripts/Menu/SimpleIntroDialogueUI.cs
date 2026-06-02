using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialogue
{
    /// <summary>
    /// Упрощенный диалоговый UI для интро
    /// </summary>
    public class SimpleIntroDialogueUI : MonoBehaviour, IDialogueView
    {
        [Header("UI Elements")] [SerializeField]
        private CanvasGroup container;

        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Button continueButton;
        [SerializeField] private GameObject continueIndicator;

        [Header("Animation")] [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float textFadeDuration = 0.3f;

        public bool IsTyping { get; private set; }
        public bool WaitingForInput { get; private set; }

        private bool shouldCompleteInstantly;
        private string currentFullText;

        private Coroutine showCoroutine;
        private Coroutine hideCoroutine;
        private Coroutine displayCoroutine;

        private void Awake()
        {
            gameObject.SetActive(false);

            if (container != null)
            {
                container.alpha = 0f;
                container.gameObject.SetActive(false);
            }

            ClearPanel();

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinuePressed);
            }
        }

        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinuePressed);
            }
        }

        private void ClearPanel()
        {
            if (dialogueText != null)
            {
                dialogueText.text = "";
                dialogueText.maxVisibleCharacters = 0;
                dialogueText.alpha = 0f;
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }

            if (continueIndicator != null)
            {
                continueIndicator.SetActive(false);
            }

            WaitingForInput = false;
        }

        private void OnContinuePressed()
        {
            if (IsTyping)
            {
                CompleteCurrentLine();
            }
            else if (WaitingForInput)
            {
                WaitingForInput = false;
            }
        }

        public IEnumerator Show()
        {
            StopAllActiveCoroutines();

            if (container == null) yield break;

            ClearPanel();

            container.alpha = 0f;
            gameObject.SetActive(true);
            container.gameObject.SetActive(true);

            var elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / fadeInDuration;
                container.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            container.alpha = 1f;
            showCoroutine = null;
        }

        public IEnumerator Hide()
        {
            StopAllActiveCoroutines();

            if (container == null) yield break;

            var elapsed = 0f;
            var startAlpha = container.alpha;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / fadeOutDuration;
                container.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            container.alpha = 0f;
            container.gameObject.SetActive(false);
            ClearPanel();
            gameObject.SetActive(false);

            hideCoroutine = null;
        }

        public IEnumerator DisplayLine(DialogueLine line, float typewriterSpeed)
        {
            IsTyping = true;
            shouldCompleteInstantly = false;
            currentFullText = line.text;

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }

            if (continueIndicator != null)
            {
                continueIndicator.SetActive(false);
            }


            if (dialogueText != null)
            {
                dialogueText.alpha = 0f;
                var elapsed = 0f;
                while (elapsed < textFadeDuration)
                {
                    elapsed += Time.deltaTime;
                    dialogueText.alpha = Mathf.Lerp(0f, 1f, elapsed / textFadeDuration);
                    yield return null;
                }

                dialogueText.alpha = 1f;
            }


            dialogueText.text = currentFullText;
            dialogueText.ForceMeshUpdate();

            var totalChars = dialogueText.textInfo.characterCount;
            dialogueText.maxVisibleCharacters = 0;

            for (var i = 0; i <= totalChars; i++)
            {
                if (shouldCompleteInstantly)
                {
                    dialogueText.maxVisibleCharacters = totalChars;
                    break;
                }

                dialogueText.maxVisibleCharacters = i;

                var delay = 1f / typewriterSpeed;

                if (i < totalChars)
                {
                    var c = dialogueText.textInfo.characterInfo[i].character;
                    delay = GetCharacterDelay(c, delay);
                }

                yield return new WaitForSeconds(delay);
            }

            IsTyping = false;


            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
            }

            if (continueIndicator != null)
            {
                continueIndicator.SetActive(true);
            }

            WaitingForInput = true;

            displayCoroutine = null;
        }

        public void CompleteCurrentLine()
        {
            shouldCompleteInstantly = true;

            if (dialogueText != null && dialogueText.textInfo != null)
            {
                dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
            }

            IsTyping = false;

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
            }

            if (continueIndicator != null)
            {
                continueIndicator.SetActive(true);
            }
        }

        private void StopAllActiveCoroutines()
        {
            if (showCoroutine != null)
            {
                StopCoroutine(showCoroutine);
                showCoroutine = null;
            }

            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }

            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
                displayCoroutine = null;
            }

            IsTyping = false;
            shouldCompleteInstantly = false;
        }

        private float GetCharacterDelay(char character, float baseDelay)
        {
            return character switch
            {
                '.' or '!' or '?' => baseDelay * 3f,
                ',' or ';' => baseDelay * 2f,
                _ when char.IsWhiteSpace(character) => 0f,
                _ => baseDelay
            };
        }
    }
}