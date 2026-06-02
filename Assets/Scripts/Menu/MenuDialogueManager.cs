using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UI.Dialogue;

namespace UI.Menu
{
    /// <summary>
    /// Диалоговый менеджер для интро в главном меню
    /// </summary>
    public class MenuDialogueManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private SimpleIntroDialogueUI dialogueUI;
        
        [Header("Intro Settings")]
        [SerializeField] private DialogueData introDialogue;
        [SerializeField] private string gameplaySceneName = "SampleScene";
        [SerializeField] private float typewriterSpeed = 30f;
        
        private bool isDialogueActive = false;
        private bool canSkip = false;

        public void StartIntroAndLoadGame()
        {
            if (introDialogue == null || dialogueUI == null)
            {
                LoadGameplayDirectly();
                return;
            }

            StartCoroutine(IntroSequence());
        }

        private IEnumerator IntroSequence()
        {
            isDialogueActive = true;
            canSkip = false;

            yield return dialogueUI.Show();

            yield return new WaitForSeconds(0.5f);
            
            canSkip = true;

            foreach (var line in introDialogue.lines)
            {
                yield return dialogueUI.DisplayLine(line, typewriterSpeed);
                
                yield return new WaitUntil(() => !dialogueUI.WaitingForInput);
                
                if (line.delayAfter > 0f)
                {
                    yield return new WaitForSeconds(line.delayAfter);
                }
            }

            yield return dialogueUI.Hide();

            isDialogueActive = false;

            LoadGameplayDirectly();
        }

        private void LoadGameplayDirectly()
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
        
        public void SkipIntro()
        {
            if (isDialogueActive && canSkip)
            {
                StopAllCoroutines();
                StartCoroutine(SkipSequence());
            }
        }
        
        private IEnumerator SkipSequence()
        {
            if (dialogueUI != null)
            {
                yield return dialogueUI.Hide();
            }
            LoadGameplayDirectly();
        }
    }
}