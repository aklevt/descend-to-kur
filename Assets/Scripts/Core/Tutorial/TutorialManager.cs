using UnityEngine;
using UI.Tutorial;

namespace Core.Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [SerializeField] private TutorialOverlayUI tutorialUI;
        
        [Header("Debug")]
        [Tooltip("Дефолтный туториал для вызова через кнопку в меню/интерфейсе")]
        [SerializeField] private TutorialData defaultTutorialData;
        
        [SerializeField] private float cameraTutorialXOffset = -2f;

        private TutorialData currentTutorial;
        private int currentStepIndex;
        private bool isActive;
        
        private bool isTriggeredByButton; 

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (tutorialUI != null)
            {
                tutorialUI.Initialize(this);
            }
        }

        /// <summary>
        /// Универсальный запуск туториала с флагом источника.
        /// </summary>
        public void StartTutorial(TutorialData data, bool isFromButton = false)
        {
            if (data == null)
            {
                return;
            }

            currentTutorial = data;
            currentStepIndex = 0;
            isActive = true;
            isTriggeredByButton = isFromButton;

            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.SetTutorialOffset(cameraTutorialXOffset);
            }

            Debug.Log($"[TutorialManager] Туториал '{data.name}' запущен. Вызов через кнопку интерфейса: {isTriggeredByButton}");

            UpdateStepDisplay();
        }

        public void StartDefaultTutorialFromButton()
        {
            if (defaultTutorialData != null)
            {
                StartTutorial(defaultTutorialData, isFromButton: true);
            }
        }

        public void NextStep()
        {
            if (!isActive || currentTutorial == null) return;

            if (currentStepIndex < currentTutorial.steps.Count - 1)
            {
                currentStepIndex++;
                UpdateStepDisplay();
            }
            else
            {
                StopTutorial();
            }
        }

        public void PreviousStep()
        {
            if (!isActive || currentTutorial == null) return;

            if (currentStepIndex > 0)
            {
                currentStepIndex--;
                UpdateStepDisplay();
            }
        }

        public void StopTutorial()
        {
            isActive = false;
            currentTutorial = null;
            currentStepIndex = 0;
            isTriggeredByButton = false; 

            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.SetTutorialOffset(0f);
            }

            if (tutorialUI != null)
            {
                tutorialUI.Hide();
            }
        }

        private void UpdateStepDisplay()
        {
            if (tutorialUI == null || currentTutorial == null) return;

            var step = currentTutorial.steps[currentStepIndex];
            
            bool hasNext = currentStepIndex < currentTutorial.steps.Count - 1;
            bool hasBack = currentStepIndex > 0;

            tutorialUI.Show(
                step.speakerName, 
                step.text, 
                step.speakerAvatar, 
                hasNext, 
                hasBack
            );

            if (!isTriggeredByButton && step.hasAutoTrigger && !string.IsNullOrEmpty(step.triggerName))
            {
                // Для интерактива
            }
        }
    }
}