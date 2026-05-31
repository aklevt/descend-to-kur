using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Entities;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Бьёт на 1 клетку, наносит большой урон и отбрасывает цель.
    /// </summary>
    [CreateAssetMenu(fileName = "TankPunchAbility", menuName = "Abilities/TankPunch")]
    public class TankPunchAbilityData : AbilityData
    {
        [Header("Knockback")] [Tooltip("Максимальная дистанция отброса")] [SerializeField]
        private int knockbackDistance = 2;

        [Header("Visual")] [Tooltip("Задержка перед")] [SerializeField]
        private float impactPause = 0.1f;

        public override List<Vector3Int> GetTargetCellsFrom(Vector3Int origin, BaseEntity actor)
        {
            return GridManager.Instance.GetAttackableCellsInRadius(origin, 1)
                .SelectMany(x => RadiusToEffectCells(x, actor))
                .ToList();
        }

        public List<Vector3Int> RadiusToEffectCells(Vector3Int hoveredCell, BaseEntity actor)
        {
            var effectCells = new List<Vector3Int>();
            Vector3Int rayVector;
            Vector3Int deltaVector;
            if (hoveredCell - actor.CurrentCell == Vector3Int.up)
            {
                rayVector = Vector3Int.up;
                deltaVector = Vector3Int.right;
            }
            else if (hoveredCell - actor.CurrentCell == Vector3Int.down)
            {
                rayVector = Vector3Int.down;
                deltaVector = Vector3Int.right;
            }
            else if (hoveredCell - actor.CurrentCell == Vector3Int.right)
            {
                rayVector = Vector3Int.right;
                deltaVector = Vector3Int.up;
            }
            else if (hoveredCell - actor.CurrentCell == Vector3Int.left)
            {
                rayVector = Vector3Int.left;
                deltaVector = Vector3Int.up;
            }
            else
            {
                return effectCells;
            }

            for (var j = 2; j > 0; j--)
            {
                for (var i = -1; i <= 1; i++)
                {
                    effectCells.Add(actor.CurrentCell + j * rayVector + i * deltaVector);
                }
            }

            return effectCells;
        }

        public override List<Vector3Int> GetEffectCells(Vector3Int hoveredCell, BaseEntity actor)
        {
            return new List<Vector3Int> { hoveredCell };
        }

        public override Vector3Int? ChooseTarget(BaseEntity actor)
        {
            var playerCell = PlayerMovement.Instance?.CurrentCell;
            if (playerCell == null) return null;

            var available = GetTargetCells(actor);
            return available.Contains(playerCell.Value) ? playerCell : null;
        }

        public override bool IsValidTarget(Vector3Int targetCell, BaseEntity caster)
        {
            var target = GridManager.Instance.GetEntityAt(targetCell);
            return target != null && target != caster.gameObject;
        }

        public override IEnumerator Execute(BaseEntity actor, Vector3Int targetCell)
        {
            var targetObj = GridManager.Instance.GetEntityAt(targetCell);
            if (targetObj == null) yield break;

            var targetEntity = targetObj.GetComponent<BaseEntity>();
            var targetHealth = targetObj.GetComponent<Health>();

            if (targetEntity == null) yield break;

            var damage = GetCalculatedDamage(actor);
            var knockbackDir = targetCell - actor.CurrentCell;

            // Анимация удара
            yield return actor.StartCoroutine(actor.PerformAttack(
                targetObj.transform.position,
                "TankPunch",
                () =>
                {
                    targetHealth?.TakeDamage(damage);
                    CameraFollow.Instance?.ShakeHeavy();
                    ApplyKnockback(targetEntity, knockbackDir);
                },
                forceProceduralPunch
            ));

            while (targetEntity != null && targetEntity.IsMoving)
            {
                yield return null;
            }
        }

        private void ApplyKnockback(BaseEntity target, Vector3Int direction)
        {

            if (target == null) return;

            var finalCell = target.CurrentCell;

            for (var i = 1; i <= knockbackDistance; i++)
            {
                var checkCell = target.CurrentCell + GetNearestDirection(direction) * i;

                if (!GridManager.Instance.IsCellKnockbackable(checkCell))
                {
                    break;
                }

                if (GridManager.Instance.HasBlockingTileObject(checkCell))
                {
                    finalCell = checkCell;
                    break;
                }

                finalCell = checkCell;
            }

            if (finalCell != target.CurrentCell)
            {
                Debug.Log($"[TankPunch] Отброс {target.gameObject.name}: {target.CurrentCell} -> {finalCell}");
                target.MoveDirectly(finalCell, playWalkAnimation:false);
            }
        }

        /// <summary>
        /// Возвращает ближайшее направление для заданного вектора.
        /// Если вектор одинаково близок к двум направлениям, возвращает их сумму.
        /// </summary>
        public static Vector3Int GetNearestDirection(Vector3Int vector)
        {
            // Константы направлений
            Vector3Int up = new Vector3Int(0, 1, 0);
            Vector3Int down = new Vector3Int(0, -1, 0);
            Vector3Int right = new Vector3Int(1, 0, 0);
            Vector3Int left = new Vector3Int(-1, 0, 0);

            // Игнорируем нулевой вектор
            if (vector == Vector3Int.zero)
                return Vector3Int.zero;

            // Нормализуем вектор к единичной длине по каждой оси для сравнения углов
            Vector2 direction2D = new Vector2(vector.x, vector.y).normalized;

            // Вычисляем косинусы углов между вектором и каждым направлением
            float cosUp = Vector2.Dot(direction2D, Vector2.up);
            float cosDown = Vector2.Dot(direction2D, Vector2.down);
            float cosRight = Vector2.Dot(direction2D, Vector2.right);
            float cosLeft = Vector2.Dot(direction2D, Vector2.left);

            // Находим максимальное значение косинуса (ближайший угол)
            float maxCos = Mathf.Max(cosUp, cosDown, cosRight, cosLeft);

            // Находим направление, к которому ближе всего данный вектор
            // Если 2 напралвения одинаково близко, берем диагональ
            Vector3Int result = Vector3Int.zero;

            if (Mathf.Approximately(cosUp, maxCos))
                result += up;

            if (Mathf.Approximately(cosDown, maxCos))
                result += down;

            if (Mathf.Approximately(cosRight, maxCos))
                result += right;

            if (Mathf.Approximately(cosLeft, maxCos))
                result += left;

            return result;
        }
    }
}