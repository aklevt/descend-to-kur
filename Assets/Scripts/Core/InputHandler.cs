using Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Core
{
    public class InputHandler : MonoBehaviour
    {
        private Camera mainCamera;
        private Vector3Int lastHoveredCell;
        private bool lastAltState;

        private const float PreviewHoverDelay = 0.3f;
        private Vector3Int previewHoverCell;
        private float previewHoverTimer;
        private bool previewShown;
        
        private Vector2 lastMousePosition;
        private const float MouseMovementThreshold = 5f;
        
        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void Start()
        {
            if (Mouse.current != null)
            {
                lastMousePosition = Mouse.current.position.ReadValue();
            }
        }

        private void Update()
        {
            if (!Application.isFocused) return;

            if (CheckPriorityWarningInput())
            {
                // return;
            }
            
            HandleSystemInput();
            
            if (!IsLevelReady()) return;
            
            var gameState = GameStateManager.Instance?.CurrentState ?? GameState.Gameplay;
            
            switch (gameState)
            {
                case GameState.Gameplay:
                    HandleGameplayInput();
                    break;
                    
                case GameState.Dialog:
                case GameState.Tutorial:
                    HandleCameraInput(); 
                    HandleDialogueInput();
                    break;
                    
                case GameState.Paused:
                case GameState.GameOver:
                case GameState.Transition:
                    break;
            }
        }
        
        private bool CheckPriorityWarningInput()
        {
            if (UI.UIController.Instance?.IsPriorityWarningActive != true)
                return false;

            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                UI.UIController.Instance.DismissPriorityWarning();
                return true;
            }

            if (Mouse.current != null)
            {
                var currentMousePos = Mouse.current.position.ReadValue();
                var delta = Vector2.Distance(currentMousePos, lastMousePosition);

                if (delta > MouseMovementThreshold)
                {
                    lastMousePosition = currentMousePos;
                    UI.UIController.Instance.DismissPriorityWarning();
                    return true;
                }
            }

            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
            {
                UI.UIController.Instance.DismissPriorityWarning();
                return true;
            }

            return true;
        }
        
        /// <summary>
        /// Системный ввод, который работает всегда
        /// </summary>
        private void HandleSystemInput()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                var gameState = GameStateManager.Instance?.CurrentState ?? GameState.Gameplay;
        
                if (gameState is GameState.Dialog or GameState.Tutorial) return;
                
                UI.UIManager.Instance?.HandleEscapePress();
            }
        }
        
        private void HandleDialogueInput()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
    
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                UI.Dialogue.DialogueManager.Instance?.SkipDialogue();
                return;
            }

            var advancePressed = kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                advancePressed = true;
            }

            if (advancePressed)
            {
                UI.Dialogue.DialogueManager.Instance?.AdvanceDialogue();
            }
        }
        
        /// <summary>
        /// Ввод во время геймплея
        /// </summary>
        private void HandleGameplayInput()
        {
            HandleCameraInput();
            HandleAbilityHotkeys();
            HandleEndTurnInput();
            HandleMouseInput();
            
            if (Mouse.current != null)
            {
                lastMousePosition = Mouse.current.position.ReadValue();
            }
            
            if (AbilityController.Instance?.ConsumeHoverUpdateRequest() == true)
            {
                ForceHandleCellHover();
            }
        }

        private void HandleCameraInput()
        {
            // ПКМ - возврат камеры к игроку
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                CameraFollow.Instance?.ResetFocus();
            }

            // WASD - перемещение камеры
            var movement = GetCameraMovementInput();
            if (movement.sqrMagnitude > 0.01f)
            {
                CameraFollow.Instance?.MoveFreeLook(movement.normalized);
            }
        
            // Скролл колеса мыши - зум
            var scrollDelta = Mouse.current?.scroll.ReadValue().y ?? 0f;
            if (Mathf.Abs(scrollDelta) > 0.1f)
            {
                CameraFollow.Instance?.Zoom(scrollDelta * 0.01f);
            }
        }

        private void HandleAbilityHotkeys()
        {
            if (!IsPlayerTurn()) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            for (var i = 0; i < 9; i++)
            {
                var key = (Key)((int)Key.Digit1 + i);
                if (kb[key].wasPressedThisFrame)
                {
                    AbilityController.Instance.SelectAbilityByIndex(i);
                    break;
                }
            }
        }
        
        /// <summary>
        /// Принудительно обрабатывает текущую позицию мыши
        /// </summary>
        private void ForceHandleCellHover()
        {
            if (Mouse.current == null) return;
    
            var mousePos = Mouse.current.position.ReadValue();
            var worldPoint = mainCamera.ScreenToWorldPoint(
                new Vector3(mousePos.x, mousePos.y, -mainCamera.transform.position.z));
            var hoveredCell = GridManager.Instance?.WorldToCell(worldPoint) ?? Vector3Int.zero;

            AbilityController.Instance?.HandleCellHover(hoveredCell);
            lastHoveredCell = hoveredCell;
        }

        private void HandleEndTurnInput()
        {
            if (!IsPlayerTurn()) return;

            // Пробел - завершить ход
            if (Keyboard.current?.spaceKey.wasPressedThisFrame == true)
            {
                TurnManager.Instance?.EndPlayerTurn();
            }
        }
        
        /// <summary>
        /// Обработка ввода мышью
        /// </summary>
        private void HandleMouseInput()
        {
            if (Mouse.current == null) return;
            if (IsPointerOverUI()) return;
            if (!IsPlayerTurn()) return;
            
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleMouseClick();
            }
            
            HandleCellHover();
            HandleAltToggle();
            UpdateEnemyPreview();
        }

        private void HandleMouseClick()
        {
            var mousePos = Mouse.current.position.ReadValue();
            var worldPoint = mainCamera.ScreenToWorldPoint(
                new Vector3(mousePos.x, mousePos.y, -mainCamera.transform.position.z));
            var clickedCell = GridManager.Instance.WorldToCell(worldPoint);

            AbilityController.Instance.HandleCellClick(clickedCell);
        }

        private void HandleCellHover()
        {
            var mousePos = Mouse.current.position.ReadValue();
            var worldPoint = mainCamera.ScreenToWorldPoint(
                new Vector3(mousePos.x, mousePos.y, -mainCamera.transform.position.z));
            var hoveredCell = GridManager.Instance.WorldToCell(worldPoint);

            if (hoveredCell != lastHoveredCell)
            {
                lastHoveredCell = hoveredCell;
                AbilityController.Instance.HandleCellHover(hoveredCell);
                HandleEnemyInfoHover(hoveredCell);
            }
        }
        
        private void HandleEnemyInfoHover(Vector3Int hoveredCell)
        {
            var entity = GridManager.Instance?.GetEntityAt(hoveredCell);
            
            if (entity != null)
            {
                var enemyBase = entity.GetComponent<Entities.EnemyBase>();
                if (enemyBase != null)
                {
                    UI.HUD.EnemyInfoManager.Instance?.OnEnemyHover(entity);
                    return;
                }
            }
            
            UI.HUD.EnemyInfoManager.Instance?.OnHoverEnd();
        }

        
        /// <summary>
        /// При нажатии/отпускании Alt обновляет превью
        /// </summary>
        private void HandleAltToggle()
        {
            if (!CanShowPreview())
            {
                if (lastAltState)
                {
                    lastAltState = false;
                    EnemyPreviewSystem.Instance?.RefreshPreview(false);
                }
                return;
            }
            
            var kb = Keyboard.current;
            if (kb == null) return;

            var altHeld = kb.leftAltKey.isPressed || kb.rightAltKey.isPressed;

            if (altHeld != lastAltState)
            {
                lastAltState = altHeld;
                EnemyPreviewSystem.Instance?.RefreshPreview(altHeld);
            }
        }
        
        /// <summary>
        /// Показ превью врага с задержкой
        /// </summary>
        private void UpdateEnemyPreview()
        {
            if (!CanShowPreview())
            {
                if (previewShown)
                {
                    EnemyPreviewSystem.Instance?.HidePreview();
                    previewShown = false;
                }
                previewHoverTimer = 0f;
                return;
            }
            
            if (Mouse.current == null) return;

            var mousePos = Mouse.current.position.ReadValue();
            var worldPoint = mainCamera.ScreenToWorldPoint(
                new Vector3(mousePos.x, mousePos.y, -mainCamera.transform.position.z));
            var hoveredCell = GridManager.Instance.WorldToCell(worldPoint);

            var altHeld = Keyboard.current?.leftAltKey.isPressed == true
                          || Keyboard.current?.rightAltKey.isPressed == true;

            if (hoveredCell != previewHoverCell)
            {
                previewHoverCell = hoveredCell;
                previewHoverTimer = 0f;

                if (previewShown)
                {
                    EnemyPreviewSystem.Instance?.HidePreview();
                    previewShown = false;
                }
                return;
            }

            if (previewShown)
            {
                var stillShown = EnemyPreviewSystem.Instance?.TryShowPreview(hoveredCell, altHeld) ?? false;
                if (!stillShown)
                {
                    EnemyPreviewSystem.Instance?.HidePreview();
                    previewShown = false;
                }
                return;
            }

            previewHoverTimer += Time.unscaledDeltaTime;
            if (previewHoverTimer < PreviewHoverDelay) return;

            var shown = EnemyPreviewSystem.Instance?.TryShowPreview(hoveredCell, altHeld) ?? false;
            previewShown = shown;

            if (!shown)
            {
                previewHoverTimer = 0f;
            }
        }
        
        private bool CanShowPreview()
        {
            if (!IsPlayerTurn()) return false;

            if (PlayerMovement.Instance != null && PlayerMovement.Instance.IsMoving) return false;
            
            if (AbilityController.Instance != null && AbilityController.Instance.IsExecuting) return false;

            return true;
        }

        /// <summary>
        /// Показывает превью зоны врага при наведении
        /// </summary>
        private Vector2 GetCameraMovementInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;

            var movement = Vector2.zero;
            if (kb.wKey.isPressed) movement.y += 1f;
            if (kb.sKey.isPressed) movement.y -= 1f;
            if (kb.aKey.isPressed) movement.x -= 1f;
            if (kb.dKey.isPressed) movement.x += 1f;

            return movement;
        }
        
        #region Helper Methods

        private bool IsLevelReady()
        {
            return Core.LevelController.Instance?.IsLevelLoaded == true;
        }

        private bool IsPlayerTurn()
        {
            return TurnManager.Instance?.CurrentState == TurnState.PlayerTurn;
        }

        private bool IsPointerOverUI()
        {
            return EventSystem.current?.IsPointerOverGameObject() == true;
        }

        #endregion
    }
}