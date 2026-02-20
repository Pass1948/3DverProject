using UnityEngine;

[System.Serializable]
public class ItemDefSO
{
    [Header("Key")]
    public int id;    // 고유 아이디값
    public string name;
    [Header("UI")]
    public Sprite icon;
    [Header("프리팹")]
    public string prefabPath;
}
