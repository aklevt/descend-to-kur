
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UI.Dialogue;

namespace Intro
{
    /// <summary>
    /// Управляет последовательностью интро
    /// </summary>
    public class IntroManager : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private NarrativeUI narrativeUI;
        [SerializeField] private ImageDisplayUI imageUI;
        [SerializeField] private GameObject skipButton;
        
        [Header("Data")]
        [SerializeField] private IntroSequence introSequence;

        private int currentStepIndex = 0;
        private bool isPlaying = false;
        private bool shouldSkip = false;
        private Coroutine playCoroutine;

        private void Awake()
        {
            if (narrativeUI != null)
            {
                narrativeUI.SetClickListener(OnContinueClicked);
            }
            
            if (imageUI != null)
            {
                imageUI.SetClickListener(OnContinueClicked);
            }
        }

        public void StartIntro()
        {
            if (introSequence == null)
            {
                Debug.LogError("[IntroManager] IntroSequence не назначен!");
                LoadGameScene();
                return;
            }

            if (skipButton != null)
                skipButton.SetActive(introSequence.canSkip);

            playCoroutine = StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            isPlaying = true;
            shouldSkip = false;
            currentStepIndex = 0;

            while (currentStepIndex < introSequence.steps.Count && !shouldSkip)
            {
                var step = introSequence.steps[currentStepIndex];
                
                yield return ProcessStep(step);
                
                if (shouldSkip) break;
                
                if (step.delayAfter > 0f)
                {
                    yield return new WaitForSeconds(step.delayAfter);
                }
                
                currentStepIndex++;
            }

            isPlaying = false;
            LoadGameScene();
        }

        private IEnumerator ProcessStep(IntroStep step)
        {
            switch (step.type)
            {
                case StepType.Narrative:
                    yield return ShowNarrative(step);
                    break;
                    
                case StepType.Dialogue:
                    yield return ShowDialogue(step);
                    break;
                    
                case StepType.Image:
                    yield return ShowImage(step);
                    break;
            }
        }

        private IEnumerator ShowNarrative(IntroStep step)
        {
            if (narrativeUI == null) yield break;

            yield return narrativeUI.FadeIn();
            
            yield return narrativeUI.ShowText(step.narrativeText, introSequence.narrativeTypeSpeed);
            
            
            yield return new WaitUntil(() => !narrativeUI.WaitingForInput || shouldSkip);
            
            yield return narrativeUI.FadeOut();
        }

        private IEnumerator ShowDialogue(IntroStep step)
        {
            if (step.dialogue == null)
            {
                Debug.LogWarning("[IntroManager] DialogueData отсутствует в шаге!");
                yield break;
            }

            
            var dialogueManager = DialogueManager.Instance;
            if (dialogueManager == null)
            {
                Debug.LogError("[IntroManager] DialogueManager не найден!");
                yield break;
            }

            dialogueManager.StartDialogue(step.dialogue);
            
            
            yield return new WaitUntil(() => !dialogueManager.IsDialogueActive || shouldSkip);
        }

        private IEnumerator ShowImage(IntroStep step)
        {
            if (imageUI == null) yield break;

            yield return imageUI.Show(step.image, step.imageCaption);
            
            
            yield return new WaitUntil(() => !imageUI.WaitingForInput || shouldSkip);
            
            yield return imageUI.Hide();
        }

        private void OnContinueClicked()
        {
            if (!isPlaying) return;

            
            if (narrativeUI != null && narrativeUI.IsTyping)
            {
                narrativeUI.CompleteText();
            }
            
            else if (narrativeUI != null && narrativeUI.WaitingForInput)
            {
                narrativeUI.WaitingForInput = false;
            }
            else if (imageUI != null && imageUI.WaitingForInput)
            {
                imageUI.WaitingForInput = false;
            }
        }

        public void SkipIntro()
        {
            if (!isPlaying || !introSequence.canSkip) return;

            shouldSkip = true;
            
            
            var dialogueManager = DialogueManager.Instance;
            if (dialogueManager != null && dialogueManager.IsDialogueActive)
            {
                dialogueManager.SkipDialogue();
            }
        }

        private void LoadGameScene()
        {
            var sceneName = introSequence?.sceneToLoad ?? "SampleScene";
            SceneManager.LoadScene(sceneName);
        }
    }
}