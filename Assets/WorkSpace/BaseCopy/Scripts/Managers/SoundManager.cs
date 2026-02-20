using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    private AudioMixer mixer;
    private readonly string masterVolumeParam = "Master";
    private readonly string bgmVolumeParam = "BGM";
    private readonly string sfxVolumeParam = "SFX";
    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private SoundDataSO currentBGM;
    private Coroutine fadeCoroutine;
    private SoundData soundDB;

    // 이름 → 데이터 매핑
    private readonly Dictionary<string, SoundDataSO> soundDict = new();

    // (옵션) 씬별 BGM 매핑: 씬이름 → soundName
    // string 으로 string 을 매핑해서 의미가 크게 없을듯?
    private Dictionary<string, string> sceneBGMMapping = new()
    {
        { "StartScene", "StartBGM" },
        { "MainScene",  "MainBGM"  },
        { "PlayScene",  "PlayBGM"  },
    };

    private void Awake()
    {
        InitializeDB();  // DB를 딕셔너리로 빌드
        // 필요하면 여기서 씬 이벤트 구독 후 씬 진입 시 PlaySceneBGM 호출
    }

    // ===================== DB 초기화 =====================
    private void InitializeDB()
    {
        mixer = GameManager.Resource.Load<AudioMixer>(GamePath.Sound + "AudioMixer");
        bgmSource = GameManager.Resource.Instantiate<AudioSource>(GamePath.Sound+"BGM", this.transform);
        sfxSource = GameManager.Resource.Instantiate<AudioSource>(GamePath.Sound + "SFX", this.transform);

        // db 를 들고있을때 실제 오브젝트 참조하고 있다면 메모리 점유가 커짐 -> 이후 어드레서블 이용하면 개선 가능
        soundDB = GameManager.Resource.Load<SoundData>(GamePath.Data+"SoundData");

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

    // ===================== 씬별 BGM =====================
    public void PlaySceneBGM(string sceneName)
    {
        if (sceneBGMMapping != null && sceneBGMMapping.TryGetValue(sceneName, out string bgmName))
            PlayBGM(bgmName);
    }

    // ===================== Static 헬퍼 =====================
    public static void OnPlaySound(string soundName)
    {
        GameManager.Sound.PlaySound(soundName);
    }
    public static void OnPlaySound(SoundDataSO soundData)
    {
        GameManager.Sound.PlaySound(soundData);
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
        if (data == null || data.soundType != SoundType.BGM) return;

        var clip = GetAudioClip(data);
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] BGM 클립 없음: {data.soundName}", this);
            return;
        }

        bgmSource.loop = loop;

        // 동일 클립이면 넘어갈지 여부는 선택. 볼륨만 갱신하고 재생 유지.
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

    // ===================== SFX =====================
    public void PlaySFX(string soundName)
    {
        if (TryGet(soundName, out var data)) PlaySFX(data);
        else Debug.LogWarning($"[SoundManager] SFX 미등록: {soundName}", this);
    }

    public void PlaySFX(SoundDataSO data)
    {
        if (data == null || data.soundType != SoundType.SFX) return;

        var clip = GetAudioClip(data);
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] SFX 클립 없음: {data.soundName}", this);
            return;
        }

        sfxSource.volume = data.volume;
        sfxSource.PlayOneShot(clip);
    }

    // ===================== 볼륨/뮤트 =====================
    public static void SetMute(SoundType type, bool isOn)
    {
        if (type == SoundType.BGM) SetBGMMute(isOn);
        else SetSFXMute(isOn);
    }

    public static void SetBGMMute(bool isOn)
    {
        if (GameManager.Sound?.bgmSource == null) return;
        GameManager.Sound.bgmSource.mute = isOn;
    }

    public static void SetSFXMute(bool isOn)
    {
        if (GameManager.Sound?.sfxSource == null) return;
        GameManager.Sound.sfxSource.mute = isOn;
    }

    public void SetVolume(VolumeType type, float volume)
    {
        string param = type == VolumeType.Master ? masterVolumeParam : type == VolumeType.BGM ? bgmVolumeParam : sfxVolumeParam;
        float volumeDB = (volume <= 0.0001f) ? -80f : Mathf.Log10(Mathf.Clamp01(volume)) * 20f;
        mixer.SetFloat(param, volumeDB);
    }

    public float GetVolume(VolumeType type)
    {
        string param = type == VolumeType.Master ? masterVolumeParam :
                       type == VolumeType.BGM ? bgmVolumeParam : sfxVolumeParam;

        if (mixer != null && mixer.GetFloat(param, out float db))
            return Mathf.Pow(10f, db / 20f);

        return 1f;
    }

    public void ResetVolumes()
    {
        SetVolume(VolumeType.Master, 1f);
        SetVolume(VolumeType.BGM, 1f);
        SetVolume(VolumeType.SFX, 1f);
    }

    // ===================== 페이드 =====================
    public void FadeOutBGM(float duration)
    {
        if (currentBGM == null || bgmSource == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeVolumeCoroutine(bgmSource.volume, 0f, duration));
    }

    public void FadeInBGM(float duration)
    {
        if (currentBGM == null || bgmSource == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        // target을 currentBGM.volume로
        fadeCoroutine = StartCoroutine(FadeVolumeCoroutine(bgmSource.volume, currentBGM.volume, duration));
    }

    private System.Collections.IEnumerator FadeVolumeCoroutine(float from, float to, float duration)
    {
        if (duration <= 0f) { bgmSource.volume = to; yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        bgmSource.volume = to;
    }

    // ===================== 유틸 =====================
    private bool TryGet(string soundName, out SoundDataSO data)
    {
        if (string.IsNullOrWhiteSpace(soundName))
        {
            data = null;
            return false;
        }

        if (soundDict.TryGetValue(soundName, out data) && data != null)
            return true;

        // DB가 갱신되었을 수 있으니 한 번 재빌드 시도(옵션)
        InitializeDB();
        return soundDict.TryGetValue(soundName, out data) && data != null;
    }

    private AudioClip GetRandomClip(SoundDataSO data)
    {
        if (data == null || data.audioClips == null || data.audioClips.Length == 0) return null;
        if (data.audioClips.Length == 1) return data.audioClips[0];
        int idx = Random.Range(0, data.audioClips.Length);
        return data.audioClips[idx];
    }

    private AudioClip GetAudioClip(SoundDataSO data)
    {
        AudioClip a = GameManager.Resource.Load<AudioClip>(GamePath.Sound + data.soundName);
        if (data == null || data.soundName == null || a == null) return null;
        return a;
    }
}
