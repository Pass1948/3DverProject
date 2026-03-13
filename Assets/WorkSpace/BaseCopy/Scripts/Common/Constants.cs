using System;

public enum EventType
{
    // --- 슬라인랜처 ---
    Spawn,
    SelectSlot,
    SlotChanged,
    // --- PEAK ---
    StaminaChanged,
    ClimbCheck,


}



public static class GamePath
{
    public const string UI = "UI/";
    public const string Canvas = UI + "Canvas";
    public const string Unit = "Unit/";
    public const string Data = "Data/";
    public const string Sound = "Sound/";
}

// --- 슬라임랜쳐 ---
public static class Game1Path
{
    public const string UI = "Game1/UI/";
    public const string Prefab = "Game1/Prefab/";
    public const string Sound = "Game1/Sound/";
}
// --- PEAK ---
public static class Game2Path
{
    public const string UI = "Game2/UI/";
    public const string Prefab = "Game2/Prefab/";
}
public enum LocomotionState
{
    Grounded,
    Airborne,
    Climbing
}
