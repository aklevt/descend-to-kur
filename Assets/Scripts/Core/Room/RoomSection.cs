using System.Collections.Generic;
using Entities;
using UnityEngine;

namespace Core.Room
{
    public class RoomSection : MonoBehaviour
    {
        [Header("Boundaries")]
        [Tooltip("Левая граница")]
        public Transform leftBoundary;

        [Tooltip("Правая граница")]
        public Transform rightBoundary;

        [Tooltip("Смещение левой границы камеры в клетках (положительное значение => камера может заходить левее границы)")]
        [SerializeField]
        private float leftCameraOffset = 3f;

        [Tooltip("Смещение правой границы камеры в клетках (положительное значение => камера может заходить правее границы)")]
        [SerializeField]
        private float rightCameraOffset = 3f;

        [Header("Auto-complete Settings")]
        [Tooltip("Автоматически завершать уровень при входе в эту секцию, если в ней нет врагов")]
        [SerializeField] 
        private bool autoCompleteIfEmpty = false;

        public float LeftCameraOffset => leftCameraOffset;
        public float RightCameraOffset => rightCameraOffset;
        public bool AutoCompleteIfEmpty => autoCompleteIfEmpty;

        public float LeftX => leftBoundary != null ? leftBoundary.position.x : transform.position.x;
        public float RightX => rightBoundary != null ? rightBoundary.position.x : transform.position.x;

        public bool IsActive { get; private set; }
        public bool IsCleared { get; private set; }
        public int SectionIndex { get; set; }

        private List<EnemyBase> enemies = new();
        public List<EnemyBase> Enemies => enemies;

        private void Awake() => CollectEnemies();

        public void CollectEnemies()
        {
            enemies = new List<EnemyBase>(GetComponentsInChildren<EnemyBase>(true));
        }

        public void Initialize()
        {
            IsActive = false;
            IsCleared = false;
            SetEnemiesActive(false);
            
            if (enemies.Count == 0 && autoCompleteIfEmpty)
            {
                IsCleared = true;
                Debug.Log($"<color=cyan>[RoomSection]</color> Секция {SectionIndex} изначально пуста и автоматом завершается");
            }
        }

        public void SetActive(bool active)
        {
            IsActive = active;
            SetEnemiesActive(active);

            if (active)
            {
                CheckCleared();
            }
        }

        private void SetEnemiesActive(bool active)
        {
            foreach (var enemy in enemies)
                if (enemy != null)
                    enemy.gameObject.SetActive(active);
        }

        public void CheckCleared()
        {
            if (IsCleared) return;
            
            if (enemies.Count == 0)
            {
                if (autoCompleteIfEmpty || IsActive)
                {
                    IsCleared = true;
                    Debug.Log($"<color=cyan>[RoomSection]</color> Секция {SectionIndex} пуста и завершается");
                }
                return;
            }
            
            if (!IsActive) return;

            IsCleared = enemies.TrueForAll(e => e == null || e.IsPhysicallyDead());
        }

        public bool ContainsEnemy(EnemyBase enemy) => enemies.Contains(enemy);

        public Bounds GetCameraBounds()
        {
            var minX = LeftX - leftCameraOffset;
            var maxX = RightX + rightCameraOffset;

            var centerY = 0f;
            var height = 20f;

            if (CameraFollow.Instance != null)
            {
                var roomBounds = CameraFollow.Instance.GetRoomBounds();
                if (roomBounds.size.magnitude > 0.1f)
                {
                    centerY = roomBounds.center.y;
                    height = roomBounds.size.y;
                }
            }

            return new Bounds(
                new Vector3((minX + maxX) / 2f, centerY, 0),
                new Vector3(maxX - minX, height, 1f)
            );
        }


        private void OnDrawGizmos()
        {
            if (leftBoundary == null || rightBoundary == null) return;

            Gizmos.color = Color.cyan;

            var leftPos = leftBoundary.position;
            var rightPos = rightBoundary.position;

            // Левая вертикальная линия
            Gizmos.DrawLine(
                new Vector3(leftPos.x, leftPos.y - 10f, 0f),
                new Vector3(leftPos.x, leftPos.y + 10f, 0f)
            );

            // Правая вертикальная линия
            Gizmos.DrawLine(
                new Vector3(rightPos.x, rightPos.y - 10f, 0f),
                new Vector3(rightPos.x, rightPos.y + 10f, 0f)
            );
        }
    }
}