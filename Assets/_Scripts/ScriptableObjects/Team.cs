using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewTeam", menuName = "Assets/Teams/Team")]
public class Team : ScriptableObject {
    public string defaultLayerName;
    public int defaultLayerInt;
    public TeamFaction defaultTeam;
    public Color defaultColor;

    public TeamConfiguration defaultTeamConfiguration = new TeamConfiguration();

    public void OnEnable() {
        defaultTeamConfiguration = new TeamConfiguration(defaultLayerName, defaultLayerInt, defaultTeam, defaultColor);
    }

    public void OnValidate() {
        defaultTeamConfiguration = new TeamConfiguration(defaultLayerName, defaultLayerInt, defaultTeam, defaultColor);
    }
}

public enum TeamFaction {
    Neutral,
    TeamOne,
    TeamTwo,
    TeamThree,
    TeamFour,
    IA
}

[Serializable]
public struct TeamConfiguration {
    [SerializeField] private string teamName;
    [SerializeField] private int teamLayer;
    [SerializeField] private TeamFaction teamFaction;
    [SerializeField] private Color teamColor;

    public TeamConfiguration(string _teamName, int _layer, TeamFaction _team, Color _teamColor) {
        teamName = _teamName;
        teamLayer = _layer;
        teamFaction = _team;
        teamColor = _teamColor;
    }

    public string Name => teamName;
    public int Layer => teamLayer;
    public TeamFaction TeamFaction => teamFaction;
    public Color Color => teamColor;
}
