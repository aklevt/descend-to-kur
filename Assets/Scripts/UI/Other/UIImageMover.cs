using System.Collections;
using UnityEngine;

namespace UI.Effects
{
    [RequireComponent(typeof(RectTransform))]
    public class UIImageMover : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float startX = -550f;
        [SerializeField] private float endX = 550f;
        [SerializeField] private float duration = 5f;

        [Header("Trigger Options")]
        [SerializeField] private bool moveOnStart = true;

        private RectTransform rectTransform;
        private Coroutine movementCoroutine;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (moveOnStart)
            {
                StartMovement();
            }
        }

        private void OnDisable()
        {
            StopMovement();
        }

        /// <summary>
        /// Запускает движение картинки по оси X.
        /// </summary>
        public void StartMovement()
        {
            StopMovement();
            movementCoroutine = StartCoroutine(MoveXSequence());
        }

        /// <summary>
        /// Принудительно останавливает движение.
        /// </summary>
        public void StopMovement()
        {
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
            }
        }

        private IEnumerator MoveXSequence()
        {
            float elapsed = 0f;
            Vector3 currentPos = rectTransform.anchoredPosition3D;

            currentPos.x = startX;
            rectTransform.anchoredPosition3D = currentPos;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                
                float percentage = elapsed / duration;

                currentPos.x = Mathf.Lerp(startX, endX, percentage);
                rectTransform.anchoredPosition3D = currentPos;

                yield return null;
            }

            currentPos.x = endX;
            rectTransform.anchoredPosition3D = currentPos;
            
            movementCoroutine = null; 
        }
    }
}