using System;

public enum EventType
{ 
    Spawn,
    SelectSlot,
    SlotChanged,
}
public static class GamePath
{
    public const string UI = "UI/";
    public const string Canvas = UI + "Canvas";
    public const string Unit = "Unit/";
    public const string Data = "Data/";
    public const string Sound = "Sound/";
}
public static class Game1Path // 슬라임랜쳐
{
    public const string UI = "Game1/UI/";
    public const string Prefab = "Game1/Prefab/";
    public const string Sound = "Game1/Sound/";
}
