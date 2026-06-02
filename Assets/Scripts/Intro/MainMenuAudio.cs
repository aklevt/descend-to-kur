
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MainMenuAudio : MonoBehaviour
    {
        [Header("Audio Components")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TextMeshProUGUI volumeText;

        private void Start()
        {
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                LoadAudioSettings();
            }
        }

        /// <summary>
        /// Загрузка настроек при старте игры/меню
        /// </summary>
        private void LoadAudioSettings()
        {
            if (volumeSlider == null) return;

            
            var savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);

            volumeSlider.SetValueWithoutNotify(savedVolume);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetVolume(savedVolume);
            }

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
            
            if (volumeSlider != null && PlayerPrefs.HasKey("MasterVolume"))
            {
                var savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
                volumeSlider.value = savedVolume;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetVolume(savedVolume);
                }
            }
        }
    }
}