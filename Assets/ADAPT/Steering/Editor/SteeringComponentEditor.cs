using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(SteeringComponent))]
public class SteeringComponentEditor : Editor {
    SerializedObject so;
    SerializedProperty sp_radius;
	SerializedProperty sp_height;
	SerializedProperty sp_acceleration;
	SerializedProperty sp_maxSpeed;
    SerializedProperty sp_holdingRadius;
    SerializedProperty sp_orientate;
    SerializedProperty sp_walkingOrientationSpeed;
    SerializedProperty sp_arrivingOrientationSpeed;
    SerializedProperty sp_arrivingRadius;
    SerializedProperty sp_orientationQuality;
	
    void OnEnable()
    {
        so = new SerializedObject(target);
        sp_radius = so.FindProperty("radius");
        sp_height = so.FindProperty("height");
        sp_acceleration = so.FindProperty("acceleration");
        sp_maxSpeed = so.FindProperty("maxSpeed");
        sp_holdingRadius = so.FindProperty("holdingRadius");
        sp_orientate = so.FindProperty("orientate");
		sp_walkingOrientationSpeed = so.FindProperty("walkingOrientationSpeed");
		sp_arrivingOrientationSpeed = so.FindProperty("arrivingOrientationSpeed");
        sp_arrivingRadius = so.FindProperty("arrivingRadius");
        sp_orientationQuality = so.FindProperty("orientationQuality");
    }

	public override void OnInspectorGUI()
    {
        so.Update();
		
        GUILayout.Label("Actor Properties");
        EditorGUILayout.PropertyField(sp_radius);
        EditorGUILayout.PropertyField(sp_height);
        
		EditorGUILayout.Separator();
		
        GUILayout.Label("Movement");
        EditorGUILayout.PropertyField(sp_acceleration);
        EditorGUILayout.PropertyField(sp_maxSpeed);
        EditorGUILayout.PropertyField(sp_holdingRadius);
		
		EditorGUILayout.Separator();
        
		GUILayout.Label("Orientation");
		EditorGUILayout.PropertyField(sp_orientate);
        EditorGUILayout.PropertyField(sp_walkingOrientationSpeed);
        EditorGUILayout.PropertyField(sp_arrivingOrientationSpeed);
        EditorGUILayout.PropertyField(sp_arrivingRadius);
        EditorGUILayout.PropertyField(sp_orientationQuality);
		
        so.ApplyModifiedProperties();
    }
}