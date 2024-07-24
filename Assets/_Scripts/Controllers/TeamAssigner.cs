using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TeamAssigner : MonoBehaviour {
    public Team team;
    public TeamConfiguration currentTeamConfiguration = new TeamConfiguration();

    public void ReloadTeamConfiguration() {
        SetTeamConfiguration(team.defaultTeamConfiguration);
    }

    public void SetTeamConfiguration(TeamConfiguration sourceTeamConfiguration) {
        currentTeamConfiguration = sourceTeamConfiguration;
    }
}