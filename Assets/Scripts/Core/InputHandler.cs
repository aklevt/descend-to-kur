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
        
        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (!Application.isFocused) return;
            
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
                    break;
                    
                case GameState.Paused:
                case GameState.GameOver:
                case GameState.Transition:
                    break;
            }
        }
        
        /// <summary>
        /// Системный ввод, который работает всегда
        /// </summary>
        private void HandleSystemInput()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                UI.UIManager.Instance?.HandleEscapePress();
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
            }
        }
        
        /// <summary>
        /// При нажатии/отпускании Alt обновляет превью
        /// </summary>
        private void HandleAltToggle()
        {
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