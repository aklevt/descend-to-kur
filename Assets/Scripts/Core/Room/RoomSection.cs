using System.Collections.Generic;
using Entities;
using UnityEngine;

namespace Core.Room
{
    public class RoomSection : MonoBehaviour
    {
        [Header("Boundaries")] // 
        [Tooltip("Левая граница")]
        public Transform leftBoundary;

        [Tooltip("Правая граница")] //
        public Transform rightBoundary;

        [Tooltip(
            "Смещение левой границы камеры в клетках (положительное значение => камера может заходить левее границы)")]
        [SerializeField]
        private float leftCameraOffset = 3f;

        [Tooltip(
            "Смещение правой границы камеры в клетках (положительное значение => камера может заходить правее границы)")]
        [SerializeField]
        private float rightCameraOffset = 3f;

        public float LeftCameraOffset => leftCameraOffset;
        public float RightCameraOffset => rightCameraOffset;

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
        }

        public void SetActive(bool active)
        {
            IsActive = active;
            SetEnemiesActive(active);
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
            // Проверка, есть ли кто живой физически
            IsCleared = enemies.Count > 0 &&
                        enemies.TrueForAll(e => e == null || !e.gameObject.activeSelf || e.IsPhysicallyDead());
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