using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameEventListener : MonoBehaviour, IGameEventListener {
    //[InfoBox("Game Event to Listen to.")]
    [Tooltip("Event to register with.")]
    [SerializeField] private GameEvent gameEvent;

    [Tooltip("Response to invoke when Event is raised.")]
    //[InfoBox("Unity Events to perform when the Game Events is Raised")]
    [SerializeField] private UnityEvent onEventTriggered;

    public void OnEnable() {
        if (gameEvent != null) gameEvent.AddListener(this);
    }

    public void OnDisable() {
        gameEvent.RemoveListener(this);
    }

    public void OnEventRaised() {
        onEventTriggered?.Invoke();
    }
}
