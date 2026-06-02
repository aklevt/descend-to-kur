using UnityEngine;
using UI.Dialogue;

namespace Core.Tutorial
{
    public class RoomTutorialTrigger : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private TutorialData tutorialData;
        
        [Header("Optional Condition")]
        [Tooltip("Если нужно подождать завершения конкретного диалога. Если пусто, туториал включится сразу при старте сцены.")]
        [SerializeField] private DialogueData storyDialogueToWait;

        private bool hasTriggered = false;
        private bool wasDialogueDetected = false;

        private void Update()
        {
            if (hasTriggered) return;

            if (storyDialogueToWait != null)
            {
                if (DialogueManager.Instance != null)
                {
                    bool isDialogueActive = DialogueManager.Instance.IsDialogueActive;

                    if (isDialogueActive && !wasDialogueDetected)
                    {
                        wasDialogueDetected = true;
                    }

                    if (wasDialogueDetected && !isDialogueActive)
                    {
                        TriggerTutorial();
                    }
                }
            }
            else
            {
                TriggerTutorial();
            }
        }

        private void TriggerTutorial()
        {
            if (TutorialManager.Instance != null)
            {
                hasTriggered = true;
                TutorialManager.Instance.StartTutorial(tutorialData);
                enabled = false;
            }
        }
    }
}