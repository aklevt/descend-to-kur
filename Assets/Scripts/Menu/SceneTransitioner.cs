using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class SceneTransitioner : MonoBehaviour
    {
        public static SceneTransitioner Instance { get; private set; }

        [Header("UI Components")] [SerializeField]
        private CanvasGroup canvasGroup;

        [Header("Settings")] [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private float delayBetweenScenes = 1.0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);


                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SwitchScene(string sceneName)
        {
            StartCoroutine(TransitionSequence(sceneName));
        }

        private IEnumerator TransitionSequence(string sceneName)
        {
            canvasGroup.blocksRaycasts = true;
            yield return StartCoroutine(Fade(1f));


            yield return new WaitForSeconds(0.1f);


            var asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }


            yield return new WaitForSeconds(delayBetweenScenes);


            yield return StartCoroutine(Fade(0f));


            canvasGroup.blocksRaycasts = false;
        }

        private IEnumerator Fade(float targetAlpha)
        {
            var startAlpha = canvasGroup.alpha;
            float time = 0;

            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }
    }
}