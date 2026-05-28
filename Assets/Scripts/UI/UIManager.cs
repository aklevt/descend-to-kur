using UI.Dialogue;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Управляет UI-окнами
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private PauseMenu pauseMenu;
        
        [Header("Overlay")]
        [SerializeField] private CanvasGroup darkenOverlay;
        [SerializeField] private float overlayAlpha = 0.5f;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            if (pauseMenu != null)
                pauseMenu.Hide();
                
            if (darkenOverlay != null)
            {
                darkenOverlay.alpha = 0f;
                darkenOverlay.gameObject.SetActive(false);
                darkenOverlay.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// Обработка нажатия ESC
        /// </summary>
        public void HandleEscapePress()
        {
            if (Core.GameStateManager.Instance == null) return;
    
            if (Core.GameStateManager.Instance.CurrentState == Core.GameState.Transition) return;

            var state = Core.GameStateManager.Instance.CurrentState;

            switch (state)
            {
                case Core.GameState.Paused:
                    ClosePause();
                    break;

                case Core.GameState.Gameplay:
                    OpenPause();
                    break;

                case Core.GameState.GameOver:
                case Core.GameState.Transition:
                    TransitionScreenManager.Instance?.SkipScreen();
                    break;

                case Core.GameState.Dialog:
                    DialogueManager.Instance?.SkipDialogue();
                    break;
                case Core.GameState.Tutorial:
                    break;
            }
        }

        /// <summary>
        /// Переключить паузу (ESC)
        /// </summary>
        public void TogglePause()
        {
            if (Core.GameStateManager.Instance == null) return;

            if (Core.GameStateManager.Instance.CurrentState == Core.GameState.Paused)
            {
                ClosePause();
            }
            else if (Core.GameStateManager.Instance.CanPause)
            {
                OpenPause();
            }
        }

        private void OpenPause()
        {
            if (pauseMenu != null)
                pauseMenu.Show();
            
            ShowDarkenOverlay();
            
            Core.GameStateManager.Instance?.SetState(Core.GameState.Paused);
        }

        private void ClosePause()
        {
            if (pauseMenu != null)
                pauseMenu.Hide();
            
            HideDarkenOverlay();
            
            Core.GameStateManager.Instance?.SetState(Core.GameState.Gameplay);
        }

        private void ShowDarkenOverlay()
        {
            if (darkenOverlay != null)
            {
                darkenOverlay.alpha = overlayAlpha;
                darkenOverlay.gameObject.SetActive(true);
            }
        }

        private void HideDarkenOverlay()
        {
            if (darkenOverlay != null)
            {
                darkenOverlay.alpha = 0f;
                darkenOverlay.gameObject.SetActive(false);
            }
        }


        // public void ShowDialog(DialogData data) 
        // { 
        //     Core.GameStateManager.Instance?.SetState(Core.GameState.Dialog);
        // }
        // 
        // public void CloseDialog() 
        // { 
        //     Core.GameStateManager.Instance?.ReturnToGameplay();
        // }
    }
}