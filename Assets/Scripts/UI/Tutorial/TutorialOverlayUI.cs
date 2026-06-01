using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.Tutorial;

namespace UI.Tutorial
{
    public class TutorialOverlayUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject container;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image avatarImage;

        [Header("Controls")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button closeButton;

        private TutorialManager manager;

        public void Initialize(TutorialManager tutorialManager)
        {
            manager = tutorialManager;

            nextButton.onClick.AddListener(OnNextPressed);
            backButton.onClick.AddListener(OnBackPressed);
            closeButton.onClick.AddListener(OnClosePressed);

            Hide();
        }

        private void OnDestroy()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNextPressed);
            if (backButton != null) backButton.onClick.RemoveListener(OnBackPressed);
            if (closeButton != null) closeButton.onClick.RemoveListener(OnClosePressed);
        }

        public void Show(string title, string description, Sprite avatar, bool hasNext, bool hasBack)
        {
            container.SetActive(true);
            
            if (titleText != null) titleText.text = title;
            if (descriptionText != null) descriptionText.text = description;

            if (avatarImage != null)
            {
                avatarImage.sprite = avatar;
                avatarImage.gameObject.SetActive(avatar != null);
            }

            if (nextButton != null)
            {
                nextButton.interactable = hasNext;
            }

            if (backButton != null)
            {
                backButton.interactable = hasBack;
            }
        }

        public void Hide()
        {
            if (container != null) container.SetActive(false);
        }

        private void OnNextPressed() => manager.NextStep();
        private void OnBackPressed() => manager.PreviousStep();
        private void OnClosePressed() => manager.StopTutorial();
    }
}