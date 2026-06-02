using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialogue
{
    public class InGameDialogueUI : BaseDialogueView
    {
        [Header("UI Elements")]
        [SerializeField] private CanvasGroup dialoguePanel;
        [SerializeField] private Image speakerNameBackground;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private GameObject continueIndicator;
        [SerializeField] private Button clickCatcherButton;

        protected override void InitializeView()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.alpha = 0f;
                dialoguePanel.gameObject.SetActive(false);
            }
            ClearUI();
        }

        protected override void SetupInputListeners()
        {
            if (clickCatcherButton != null)
                clickCatcherButton.onClick.AddListener(HandleClick);
        }

        protected override void CleanupInputListeners()
        {
            if (clickCatcherButton != null)
                clickCatcherButton.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            DialogueManager.Instance?.AdvanceDialogue();
        }

        protected override void ClearUI()
        {
            if (dialogueText != null)
            {
                dialogueText.text = "";
                dialogueText.maxVisibleCharacters = 0;
            }

            if (speakerNameText != null)
            {
                speakerNameText.text = "";
            }

            if (speakerNameBackground != null)
            {
                speakerNameBackground.gameObject.SetActive(false);
            }

            if (continueIndicator != null)
                continueIndicator.SetActive(false);
        }

        protected override IEnumerator ShowAnimation()
        {
            if (dialoguePanel == null) yield break;

            dialoguePanel.gameObject.SetActive(true);
            
            var elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                dialoguePanel.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            dialoguePanel.alpha = 1f;
        }

        protected override IEnumerator HideAnimation()
        {
            if (dialoguePanel == null) yield break;

            var elapsed = 0f;
            var startAlpha = dialoguePanel.alpha;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                dialoguePanel.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            
            dialoguePanel.alpha = 0f;
            dialoguePanel.gameObject.SetActive(false);
        }

        protected override IEnumerator DisplayLineAnimation(DialogueLine line, float typewriterSpeed)
        {
            SetupSpeakerName(line.speakerName);
            
            if (continueIndicator != null)
                continueIndicator.SetActive(false);

            yield return TypewriterEffect(dialogueText, line.text, typewriterSpeed);
        }

        protected override void OnLineCompleted()
        {
            if (dialogueText != null && dialogueText.textInfo != null)
            {
                dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
            }
            
            if (continueIndicator != null)
                continueIndicator.SetActive(true);
        }

        private void SetupSpeakerName(string speakerName)
        {
            if (speakerNameBackground != null)
            {
                if (string.IsNullOrEmpty(speakerName))
                {
                    speakerNameBackground.gameObject.SetActive(false);
                }
                else
                {
                    speakerNameBackground.gameObject.SetActive(true);
                    
                    if (speakerNameText != null)
                    {
                        speakerNameText.text = speakerName;
                    }
                }
            }
            else if (speakerNameText != null)
            {
                if (string.IsNullOrEmpty(speakerName))
                {
                    speakerNameText.gameObject.SetActive(false);
                }
                else
                {
                    speakerNameText.gameObject.SetActive(true);
                    speakerNameText.text = speakerName;
                }
            }
        }
    }
}