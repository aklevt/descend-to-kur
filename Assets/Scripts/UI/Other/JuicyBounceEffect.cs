using System.Collections;
using UnityEngine;

namespace UI.Dialogue
{
    public class JuicyBounceEffect : MonoBehaviour
    {
        [Header("Appearance Bounce (Scale)")]
        [SerializeField] private float bounceDuration = 0.4f;
        [SerializeField] private Vector3 targetScale = new Vector3(1.15f, 1.15f, 1f);
        [SerializeField] private AnimationCurve bounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Idle Animation (Rotation)")]
        [SerializeField] private float rotationSpeed = 3f;
        [SerializeField] private float maxRotationAngle = 5f;

        private Vector3 originalScale;
        private Coroutine idleRoutine;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        private void OnEnable()
        {
            transform.localScale = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            
            StartCoroutine(AppearanceBounceRoutine());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            transform.localScale = originalScale;
            transform.localRotation = Quaternion.identity;
        }

        private IEnumerator AppearanceBounceRoutine()
        {
            float elapsed = 0f;

            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / bounceDuration;
                
                float curveValue = bounceCurve.Evaluate(progress);
                
                transform.localScale = Vector3.LerpUnclamped(Vector3.zero, targetScale, curveValue);
                yield return null;
            }

            elapsed = 0f;
            float returnDuration = bounceDuration * 0.5f;
            Vector3 currentScale = transform.localScale;

            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(currentScale, originalScale, elapsed / returnDuration);
                yield return null;
            }

            transform.localScale = originalScale;

            idleRoutine = StartCoroutine(IdleRotationRoutine());
        }

        private IEnumerator IdleRotationRoutine()
        {
            while (true)
            {
                float angle = Mathf.Sin(Time.time * rotationSpeed) * maxRotationAngle;
                transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
        }
    }
}