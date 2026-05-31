using UnityEngine;
using System.Collections;
using TMPro;

namespace UI
{
    public class UIController : MonoBehaviour
    {
        public static UIController Instance { get; private set; }

        [Header("Popups")] 
        [SerializeField] private GameObject warningPopup;
        [SerializeField] private TextMeshProUGUI warningText;
        [SerializeField] private TextMeshProUGUI warningDescriptionText; 

        [SerializeField] private Transform warningTextTransform;

        [Header("Durations & Animations")] 
        [SerializeField] private float popupDuration = 1.0f;
        [SerializeField] private float fadeDuration = 0.2f;
        [SerializeField] private float floatOffset = 10f;

        private Coroutine currentPopupCoroutine;
        private CanvasGroup warningCanvasGroup;
        private Vector3 originalPosition;
        private bool hasTextTransform;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            InitializeAnimationComponents();
        }

        private void Start()
        {
            HideAllPopups();
        }

        private void InitializeAnimationComponents()
        {
            if (warningPopup == null) return;

            warningCanvasGroup = warningPopup.GetComponent<CanvasGroup>();

            if (warningTextTransform == null && warningText != null)
            {
                warningTextTransform = warningText.transform;
            }

            hasTextTransform = warningTextTransform != null;
            if (hasTextTransform)
            {
                originalPosition = warningTextTransform.localPosition;
            }
        }

        public void ShowEnergyWarning() => ShowWarning("Недостаточно энергии!", "Выберите другую способность или завершите ход");
        public void ShowStepsWarning() => ShowWarning("Недостаточно шагов!", "Вы исчерпали лимит перемещений на этот ход");
        public void ShowStepsEndedWarning() => ShowWarning("Доступные шаги закончились!", "Вы исчерпали лимит перемещений на этот ход");

        /// <summary>
        /// Показывает окно предупреждения с основным текстом и необязательным описанием
        /// </summary>
        public void ShowWarning(string message, string description = "")
        {
            if (warningPopup == null) return;
            if (isSuppressed) return;

            if (warningText != null)
            {
                warningText.text = message;
            }

            if (warningDescriptionText != null)
            {
                if (!string.IsNullOrEmpty(description))
                {
                    warningDescriptionText.text = description;
                    warningDescriptionText.gameObject.SetActive(true);
                }
                else
                {
                    warningDescriptionText.gameObject.SetActive(false);
                }
            }

            if (currentPopupCoroutine != null)
            {
                StopCoroutine(currentPopupCoroutine);
            }

            if (warningCanvasGroup != null)
            {
                currentPopupCoroutine = StartCoroutine(ShowPopupFadeRoutine());
            }
            else
            {
                currentPopupCoroutine = StartCoroutine(ShowPopupRoutine(warningPopup, popupDuration));
            }
        }

        private IEnumerator ShowPopupFadeRoutine()
        {
            warningPopup.SetActive(true);
            var elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                var progress = elapsed / fadeDuration;

                warningCanvasGroup.alpha = progress;

                if (hasTextTransform)
                {
                    warningTextTransform.localPosition =
                        originalPosition + new Vector3(0, Mathf.Lerp(-floatOffset, 0, progress), 0);
                }

                yield return null;
            }

            warningCanvasGroup.alpha = 1f;
            if (hasTextTransform) warningTextTransform.localPosition = originalPosition;

            yield return new WaitForSeconds(popupDuration);

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                var progress = elapsed / fadeDuration;

                warningCanvasGroup.alpha = 1f - progress;
                yield return null;
            }

            ResetFadeState();
        }

        private IEnumerator ShowPopupRoutine(GameObject popup, float duration)
        {
            popup.SetActive(true);
            yield return new WaitForSeconds(duration);
            popup.SetActive(false);
        }

        private void HideAllPopups()
        {
            if (warningCanvasGroup != null)
            {
                ResetFadeState();
            }
            else if (warningPopup != null)
            {
                warningPopup.SetActive(false);
            }
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

        private void ResetFadeState()
        {
            if (warningCanvasGroup != null)
                warningCanvasGroup.alpha = 0f;

            if (hasTextTransform)
                warningTextTransform.localPosition = originalPosition;

            if (warningPopup != null)
                warningPopup.SetActive(false);
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