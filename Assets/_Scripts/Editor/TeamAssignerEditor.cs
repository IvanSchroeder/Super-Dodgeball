using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using ExtensionMethods;

[CustomEditor(typeof(TeamAssigner))]
[Serializable]
public class TeamAssignerEditor : Editor {
    public override void OnInspectorGUI() {

        TeamAssigner teamAssigner = (TeamAssigner)target;

        base.OnInspectorGUI();

        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Reload Team Configuration")) {
            teamAssigner.ReloadTeamConfiguration();

            if (teamAssigner.transform.HasComponentInHierarchy<BallController>()) {
                var ballController = teamAssigner.transform.GetComponentInHierarchy<BallController>();
                ballController.SetBallTeam(teamAssigner.currentTeamConfiguration);
                ballController.SetBallColor(teamAssigner.currentTeamConfiguration.Color);
            }
            else if (teamAssigner.transform.HasComponentInHierarchy<PlayerController>()) {
                teamAssigner.gameObject.GetComponentInHierarchy<BoxCollider2D>().gameObject.layer = teamAssigner.currentTeamConfiguration.Layer;
            }
        }

        GUILayout.EndHorizontal();
    }
}
