using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGameEvent", menuName = "Assets/Events/Game Event")]
public class GameEvent : ScriptableObject {
    private readonly List<IGameEventListener> EventListeners = new List<IGameEventListener>();

    public void Raise() {
        for (int i = EventListeners.Count - 1; i >= 0; i--) EventListeners[i].OnEventRaised();
    }

    public void AddListener(IGameEventListener listener) {
        if (!EventListeners.Contains(listener)) EventListeners.Add(listener);
    }

    public void RemoveListener(IGameEventListener listener) {
        if (EventListeners.Contains(listener)) EventListeners.Remove(listener);
    }
}
