using System.Collections.Generic;
using Abilities;
using Entities;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Управляет отображением кнопок способностей игрока.
    /// Слоты назначаются вручную в инспекторе.
    /// </summary>
    public class AbilityBar : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Ability Slots")]
        [SerializeField] private List<AbilityButtonSlot> slots = new();

        [Header("Settings")]
        [SerializeField] private bool autoSelectFirstAbility = true;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            RefreshSlots();
            
            if (autoSelectFirstAbility)
                SelectFirstAbility();

            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Slot Management

        /// <summary>
        /// Обновить отображение всех слотов на основе доступных способностей
        /// </summary>
        private void RefreshSlots()
        {
            var abilities = GetPlayerAbilities();

            for (var i = 0; i < slots.Count; i++)
            {
                if (abilities != null && i < abilities.Count)
                {
                    slots[i].Setup(abilities[i], i);
                }
                else
                {
                    slots[i].Hide(i);
                }
            }
        }

        private IReadOnlyList<AbilityData> GetPlayerAbilities()
        {
            return PlayerMovement.Instance?.Abilities;
        }

        #endregion

        #region Public API (для AbilityController)

        /// <summary>
        /// Выбрать способность по индексу
        /// </summary>
        public void OnAbilitySelected(int index)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                slots[i].SetSelected(i == index);
            }
        }

        /// <summary>
        /// Снять выделение со всех слотов
        /// </summary>
        public void DeselectAllSlots()
        {
            foreach (var slot in slots)
            {
                slot.SetSelected(false);
            }
        }

        /// <summary>
        /// Запустить предупреждающую анимацию на слоте
        /// </summary>
        public void TriggerWarningFlash(int index)
        {
            if (index >= 0 && index < slots.Count)
            {
                slots[index].TriggerFlash();
            }
        }

        /// <summary>
        /// Обновить слоты
        /// </summary>
        public void Refresh()
        {
            RefreshSlots();
        }

        #endregion

        #region Event Handlers

        private void SelectFirstAbility()
        {
            var abilities = GetPlayerAbilities();
            
            if (slots.Count > 0 && abilities != null && abilities.Count > 0)
            {
                OnAbilitySelected(0);
            }
        }

        private void OnTurnChanged(TurnState state)
        {
            var isPlayerTurn = state == TurnState.PlayerTurn;
            
            foreach (var slot in slots)
            {
                slot.SetInteractable(isPlayerTurn);
            }
        }

        private void SubscribeToEvents()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnStateChanged += OnTurnChanged;
        }

        private void UnsubscribeFromEvents()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnStateChanged -= OnTurnChanged;
        }

        #endregion
    }
}