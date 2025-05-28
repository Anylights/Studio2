using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

// 音频管理类
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance = null;

    public Sound[] sounds;

    private Dictionary<string, AudioSource> audioSourcesDic;
    private Dictionary<string, Coroutine> fadeCoroutines;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        audioSourcesDic = new Dictionary<string, AudioSource>();
        fadeCoroutines = new Dictionary<string, Coroutine>();

        for (int i = 0; i < sounds.Length; i++)
        {
            GameObject soundGameObject = new GameObject("Sound_" + i + "_" + sounds[i].name);
            soundGameObject.transform.SetParent(this.transform);
            sounds[i].SetSource(soundGameObject.AddComponent<AudioSource>());
            audioSourcesDic.Add(sounds[i].name, soundGameObject.GetComponent<AudioSource>());
        }

        PlaySound("Start", 1, false);
    }

    [YarnCommand("play_sound")]
    public static void Play(string soundName, float volume = 1, bool loop = false)
    {
        if (Instance == null)
        {
            Debug.LogError("AudioManager instance not found!");
            return;
        }
        Instance.PlaySound(soundName, volume, loop);
        Debug.Log("PlaySound: " + soundName);
    }

    public void PlaySound(string soundName, float volume = 1, bool loop = false)
    {
        if (!audioSourcesDic.ContainsKey(soundName))
        {
            Debug.LogWarning($"Sound {soundName} not found!");
            return;
        }

        AudioSource source = audioSourcesDic[soundName];
        if (!source.isPlaying)
        {
            source.volume = volume;
            source.loop = loop;
            source.Play();
        }
    }

    /// <summary>
    /// 检查指定名称的音效是否存在
    /// </summary>
    /// <param name="soundName">音效名称</param>
    /// <returns>如果音效存在返回true，否则返回false</returns>
    public bool HasSound(string soundName)
    {
        return audioSourcesDic != null && audioSourcesDic.ContainsKey(soundName);
    }

    [YarnCommand("pause_sound")]
    public static void Pause(string soundName)
    {
        if (Instance == null)
        {
            Debug.LogError("AudioManager instance not found!");
            return;
        }
        Instance.PauseSound(soundName);
    }

    private void PauseSound(string soundName)
    {
        if (!audioSourcesDic.ContainsKey(soundName))
        {
            Debug.LogWarning($"Sound {soundName} not found!");
            return;
        }

        AudioSource source = audioSourcesDic[soundName];
        source.Pause();
    }

    [YarnCommand("unpause_sound")]
    public static void UnPause(string soundName)
    {
        if (Instance == null)
        {
            Debug.LogError("AudioManager instance not found!");
            return;
        }
        Instance.UnPauseSound(soundName);
    }

    private void UnPauseSound(string soundName)
    {
        if (!audioSourcesDic.ContainsKey(soundName))
        {
            Debug.LogWarning($"Sound {soundName} not found!");
            return;
        }

        AudioSource source = audioSourcesDic[soundName];
        source.UnPause();
    }

    [YarnCommand("set_volume")]
    public static void SetVolume(string soundName, float volume)
    {
        if (Instance == null)
        {
            Debug.LogError("AudioManager instance not found!");
            return;
        }
        Instance.SetSoundVolume(soundName, volume);
    }

    private void SetSoundVolume(string soundName, float volume)
    {
        if (!audioSourcesDic.ContainsKey(soundName))
        {
            Debug.LogWarning($"Sound {soundName} not found!");
            return;
        }

        AudioSource source = audioSourcesDic[soundName];
        source.volume = volume;
    }

    [YarnCommand("stop_sound")]
    public static void Stop(string soundName)
    {
        if (Instance == null)
        {
            Debug.LogError("AudioManager instance not found!");
            return;
        }
        Instance.StopSound(soundName);
    }

    private void StopSound(string soundName)
    {
        if (!audioSourcesDic.ContainsKey(soundName))
        {
            Debug.LogWarning($"Sound {soundName} not found!");
            return;
        }

        AudioSource source = audioSourcesDic[soundName];
        source.Stop();
    }

    [YarnCommand("stop_sound_fade")]
    public static void StopWithFade(string soundName, float fadeDuration = 1f)
    {
        if (Instance == null)
        {
            Debug.LogError("AudioManager instance not found!");
            return;
        }
        Instance.StopSoundWithFade(soundName, fadeDuration);
    }

    private void StopSoundWithFade(string soundName, float fadeDuration = 1f)
    {
        if (!audioSourcesDic.ContainsKey(soundName))
        {
            Debug.LogWarning($"Sound {soundName} not found!");
            return;
        }

        // 如果已经有正在进行的渐出协程，先停止它
        if (fadeCoroutines.ContainsKey(soundName) && fadeCoroutines[soundName] != null)
        {
            StopCoroutine(fadeCoroutines[soundName]);
        }

        // 开始新的渐出协程
        fadeCoroutines[soundName] = StartCoroutine(FadeOut(soundName, fadeDuration));
    }

    [YarnCommand("stop_all_sounds")]
    public static void StopAll()
    {
        if (Instance == null)
        {
            Debug.LogError("AudioManager instance not found!");
            return;
        }
        Instance.StopAllSounds();
    }

    private void StopAllSounds()
    {
        foreach (var audioSource in audioSourcesDic.Values)
        {
            audioSource.Stop();
        }
    }

    [YarnCommand("stop_all_sounds_fade")]
    public static void StopAllWithFade(float fadeDuration = 1f)
    {
        if (Instance == null)
        {
            Debug.LogError("AudioManager instance not found!");
            return;
        }
        Instance.StopAllSoundsWithFade(fadeDuration);
    }

    private void StopAllSoundsWithFade(float fadeDuration = 1f)
    {
        foreach (var soundName in audioSourcesDic.Keys)
        {
            StopSoundWithFade(soundName, fadeDuration);
        }
    }

    private IEnumerator FadeOut(string soundName, float fadeDuration)
    {
        AudioSource source = audioSourcesDic[soundName];
        float startVolume = source.volume;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
            yield return null;
        }

        source.Stop();
        source.volume = startVolume; // 恢复原始音量
        fadeCoroutines.Remove(soundName);
    }
}

[System.Serializable]
public class Sound
{
    public AudioClip clip;
    public string name;

    public float volume = 1;
    public bool loop = false;

    public Sound(string name, AudioClip clip)
    {
        this.name = name;
        this.clip = clip;
    }

    public void SetSource(AudioSource source)
    {
        source.clip = clip;
        source.volume = volume;
        source.loop = loop;
    }
}