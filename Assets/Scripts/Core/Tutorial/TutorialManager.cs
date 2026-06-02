using UnityEngine;
using UnityEngine.UI;
using UI.Tutorial;
using System;
using System.Linq;
using Core.Room;
using Entities;
using System.Collections;

namespace Core.Tutorial
{
    /// <summary>
    /// Центральный менеджер интерактивного обучения
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [SerializeField] private TutorialOverlayUI tutorialUI;
        
        [Header("Blinking Settings")]
        [SerializeField] private Image blinkingImage;
        [SerializeField] private float blinkSpeed = 6f;
        
        [Header("Debug")]
        [Tooltip("Дефолтный туториал для вызова через кнопку в меню/интерфейсе")]
        [SerializeField] private TutorialData defaultTutorialData;
        
        [SerializeField] private float cameraTutorialXOffset = -2f;

        private TutorialData currentTutorial;
        private int currentStepIndex;
        private bool isActive;
        private bool isTriggeredByButton; 

        private Coroutine blinkCoroutine;
        private const float DefaultAlpha = 0.3882353f;
        private const float MaxAlpha = 1f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            if (tutorialUI != null)
            {
                tutorialUI.Initialize(this);
            }

            SubscribeToEvents();
            ResetImageAlpha();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnEnemyKilled += HandleEnemyKilled;
                TurnManager.Instance.OnStateChanged += HandleTurnStateChanged;
            }

            SectionManager.OnGlobalSectionEntered += HandleSectionEntered;

            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.OnCellChanged += HandlePlayerCellChanged;
            }

            if (LevelController.Instance != null)
            {
                LevelController.Instance.OnRoomCleared += StopTutorial;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnEnemyKilled -= HandleEnemyKilled;
                TurnManager.Instance.OnStateChanged -= HandleTurnStateChanged;
            }

            SectionManager.OnGlobalSectionEntered -= HandleSectionEntered;

            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.OnCellChanged -= HandlePlayerCellChanged;
            }

            if (LevelController.Instance != null)
            {
                LevelController.Instance.OnRoomCleared -= StopTutorial;
            }
        }

        public void StartTutorial(TutorialData data, bool isFromButton = false)
        {
            if (data == null) return;

            currentTutorial = data;
            currentStepIndex = 0;
            isActive = true;
            isTriggeredByButton = isFromButton;

            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.SetTutorialOffset(cameraTutorialXOffset);
            }

            Debug.Log($"[TutorialManager] Туториал '{data.name}' запущен. Вызов через кнопку интерфейса: {isTriggeredByButton}");

            StopBlinking();

            if (!isTriggeredByButton)
            {
                blinkCoroutine = StartCoroutine(BlinkImageRoutine());
            }
            else
            {
                ResetImageAlpha();
            }

            UpdateStepDisplay();
        }

        public void StartDefaultTutorialFromButton()
        {
            if (defaultTutorialData != null)
            {
                StartTutorial(defaultTutorialData, isFromButton: true);
            }
        }

        public void NextStep()
        {
            if (!isActive || currentTutorial == null) return;

            StopBlinking();

            if (currentStepIndex < currentTutorial.steps.Count - 1)
            {
                currentStepIndex++;
                UpdateStepDisplay();
            }
            else
            {
                StopTutorial();
            }
        }

        public void PreviousStep()
        {
            if (!isActive || currentTutorial == null) return;

            StopBlinking();

            if (currentStepIndex > 0)
            {
                currentStepIndex--;
                UpdateStepDisplay();
            }
        }

        public void StopTutorial()
        {
            isActive = false;
            currentTutorial = null;
            currentStepIndex = 0;
            isTriggeredByButton = false; 

            StopBlinking();

            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.SetTutorialOffset(0f);
            }

            if (tutorialUI != null)
            {
                tutorialUI.Hide();
            }
        }

        private void UpdateStepDisplay()
        {
            if (tutorialUI == null || currentTutorial == null) return;

            var step = currentTutorial.steps[currentStepIndex];
            
            var hasNext = currentStepIndex < currentTutorial.steps.Count - 1;
            var hasBack = currentStepIndex > 0;

            tutorialUI.Show(
                step.speakerName, 
                step.text, 
                step.objective,
                step.speakerAvatar, 
                hasNext, 
                hasBack
            );

            if (!isTriggeredByButton && step.hasAutoTrigger && !string.IsNullOrEmpty(step.triggerName))
            {
            }
        }

        private bool IsCurrentActionActionType(TutorialActionType actionType, out TutorialStep step)
        {
            step = default;
            if (!isActive || currentTutorial == null || isTriggeredByButton) return false;

            step = currentTutorial.steps[currentStepIndex];
            return step.requiredAction == actionType;
        }

        private IEnumerator BlinkImageRoutine()
        {
            if (blinkingImage == null) yield break;

            while (true)
            {
                var pingPong = Mathf.PingPong(Time.time * blinkSpeed, 1f);
                var alpha = Mathf.Lerp(DefaultAlpha, MaxAlpha, pingPong);
                
                var color = blinkingImage.color;
                color.a = alpha;
                blinkingImage.color = color;
                
                yield return null;
            }
        }

        private void StopBlinking()
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
            ResetImageAlpha();
        }

        private void ResetImageAlpha()
        {
            if (blinkingImage == null) return;

            var color = blinkingImage.color;
            color.a = DefaultAlpha;
            blinkingImage.color = color;
        }

        #region Event Handlers

        private void HandleEnemyKilled()
        {
            if (IsCurrentActionActionType(TutorialActionType.KillMonster, out _))
            {
                Debug.Log("[TutorialManager] Условие KillMonster выполнено");
                NextStep();
            }
        }

        private void HandleTurnStateChanged(TurnState newState)
        {
            if (newState == TurnState.EnemyTurn)
            {
                if (IsCurrentActionActionType(TutorialActionType.EndTurn, out _))
                {
                    Debug.Log("[TutorialManager] Условие EndTurn выполнено");
                    NextStep();
                }
            }
        }

        private void HandleSectionEntered(int sectionIndex)
        {
            if (IsCurrentActionActionType(TutorialActionType.EnterSection, out var step))
            {
                if (step.targetIndex == sectionIndex)
                {
                    Debug.Log($"[TutorialManager] Условие EnterSection выполнено для секции {sectionIndex}");
                    NextStep();
                }
            }
        }

        private void HandlePlayerCellChanged(Vector3Int newCell)
        {
            if (IsCurrentActionActionType(TutorialActionType.ClickCell, out _))
            {
                Debug.Log("[TutorialManager] Условие ClickCell выполнено");
                NextStep();
                return; 
            }

            if (IsCurrentActionActionType(TutorialActionType.ApproachEnemy, out _))
            {
                var hasEnemyClose = FindObjectsOfType<EnemyBase>()
                    .Any(enemy => GridManager.Instance != null && 
                                  GridManager.Instance.GetPathDistance(newCell, enemy.CurrentCell, gameObject) <= 1);

                if (hasEnemyClose)
                {
                    Debug.Log("[TutorialManager] Условие ApproachEnemy выполнено");
                    NextStep();
                }
            }
        }

        public void HandleAbilitySelected(int abilityIndex)
        {
            if (IsCurrentActionActionType(TutorialActionType.SelectAbility, out var step))
            {
                if (step.targetIndex == abilityIndex)
                {
                    Debug.Log($"[TutorialManager] Условие SelectAbility выполнено для способности {abilityIndex}");
                    NextStep();
                }
            }
        }

        public void NotifyEnemyHovered()
        {
            if (IsCurrentActionActionType(TutorialActionType.HoverEnemy, out _))
            {
                Debug.Log("[TutorialManager] Условие HoverEnemy выполнено");
                NextStep();
            }
        }
        
        public void NotifyEnemyAltHovered()
        {
            if (IsCurrentActionActionType(TutorialActionType.HoverAltEnemy, out _))
            {
                Debug.Log("[TutorialManager] Условие HoverAltEnemy выполнено");
                NextStep();
            }
        }

        #endregion
    }
}