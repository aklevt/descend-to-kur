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
        private AbilityValidator validator = new AbilityValidator();
        private List<Vector3Int> availableCells = new();
        private AbilityData selectedAbility;
        private bool isExecuting;
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
            RefreshAbilityOverlay();
            RequestHoverUpdate();
        }

        #endregion

        #region Grid Interaction

        public void HandleCellClick(Vector3Int clickedCell)
        {
            if (!CanProcessCellClick()) return;

            if (!ValidatePlayerStateForAction()) return;

            if (!ValidateAbilityUsage(clickedCell)) return;

            StartCoroutine(ExecuteSelectedAbility(clickedCell));
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
            if (RoomController.Current != null)
            {
                var roomCheck = RoomController.Current.ValidateActionInRoom(targetCell, selectedAbility);
        
                if (!roomCheck.Success)
                {
                    UIController.Instance?.ShowWarning(roomCheck.ErrorMessage);
                    return false;
                }
            }

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
                if (!reachableSet.Contains(c)) faded.Add(c);

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
                return;
            }

            var abilities = PlayerAbilities;
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