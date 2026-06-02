using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;
    [SerializeField] private float defaultFadeDuration = 1.0f;

    private AudioSource activeSource;
    private Coroutine fadeCoroutine;
    private float masterVolume = 1.0f;
    private float currentVolumeMultiplier = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        activeSource = musicSourceA;
        
        
        
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
    }

    public void PlayMusic(AudioClip clip, bool loop = true, float fadeDuration = -1f, float targetVolumeMultiplier = 1.0f)
    {
        var duration = fadeDuration < 0 ? defaultFadeDuration : fadeDuration;
        currentVolumeMultiplier = Mathf.Clamp01(targetVolumeMultiplier);

        if (clip == null)
        {
            StopMusic(duration);
            return;
        }

        if (activeSource.clip == clip && activeSource.isPlaying)
        {
            FadeVolume(currentVolumeMultiplier, duration);
            return;
        }

        var newSource = (activeSource == musicSourceA) ? musicSourceB : musicSourceA;

        newSource.clip = clip;
        newSource.loop = loop;
        newSource.Play();

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(CrossFadeCoroutine(newSource, duration));
    }

    public void FadeVolume(float targetMultiplier, float fadeDuration = -1f)
    {
        currentVolumeMultiplier = Mathf.Clamp01(targetMultiplier);
        var duration = fadeDuration < 0 ? defaultFadeDuration : fadeDuration;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeVolumeCoroutine(activeSource, duration));
    }

    public void StopMusic(float fadeDuration = -1f)
    {
        var duration = fadeDuration < 0 ? defaultFadeDuration : fadeDuration;
        currentVolumeMultiplier = 0f;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeVolumeCoroutine(activeSource, duration, stopOnZero: true));
    }

    public void SetVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        
        
        if (fadeCoroutine == null && activeSource != null)
        {
            activeSource.volume = masterVolume * currentVolumeMultiplier;
        }
    }

    private IEnumerator CrossFadeCoroutine(AudioSource newSource, float duration)
    {
        var oldSource = activeSource;
        activeSource = newSource;

        float time = 0;
        var startOldVol = oldSource.volume;

        while (time < duration)
        {
            time += Time.deltaTime;
            var t = time / duration;

            
            var currentTargetVol = masterVolume * currentVolumeMultiplier;

            oldSource.volume = Mathf.Lerp(startOldVol, 0, t);
            newSource.volume = Mathf.Lerp(0, currentTargetVol, t);
            yield return null;
        }

        oldSource.volume = 0;
        oldSource.Stop();
        newSource.volume = masterVolume * currentVolumeMultiplier;
        fadeCoroutine = null;
    }

    private IEnumerator FadeVolumeCoroutine(AudioSource source, float duration, bool stopOnZero = false)
    {
        float time = 0;
        var startVol = source.volume;

        while (time < duration)
        {
            time += Time.deltaTime;
            
            
            var currentTargetVol = masterVolume * currentVolumeMultiplier;
            source.volume = Mathf.Lerp(startVol, currentTargetVol, time / duration);
            yield return null;
        }

        source.volume = masterVolume * currentVolumeMultiplier;
        if (stopOnZero && source.volume <= 0f)
        {
            source.Stop();
        }
        fadeCoroutine = null;
    }
}