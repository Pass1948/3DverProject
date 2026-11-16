public readonly struct StaminaChangedEvent : IEvent
{
    public readonly float Current;
    public readonly float Max;
    public StaminaChangedEvent(float current, float max) { Current = current; Max = max; }
}

public readonly struct ClimbCheckEvent : IEvent
{
    public readonly bool HasClimbSurface;
    public readonly LocomotionState State;
    public readonly float Stamina;
    public ClimbCheckEvent(bool hasClimbSurface, LocomotionState state, float stamina) { HasClimbSurface = hasClimbSurface; State = state; Stamina = stamina; }
}

public readonly struct InteractTextEvent : IEvent
{
    public readonly PeakCarryable Target;
    public InteractTextEvent(PeakCarryable target) { Target = target; }
}

public readonly struct InfoButtonTextEvent : IEvent
{
    public readonly bool IsHeld;
    public InfoButtonTextEvent(bool isHeld) { IsHeld = isHeld; }
}
