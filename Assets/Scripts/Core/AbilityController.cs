using System.Collections;
using System.Collections.Generic;
using Abilities;
using Core.Room;
using Entities;
using Stats;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    public class AbilityController : MonoBehaviour
    {
        public static AbilityController Instance { get; private set; }

        #region Configuration

        [SerializeField] private AbilityBar abilityBar;

        [Header("Info Panel")] [SerializeField]
        private AbilityInfoPanel abilityInfoPanel;

        private AbilityValidator validator = new AbilityValidator();
        private List<Vector3Int> availableCells = new();
        private AbilityData selectedAbility;
        private bool isExecuting;
        public bool IsExecuting => isExecuting;

        private bool isDead;
        private bool isInputBlocked;
        private bool needsHoverUpdate;

        public AbilityData SelectedAbility => selectedAbility;

        public List<Vector3Int> AvailableCells => availableCells;

        private IReadOnlyList<AbilityData> PlayerAbilities =>
            PlayerMovement.Instance?.Abilities;

        private bool IsPlayerTurnActive =>
            !isDead &&
            !isInputBlocked &&
            TurnManager.Instance != null &&
            TurnManager.Instance.CurrentState == TurnState.PlayerTurn &&
            PlayerMovement.Instance != null;

        #endregion

        #region Initialization

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnStateChanged += HandleTurnChanged;
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnStateChanged -= HandleTurnChanged;
        }

        private void Update()
        {
            if (!IsPlayerTurnActive || isExecuting) return;

            if (ShouldRefreshAbilityGrid())
                RefreshAbilityOverlay();
        }

        #endregion

        #region Input Control API

        public void BlockInput()
        {
            isInputBlocked = true;
            ClearSelection();
            abilityBar?.DeselectAllSlots();
            abilityInfoPanel?.Hide();
        }

        /// <summary>
        /// Разблокировать ввод (при загрузке новой комнаты)
        /// </summary>
        public void UnblockInput()
        {
            isInputBlocked = false;
            isDead = false;
        }

        public void DisableAllOverlaysAfterDeath()
        {
            isDead = true;
            selectedAbility = null;
            ClearSelection();
            abilityInfoPanel?.Hide();
        }

        #endregion

        #region Ability Selection

        public void SelectAbilityByIndex(int index)
        {
            if (LevelController.Instance != null && !LevelController.Instance.IsLevelLoaded) return;

            var abilities = PlayerAbilities;
            if (abilities == null || index >= abilities.Count)
                return;

            var targetAbility = abilities[index];

            CheckAbilityResourcesAndWarn(targetAbility, index);

            abilityBar?.OnAbilitySelected(index);
            SelectAbility(abilities[index]);
            RefreshAbilityOverlay();
        }

        private void CheckAbilityResourcesAndWarn(AbilityData targetAbility, int index)
        {
            validator.ValidateAbilityAndWarn(targetAbility, index, abilityBar);
        }

        private void SelectAbility(AbilityData ability)
        {
            if (selectedAbility == ability) return;
            selectedAbility = ability;
            RefreshInfoPanel();
            RefreshAbilityOverlay();
            RequestHoverUpdate();
        }

        /// <summary>
        /// Обновить инфо-панель под текущую способность
        /// </summary>
        public void RefreshInfoPanel()
        {
            if (abilityInfoPanel == null) return;

            if (selectedAbility == null || PlayerMovement.Instance == null)
            {
                abilityInfoPanel.Hide();
                return;
            }

            abilityInfoPanel.Show(selectedAbility, PlayerMovement.Instance);
        }

        #endregion

        #region Grid Interaction

        public void HandleCellClick(Vector3Int clickedCell)
        {
            if (!CanProcessCellClick()) return;

            if (!ValidatePlayerStateForAction()) return;

            var player1 = PlayerMovement.Instance;
            if (player1 == null) return;
            
            if (RoomController.Current != null)
            {
                var roomCheck = RoomController.Current.ValidateActionInRoom(clickedCell, selectedAbility);
                if (!roomCheck.Success)
                {
                    UIController.Instance?.ShowWarning(roomCheck.ErrorMessage);
                    return;
                }
            }

            // Доступная клетка
            if (availableCells.Contains(clickedCell))
            {
                if (selectedAbility is not MoveAbilityData)
                {
                    if (!selectedAbility.IsValidTarget(clickedCell, player1))
                    {
                        UIController.Instance?.ShowWarning("Нет цели!",
                            "В области действия способности нет подходящей цели для атаки.");
                        return;
                    }
                }

                if (!ValidateAbilityUsage(clickedCell)) return;
                StartCoroutine(ExecuteSelectedAbility(clickedCell));
                return;
            }

            // Проверка нажатия на себя
            if (selectedAbility is not MoveAbilityData && clickedCell != player1.CurrentCell)
            {
                var theoreticalCells = selectedAbility.GetTheoreticalCellsFrom(player1.CurrentCell, player1);

                if ((theoreticalCells.Count == 1 || selectedAbility is ShieldAbilityData) &&
                    theoreticalCells.Contains(player1.CurrentCell))
                {
                    UIController.Instance?.ShowWarning("Неверная цель!",
                        "Способность можно применить только на себя.");
                    return;
                }
            }

            // Клик по игроку
            if (clickedCell == player1.CurrentCell)
            {
                if (selectedAbility is MoveAbilityData)
                {
                    UIController.Instance?.ShowWarning("Вы уже здесь!", "Вы стоите на этой клетке");
                }
                else
                {
                    UIController.Instance?.ShowWarning("Неверная цель!",
                        "Вы не можете применить эту способность на себя");
                }

                return;
            }

            // Внутри теоретического радиуса
            if (selectedAbility != null && PlayerMovement.Instance != null)
            {
                var player = PlayerMovement.Instance;
                var theoretical = selectedAbility.GetTheoreticalCellsFrom(player.CurrentCell, player);

                if (theoretical.Contains(clickedCell))
                {
                    // Режим перемещения
                    if (selectedAbility is MoveAbilityData)
                    {
                        // Клик в монстра
                        if (GridManager.Instance != null && GridManager.Instance.GetEntityAt(clickedCell) != null)
                            return;

                        // Проверка стен
                        if (GridManager.Instance != null &&
                            !GridManager.Instance.IsCellWalkable(clickedCell, player.gameObject))
                        {
                            UIController.Instance?.ShowWarning("Проход заблокирован!",
                                "Через эту клетку нельзя пройти");
                            return;
                        }

                        var stats = player.Stats;
                        int distance = Mathf.Abs(clickedCell.x - player.CurrentCell.x) +
                                       Mathf.Abs(clickedCell.y - player.CurrentCell.y);

                        // Нехватка шагов
                        if (distance > stats.RemainingSteps)
                        {
                            if (stats.RemainingSteps <= 0)
                            {
                                UIController.Instance?.ShowWarning("Доступные шаги закончились!",
                                    "Вы исчерпали лимит перемещений на этот ход");
                            }
                            else
                            {
                                UIController.Instance?.ShowWarning("Слишком далеко!",
                                    "Вам не хватает шагов, чтобы добраться до этой клетки");
                            }
                        }
                        // Нехватка энергии
                        else if (distance > stats.Energy)
                        {
                            UIController.Instance?.ShowWarning("Недостаточно энергии!",
                                "Не хватает энергии для перемещения на такое расстояние");
                        }
                    }
                    // Режим способности
                    else
                    {
                        // Лимит использования
                        if (selectedAbility.IsTurnLimitExceeded())
                        {
                            UIController.Instance?.ShowWarning("Лимит исчерпан!",
                                $"Способность '{selectedAbility.abilityName}' можно использовать только один раз за ход");
                            return;
                        }

                        // Проверка цели
                        if (!selectedAbility.IsValidTarget(clickedCell, player))
                        {
                            if (selectedAbility is StunAbilityData && GridManager.Instance != null)
                            {
                                var entity = GridManager.Instance.GetEntityAt(clickedCell);
                                if (entity != null && entity != player.gameObject)
                                {
                                    UIController.Instance?.ShowWarning("Уже оглушен!",
                                        "Этого монстра нельзя оглушить повторно на этом ходу");
                                    return;
                                }
                            }

                            UIController.Instance?.ShowWarning("Нет цели!",
                                "В области действия способности нет подходящей цели для атаки");
                        }
                        // Нехватка энергии
                        else
                        {
                            UIController.Instance?.ShowWarning("Недостаточно энергии!",
                                "Способность достает до этой клетки, но у вас не хватает энергии на её применение");
                        }
                    }

                    return;
                }
            }

            // Клик в монстра вне радиуса
            if (GridManager.Instance != null && GridManager.Instance.GetEntityAt(clickedCell) != null)
            {
                return;
            }

            // Вне радиуса
            if (selectedAbility is MoveAbilityData)
            {
                UIController.Instance?.ShowWarning("Клетка недоступна!", "До этой клетки нельзя добраться за один ход");
            }
            else
            {
                UIController.Instance?.ShowWarning("Слишком далеко!", $"Эта клетка находится вне радиуса действия способности");
            }
        }

        /// <summary>
        /// Базовые проверки возможности обработки клика
        /// </summary>
        private bool CanProcessCellClick()
        {
            return IsPlayerTurnActive && !isExecuting && selectedAbility != null;
        }

        /// <summary>
        /// Проверки состояния игрока (заморозка, движение)
        /// </summary>
        private bool ValidatePlayerStateForAction()
        {
            return validator.IsPlayerReadyForAction();
        }

        /// <summary>
        /// Проверки способности и цели
        /// </summary>
        private bool ValidateAbilityUsage(Vector3Int targetCell)
        {
            // if (RoomController.Current != null)
            // {
            //     var roomCheck = RoomController.Current.ValidateActionInRoom(targetCell, selectedAbility);
            //
            //     if (!roomCheck.Success)
            //     {
            //         UIController.Instance?.ShowWarning(roomCheck.ErrorMessage);
            //         return false;
            //     }
            // }

            return validator.CanUseAbilityOnTarget(selectedAbility, targetCell, availableCells);
        }

        public void HandleCellHover(Vector3Int hoveredCell)
        {
            if (!IsPlayerTurnActive) return;

            if (!availableCells.Contains(hoveredCell))
            {
                GridHighlighter.Instance.ClearEffect();
                return;
            }

            var effectCells = selectedAbility.GetEffectCells(hoveredCell, PlayerMovement.Instance);
            GridHighlighter.Instance.HighlightEffect(effectCells, selectedAbility.effectColor);
        }

        /// <summary>
        /// Запросить обновление подсветки эффекта
        /// </summary>
        public void RequestHoverUpdate()
        {
            needsHoverUpdate = true;
        }

        /// <summary>
        /// Проверить и сбросить флаг запроса обновления подсветки
        /// </summary>
        public bool ConsumeHoverUpdateRequest()
        {
            if (!needsHoverUpdate) return false;
            needsHoverUpdate = false;
            return true;
        }

        #endregion

        #region Execution Coroutines

        private IEnumerator ExecuteSelectedAbility(Vector3Int targetCell)
        {
            isExecuting = true;
            var ability = selectedAbility;

            if ((ability is not MoveAbilityData) && PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.Stats.SpendEnergy(ability.energyCost);
            }

            ClearSelection();
            yield return ability.Execute(PlayerMovement.Instance, targetCell);

            isExecuting = false;
            RefreshAbilityOverlay();
            RefreshInfoPanel();
        }

        public void CancelExecution()
        {
            StopAllCoroutines();
            isExecuting = false;
        }

        #endregion

        #region Overlay Updates

        public void RefreshAbilityOverlay()
        {
            ClearSelection();

            if (!IsPlayerTurnActive || selectedAbility == null) return;

            var player = PlayerMovement.Instance;

            availableCells = selectedAbility.GetTargetCells(player);
            var theoretical = selectedAbility.GetTheoreticalCellsFrom(player.CurrentCell, player);

            var reachableSet = new HashSet<Vector3Int>(availableCells);
            var faded = new List<Vector3Int>();
            foreach (var c in theoretical)
                if (!reachableSet.Contains(c))
                    faded.Add(c);

            GridHighlighter.Instance.HighlightCellsTwoLayers(
                faded,
                availableCells,
                selectedAbility.highlightColor
            );
        }

        private void ClearSelection()
        {
            availableCells.Clear();
            GridHighlighter.Instance.Clear();
        }

        private bool ShouldRefreshAbilityGrid() =>
            !PlayerMovement.Instance.IsMoving &&
            selectedAbility != null &&
            availableCells.Count == 0;

        #endregion

        #region Event Handlers

        private void HandleTurnChanged(TurnState newState)
        {
            if (newState != TurnState.PlayerTurn)
            {
                ClearSelection();
                abilityBar?.DeselectAllSlots();
                abilityInfoPanel?.Hide();
                return;
            }

            var abilities = PlayerAbilities;
            if (abilities != null)
            {
                foreach (var ability in abilities)
                {
                    if (ability is ShieldAbilityData shield) shield.ResetTurnLimit();
                    if (ability is StunAbilityData stun) stun.ResetTurnLimit();
                }
            }

            if (abilities != null && abilities.Count > 0)
                SelectAbilityByIndex(0);
            else
            {
                ClearSelection();
            }
        }

        #endregion
    }
}