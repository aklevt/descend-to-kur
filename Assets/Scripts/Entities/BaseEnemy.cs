using System.Collections;
using System.Linq;
using Abilities;
using Core;
using Core.Room;
using UnityEngine;
using Entities.AI;

namespace Entities
{
    /// <summary>
    /// Базовый класс для всех врагов
    /// Конкретные враги переопределяют ExecuteAction для своей логики
    /// </summary>
    public abstract class EnemyBase : BaseEntity
    {
        private IEnemyAI enemyAI;

        #region Initialization

        protected override void Start()
        {
            base.Start();
            InitializeOnGrid();
            
            enemyAI = CreateAI();

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.RegisterEnemy(this);
            }
        }

        protected virtual void OnDestroy()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.UnregisterEnemy(this);
        }
        
        protected virtual IEnemyAI CreateAI()
        {
            if (Abilities.Count > 0)
            {
                if (Abilities[0] is QueenRangedAttackAbilityData)
                    return new QueenRangedEnemyAI();

                if (Abilities[0] is RangedAttackAbilityData)
                    return new RangedEnemyAI();
            }

            return new SimpleAttackerEnemyAI();
        }

        #endregion

        #region Turn Logic

        /// <summary>
        /// Выполнить полный ход врага: движение и действие
        /// </summary>
        public IEnumerator DoTurn()
        {
            var health = GetComponent<Health>();
            if (health == null || health.IsDead) yield break;
            
            if (RoomController.Current != null && !RoomController.Current.IsEnemyActive(this))
            {
                Debug.Log($"<color=grey>[{gameObject.name}]</color> Пропуск хода, т.к. секция неактивна");
                yield break;
            }

            var canAct = OnTurnStart();

            if (!canAct)
            {
                yield return new WaitForSeconds(GetScaledTime(0.5f));
                yield break;
            }


            Debug.Log($"{gameObject.name} готовится к ходу...");
            yield return new WaitForSeconds(GetScaledTime(0.1f));

            var bestMove = GetBestMove();

            if (bestMove.HasValue && bestMove.Value != CurrentCell)
            {
                MoveToCell(bestMove.Value);

                while (IsMoving)
                {
                    yield return null;
                }
            }

            if (health.IsDead) yield break;

            yield return new WaitForSeconds(GetScaledTime(0.1f));

            yield return ExecuteAction();

            yield return new WaitForSeconds(GetScaledTime(0.1f));
            Debug.Log($"{gameObject.name} закончил ход");
        }

        /// <summary>
        /// Вычисляет лучший ход в сторону игрока
        /// </summary>
        protected virtual Vector3Int? GetBestMove()
        {
            return enemyAI.CalculateBestMove(this);
        }

        #endregion

        #region Abilities

        /// <summary>
        /// Попытается использовать способность по индексу, если:
        /// 1. Индекс верный 
        /// 2. Способность можно использовать
        /// 3. Есть доступная цель
        /// Обычные враги всегда используют [0], босс может переопределить ExecuteAction для сложной логики выбора способности
        /// </summary>
        protected IEnumerator TryUseAbility(int abilityIndex)
        {
            if (abilityIndex >= Abilities.Count) yield break;

            var ability = Abilities[abilityIndex];
            if (!ability.CanUse(this)) yield break;

            var target = ability.ChooseTarget(this);
            if (!target.HasValue) yield break;

            yield return StartCoroutine(ability.Execute(this, target.Value));
        }

        /// <summary>
        /// Реализуют конкретные враги для своего хода
        /// Переопределение для более сложного поведения (AI, выбора способности)
        /// </summary>
        protected abstract IEnumerator ExecuteAction();

        #endregion
    }
}