using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public struct Neighbor{
	public int triggers;
	public GameObject gameObject;
	
	public AnimationInterface animationInterface;
	public Planner planning; // only for FootstepPlanningDomain
}

[RequireComponent (typeof (CharacterController))]
// Component to detect the other agents as they enter the vision collider of the agent
public class NeighbourAgents : MonoBehaviour {
	
	public Dictionary<int,Neighbor> neighbors;
	
	[HideInInspector] public int agentsLayer;
	
	[HideInInspector] public bool initialized;
	
	[HideInInspector] public bool newNeighbor;
	
	public GameObject visionCollider;
	
	public Observable observable; 
	
	public bool I_AM_ALEJANDRO;
	private string animationInterfaceComponentName;
	
	//public GameObject[] initNeighbors;
	
	void Awake()
	{
		//visionCollider.active = false;
				
		initialized = false;	
		observable = new Observable();
		
		if ( I_AM_ALEJANDRO )
			animationInterfaceComponentName = "FootstepPlanningTest";
		else
			animationInterfaceComponentName = "AgentBrain";
		
	}
	
	// Use this for initialization
	public void Init () {
				
		neighbors = new Dictionary<int, Neighbor>();
		agentsLayer = LayerMask.NameToLayer("Agents");
		
		//visionCollider.active = true;
		
		newNeighbor = false;
		
		//visionCollider.transform.RotateAroundLocal(new Vector3(0,1,0),-90);

		//foreach (GameObject n in initNeighbors)
		//	AddNeighbor(n);		
		
		initialized = true;		
	}
	
	
	
	/*
	// Update is called once per frame
	void Update () {
		
	}
	*/
	
	
	
	void OnTriggerEnter(Collider other)
	{	
		NeighbourAgents na = other.transform.parent.gameObject.GetComponent("NeighbourAgents") as NeighbourAgents;		
		if (na != null)
			na.HasDetected(this.gameObject);			
	}
	
	
	public void HasDetected(GameObject other) 
	{		
		if (!initialized)
			return;
				
		int colliderId = other.GetInstanceID();
		
		// if the object that collided is not the gameObject to which this script is attached
		if ( colliderId != gameObject.GetInstanceID())		
		{				
			// if the object that collided is an agent
			if ( other.layer.Equals(agentsLayer) )
			{				
				NeighbourAgents otherNeighborhood = other.GetComponent("NeighbourAgents") as NeighbourAgents;
				Dictionary<int,Neighbor> otherNeighbors = otherNeighborhood.neighbors;
								
				// if the agent has not the current agent as a neighbor
				if ( !otherNeighbors.ContainsKey(gameObject.GetInstanceID()))
				{
				
					// if the agent is not already a possible neighbor
					if (!neighbors.ContainsKey(colliderId))
					{
						// Add the object that collided to the possible neighbors
						Neighbor neighborAgent = new Neighbor();
						neighborAgent.triggers = 1;
						neighborAgent.planning = null;
						neighborAgent.gameObject = other;
						neighbors.Add(colliderId, neighborAgent);
				
						//Debug.Log(colliderId + " is a possible neighbor of " + gameObject.GetInstanceID());
						
					}
					else // if it is already a possible neighbor
					{
						neighbors.Remove(colliderId);
						
						
						Neighbor neighborAgent = new Neighbor();
						// Increase the number of triggers
						neighborAgent.triggers = 2;
						if (!ADAFootstepTest.ADA_PLANNER_IN_USE)
						{
							neighborAgent.planning = other.GetComponent("FootstepPlanningTest") as Planner;
							neighborAgent.animationInterface = other.GetComponent(animationInterfaceComponentName) as AnimationInterface;
						}
						else
						{
							neighborAgent.planning = other.GetComponent("ADAFootstepTest") as Planner;
							neighborAgent.animationInterface = other.GetComponent(animationInterfaceComponentName) as AnimationInterface;
						}
						neighborAgent.gameObject = other;
						neighbors.Add(colliderId,neighborAgent);
						
						// MUBBASIR -- WE WENT FROM 1 TO 2 -- WE SHOULD REPLAN 
						observable.notifyObservers(Event.NEIGBHOR_AGENTS_CHANGED, null);
						
						newNeighbor = true;
						
						// If it has triggered the two triggers it will be considered as a neighbor by the domain						
						//Debug.Log(colliderId + " is neighbor of " + gameObject.GetInstanceID());
					}					
				}
			}
		}		
    }
	
	void OnTriggerExit(Collider other)
	{
		NeighbourAgents na = other.transform.parent.gameObject.GetComponent("NeighbourAgents") as NeighbourAgents;		
		if (na != null)
			na.HasLostOfSight(this.gameObject);			
	}
	
	public void HasLostOfSight(GameObject other)
	{
		if (!initialized)
			return;
		
		int colliderId = other.GetInstanceID();
		
		// if the object that collided is an agent
		if ( other.layer.Equals(agentsLayer) )
		{
			// if the agent was a neighbor
			if (neighbors.ContainsKey(colliderId))
			{							
				if (neighbors[colliderId].triggers == 1)
				{
					neighbors.Remove(colliderId);					
				}
				else
				{
					neighbors.Remove(colliderId);
					
					
					// Add the object that collided to the possible neighbors
					Neighbor neighborAgent = new Neighbor();
					neighborAgent.triggers = 1;
					neighborAgent.planning = null;
					neighborAgent.gameObject = other;
					neighbors.Add(colliderId, neighborAgent);
					
					// MUBBASIR WE WENT FROM 2 TO 1 -- SHOULD WE REPLAN I DONT THINK SO 
					//observable.notifyObservers(Event.NEIGBHOR_AGENTS_CHANGED, null);
					
					// It is anymore a neighbor					
					//Debug.Log(colliderId + " is not animore a neighbor of " + gameObject.GetInstanceID());
				}					
			}			
		}
	}
}
