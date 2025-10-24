using UnityEngine;
using System.Collections.Generic;

public class GameEventManager : MonoBehaviour
{
    public List<GameEventSO> events;
    public float eventInterval; 
    public float eventTimer;
    private GameEventSO currentEvent;
    public float currentEventTimer;

    void Start()
    {
        eventInterval = Random.Range(45f,70f);
        eventTimer = eventInterval;
    }
    void Update()
    {
        if (currentEvent == null)
        {
            eventTimer -= Time.deltaTime;
            if (eventTimer <= 0f)
            {
                TriggerRandomEvent();
                eventTimer = eventInterval;
            }
        }
        else
        {
            currentEventTimer -= Time.deltaTime;
            if (currentEventTimer <= 0f)
            {
                EndEvent();
            }
        }
    }
    void TriggerRandomEvent()
    {
        int index = Random.Range(0, events.Count);
        currentEvent = events[index];
        currentEventTimer = currentEvent.duration;

        currentEvent.StartEvent(this);
    }

    void EndEvent()
    {
        if (currentEvent != null)
        {
            currentEvent.EndEvent(this);
            currentEvent = null;
        }
        eventInterval = Random.Range(45f, 70f);
        eventTimer = eventInterval;
    }

    public GameEvent GetCurrentEvent()
    {
        if (currentEvent != null)
        {
            // Se um evento (Scriptable Object) está ativo,
            // retorna o tipo que configuramos no Inspector.
            return currentEvent.eventType;
        }
        else
        {
            // Se nenhum evento está ativo, retorna 'None'.
            return GameEvent.None;
        }
    }
}