using Entities;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.HUD
{
    /// <summary>
    /// Отображает детальную информацию о враге при наведении
    /// </summary>
    public class EnemyInfoDisplay : MonoBehaviour
    {
        [Header("UI Element Containers")]
        [SerializeField] private GameObject nameContainer;
        
        [SerializeField] private GameObject healthTextContainer;
        
        [SerializeField] private GameObject damageContainer;
        
        [SerializeField] private GameObject altHintContainer;

        [Header("UI Text Components (For Data Updates)")]
        [SerializeField] private TextMeshProUGUI enemyNameText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI damageText;
        
        private BaseEntity entity;
        private Health healthComponent;
        
        private void Awake()
        {
            entity = GetComponentInParent<BaseEntity>();
            healthComponent = GetComponentInParent<Health>();
        }

        private void Start()
        {
            Hide();
        }

        public void Show(bool showAltHint, bool inAttackRange)
        {
            if (entity == null) return;
            
            UpdateTextData();

            SetElementsActive(true);

            if (altHintContainer != null)
            {
                altHintContainer.SetActive(showAltHint && !inAttackRange);
            }
        }

        public void Hide()
        {
            SetElementsActive(false);
            
            if (altHintContainer != null) 
                altHintContainer.SetActive(false);
        }
        
        /// <summary>
        /// Переключает состояние видимости конкретных элементов
        /// </summary>
        private void SetElementsActive(bool isActive)
        {
            if (nameContainer != null) nameContainer.SetActive(isActive);
            if (healthTextContainer != null) healthTextContainer.SetActive(isActive);
            if (damageContainer != null) damageContainer.SetActive(isActive);
        }

        /// <summary>
        /// Обновляет текстовые данные из сущности врага
        /// </summary>
        private void UpdateTextData()
        {
            if (enemyNameText != null && entity.Stats != null)
                enemyNameText.text = entity.Stats.Name;
            
            if (healthText != null && healthComponent != null && entity.Stats != null)
            {
                healthText.text = $"{entity.Stats.Health}/{entity.Stats.MaxHealth}";
            }
            
            if (damageText != null && entity.Stats != null)
                damageText.text = $"Урон: {entity.Stats.AttackDamage}";
        }
    }
}