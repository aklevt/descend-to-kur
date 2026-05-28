using UnityEngine;
using System.Collections;
using TMPro;

namespace UI
{
    /// <summary>
    /// Отвечает за управление UI-элементы, подписывается на соответствующие события
    /// </summary>
    public class UIController : MonoBehaviour
    {
        public static UIController Instance { get; private set; }

        [Header("Popups")] 
        [SerializeField] private GameObject warningPopup;
        [SerializeField] private TextMeshProUGUI warningText;

        [Header("Durations")] 
        [SerializeField] private float popupDuration = 1.0f;

        private Coroutine currentPopupCoroutine;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            HideAllPopups();
        }

        /// <summary>
        /// Показать предупреждение о нехватке энергии
        /// </summary>
        public void ShowEnergyWarning()
        {
            ShowWarning("Недостаточно энергии!");
        }
        
        public void ShowStepsWarning()
        {
            ShowWarning("Недостаточно шагов!");
        }
        
        public void ShowStepsEndedWarning()
        {
            ShowWarning("Доступные шаги закончились!");
        }
        
        public void ShowWarning(string message)
        {
            if (warningPopup == null) return;
            if (isSuppressed) return;

            if (warningText != null)
            {
                warningText.text = message;
            }

            if (currentPopupCoroutine != null)
            {
                StopCoroutine(currentPopupCoroutine);
            }

            currentPopupCoroutine = StartCoroutine(ShowPopupRoutine(warningPopup, popupDuration));
        }

        private IEnumerator ShowPopupRoutine(GameObject popup, float duration)
        {
            popup.SetActive(true);
            yield return new WaitForSeconds(duration);
            popup.SetActive(false);
        }

        private void HideAllPopups()
        {
            if (warningPopup != null)
                warningPopup.SetActive(false);
        }
        
        public void HideWarning()
        {
            if (currentPopupCoroutine != null)
            {
                StopCoroutine(currentPopupCoroutine);
                currentPopupCoroutine = null;
            }
            HideAllPopups();
        }
        private bool isSuppressed = false;

        public void SuppressPopups()
        {
            isSuppressed = true;
            HideWarning();
        }

        public void UnsuppressPopups()
        {
            isSuppressed = false;
        }

    }
}