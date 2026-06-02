using UnityEngine;

namespace Intro
{
    public class MainMenuInitializer : MonoBehaviour
    {
        [SerializeField] private AudioClip menuMusic;

        private void Start()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(menuMusic, loop: true);
            }
        }
    }
}