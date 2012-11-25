using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

public class MovableObstacleManager {
	
	public GameObject[] obstacles;
	// Use this for initialization
	public MovableObstacleManager()
	{
		obstacles = GameObject.FindGameObjectsWithTag("movable obstacles");	
	}
	
	public GameObject[] Obstacles()
	{
		return obstacles;
	}
	
	
}
