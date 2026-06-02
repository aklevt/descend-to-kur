// --- FILE: Assets/Scripts/UI/PauseMenu.cs ---
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

        [Header("Scene Names")] 
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Audio")] 
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TextMeshProUGUI volumeText;
        [SerializeField] private float musicFadeDuration = 0.5f;

        private void Start()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);

            SetupAudio();
        }

        public void Show()
        {
            SyncSliderWithTrack();

            if (panel != null)
                panel.SetActive(true);
            
            // Вызов приглушения убран. Музыка продолжает играть как раньше.
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
            
            // Вызов возврата громкости убран.
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

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopMusic(musicFadeDuration);
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void TogglePause()
        {
            UIManager.Instance?.TogglePause();
        }

        #region Audio

        private void SetupAudio()
        {
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                var savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
                volumeSlider.SetValueWithoutNotify(savedVolume);
                UpdateVolumeText();
            }
        }

        private void SyncSliderWithTrack()
        {
            if (volumeSlider == null) return;
            var savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.SetValueWithoutNotify(savedVolume);
            UpdateVolumeText();
        }

        private void OnVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetVolume(value);
            }

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
            SyncSliderWithTrack();
        }

        #endregion
    }
}