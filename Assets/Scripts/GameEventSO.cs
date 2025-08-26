using UnityEngine;

public abstract class GameEventSO : ScriptableObject
{
    public string eventName;
    public float duration = 10f;
    public abstract void StartEvent(GameEventManager manager);
    public abstract void EndEvent(GameEventManager manager);
}