using Abilities;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Entities;

namespace UI
{
    /// <summary>
    /// Слот способности
    /// </summary>
    public class AbilityButtonSlot : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Image slotBackground;
        [SerializeField] private Button button;
        [SerializeField] private Image itemImage;
        [SerializeField] private GameObject energyContainer;
        
        [SerializeField] private RectTransform energyRectTransform; 
        [SerializeField] private float normalWidth = 100f;       
        [SerializeField] private float selectedWidth = 150f;     
        
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI hotkeyText;
        [SerializeField] private TextMeshProUGUI costText;

        [Header("Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float normalAlpha = 60f / 255f;
        [Range(0f, 1f)]
        [SerializeField] private float selectedAlpha = 1f;
        [SerializeField] private Sprite unavailableSprite;
        [SerializeField] private Color warningColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color buttonOriginalColor = new Color(0.4339623f, 324689f, 0.1862763f);
        [SerializeField] private float flashDuration = 0.15f;

        #endregion

        #region Private Fields

        private AbilityData currentAbility;
        private int currentIndex;
        private Coroutine flashCoroutine;
        private Image buttonImage;
        private bool isAvailable;
        private Sprite originalSprite;

        #endregion

        #region Properties

        public bool IsSelected { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            buttonImage = button.GetComponent<Image>();
        }

        #endregion

        #region Public API

        public void Setup(AbilityData ability, int index)
        {
            currentAbility = ability;
            currentIndex = index; 
            isAvailable = true;

            if (itemImage != null)
                originalSprite = itemImage.sprite;

            UpdateTexts(ability, index);
            ResetSelectionState();
            SetupButtonListener();
            ShowAsAvailable();
        }

        public void Hide(int index)
        {
            currentAbility = null;
            isAvailable = false;
            
            if (hotkeyText != null)
                hotkeyText.text = ((index % 9) + 1).ToString();
            
            ShowAsUnavailable();
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;

            if (!isAvailable)
            {
                IsSelected = false;
                return;
            }

            if (slotBackground != null)
            {
                var targetAlpha = selected ? selectedAlpha : normalAlpha;
                SetAlpha(slotBackground, targetAlpha);
            }
            
            if (energyRectTransform != null)
            {
                var targetWidth = selected ? selectedWidth : normalWidth;
                energyRectTransform.sizeDelta = new Vector2(targetWidth, energyRectTransform.sizeDelta.y);
            }
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
                button.interactable = value && isAvailable;
        }

        public void TriggerFlash()
        {
            if (isAvailable)
                PlayWarningEffect();
        }

        #endregion

        #region Private Methods

        private void UpdateTexts(AbilityData ability, int index)
        {
            if (nameText != null)
                nameText.text = ability.abilityName;

            if (hotkeyText != null)
                hotkeyText.text = ((index % 9) + 1).ToString();

            if (costText != null && energyContainer != null)
            {
                var hasCost = ability.energyCost > 0;
                energyContainer.SetActive(hasCost);
                if (hasCost)
                    costText.text = ability.energyCost.ToString();
            }
        }

        private void ResetSelectionState()
        {
            StopFlashIfRunning();
            IsSelected = false;
            
            if (slotBackground != null)
                SetAlpha(slotBackground, normalAlpha);
            
            if (energyRectTransform != null)
                energyRectTransform.sizeDelta = new Vector2(normalWidth, energyRectTransform.sizeDelta.y);
        }

        private void SetupButtonListener()
        {
            if (button == null) return;
            
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClick);
        }

        private void ShowAsAvailable()
        {
            if (nameText != null)
                nameText.gameObject.SetActive(true);

            if (energyContainer != null)
            {
                var hasCost = currentAbility != null && currentAbility.energyCost > 0;
                energyContainer.SetActive(hasCost);
            }

            if (itemImage != null && originalSprite != null)
                itemImage.sprite = originalSprite;

            if (slotBackground != null)
                SetAlpha(slotBackground, normalAlpha);

            if (button != null)
                button.interactable = true;
        }

        private void ShowAsUnavailable()
        {
            if (nameText != null)
                nameText.gameObject.SetActive(false);

            if (energyContainer != null)
                energyContainer.SetActive(false);
            
            if (hotkeyText != null)
                hotkeyText.gameObject.SetActive(true);

            if (itemImage != null && unavailableSprite != null)
                itemImage.sprite = unavailableSprite;

            if (slotBackground != null)
                SetAlpha(slotBackground, normalAlpha);

            if (button != null)
                button.interactable = false;
            
            if (energyRectTransform != null)
                energyRectTransform.sizeDelta = new Vector2(normalWidth, energyRectTransform.sizeDelta.y);

            IsSelected = false;
        }

        private void OnButtonClick()
        {
            if (!isAvailable)
                return;

            var shouldFlash = ShouldShowWarning();

            if (shouldFlash)
                PlayWarningEffect();

            if (AbilityController.Instance != null)
                AbilityController.Instance.SelectAbilityByIndex(currentIndex);

            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        private bool ShouldShowWarning()
        {
            if (PlayerMovement.Instance == null || currentAbility == null)
                return false;

            var stats = PlayerMovement.Instance.Stats;

            if (currentAbility is MoveAbilityData)
                return stats.RemainingSteps <= 0 || stats.Energy <= 0;

            return !stats.HasEnergyForAction(currentAbility.energyCost);
        }

        private void SetAlpha(Image image, float alpha)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private void PlayWarningEffect()
        {
            if (buttonImage == null) return;
            
            StopFlashIfRunning();
            flashCoroutine = StartCoroutine(FlashRoutine());
        }

        private void StopFlashIfRunning()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
                
                if (buttonImage != null)
                    buttonImage.color = Color.white;
            }
        }

        private IEnumerator FlashRoutine()
        {
            // Color originalColor = buttonImage.color;
            
            buttonImage.color = warningColor;
            yield return new WaitForSeconds(flashDuration);
            buttonImage.color = buttonOriginalColor;
            
            flashCoroutine = null;
        }

        #endregion
    }
}