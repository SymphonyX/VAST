using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(SteeringManager))]
public class SteeringManagerEditor : Editor
{
    SerializedObject so;
    SerializedProperty sp_navmesh;
    SerializedProperty sp_maxAgents;
    SerializedProperty sp_maxAgentRadius;
	SerializedProperty sp_maxCount;
	SerializedProperty sp_maxAreaCost;
	SerializedProperty sp_densityBasedPath;

    void OnEnable()
    {
        so = new SerializedObject(target);
        sp_navmesh = so.FindProperty("navmesh");
        sp_maxAgents = so.FindProperty("maxAgents");
        sp_maxAgentRadius = so.FindProperty("maxAgentRadius");
		sp_maxCount = so.FindProperty("MAX_COUNT");
		sp_maxAreaCost = so.FindProperty("MAX_AREA_COST");
		sp_densityBasedPath = so.FindProperty("densityBasedPath");
    }

    public override void OnInspectorGUI()
    {
        so.Update();
        EditorGUILayout.PropertyField(sp_navmesh);
        EditorGUILayout.PropertyField(sp_maxAgents);
        EditorGUILayout.PropertyField(sp_maxAgentRadius);
		EditorGUILayout.PropertyField(sp_maxCount);
		EditorGUILayout.PropertyField(sp_maxAreaCost);
		EditorGUILayout.PropertyField(sp_densityBasedPath);
        so.ApplyModifiedProperties();
    }
}