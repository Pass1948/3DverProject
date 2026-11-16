using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SoundManager : ManagerBase
{
    private const string MasterVolumeParam = "Master";
    private const string BgmVolumeParam = "BGM";
    private const string SfxVolumeParam = "SFX";

    private AudioMixer mixer;
    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private SoundDataSO currentBGM;
    private Coroutine fadeCoroutine;
    private SoundData soundDB;

    private bool isBgmMuted;
    private bool isSfxMuted;
    private bool mixerWarned;
    private Action<SceneLoadedEvent> sceneLoadedHandler;

    // 이름 → 데이터 매핑
    private readonly Dictionary<string, SoundDataSO> soundDict = new Dictionary<string, SoundDataSO>();

    private enum SceneBGMAction { Play, Stop }

    private class SceneBGMRule
    {
        public SceneBGMAction action;
        public string bgmName;
        public float fadeSeconds;
    }

    // 씬별 BGM 규칙: 씬 경로 → 규칙 (규칙 없음 = 유지)
    private readonly Dictionary<string, SceneBGMRule> sceneBGMMap = new Dictionary<string, SceneBGMRule>();

    protected override void Initialize()
    {
        mixer = GameManager.Resource.Load<AudioMixer>(CorePaths.AudioMixer);
        if (mixer == null) Debug.LogWarning("[SoundManager] AudioMixer를 불러오지 못했습니다.", this);

        bgmSource = GameManager.Resource.Instantiate<AudioSource>(CorePaths.BGMSource, this.transform);
        sfxSource = GameManager.Resource.Instantiate<AudioSource>(CorePaths.SFXSource, this.transform);
        if (bgmSource == null) Debug.LogWarning("[SoundManager] BGM AudioSource를 만들지 못했습니다.", this);
        if (sfxSource == null) Debug.LogWarning("[SoundManager] SFX AudioSource를 만들지 못했습니다.", this);
        if (bgmSource != null) bgmSource.mute = isBgmMuted;
        if (sfxSource != null) sfxSource.mute = isSfxMuted;

        BuildDB();

        sceneLoadedHandler = OnSceneLoaded;
        GameManager.Event?.Subscribe(sceneLoadedHandler);
    }

    protected override void Shutdown()
    {
        if (sceneLoadedHandler != null)
        {
            GameManager.Event?.Unsubscribe(sceneLoadedHandler);
            sceneLoadedHandler = null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        soundDict.Clear();
        sceneBGMMap.Clear();
    }

    // ===================== DB =====================
    private void BuildDB()
    {
        // db 를 들고있을때 실제 오브젝트 참조하고 있다면 메모리 점유가 커짐 -> 이후 어드레서블 이용하면 개선 가능
        soundDB = GameManager.Resource.Load<SoundData>(CorePaths.SoundData);

        soundDict.Clear();

        if (soundDB == null || soundDB.soundInfo == null || soundDB.soundInfo.Count == 0)
        {
            Debug.LogWarning("[SoundManager] SoundDB 비어있음. SoundData SO를 할당하고 항목을 추가하세요.", this);
            return;
        }

        foreach (var d in soundDB.soundInfo)
        {
            if (d == null) continue;
            if (string.IsNullOrWhiteSpace(d.soundName))
            {
                Debug.LogWarning("[SoundManager] soundName이 비어있는 항목이 있습니다.", this);
                continue;
            }
            if (!soundDict.TryAdd(d.soundName, d))
            {
                Debug.LogWarning($"[SoundManager] 중복 soundName: {d.soundName}. 첫 항목만 사용합니다.", this);
            }
        }
    }

    public void ReloadDB()
    {
        BuildDB();
    }

    // ===================== 씬별 BGM =====================
    public void SetSceneBGM(string scenePath, string bgmName, float fadeSeconds = 0f)
    {
        if (string.IsNullOrEmpty(scenePath) || string.IsNullOrEmpty(bgmName))
        {
            Debug.LogWarning("[SoundManager] SetSceneBGM: scenePath와 bgmName은 비어 있을 수 없습니다.", this);
            return;
        }
        sceneBGMMap[scenePath] = new SceneBGMRule { action = SceneBGMAction.Play, bgmName = bgmName, fadeSeconds = fadeSeconds };
    }

    public void SetSceneBGMStop(string scenePath, float fadeSeconds = 0f)
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogWarning("[SoundManager] SetSceneBGMStop: scenePath는 비어 있을 수 없습니다.", this);
            return;
        }
        sceneBGMMap[scenePath] = new SceneBGMRule { action = SceneBGMAction.Stop, fadeSeconds = fadeSeconds };
    }

    public void RemoveSceneBGM(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath)) return;
        sceneBGMMap.Remove(scenePath);
    }

    public void ClearSceneBGM()
    {
        sceneBGMMap.Clear();
    }

    private void OnSceneLoaded(SceneLoadedEvent e)
    {
        if (e.Mode != LoadSceneMode.Single) return;
        if (!sceneBGMMap.TryGetValue(e.Path, out SceneBGMRule rule)) return;

        if (rule.action == SceneBGMAction.Play)
        {
            if (rule.fadeSeconds > 0f) ChangeBGM(rule.bgmName, rule.fadeSeconds);
            else PlayBGM(rule.bgmName);
        }
        else
        {
            StopBGM(rule.fadeSeconds);
        }
    }

    // ===================== 이름/데이터로 재생 =====================
    public void PlaySound(string soundName)
    {
        if (TryGet(soundName, out var data))
            PlaySound(data);
        else
            Debug.LogWarning($"[SoundManager] 사운드 미등록: {soundName}", this);
    }

    public void PlaySound(SoundDataSO data)
    {
        if (data == null) return;

        if (data.soundType == SoundType.BGM) PlayBGM(data);
        else PlaySFX(data);
    }

    // ===================== BGM =====================
    public void PlayBGM(string soundName, bool loop = true)
    {
        if (TryGet(soundName, out var data))
            PlayBGM(data, loop);
        else
            Debug.LogWarning($"[SoundManager] BGM 미등록: {soundName}", this);
    }

    public void PlayBGM(SoundDataSO data, bool loop = true)
    {
        if (data == null || bgmSource == null) return;

        if (data.soundType != SoundType.BGM)
        {
            Debug.LogWarning($"[SoundManager] BGM 타입이 아닙니다: {data.soundName}", this);
            return;
        }

        StopFade();

        var clip = GetAudioClip(data);
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] BGM 클립 없음: Resources/Sound/{data.soundName} (파일 이름은 soundName과 같아야 합니다)", this);
            return;
        }

        bgmSource.loop = loop;

        // 동일 클립이면 볼륨만 갱신하고 재생 유지.
        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            bgmSource.volume = data.volume;
            currentBGM = data;
            return;
        }

        bgmSource.clip = clip;
        bgmSource.volume = data.volume;
        bgmSource.Play();
        currentBGM = data;
    }

    public void StopBGM(float fadeSeconds = 0f)
    {
        if (bgmSource == null || !bgmSource.isPlaying)
        {
            currentBGM = null;
            return;
        }

        StopFade();

        if (fadeSeconds <= 0f)
        {
            bgmSource.Stop();
            currentBGM = null;
            return;
        }

        fadeCoroutine = StartCoroutine(StopBGMCoroutine(fadeSeconds));
    }

    public void ChangeBGM(string soundName, float fadeSeconds, bool loop = true)
    {
        if (!TryGet(soundName, out var data))
        {
            Debug.LogWarning($"[SoundManager] BGM 미등록: {soundName}", this);
            return;
        }
        if (bgmSource == null) return;

        if (data.soundType != SoundType.BGM)
        {
            Debug.LogWarning($"[SoundManager] BGM 타입이 아닙니다: {data.soundName}", this);
            return;
        }

        var clip = GetAudioClip(data);
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] BGM 클립 없음: Resources/Sound/{data.soundName} (파일 이름은 soundName과 같아야 합니다)", this);
            return;
        }

        // 같은 곡이 재생 중이면 재시작하지 않고 볼륨만 갱신
        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            StopFade();
            bgmSource.loop = loop;
            bgmSource.volume = data.volume;
            currentBGM = data;
            return;
        }

        if (fadeSeconds <= 0f || !bgmSource.isPlaying)
        {
            PlayBGM(data, loop);
            return;
        }

        StopFade();
        currentBGM = data;
        fadeCoroutine = StartCoroutine(ChangeBGMCoroutine(clip, data, loop, fadeSeconds));
    }

    // ===================== SFX =====================
    public void PlaySFX(string soundName)
    {
        if (TryGet(soundName, out var data)) PlaySFX(data);
        else Debug.LogWarning($"[SoundManager] SFX 미등록: {soundName}", this);
    }

    public void PlaySFX(SoundDataSO data)
    {
        if (data == null || sfxSource == null) return;

        if (data.soundType != SoundType.SFX)
        {
            Debug.LogWarning($"[SoundManager] SFX 타입이 아닙니다: {data.soundName}", this);
            return;
        }

        var clip = GetAudioClip(data);
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] SFX 클립 없음: Resources/Sound/{data.soundName} (파일 이름은 soundName과 같아야 합니다)", this);
            return;
        }

        sfxSource.PlayOneShot(clip, data.volume);
    }

    // ===================== 볼륨/뮤트 =====================
    public void SetMute(SoundType type, bool isOn)
    {
        if (type == SoundType.BGM) SetBGMMute(isOn);
        else SetSFXMute(isOn);
    }

    public void SetBGMMute(bool isOn)
    {
        isBgmMuted = isOn;
        if (bgmSource != null) bgmSource.mute = isOn;
    }

    public void SetSFXMute(bool isOn)
    {
        isSfxMuted = isOn;
        if (sfxSource != null) sfxSource.mute = isOn;
    }

    public void SetVolume(VolumeType type, float volume)
    {
        if (!CheckMixer()) return;

        string param = GetVolumeParam(type);
        float volumeDB = (volume <= 0.0001f) ? -80f : Mathf.Log10(Mathf.Clamp01(volume)) * 20f;
        mixer.SetFloat(param, volumeDB);
    }

    public float GetVolume(VolumeType type)
    {
        if (!CheckMixer()) return 1f;

        string param = GetVolumeParam(type);
        if (mixer.GetFloat(param, out float db))
            return Mathf.Pow(10f, db / 20f);

        return 1f;
    }

    public void ResetVolumes()
    {
        SetVolume(VolumeType.Master, 1f);
        SetVolume(VolumeType.BGM, 1f);
        SetVolume(VolumeType.SFX, 1f);
    }

    private static string GetVolumeParam(VolumeType type)
    {
        return type == VolumeType.Master ? MasterVolumeParam : type == VolumeType.BGM ? BgmVolumeParam : SfxVolumeParam;
    }

    private bool CheckMixer()
    {
        if (mixer != null) return true;
        if (!mixerWarned)
        {
            mixerWarned = true;
            Debug.LogWarning("[SoundManager] AudioMixer가 없어 볼륨을 처리할 수 없습니다.", this);
        }
        return false;
    }

    // ===================== 페이드 =====================
    public void FadeOutBGM(float duration)
    {
        if (currentBGM == null || bgmSource == null) return;
        StopFade();
        fadeCoroutine = StartCoroutine(FadeVolumeCoroutine(bgmSource.volume, 0f, duration));
    }

    public void FadeInBGM(float duration)
    {
        if (currentBGM == null || bgmSource == null) return;
        StopFade();
        // target을 currentBGM.volume로
        fadeCoroutine = StartCoroutine(FadeVolumeCoroutine(bgmSource.volume, currentBGM.volume, duration));
    }

    private void StopFade()
    {
        if (fadeCoroutine == null) return;
        StopCoroutine(fadeCoroutine);
        fadeCoroutine = null;
    }

    private IEnumerator FadeVolumeCoroutine(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            bgmSource.volume = to;
            fadeCoroutine = null;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        bgmSource.volume = to;
        fadeCoroutine = null;
    }

    private IEnumerator StopBGMCoroutine(float duration)
    {
        float from = bgmSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(from, 0f, t / duration);
            yield return null;
        }
        bgmSource.volume = 0f;
        bgmSource.Stop();
        currentBGM = null;
        fadeCoroutine = null;
    }

    private IEnumerator ChangeBGMCoroutine(AudioClip clip, SoundDataSO data, bool loop, float duration)
    {
        float from = bgmSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(from, 0f, t / duration);
            yield return null;
        }

        bgmSource.volume = 0f;
        bgmSource.loop = loop;
        bgmSource.clip = clip;
        bgmSource.Play();

        t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0f, data.volume, t / duration);
            yield return null;
        }
        bgmSource.volume = data.volume;
        fadeCoroutine = null;
    }

    // ===================== 유틸 =====================
    private bool TryGet(string soundName, out SoundDataSO data)
    {
        if (string.IsNullOrWhiteSpace(soundName))
        {
            data = null;
            return false;
        }

        return soundDict.TryGetValue(soundName, out data) && data != null;
    }

    // SoundDataSO.audioClips는 이번 버전에서 사용하지 않는다. (R5에서 재정의)
    private AudioClip GetAudioClip(SoundDataSO data)
    {
        if (data == null || string.IsNullOrEmpty(data.soundName)) return null;
        return GameManager.Resource.Load<AudioClip>(CorePaths.SoundRoot + data.soundName);
    }
}
