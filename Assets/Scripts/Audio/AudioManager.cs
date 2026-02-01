using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music Tracks")]
    public AudioClip mainMenuTheme;
    public AudioClip gameTheme;

    [Header("Settings")]
    [Range(0f, 1f)] public float masterVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f; // New SFX Volume
    public float fadeDuration = 2.0f;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource sfxSource; // New Source for SFX
    private bool isSourceAPlayingActive = true;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create Music Sources
        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();
        
        // --- NEW SFX SECTION ---
        // Create SFX Source
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.volume = sfxVolume;
        // -----------------------

        sourceA.loop = true;
        sourceB.loop = true;
        sourceA.volume = 0;
        sourceB.volume = 0;
    }

    private void Start()
    {
        if (mainMenuTheme != null) PlayMusic(mainMenuTheme);
    }

    // --- NEW SFX FUNCTION ---
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            // PlayOneShot allows multiple sounds to overlap (rapid clicking)
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
    // ------------------------

    // ... (Keep your existing PlayMusic / Crossfade logic here) ...
    public void PlayMusic(AudioClip newClip)
    {
        AudioSource activeSource = isSourceAPlayingActive ? sourceA : sourceB;
        if (activeSource.clip == newClip) return;
        StopAllCoroutines();
        StartCoroutine(CrossfadeRoutine(newClip));
    }
    
    public void SwitchToGameTheme() { PlayMusic(gameTheme); }
    public void SwitchToMenuTheme() { PlayMusic(mainMenuTheme); }

    private IEnumerator CrossfadeRoutine(AudioClip newClip)
    {
        AudioSource activeSource = isSourceAPlayingActive ? sourceA : sourceB;
        AudioSource newSource = isSourceAPlayingActive ? sourceB : sourceA;
        newSource.clip = newClip;
        newSource.Play();
        newSource.volume = 0;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float percent = timer / fadeDuration;
            activeSource.volume = Mathf.Lerp(masterVolume, 0, percent);
            newSource.volume = Mathf.Lerp(0, masterVolume, percent);
            yield return null;
        }
        activeSource.Stop();
        activeSource.volume = 0;
        newSource.volume = masterVolume;
        isSourceAPlayingActive = !isSourceAPlayingActive;
    }
}