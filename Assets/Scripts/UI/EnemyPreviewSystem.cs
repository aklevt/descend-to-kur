using System.Collections.Generic;
using System.Linq;
using Abilities;
using Entities;
using UnityEngine;

namespace Core
{
    public class EnemyPreviewSystem : MonoBehaviour
    {
        public static EnemyPreviewSystem Instance { get; private set; }

        [Header("Attack Preview")] [SerializeField]
        private Color attackReachableColor = new(1f, 0.15f, 0.15f, 0.85f);

        [SerializeField] private Color attackFadedColor = new(1f, 0.15f, 0.15f, 0.30f);

        [Header("Move Preview")] [SerializeField]
        private Color movePreviewColor = new(1f, 0.75f, 0.1f, 0.65f);

        private Vector3Int lastPreviewCell = new(int.MinValue, 0, 0);
        private EnemyBase currentPreviewEnemy;
        private bool lastAltState;

        private void Awake() => Instance = this;

        public bool TryShowPreview(Vector3Int hoveredCell, bool showMove)
        {
            if (!IsPreviewAllowed(hoveredCell))
            {
                HidePreview();
                return false;
            }

            if (hoveredCell == lastPreviewCell && showMove == lastAltState && currentPreviewEnemy != null)
                return true;

            var entity = GridManager.Instance.GetEntityAt(hoveredCell);
            if (entity == null)
            {
                HidePreview();
                return false;
            }

            var enemy = entity.GetComponent<EnemyBase>();
            if (enemy == null)
            {
                HidePreview();
                return false;
            }

            UnsubscribeFromCurrent();

            lastPreviewCell = hoveredCell;
            lastAltState = showMove;
            currentPreviewEnemy = enemy;

            var health = enemy.GetComponent<Health>();
            if (health != null) health.OnDeath += HandleCurrentEnemyDied;

            GridHighlighter.Instance?.DimPlayerHighlights();
            
            Core.Tutorial.TutorialManager.Instance?.NotifyEnemyHovered();
            
            if (showMove) ShowMovePreview(enemy);
            else ShowAttackPreview(enemy);

            return true;
        }

        private bool IsPreviewAllowed(Vector3Int hoveredCell)
        {
            var ctrl = AbilityController.Instance;
            if (ctrl == null) return true;

            var selected = ctrl.SelectedAbility;
            if (selected == null) return true;

            if (selected is MoveAbilityData) return true;

            return !ctrl.AvailableCells.Contains(hoveredCell);
        }


        public void HidePreview()
        {
            UnsubscribeFromCurrent();

            if (currentPreviewEnemy == null && lastPreviewCell.x == int.MinValue) return;

            currentPreviewEnemy = null;
            lastPreviewCell = new Vector3Int(int.MinValue, 0, 0);

            GridHighlighter.Instance?.ClearPreview();
            GridHighlighter.Instance?.ResetDim();
        }

        private void UnsubscribeFromCurrent()
        {
            if (currentPreviewEnemy == null) return;
            var health = currentPreviewEnemy.GetComponent<Health>();
            if (health != null) health.OnDeath -= HandleCurrentEnemyDied;
        }

        private void HandleCurrentEnemyDied(GameObject _)
        {
            HidePreview();
        }

        public void RefreshPreview(bool showMove)
        {
            if (currentPreviewEnemy == null) return;
            lastAltState = showMove;

            if (showMove) ShowMovePreview(currentPreviewEnemy);
            else ShowAttackPreview(currentPreviewEnemy);
        }

        private void ShowMovePreview(EnemyBase enemy)
        {
            Core.Tutorial.TutorialManager.Instance?.NotifyEnemyAltHovered();
            
            var range = enemy.Stats.MoveRange;
            if (range <= 0)
            {
                GridHighlighter.Instance?.ClearPreview();
                return;
            }

            var reachable = GridManager.Instance.GetWalkableCellsInRange(
                enemy.CurrentCell, range, enemy.gameObject);

            var reachableSet = new HashSet<Vector3Int>(reachable);

            var faded = new List<Vector3Int>();
            var origin = enemy.CurrentCell;

            for (var dx = -range; dx <= range; dx++)
            for (var dy = -range; dy <= range; dy++)
            {
                var manh = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (manh == 0 || manh > range) continue;

                var cell = new Vector3Int(origin.x + dx, origin.y + dy, origin.z);
                if (!GridManager.Instance.HasFloor(cell)) continue;
                if (reachableSet.Contains(cell)) continue;

                faded.Add(cell);
            }

            GridHighlighter.Instance?.HighlightPreviewTwoLayers(
                faded,
                reachable,
                movePreviewColor
            );
        }

        private void ShowAttackPreview(EnemyBase enemy)
        {
            if (enemy.Abilities.Count == 0)
            {
                GridHighlighter.Instance?.ClearPreview();
                return;
            }

            var ability = enemy.Abilities[0];

            var theoretical = ability.GetTheoreticalCellsFrom(enemy.CurrentCell, enemy);
            var reachable = ability
                .GetTargetCellsFrom(enemy.CurrentCell, enemy)
                .SelectMany(x => ability.GetEffectCells(x, enemy))
                .ToList();

            var reachableSet = new HashSet<Vector3Int>(reachable);
            var faded = new List<Vector3Int>();
            foreach (var c in theoretical)
                if (!reachableSet.Contains(c))
                    faded.Add(c);

            GridHighlighter.Instance?.HighlightPreviewTwoLayers(
                faded,
                reachable,
                attackReachableColor
            );
        }
    }
}