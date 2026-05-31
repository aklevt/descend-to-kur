using System.Collections;
using System.Collections.Generic;
using Entities;
using FX;
using UnityEngine;

namespace Abilities
{
    [CreateAssetMenu(fileName = "StunAbility", menuName = "Abilities/Stun")]
    public class StunAbilityData : AbilityData
    {
        [Header("Range Settings")] public int minRange = 2;
        public int maxRange = 4;

        [Header("Stun Settings")] [SerializeField]
        private int stunDuration = 2;

        [Header("Visual Effects")] [Tooltip("Префаб летящего снаряда")] [SerializeField]
        private GameObject chainProjectilePrefab;

        [Tooltip("Скорость полета снаряда")] [SerializeField]
        private float projectileSpeed = 15f;

        [Tooltip("Высота дуги полета (0 - летит прямо)")] [SerializeField]
        private float arcHeight = 2f;

        [Space] [SerializeField] private GameObject stunEffectPrefab;
        [SerializeField] private float effectDuration = 0.3f;

        private HashSet<GameObject> stunnedGameObjectsThisTurn = new();

        public void ResetTurnLimit() => stunnedGameObjectsThisTurn.Clear();

        public bool IsTargetAlreadyStunned(Vector3Int cell)
        {
            var targetObj = GridManager.Instance.GetEntityAt(cell);
            if (targetObj != null && limitOncePerTurn)
            {
                return stunnedGameObjectsThisTurn.Contains(targetObj);
            }

            return false;
        }

        public override List<Vector3Int> GetTargetCellsFrom(Vector3Int origin, BaseEntity actor)
        {
            var allCells = GridManager.Instance.GetAttackableCellsInRadius(origin, maxRange, minRange);
            var targetableCells = new List<Vector3Int>();

            foreach (var cell in allCells)
            {
                if (!GridManager.Instance.IsCellShootable(cell)) continue;
                if (!GridManager.Instance.HasLineOfSight(origin, cell)) continue;
                if (IsTargetAlreadyStunned(cell)) continue;

                targetableCells.Add(cell);
            }

            return targetableCells;
        }

        public override bool IsValidTarget(Vector3Int targetCell, BaseEntity caster)
        {
            var targetObj = GridManager.Instance.GetEntityAt(targetCell);
            if (targetObj == null || targetObj == caster.gameObject) return false;
            if (IsTargetAlreadyStunned(targetCell)) return false;

            return true;
        }

        public override IEnumerator Execute(BaseEntity actor, Vector3Int targetCell)
        {
            var targetObj = GridManager.Instance.GetEntityAt(targetCell);
            if (targetObj == null) yield break;

            var targetEntity = targetObj.GetComponent<BaseEntity>();
            if (targetEntity == null) yield break;

            if (limitOncePerTurn)
            {
                stunnedGameObjectsThisTurn.Add(targetObj);
            }

            var targetPos = targetObj.transform.position + new Vector3(0, 0.5f, 0);
            actor.FlipToTarget(targetPos);

            // Анимация
            yield return actor.StartCoroutine(actor.PerformCast("RangedAttack", null));

            var spawnPos = actor.GetProjectileSpawnPosition();

            yield return PlayStunEffect(spawnPos, targetPos, actor);

            targetEntity.Freeze(stunDuration);
        }

        public override List<Vector3Int> GetEffectCells(Vector3Int hoveredCell, BaseEntity actor) =>
            new() { hoveredCell };

        public override Vector3Int? ChooseTarget(BaseEntity actor) => null;

        public override List<Vector3Int> GetTheoreticalCellsFrom(Vector3Int origin, BaseEntity actor)
        {
            var result = new List<Vector3Int>();
            for (var dx = -maxRange; dx <= maxRange; dx++)
            for (var dy = -maxRange; dy <= maxRange; dy++)
            {
                var manh = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (manh < minRange || manh > maxRange) continue;
                var cell = new Vector3Int(origin.x + dx, origin.y + dy, origin.z);
                if (!GridManager.Instance.HasFloor(cell)) continue;
                result.Add(cell);
            }

            return result;
        }

        private IEnumerator PlayStunEffect(Vector3 startPos, Vector3 endPos, BaseEntity actor)
        {
            if (chainProjectilePrefab != null)
            {
                var projectile = Instantiate(chainProjectilePrefab, startPos, Quaternion.identity);

                float distance = Vector3.Distance(startPos, endPos);
                float duration = distance / projectileSpeed;
                float time = 0f;
                Vector3 lastPos = startPos;
                Vector3 originalScale = projectile.transform.localScale;

                // Полет цепи по дуге
                while (time < duration)
                {
                    if (projectile == null) break;

                    time += Time.deltaTime * actor.GetAnimationSpeedMultiplier();
                    float linearProgress = time / duration;

                    Vector3 currentPos = Vector3.Lerp(startPos, endPos, linearProgress);
                    float arcOffset = Mathf.Sin(linearProgress * Mathf.PI) * arcHeight;
                    currentPos.y += arcOffset;

                    Vector3 direction = currentPos - lastPos;
                    if (direction != Vector3.zero)
                    {
                        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                        projectile.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                    }

                    projectile.transform.position = currentPos;
                    lastPos = currentPos;

                    yield return null;
                }

                if (projectile != null)
                {
                    projectile.transform.position = endPos;
                    float hitTime = 0f;
                    float hitDuration = 0.1f;

                    var spriteRenderer = projectile.GetComponentInChildren<SpriteRenderer>();

                    while (hitTime < hitDuration)
                    {
                        if (projectile == null) break;
                        hitTime += Time.deltaTime;
                        float p = hitTime / hitDuration;

                        projectile.transform.localScale = Vector3.Lerp(originalScale, originalScale * 2.5f, p);

                        if (spriteRenderer != null)
                        {
                            Color c = spriteRenderer.color;
                            c.a = Mathf.Lerp(1f, 0f, p);
                            spriteRenderer.color = c;
                        }

                        yield return null;
                    }

                    Destroy(projectile);
                }
            }
            else
            {
                yield return new WaitForSeconds(actor.GetScaledTime(0.1f));
            }

            if (stunEffectPrefab != null)
            {
                var effect = Instantiate(stunEffectPrefab, endPos, Quaternion.identity);
                yield return new WaitForSeconds(actor.GetScaledTime(effectDuration));
                if (effect != null) Destroy(effect);
            }
        }
    }
}