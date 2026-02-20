using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum SoundType
{
    BGM,
    SFX
}
public enum VolumeType
{
    Master,
    BGM,
    SFX
}

[System.Serializable]
public class SoundDataSO
{
    [Header("path/Key (경로/고유 이름)")]
    public string soundName;                 // ex) "StartBGM", "ButtonClick"

    [Header("종류 & 기본 볼륨")]
    public SoundType soundType = SoundType.BGM;

    [Header("종류 & 기본 볼륨")]
    public VolumeType volumeType = VolumeType.Master;

    [Range(0f, 1f)] public float volume = 1f;

    [Header("클립들 (랜덤 재생)")] // 랜덤재생용
    public AudioClip[] audioClips;           // 하나만 넣어도 됨
}
