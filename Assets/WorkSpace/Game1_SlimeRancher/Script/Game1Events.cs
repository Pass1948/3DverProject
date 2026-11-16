public readonly struct SpawnEvent : IEvent
{
}

public readonly struct SelectSlotEvent : IEvent
{
    public readonly int Index;
    public SelectSlotEvent(int index) { Index = index; }
}

public readonly struct SlotChangedEvent : IEvent
{
    public readonly int Index;
    public readonly Slots Slot;
    public readonly ItemDefSO Def;
    public SlotChangedEvent(int index, Slots slot, ItemDefSO def) { Index = index; Slot = slot; Def = def; }
}
