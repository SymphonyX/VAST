using UnityEngine;
using System.Collections;

[System.Serializable]
public class SpatialPosition
{
	public Vector3 position;
	public float radius;
}

[System.Serializable]
public class Orientation
{
	public Vector3 orientation;
	public float error;
}

[System.Serializable]
public class Times
{
	public enum TIME {exactTime, timeWindow, beforeTime, afterTime}
	
	public TIME TimeSelection;
	
}

public class GoalScript : ObstaclesScript {
	
	public SpatialPosition goalPosition;
	public Times timeSelection;
	public Vector2 timeWindow;
	public Orientation orientation;
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
