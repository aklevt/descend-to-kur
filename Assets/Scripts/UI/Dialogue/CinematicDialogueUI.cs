using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialogue
{
    public class CinematicDialogueUI : MonoBehaviour, IDialogueView
    {
        [Header("UI Elements")]
        [SerializeField] private CanvasGroup container;
        [SerializeField] private Image portraitImage; 
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private GameObject namePanel; 
        [SerializeField] private Image backgroundOverlay; 
        [SerializeField] private Image dialogueBoxWithFrame;

        [Header("Layout Switcher")]
        [SerializeField] private GameObject leftLayout;          
        [SerializeField] private GameObject rightLayout;         
        [SerializeField] private GameObject rightLayout2;        
        [SerializeField] private TextMeshProUGUI dialogueText2;   

        [Header("RightLayout Texts")]
        [SerializeField] private TextMeshProUGUI dialogueTextTop;
        [SerializeField] private TextMeshProUGUI dialogueTextCenter;

        [Header("Inline Image Settings")]
        [SerializeField] private CanvasGroup inlineImageCanvasGroup; 
        [SerializeField] private Image inlineImage;              
        [SerializeField] private float imageFadeInDuration = 0.3f;

        [Header("Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private bool showBackground = true;

        public bool IsTyping { get; private set; }
        private bool shouldCompleteInstantly;
        private TextMeshProUGUI activeTextComponent;

        private void Awake() {
            gameObject.SetActive(false);
            if(backgroundOverlay != null) backgroundOverlay.enabled = showBackground;
        }

        public IEnumerator Show() {
            if (dialogueBoxWithFrame != null) {
                dialogueBoxWithFrame.gameObject.SetActive(true);
            }

            container.alpha = 0;
            gameObject.SetActive(true);
            float elapsed = 0;
            while (elapsed < fadeInDuration) {
                elapsed += Time.deltaTime;
                container.alpha = elapsed / fadeInDuration;
                yield return null;
            }
            container.alpha = 1;
        }

        public IEnumerator Hide() {
            if (dialogueBoxWithFrame != null) {
                dialogueBoxWithFrame.gameObject.SetActive(false);
            }

            float elapsed = 0;
            while (elapsed < fadeInDuration) {
                elapsed += Time.deltaTime;
                container.alpha = 1 - (elapsed / fadeInDuration);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        public IEnumerator DisplayLine(DialogueLine line, float typewriterSpeed) {
            IsTyping = true;
            shouldCompleteInstantly = false;

            bool hasSpeaker = !string.IsNullOrEmpty(line.speakerName);

            if (dialogueTextTop != null) dialogueTextTop.gameObject.SetActive(false);
            if (dialogueTextCenter != null) dialogueTextCenter.gameObject.SetActive(false);

            if (inlineImageCanvasGroup != null) {
                inlineImageCanvasGroup.alpha = 0f;
                inlineImageCanvasGroup.gameObject.SetActive(false);
            }

            if (hasSpeaker) {
                if (leftLayout != null) leftLayout.SetActive(true);
                if (rightLayout != null) rightLayout.SetActive(false);
                if (rightLayout2 != null) rightLayout2.SetActive(true);
                
                activeTextComponent = dialogueText2;

                if (namePanel != null) namePanel.SetActive(true);
                if (speakerNameText != null) speakerNameText.text = line.speakerName;

                if (portraitImage != null) {
                    portraitImage.sprite = line.speakerPortrait;
                    portraitImage.gameObject.SetActive(line.speakerPortrait != null);
                }
            }
            else {
                if (leftLayout != null) leftLayout.SetActive(false);
                if (rightLayout != null) rightLayout.SetActive(true);
                if (rightLayout2 != null) rightLayout2.SetActive(false);
                
                if (line.textAlignment == DialogueTextAlignment.Center) {
                    activeTextComponent = dialogueTextCenter;
                }
                else {
                    activeTextComponent = dialogueTextTop;
                }

                if (activeTextComponent != null) {
                    activeTextComponent.gameObject.SetActive(true);
                }

                if (namePanel != null) namePanel.SetActive(false);
            }

            bool canShowInlineImg = !hasSpeaker && line.showInlineImage && line.speakerPortrait != null && inlineImageCanvasGroup != null && inlineImage != null;

            if (canShowInlineImg) {
                inlineImage.sprite = line.speakerPortrait;
            }

            if (activeTextComponent != null && !string.IsNullOrEmpty(line.text)) {
                activeTextComponent.text = line.text;
                activeTextComponent.maxVisibleCharacters = 0;
                
                int totalChars = line.text.Length;
                for (int i = 0; i <= totalChars; i++) {
                    if (shouldCompleteInstantly) {
                        activeTextComponent.maxVisibleCharacters = totalChars;
                        break;
                    }
                    activeTextComponent.maxVisibleCharacters = i;
                    yield return new WaitForSeconds(1f / typewriterSpeed);
                }
            }
            else if (activeTextComponent != null) {
                activeTextComponent.text = string.Empty;
            }

            if (canShowInlineImg) {
                inlineImageCanvasGroup.gameObject.SetActive(true);
                
                if (shouldCompleteInstantly) {
                    inlineImageCanvasGroup.alpha = 1f;
                }
                else {
                    float elapsed = 0f;
                    while (elapsed < imageFadeInDuration) {
                        if (shouldCompleteInstantly) {
                            break;
                        }
                        elapsed += Time.deltaTime;
                        inlineImageCanvasGroup.alpha = elapsed / imageFadeInDuration;
                        yield return null;
                    }
                    inlineImageCanvasGroup.alpha = 1f;
                }
            }

            IsTyping = false;
        }

        public void CompleteCurrentLine() => shouldCompleteInstantly = true;
        
        public void ToggleBackground(bool active) {
            showBackground = active;
            if(backgroundOverlay != null) backgroundOverlay.enabled = active;
        }
    }
}