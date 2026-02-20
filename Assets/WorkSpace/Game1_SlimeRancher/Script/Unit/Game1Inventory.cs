using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Game1Inventory : MonoBehaviour
{
    [Header("데이터")]
    private ItemDef itemDatabase;

    [Header("Slots")]
    [SerializeField] private int slotCount = 4;
    [SerializeField] private int maxPerSlot = 50;

    private Slots[] slots;
    public Slots[] _Slots => slots;

    // 빠른 조회
    private readonly Dictionary<int, ItemDefSO> defById = new();

    public int SlotCount => slots?.Length ?? 0;

    private void Awake()
    {
        itemDatabase = GameManager.Resource.Load<ItemDef>("Game1/Data/ItemDef");
        EnsureSlotsArray(); 
        RebuildCache();
        SyncAll();
    }

    public bool TryAdd(int id, int amount = 1)
    {
        if (id <= 0 || amount <= 0) return false;
        if (!defById.ContainsKey(id)) return false;

        // 1) 같은 id 슬롯 먼저 채우기
        for (int i = 0; i < slots.Length && amount > 0; i++)
        {
            if (slots[i].IsEmpty) continue;
            if (slots[i].id != id) continue;
            if (slots[i].count >= maxPerSlot) continue;

            int add = Mathf.Min(amount, maxPerSlot - slots[i].count);
            slots[i].count += add;
            amount -= add;
            RaiseChanged(i);
        }

        // 2) 빈 슬롯에 배치
        for (int i = 0; i < slots.Length && amount > 0; i++)
        {
            if (!slots[i].IsEmpty) continue;

            int add = Mathf.Min(amount, maxPerSlot);
            slots[i].id = id;
            slots[i].count = add;
            amount -= add;
            RaiseChanged(i);
        }

        return amount <= 0;
    }

    public bool TryEject(int slotIndex, Vector3 spawnPos, Vector3 dir, float impulse = 8f)
    {
        if ((uint)slotIndex >= (uint)slots.Length) return false;
        if (slots[slotIndex].IsEmpty) return false;

        int id = slots[slotIndex].id;
        if (!defById.TryGetValue(id, out var def) || def == null) return false;
        if (string.IsNullOrEmpty(def.prefabPath)) return false;

        Vector3 fwd = (dir.sqrMagnitude < 1e-6f) ? Vector3.forward : dir.normalized;
        Quaternion rot = Quaternion.LookRotation(fwd);

        GameObject go = GameManager.Resource.Instantiate<GameObject>(def.prefabPath, spawnPos, rot);
        if (go == null) return false;

        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
#if UNITY_6000_0_OR_NEWER
            rb.maxLinearVelocity = Mathf.Max(rb.maxLinearVelocity, impulse);
            rb.linearVelocity = fwd * impulse;
#else
            rb.velocity = fwd * impulse;
#endif
        }

        slots[slotIndex].count--;
        if(slots[slotIndex].count > 0) GameManager.Sound.PlaySFX("Game1/EjectSFX");
        if (slots[slotIndex].count <= 0) slots[slotIndex].Clear();

        RaiseChanged(slotIndex);
        return true;
    }

    public ItemDefSO GetDefById(int id)
    {
        defById.TryGetValue(id, out var def);
        return def;
    }

    public void RebuildCache()
    {
        defById.Clear();
        if (itemDatabase == null) return;

        var list = itemDatabase.itemDefInfo;
        for (int i = 0; i < list.Count; i++)
        {
            var def = list[i];
            if (def == null || def.id <= 0) continue;
            if (defById.ContainsKey(def.id)) continue;
            defById.Add(def.id, def);
        }
    }

    private void EnsureSlotsArray()
    {
        if (slotCount < 1) slotCount = 1;
        if (slots == null || slots.Length != slotCount)
            slots = new Slots[slotCount];
    }

    private void RaiseChanged(int i)
    {
        defById.TryGetValue(slots[i].id, out var def);
        GameManager.Event.Publish(EventType.SlotChanged, i, slots[i], def);
    }

    private void SyncAll()
    {
        for (int i = 0; i < slots.Length; i++) RaiseChanged(i);
    }
}
