using UnityEngine.SceneManagement;

public readonly struct SceneChangingEvent : IEvent
{
    public readonly string From;
    public readonly string To;
    public SceneChangingEvent(string from, string to) { From = from; To = to; }
}

public readonly struct SceneLoadedEvent : IEvent
{
    public readonly string Path;
    public readonly string Name;
    public readonly LoadSceneMode Mode;
    public SceneLoadedEvent(string path, string name, LoadSceneMode mode) { Path = path; Name = name; Mode = mode; }
}
