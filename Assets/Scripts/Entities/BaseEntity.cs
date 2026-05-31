using System;
using System.Collections;
using System.Collections.Generic;
using Abilities;
using Settings;
using Stats;
using UnityEngine;

namespace Entities
{
    /// <summary>
    /// Базовый класс для всех персонажей (игрок, враги)
    /// Наследуемые классы отвечают за все, что связано с сущностью: позиция на сетке, движение, анимация и список способностей
    /// </summary>
    public abstract class BaseEntity : MonoBehaviour
    {
        #region Configuration

        [Header("Abilities")] [SerializeField] private List<AbilityData> abilities = new();
        [Header("Stats")] [SerializeField] private EntityStatsBase baseStats;

        [Header("Live Stats")] [SerializeField]
        private EntityRuntimeStats stats = new();

        [Header("Animation Settings")] [SerializeField]
        private float localAnimationSpeedMultiplier = 1f;

        [Header("Combat")] [Tooltip("Смещение точки спауна снаряда относительно центра")] [SerializeField]
        private Vector2 projectileSpawnOffset = new(0.3f, 0.5f);

        [Header("Components")] [SerializeField]
        protected SpriteRenderer spriteRenderer;

        [SerializeField] protected Animator animator;

        public IReadOnlyList<AbilityData> Abilities => abilities;
        public EntityRuntimeStats Stats => stats;
        public Vector3Int CurrentCell { get; private set; }
        public bool IsMoving { private set; get; }
        public bool isGridInitialized = false;
        public bool IsFreeze => stats.Freeze > 0;

        private Vector3 targetWorldPos;

        public event Action<Vector3Int> OnCellChanged;

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация статов из базовых настроек
        /// </summary>
        private void InitializeStats()
        {
            if (baseStats != null && !stats.isCustomized)
            {
                stats.Initialize(baseStats);
            }

            stats.Health = stats.MaxHealth;
            stats.Energy = stats.MaxEnergy;
            stats.RemainingSteps = stats.MaxStepsPerRound;
            stats.Freeze = 0;
        }

        protected virtual void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            InitAnimator();

            isGridInitialized = false;

            // Установка статов на всякий случай, пусть будет
            // Намеренно продублирована логика. Даже если в инспекторе изменить здоровье (isCustomized == true), оно сбросится после респауна
            InitializeStats();
        }

        protected virtual void Start()
        {
        }

        private void Update()
        {
            MoveSmoothly();
        }

        private void OnValidate()
        {
            if (!stats.isCustomized && baseStats != null)
            {
                stats.Initialize(baseStats);
            }
        }


        public void RespawnAt(Vector3 worldPosition)
        {
            StopAllCoroutines();

            // Пусть дебаги пока что остаются, еще нужны будут при склейке сцен
            Debug.Log($"<color=orange>[{name}] Дебаг RespawnAt </color>");
            Debug.Log($"  worldPosition={worldPosition}");
            Debug.Log($"  isGridInitialized={isGridInitialized}");
            Debug.Log($"  CurrentCell={CurrentCell}");
            Debug.Log($"  transform.position={transform.position}");

            UnregisterFromGrid();
            ResetMovementState();
            FullRestore();
            SetupGridPosition(worldPosition);
            RegisterOnGrid();

            OnCellChanged?.Invoke(CurrentCell);

            Debug.Log($"<color=green>[{name}] === Конец дебага RespawnAt ===</color>");
            Debug.Log($"  CurrentCell={CurrentCell}");
            Debug.Log($"  transform.position={transform.position}");
            Debug.Log($"  isGridInitialized={isGridInitialized}");
        }

        private void UnregisterFromGrid()
        {
            if (isGridInitialized && CurrentCell != Vector3Int.zero)
            {
                Debug.Log($"<color=orange>[{name}]</color> Unregister в {CurrentCell}");
                GridManager.Instance?.UnregisterEntity(CurrentCell);
            }
        }

        private void ResetMovementState()
        {
            isGridInitialized = false;
            IsMoving = false;

            if (isMovingAlongPath)
            {
                StopCoroutine(nameof(MoveAlongPath));
                isMovingAlongPath = false;
                currentPath.Clear();
            }
        }

        private void SetupGridPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            Debug.Log($"<color=orange>[{name}]</color> transform.position = {worldPosition}");

            CurrentCell = GridManager.Instance.WorldToCell(worldPosition);
            Debug.Log($"<color=orange>[{name}]</color> Результат WorldToCell: {CurrentCell}");

            var cellCenter = GridManager.Instance.GetCellCenterWorld(CurrentCell);
            cellCenter.z = worldPosition.z;

            transform.position = cellCenter;
            targetWorldPos = cellCenter;

            Debug.Log($"<color=orange>[{name}]</color> Центр клетки: {cellCenter}");
        }

        private void RegisterOnGrid()
        {
            GridManager.Instance.RegisterFixedEntity(CurrentCell, gameObject);
            isGridInitialized = true;
        }

        public void InitializeOnGrid()
        {
            InitializeStats();
            UpdateVisualStatus();

            CurrentCell = GridManager.Instance.WorldToCell(transform.position);
            PlaceOnCell();
            IsMoving = false;

            GridManager.Instance.RegisterFixedEntity(CurrentCell, gameObject);
            isGridInitialized = true;
        }

        /// <summary>
        /// Полное восстановление здоровья и ресурсов
        /// </summary>
        public void FullRestore()
        {
            InitializeStats();
            stats.ResetShield();
            
            if (stats != null)
            {
                stats.ResetShield();
            }
            
            UpdateVisualStatus();
            Debug.Log($"<color=green>[{name}]</color> Полное восстановление: HP={stats.Health}, Energy={stats.Energy}");
        }

        #endregion

        #region Game Logic

        public bool OnTurnStart()
        {
            if (stats.Freeze > 0)
            {
                Debug.Log($"<color=cyan>{name}</color> пропускает ход. Осталось: {stats.Freeze}");
                return false;
            }

            if (this is PlayerMovement)
            {
                var maxSteps = Mathf.Min(stats.MaxStepsPerRound, stats.Energy);
                stats.ResetStepsTo(maxSteps);

                Debug.Log(
                    $"<color=green>[{name}]</color> Начало хода. Энергия: {stats.Energy}, Доступно шагов: {stats.RemainingSteps}");
            }

            return true;
        }

        public void OnRoundEnd()
        {
            if (stats.Freeze > 0)
            {
                stats.Freeze--;

                if (stats.Freeze == 0)
                {
                    Debug.Log($"<color=cyan>{name}</color> разморозился");
                }
            }

            stats.ProcessShieldEffect();
            UpdateVisualStatus();
        }

        public void Freeze(int turns)
        {
            stats.Freeze = Mathf.Max(stats.Freeze, turns);
            UpdateVisualStatus();
            Debug.Log($"<color=cyan>{name}</color> заморожен на {stats.Freeze} ходов!");
        }

        public bool IsPhysicallyDead()
        {
            var health = GetComponent<Health>();
            return health != null && health.IsDead;
        }

        #endregion

        #region Grid Movement

        private List<Vector3Int> currentPath = new();
        private int currentPathIndex = 0;
        private bool isMovingAlongPath = false;

        /// <summary>
        /// Запускает движение к целевой клетке по пути из проходимых клеток
        /// </summary>
        public void MoveToCell(Vector3Int targetCell)
        {
            if (isMovingAlongPath)
            {
                StopCoroutine(nameof(MoveAlongPath));
                isMovingAlongPath = false;
                currentPath.Clear();
            }

            var path = GridManager.Instance.GetPath(CurrentCell, targetCell, gameObject);
            if (path.Count <= 1) return;
            StartCoroutine(MoveAlongPath(path));
        }

        /// <summary>
        /// Корутина для пошагового движения по пути
        /// </summary>
        private IEnumerator MoveAlongPath(List<Vector3Int> path)
        {
            if (isMovingAlongPath) yield break;

            isMovingAlongPath = true;
            currentPath = path;
            currentPathIndex = 1;

            UpdateMovementAnimation(true);

            while (currentPathIndex < currentPath.Count)
            {
                var nextCell = currentPath[currentPathIndex];

                FlipToTarget(nextCell);

                GridManager.Instance.MoveEntity(CurrentCell, nextCell, gameObject);
                CurrentCell = nextCell;

                OnCellChanged?.Invoke(CurrentCell);

                targetWorldPos = GridManager.Instance.GetCellCenterWorld(nextCell);
                targetWorldPos.z = transform.position.z;
                IsMoving = true;

                while (IsMoving)
                {
                    yield return null;
                }

                currentPathIndex++;
            }

            isMovingAlongPath = false;
            currentPath.Clear();

            UpdateMovementAnimation(false);

            GridManager.Instance.TriggerTileObjectEnter(CurrentCell, this);
        }
        
        /// <summary>
        /// Перемещение по прямой (для отбрасывания)
        /// </summary>
        public void MoveDirectly(Vector3Int targetCell, bool playWalkAnimation = false)
        {
            FlipToTarget(targetCell);
            GridManager.Instance.MoveEntity(CurrentCell, targetCell, gameObject);
            CurrentCell = targetCell;

            OnCellChanged?.Invoke(CurrentCell);

            targetWorldPos = GridManager.Instance.GetCellCenterWorld(targetCell);
            targetWorldPos.z = transform.position.z;
            IsMoving = true;

            UpdateMovementAnimation(playWalkAnimation);
        }

        /// <summary>
        /// Выполняет мгновенный телепорт сущности в указанную клетку сетки
        /// Пере-регистрирует сущность в <see cref="GridManager"/>, сбрасывает состояние движения
        /// </summary>
        /// <param name="targetCell">Координаты целевой клетки на сетке</param>
        public void TeleportToCell(Vector3Int targetCell)
        {
            if (isMovingAlongPath)
            {
                StopCoroutine(nameof(MoveAlongPath));
                isMovingAlongPath = false;
                currentPath.Clear();
            }

            GridManager.Instance.UnregisterEntity(CurrentCell);
            CurrentCell = targetCell;
            IsMoving = false;

            PlaceOnCell();
            GridManager.Instance.RegisterFixedEntity(CurrentCell, gameObject);

            OnCellChanged?.Invoke(CurrentCell);
        }

        public void MoveSmoothly()
        {
            if (!IsMoving) return;

            var dt = Mathf.Min(Time.deltaTime, 0.03f);
            var baseSpeed = SettingsManager.Instance?.Settings.baseMoveSpeed ?? 8f;
            var scaledSpeed = baseSpeed * GetAnimationSpeedMultiplier();

            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, scaledSpeed * dt);

            if (Vector3.Distance(transform.position, targetWorldPos) < 0.001f)
            {
                transform.position = targetWorldPos;
                IsMoving = false;

                if (!isMovingAlongPath)
                {
                    UpdateMovementAnimation(false);
                }

                OnArrivingToTarget();
            }
        }

        private void PlaceOnCell()
        {
            UpdateTargetPosition(CurrentCell);
            transform.position = targetWorldPos;
        }

        /// <summary>
        /// Перемещение без регистрации в GridManager (для анимации мертвых сущностей)
        /// </summary>
        public void PhantomMoveToCell(Vector3Int targetCell)
        {
            FlipToTarget(targetCell);
            targetWorldPos = GridManager.Instance.GetCellCenterWorld(targetCell);
            targetWorldPos.z = transform.position.z;
            IsMoving = true;
        }

        private void UpdateTargetPosition(Vector3Int cell)
        {
            targetWorldPos = GridManager.Instance.GetCellCenterWorld(cell);
            targetWorldPos.z = transform.position.z;
        }

        protected virtual void OnArrivingToTarget()
        {
            if (!isMovingAlongPath)
            {
                GridManager.Instance.TriggerTileObjectEnter(CurrentCell, this);
            }
        }

        #endregion

        #region Animation & Visual Effects

        /// <summary>
        /// Простейшая анимация удара с возможностью вызова действия в сам момент удара
        /// </summary>
        public IEnumerator PunchAnimation(Vector3 targetPos, Action onHit = null)
        {
            var startPos = transform.position;
            var punchPos = Vector3.Lerp(startPos, targetPos, 0.3f);
            var duration = GetScaledTime(0.15f);
            var elapsed = 0f;

            FlipToTarget(targetPos);

            // Удар вперед
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, punchPos, elapsed / duration);
                yield return null;
            }

            transform.position = punchPos;

            onHit?.Invoke();

            yield return new WaitForSeconds(GetScaledTime(0.05f));

            // Возврат назад
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(punchPos, startPos, elapsed / duration);
                yield return null;
            }

            transform.position = startPos;
        }

        public void FlipToTarget(Vector3Int targetCell)
        {
            UpdateSpriteFlip(targetCell.x - CurrentCell.x);
        }

        public void FlipToTarget(Vector3 targetPosition)
        {
            UpdateSpriteFlip(targetPosition.x - transform.position.x);
        }

        private void UpdateSpriteFlip(float horizontalDirection)
        {
            if (horizontalDirection != 0)
                spriteRenderer.flipX = horizontalDirection < 0;
        }

        public void UpdateVisualStatus()
        {
            if (spriteRenderer == null) return;

            if (IsFreeze)
                spriteRenderer.color = new Color(0.5f, 0.7f, 1f);
            else if (stats.HasActiveShield)
                spriteRenderer.color = new Color(1f, 1f, 0.5f);
            else
                spriteRenderer.color = Color.white;
        }

        /// <summary>
        /// Возвращает (World) позицию для запуска снаряда с учетом поворота спрайта
        /// </summary>
        public virtual Vector3 GetProjectileSpawnPosition()
        {
            var directionMultiplier = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
            var offset = new Vector3(projectileSpawnOffset.x * directionMultiplier, projectileSpawnOffset.y, 0f);
            return transform.position + offset;
        }

        #endregion

        #region Diagonal Punch Animation

        /// <summary>
        /// Анимация удара с диагональным смещением для вертикальных атак
        /// </summary>
        public IEnumerator DiagonalPunchAnimation(Vector3 targetPos, Action onHit = null)
        {
            var startPos = transform.position;
            var duration = GetScaledTime(0.15f);
            var elapsed = 0f;

            var isVertical = IsVerticalAttack(startPos, targetPos);
            var preAttackPos = startPos;

            if (isVertical)
            {
                var diagOffset = GetDiagonalOffset(startPos, targetPos);

                if (diagOffset != Vector3.zero)
                {
                    UpdateSpriteFlip(-diagOffset.x);

                    preAttackPos = startPos + diagOffset;

                    elapsed = 0f;
                    while (elapsed < duration * 0.8f)
                    {
                        elapsed += Time.deltaTime;
                        transform.position = Vector3.Lerp(startPos, preAttackPos, elapsed / (duration * 0.8f));
                        yield return null;
                    }

                    transform.position = preAttackPos;
                }
            }
            else
            {
                FlipToTarget(targetPos);
            }

            // Выпад к цели
            var punchPos = Vector3.Lerp(preAttackPos, targetPos, 0.35f);
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(preAttackPos, punchPos, elapsed / duration);
                yield return null;
            }

            transform.position = punchPos;
            onHit?.Invoke();

            yield return new WaitForSeconds(GetScaledTime(0.05f));

            // Возврат на начальную позицию
            elapsed = 0f;
            while (elapsed < duration * 1.5f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(punchPos, startPos, elapsed / (duration * 1.5f));
                yield return null;
            }

            transform.position = startPos;
        }

        /// <summary>
        /// Вычисляет диагональное смещение для атаки с учетом препятствий
        /// </summary>
        private Vector3 GetDiagonalOffset(Vector3 attackerPos, Vector3 targetPos)
        {
            var targetCell = GridManager.Instance.WorldToCell(targetPos);

            var leftCell = targetCell + Vector3Int.left;
            var rightCell = targetCell + Vector3Int.right;

            var leftFree = IsCellFreeForDiagonalPunch(leftCell);
            var rightFree = IsCellFreeForDiagonalPunch(rightCell);

            float sideX;

            if (leftFree && rightFree)
            {
                var dirToTarget = targetPos.x - attackerPos.x;
                sideX = dirToTarget > 0 ? 0.25f : -0.25f;
            }
            else if (rightFree)
                sideX = 0.25f;
            else if (leftFree)
                sideX = -0.25f;
            else
                sideX = 0f;

            var verticalOffset = (targetPos.y - attackerPos.y) * 0.3f;

            return new Vector3(sideX, verticalOffset, 0);
        }

        /// <summary>
        /// Проверяет можно ли использовать клетку для диагональной атаки
        /// Учитывает все препятствия
        /// </summary>
        private bool IsCellFreeForDiagonalPunch(Vector3Int cell)
        {
            var options = new CellCheckOptions
            {
                checkEntities = true,
                currentEntity = gameObject,
                useBFS = true
            };

            return GridManager.Instance.IsCellPassable(cell, options);
        }

        /// <summary>
        /// Определяет, вертикальная ли атака
        /// </summary>
        private bool IsVerticalAttack(Vector3 from, Vector3 to)
        {
            var diff = to - from;
            return Mathf.Abs(diff.y) > Mathf.Abs(diff.x);
        }

        #endregion

        #region Animation & Visual Caching

        private HashSet<string> animatorParameters = new();
        private Action activeHitCallback;
        private bool hitTriggered;

        protected virtual void InitAnimator()
        {
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                foreach (var param in animator.parameters)
                {
                    animatorParameters.Add(param.name);
                }

                animator.speed = GetAnimationSpeedMultiplier();
            }
        }

        public bool HasParameter(string paramName) => animatorParameters.Contains(paramName);

        private void UpdateMovementAnimation(bool moving)
        {
            if (animator != null && HasParameter("isMoving"))
            {
                animator.SetBool("isMoving", moving);

                if (moving)
                {
                    animator.speed = GetAnimationSpeedMultiplier();
                }
                else
                {
                    animator.speed = GetIdleAnimationSpeed();
                }
            }
        }

        /// <summary>
        /// Скорость для Idle анимации
        /// </summary>
        public float GetIdleAnimationSpeed()
        {
            var globalMultiplier = SettingsManager.Instance?.Settings.globalAnimationSpeedMultiplier ?? 1f;
            var idleMultiplier = SettingsManager.Instance?.Settings.idleAnimationSpeedMultiplier ?? 1f;
            return globalMultiplier * idleMultiplier * localAnimationSpeedMultiplier;
        }

        #endregion

        #region Smart Animation Player

        /// <summary>
        /// Animation Event "TriggerHit"
        /// </summary>
        public void TriggerHit()
        {
            if (!hitTriggered)
            {
                hitTriggered = true;
                activeHitCallback?.Invoke();
                activeHitCallback = null;
            }
        }

        /// <summary>
        /// Универсальный метод атаки ближнего боя. 
        /// </summary>
        public IEnumerator PerformAttack(Vector3 targetPos, string triggerName, Action onHit,
            bool forceProceduralPunch = false, bool useDiagonalPunch = false)
        {
            FlipToTarget(targetPos);

            if (animator == null)
            {
                if (useDiagonalPunch)
                    yield return StartCoroutine(DiagonalPunchAnimation(targetPos, onHit));
                else
                    yield return StartCoroutine(PunchAnimation(targetPos, onHit));
                yield break;
            }

            var hasAnimatorTrigger = HasParameter(triggerName);

            if (hasAnimatorTrigger)
            {
                hitTriggered = false;
                activeHitCallback = onHit;

                animator.speed = GetAnimationSpeedMultiplier();
                animator.SetTrigger(triggerName);

                if (forceProceduralPunch)
                {
                    Coroutine punchCoroutine = null;
                    if (useDiagonalPunch)
                        punchCoroutine = StartCoroutine(DiagonalPunchAnimation(targetPos, null));
                    else
                        punchCoroutine = StartCoroutine(PunchAnimation(targetPos, null));

                    var maxWaitTime = 2f / GetAnimationSpeedMultiplier();
                    var elapsed = 0f;

                    while (!hitTriggered && elapsed < maxWaitTime)
                    {
                        elapsed += Time.deltaTime;
                        yield return null;
                    }

                    if (!hitTriggered)
                    {
                        Debug.LogWarning($"[Animation] На {name} не сработало TriggerHit для {triggerName}");
                        TriggerHit();
                    }

                    if (punchCoroutine != null)
                        yield return punchCoroutine;
                }
                else
                {
                    var maxWaitTime = 2f / GetAnimationSpeedMultiplier();
                    var elapsed = 0f;

                    while (!hitTriggered && elapsed < maxWaitTime)
                    {
                        elapsed += Time.deltaTime;
                        yield return null;
                    }

                    if (!hitTriggered)
                    {
                        Debug.LogWarning($"[Animation] На {name} не сработало TriggerHit для {triggerName}");
                        TriggerHit();
                    }
                }
            }
            else
            {
                Debug.LogWarning(
                    $"[Animation] У {name} нет параметра '{triggerName}' в Animator. " +
                    $"Используется программная атака."
                );

                if (useDiagonalPunch)
                    yield return StartCoroutine(DiagonalPunchAnimation(targetPos, onHit));
                else
                    yield return StartCoroutine(PunchAnimation(targetPos, onHit));

                yield break;
            }

            animator.speed = GetIdleAnimationSpeed();
            yield return new WaitForSeconds(GetScaledTime(0.2f));
        }

        /// <summary>
        /// Универсальный метод для выстрелов.
        /// </summary>
        public IEnumerator PerformCast(string triggerName, Action onCastTrigger)
        {
            if (animator != null && HasParameter(triggerName))
            {
                hitTriggered = false;
                activeHitCallback = onCastTrigger;

                animator.SetTrigger(triggerName);

                var maxWaitTime = 2f / GetAnimationSpeedMultiplier();
                var elapsed = 0f;

                while (!hitTriggered && elapsed < maxWaitTime)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (!hitTriggered)
                {
                    TriggerHit();
                }

                yield return new WaitForSeconds(GetScaledTime(0.15f));
            }
            else
            {
                onCastTrigger?.Invoke();
                yield return null;
            }
        }

        #endregion

        #region Animation Settings & Editor

        /// <summary>
        /// Получить финальную скорость анимации с учётом глобальных и локальных настроек
        /// </summary>
        public float GetAnimationSpeedMultiplier()
        {
            var globalMultiplier = SettingsManager.Instance?.Settings.globalAnimationSpeedMultiplier ?? 1f;
            return globalMultiplier * localAnimationSpeedMultiplier;
        }

        /// <summary>
        /// Получить время с учётом множителя скорости
        /// </summary>
        public float GetScaledTime(float baseTime)
        {
            return baseTime / GetAnimationSpeedMultiplier();
        }

        // Для дебага
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !isGridInitialized) return;

            // Точка спауна снаряда
            Gizmos.color = Color.yellow;
            var spawnPos = GetProjectileSpawnPosition();
            Gizmos.DrawWireSphere(spawnPos, 0.1f);
        }
#endif

        #endregion
    }
}