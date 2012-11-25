using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Deterministic Dynamic Obstacle
public struct DDObstacle{
	public int triggers;
	public GameObject obstacle;
	public ObstaclesScript script;
	public MeshRenderer renderer;
}

// Component to detect the dynamic obstacles as they enter the vision collider of the agent

public class NeighbourObstacles : MonoBehaviour {
	
	public Dictionary<int,DDObstacle> closeObstacles;
	public List<DDObstacle> visibleObstacles;
	
	[HideInInspector] public int detObstaclesLayer;
	
	[HideInInspector] public bool initialized;
	
	[HideInInspector] public bool newObstacle;
	
	public GameObject visionCollider;
	
	public Material obstacleMaterial;
	public Material detectedObstacle;
	
	public Observable observable; 
	
	//public GameObject[] initObstacles;
	
	void Awake()
	{
		visionCollider.active = false;				
		
		initialized = false;		
		
		observable = new Observable();
	}
	
	// Use this for initialization
	public void Init () {
		
		closeObstacles = new Dictionary<int, DDObstacle>();
		visibleObstacles = new List<DDObstacle>();
		
		detObstaclesLayer = LayerMask.NameToLayer("DeterministicObstacles");
		
		visionCollider.active = true;
		
		newObstacle = false;
		
		//foreach (GameObject n in initObstacles)
		//	AddObstacle(n);
		
		initialized = true;
	}
	

	 
	/*
	// Update is called once per frame
	void Update () {
		
	}
	*/
	
	
	public void HasDetected(GameObject other)
	{			
		if (!initialized)
			return;
		
		int colliderId = other.GetInstanceID();
		
		// if the object that collided is an deterministic dynamic obstacle
		if ( other.gameObject.layer.Equals(detObstaclesLayer) )
		{
			// if the obstacle is not already a possible obstacle
			if (!closeObstacles.ContainsKey(colliderId))
			{
				// Add the object that collided to the possible obstacles
				DDObstacle obstacle;
				obstacle.triggers = 1;
				obstacle.script = null;
				obstacle.obstacle = other;
				obstacle.renderer = other.GetComponent("MeshRenderer") as MeshRenderer;
				closeObstacles.Add(colliderId,obstacle);				
			}
			else // if it is already a possible obstacle
			{
				closeObstacles.Remove(colliderId);
				
				DDObstacle obstacle;
				obstacle.triggers = 2;
				obstacle.script = other.transform.parent.GetComponent("ObstaclesScript") as ObstaclesScript;
				obstacle.obstacle = other;
				obstacle.renderer = other.GetComponent("MeshRenderer") as MeshRenderer;
				obstacle.renderer.material = detectedObstacle;
				closeObstacles.Add(colliderId,obstacle);
				
				// If it has triggered the two triggers it will be considered as an obstacle by the domain						
				//Debug.Log(colliderId + " is close to " + gameObject.GetInstanceID());		
				
				visibleObstacles.Add(obstacle);
				
				observable.notifyObservers(Event.NEIGBHOR_DYNAMIC_OBSTACLES_CHANGED, null);
				
				newObstacle = true;
			}					
		}
	}
	
	
	public void HasLostSight(GameObject other)
	{
		if (!initialized)
			return;
		
		int colliderId = other.GetInstanceID();
		
		// if the object that collided is an deterministic dynamic obstacle
		if ( other.layer.Equals(detObstaclesLayer) )
		{
			// if the agent was a neighbor
			if (closeObstacles.ContainsKey(colliderId))
			{							
				if (closeObstacles[colliderId].triggers == 1)
				{
					closeObstacles.Remove(colliderId);					
				}
				else
				{
					visibleObstacles.Remove(closeObstacles[colliderId]);
					
					closeObstacles.Remove(colliderId);
					
					// Add the object that collided to the possible neighbors
					DDObstacle obstacle;
					obstacle.triggers = 1;
					obstacle.script = null;
					obstacle.obstacle = other;
					obstacle.renderer = other.GetComponent("MeshRenderer") as MeshRenderer;
					obstacle.renderer.material = obstacleMaterial;
					closeObstacles.Add(colliderId, obstacle);		
					
					// DO WE NEED TO REPLAN WHEN AN OBSTACLE GOT OUT ?
					//observable.notifyObservers(Event.NEIGBHOR_DYNAMIC_OBSTACLES_CHANGED, null);
										
					// It is anymore an obstacle					
					//Debug.Log(colliderId + " is not anymore close to " + gameObject.GetInstanceID());					
				}					
			}			
		}
	}
	
}
