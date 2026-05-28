using UnityEngine;

namespace Settings
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [SerializeField] private GameSettings settings;

        public GameSettings Settings => settings;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Получить задержку с учётом множителя скорости
        /// </summary>
        public float GetScaledDelay(float baseDelay)
        {
            return baseDelay / settings.globalAnimationSpeedMultiplier;
        }
    
        /// <summary>
        /// Изменить скорость анимаций (для настроек)
        /// </summary>
        public void SetAnimationSpeed(float multiplier)
        {
            settings.globalAnimationSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 5f);
        }
    }
}