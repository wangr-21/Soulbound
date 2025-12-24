using UnityEngine;
using System.Collections;

public class MainBGMController : MonoBehaviour
{
    public static MainBGMController Instance;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void FadeOut(float duration = 0.5f)
    {
        StartFade(0f, duration);
    }

    public void FadeIn(float targetVolume = 1f, float duration = 0.5f)
    {
        audioSource.UnPause();
        StartFade(targetVolume, duration);
    }

    private void StartFade(float targetVolume, float duration)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(targetVolume, duration));
    }

    private IEnumerator FadeRoutine(float targetVolume, float duration)
    {
        float startVolume = audioSource.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;

        if (Mathf.Approximately(targetVolume, 0f))
            audioSource.Pause();
    }
}
