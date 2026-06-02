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
        [SerializeField] private float popupDuration = 2.0f;
        [SerializeField] private float fadeDuration = 0.2f;
        [SerializeField] private float floatOffset = 10f;

        [Header("Priority Warning")]
        [SerializeField] private float blinkSpeed = 1.5f;
        [SerializeField] private float maxPriorityDuration = 10f;

        private Coroutine currentPopupCoroutine;
        private CanvasGroup warningCanvasGroup;
        private Vector3 originalPosition;
        private bool hasTextTransform;
        private bool isSuppressed;

        private bool isPriorityWarningActive;
        private bool isPriorityWarningWaitingForInput;

        public bool IsPriorityWarningActive => isPriorityWarningActive;

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
        /// Показывает обычное предупреждение
        /// </summary>
        public void ShowWarning(string message, string description = "")
        {
            ShowWarning(message, description, false, 0f);
        }

        /// <summary>
        /// Показывает приоритетное предупреждение с заданным временем показа
        /// </summary>
        public void ShowPriorityWarning(string message, string description, float minimumDuration)
        {
            ShowWarning(message, description, true, minimumDuration);
        }

        /// <summary>
        /// Универсальный метод показа предупреждений
        /// </summary>
        private void ShowWarning(string message, string description, bool isPriority, float customDuration)
        {
            if (warningPopup == null) return;
            if (isSuppressed) return;

            if (isPriorityWarningActive && !isPriority)
            {
                return;
            }

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

            if (isPriority)
            {
                isPriorityWarningActive = true;
                isPriorityWarningWaitingForInput = false;
                currentPopupCoroutine = StartCoroutine(ShowPriorityPopupRoutine(customDuration));
            }
            else if (warningCanvasGroup != null)
            {
                currentPopupCoroutine = StartCoroutine(ShowPopupFadeRoutine());
            }
            else
            {
                currentPopupCoroutine = StartCoroutine(ShowPopupRoutine(warningPopup, popupDuration));
            }
        }

        /// <summary>
        /// Корутина для приоритетного уведомления
        /// </summary>
        private IEnumerator ShowPriorityPopupRoutine(float minimumDuration)
        {
            warningPopup.SetActive(true);
            
            var elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = elapsed / fadeDuration;

                if (warningCanvasGroup != null)
                    warningCanvasGroup.alpha = progress;

                if (hasTextTransform)
                {
                    warningTextTransform.localPosition =
                        originalPosition + new Vector3(0, Mathf.Lerp(-floatOffset, 0, progress), 0);
                }

                yield return null;
            }

            if (warningCanvasGroup != null)
                warningCanvasGroup.alpha = 1f;
            
            if (hasTextTransform)
                warningTextTransform.localPosition = originalPosition;

            yield return new WaitForSecondsRealtime(minimumDuration);

            isPriorityWarningWaitingForInput = true;
            
            var blinkTimer = 0f;
            var timeoutTimer = 0f;
            
            while (isPriorityWarningWaitingForInput && timeoutTimer < maxPriorityDuration)
            {
                blinkTimer += Time.unscaledDeltaTime * blinkSpeed;
                timeoutTimer += Time.unscaledDeltaTime;
                
                if (warningCanvasGroup != null)
                {
                    warningCanvasGroup.alpha = Mathf.Lerp(0.6f, 1f, (Mathf.Sin(blinkTimer * Mathf.PI * 2f) + 1f) * 0.5f);
                }

                yield return null;
            }

            if (timeoutTimer >= maxPriorityDuration)
            {
                Debug.Log($"<color=red>[UIController]</color> Приоритетное уведомление закрыто по таймауту ({maxPriorityDuration}с)");
                isPriorityWarningWaitingForInput = false;
            }

            yield return HidePriorityWarning();
        }

        /// <summary>
        /// Закрывает приоритетное уведомление
        /// </summary>
        public void DismissPriorityWarning()
        {
            if (!isPriorityWarningActive) return;
            
            isPriorityWarningWaitingForInput = false;
            
            Debug.Log("<color=green>[UIController]</color> Приоритетное уведомление закрыто игроком");
        }

        /// <summary>
        /// Плавное скрытие приоритетного уведомления
        /// </summary>
        private IEnumerator HidePriorityWarning()
        {
            var elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = elapsed / fadeDuration;

                if (warningCanvasGroup != null)
                    warningCanvasGroup.alpha = 1f - progress;

                yield return null;
            }

            isPriorityWarningActive = false;
            isPriorityWarningWaitingForInput = false;
            ResetFadeState();
        }

        /// <summary>
        /// Обычная корутина с плавным появлением/исчезновением
        /// </summary>
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

            isPriorityWarningActive = false;
            isPriorityWarningWaitingForInput = false;

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