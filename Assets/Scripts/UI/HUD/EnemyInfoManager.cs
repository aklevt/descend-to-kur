using UnityEngine;
using Entities;
using Abilities;

namespace UI.HUD
{
    /// <summary>
    /// Управляет отображением панелей информации о врагах
    /// </summary>
    public class EnemyInfoManager : MonoBehaviour
    {
        public static EnemyInfoManager Instance { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private float hoverDelay = 0.3f;
        
        private GameObject currentHoveredEnemy;
        private EnemyInfoDisplay currentDisplay;
        private float hoverTimer;
        private bool isDisplayShown;
        
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }
        
        /// <summary>
        /// Вызывается при наведении на врага
        /// </summary>
        public void OnEnemyHover(GameObject enemy)
        {
            if (enemy == currentHoveredEnemy) return;
            
            HideCurrentDisplay();
            
            currentHoveredEnemy = enemy;
            hoverTimer = 0f;
            
            if (enemy != null)
            {
                currentDisplay = enemy.GetComponentInChildren<EnemyInfoDisplay>(true);
            }
        }
        
        /// <summary>
        /// Вызывается при окончании наведения
        /// </summary>
        public void OnHoverEnd()
        {
            HideCurrentDisplay();
            currentHoveredEnemy = null;
            currentDisplay = null;
            hoverTimer = 0f;
        }
        
        private void Update()
        {
            if (currentHoveredEnemy == null || currentDisplay == null) return;

            if (!isDisplayShown)
            {
                hoverTimer += Time.unscaledDeltaTime;
        
                if (hoverTimer >= hoverDelay)
                {
                    ShowCurrentDisplay();
                }
            }
            else
            {
                var showAltHint = ShouldShowAltHint();
                var inAttackRange = IsEnemyInAttackRange(currentHoveredEnemy);
        
                currentDisplay.Show(showAltHint, inAttackRange); 
            }
        }
        
        private void ShowCurrentDisplay()
        {
            if (currentDisplay == null || currentHoveredEnemy == null) return;
            
            var showAltHint = ShouldShowAltHint();
            var inAttackRange = IsEnemyInAttackRange(currentHoveredEnemy);
            
            currentDisplay.Show(showAltHint, inAttackRange);
            isDisplayShown = true;
        }

        private void HideCurrentDisplay()
        {
            if (currentDisplay != null)
            {
                currentDisplay.Hide();
            }
            isDisplayShown = false;
        }
        
        private bool ShouldShowAltHint()
        {
            if (Core.AbilityController.Instance?.IsExecuting == true) return false;
            if (PlayerMovement.Instance?.IsPhysicallyDead() == true) return false;
            if (TurnManager.Instance?.CurrentState != TurnState.PlayerTurn) return false;
            if (PlayerMovement.Instance?.IsMoving == true) return false;
            
            return true;
        }
        
        private bool IsEnemyInAttackRange(GameObject enemy)
        {
            var player = PlayerMovement.Instance;
            var abilityController = Core.AbilityController.Instance;
            
            if (player == null || abilityController == null) return false;
            
            var selectedAbility = abilityController.SelectedAbility;
            if (selectedAbility == null || selectedAbility is MoveAbilityData) return false;
            
            var enemyCell = GridManager.Instance?.WorldToCell(enemy.transform.position);
            if (enemyCell == null) return false;
            
            var attackableCells = selectedAbility.GetTargetCells(player);
            return attackableCells.Contains(enemyCell.Value);
        }
    }
}