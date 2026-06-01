using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button restartButton;

        [Header("Scene Names")] [SerializeField]
        private string mainMenuSceneName = "MainMenu";

        [Header("Audio")] [SerializeField] private Slider volumeSlider;
        [SerializeField] private TextMeshProUGUI volumeText;

        private void Start()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);

            SetupAudio();
            // Hide();
        }

        public void Show()
        {
            if (panel != null)
                panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        private void OnResumeClicked()
        {
            UIManager.Instance?.TogglePause();
        }

        private void OnRestartClicked()
        {
            Debug.Log("[PauseMenu] Комната перезапускается");

            UIManager.Instance?.TogglePause();

            Core.LevelController.Instance?.RestartCurrentRoom();
        }

        private void OnMainMenuClicked()
        {
            Debug.Log("[PauseMenu] Возврат в главное меню");

            Core.SaveSystem.SaveGame();

            Time.timeScale = 1f;

            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void TogglePause()
        {
            UIManager.Instance?.TogglePause();
        }


        //         private void OnMainMenuClicked()
        //         {
        //             Debug.Log("[PauseMenu] Quit button pressed");
        //             Application.Quit();
        //             
        // #if UNITY_EDITOR
        //             UnityEditor.EditorApplication.isPlaying = false;
        // #endif
        //         }

        #region Audio

        private void SetupAudio()
        {
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

                LoadAudioSettings();
            }
        }

        /// <summary>
        /// Загрузка сохраненных настроек звука
        /// </summary>
        private void LoadAudioSettings()
        {
            if (volumeSlider == null) return;

            var savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);

            volumeSlider.SetValueWithoutNotify(savedVolume);

            AudioListener.volume = savedVolume;

            UpdateVolumeText();
        }

        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat("MasterVolume", value);
            PlayerPrefs.Save();
            UpdateVolumeText();
        }

        private void UpdateVolumeText()
        {
            if (volumeText != null)
                volumeText.text = $"{(int)(volumeSlider.value * 100)}%";
        }

        private void OnEnable()
        {
            if (volumeSlider != null && PlayerPrefs.HasKey("MasterVolume"))
            {
                volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            }
        }

        #endregion
    }
}