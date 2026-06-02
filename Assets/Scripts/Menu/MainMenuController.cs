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
        [SerializeField] private Button exitButton;
        
        [Header("Intro")]
        [SerializeField] private SimpleIntroPanel introPanel;
        [SerializeField] private IntroData fallbackIntroData;
        
        private void Start()
        {
            continueButton.onClick.AddListener(OnContinue);
            newGameButton.onClick.AddListener(OnNewGame);
            exitButton.onClick.AddListener(OnExit);

            UpdateContinueButton();
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
            var sceneName = "SampleScene";
    
            if (Core.SceneTransitioner.Instance != null)
            {
                Core.SceneTransitioner.Instance.SwitchScene(sceneName);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}