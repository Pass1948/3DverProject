using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDef", menuName = "Scriptable Objects/ItemDef")]
public class ItemDef : ScriptableObject
{
    public List<ItemDefSO> itemDefInfo = new List<ItemDefSO>();
}
