using Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace UI.Menu
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        // [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Settings")]
        [SerializeField] private string gameplaySceneName = "Gameplay";

        private void Start()
        {
            continueButton.onClick.AddListener(OnContinue);
            newGameButton.onClick.AddListener(OnNewGame);
            // settingsButton.onClick.AddListener(OnSettings);
            exitButton.onClick.AddListener(OnExit);

            UpdateContinueButton();
            
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void UpdateContinueButton()
        {
            var hasSave = SaveSystem.HasSave();
            continueButton.interactable = hasSave;
        }

        private void OnContinue()
        {
            if (!SaveSystem.HasSave())
            {
                Debug.LogWarning("[MainMenu] Нет сохранения для продолжения");
                return;
            }

            SaveSystem.LoadGame();
            
            LoadGameplayScene();
        }

        private void OnNewGame()
        {
            SaveSystem.ClearSave();
            SaveSystem.StartNewGame();
            
            LoadGameplayScene();
        }

        // private void OnSettings()
        // {
        //     if (settingsPanel != null)
        //         settingsPanel.SetActive(true);
        // }

        private void OnExit()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private void LoadGameplayScene()
        {
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void CloseSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }
    }
}