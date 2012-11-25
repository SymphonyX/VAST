using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


public class PolygonData
{
	public List<AgentBrain> agents; // these are agents who are interested in this polygon ** (their path contains this polygon)
	public List<NonDeterministicObstacle> nonDeterministicObstacles; // we dont need this 
	public int numberOfChanges; // number of non deterministic obstacle movements 
		
	public PolygonData ()
	{
		agents = new List<AgentBrain>();
		nonDeterministicObstacles = new List<NonDeterministicObstacle>();		
		numberOfChanges = 0;
	}
	
	
}

public class Vector3ApproximateComparator : IEqualityComparer<Vector3>
{
	public bool Equals ( Vector3 v1, Vector3 v2)
	{
		if ( Mathf.Abs(v1.x - v2.x) < 0.2f && 
			Mathf.Abs(v1.z - v2.z) < 0.2f )
			return true;
		else return false;
	}
	
	public int GetHashCode (Vector3 v)
	{
		int x  = Mathf.RoundToInt(v.x);
		int z  = Mathf.RoundToInt (v.z); 
		
		return x * 1000 + z;		
	}
	
}

public static class CentralizedManager {
	
	public static int MAX_CHANGES = 5; // the maximum number of changes in an obstacle before replanning 
	
	public static float MAX_JUMP_DISTANCE = 9.1f;
	
	public static AgentBrain [] agents; 
	public static NonDeterministicObstacle [] nonDeterministicObstacles; 
	public static SimpleAgent [] simpleAgents; 
	
	public static Dictionary<uint,PolygonData> polygonDictionary;
	
	
	public static UnityEngine.Object textDisplayPrefab; 
	
	// only using x-z value
	public static Dictionary<Vector3,OffMeshLinkInformation> offMeshLinkDictionary;
	
	// Use this for initialization
	public static void Initialize () {
	
		nonDeterministicObstacles = GameObject.FindObjectsOfType(typeof(NonDeterministicObstacle)) as NonDeterministicObstacle[];
				
		polygonDictionary = new Dictionary<uint, PolygonData> ();
		
		Vector3ApproximateComparator comparator = new Vector3ApproximateComparator();
		offMeshLinkDictionary = new Dictionary<Vector3, OffMeshLinkInformation> (comparator);
			
		OffMeshLinkInformation [] offMeshLinkInformation = GameObject.FindObjectsOfType(typeof(OffMeshLinkInformation)) as OffMeshLinkInformation[];
		
		foreach(OffMeshLinkInformation info in offMeshLinkInformation)
		{
			offMeshLinkDictionary.Add(info.originalEndPosition, info);
		}
		
		agents = GameObject.FindObjectsOfType(typeof(AgentBrain)) as AgentBrain[];
		
		// initializing agent -- this must be after everything else in the world has been set up 
		foreach (AgentBrain agent in agents)
			agent.InitializeAgentBrain ();
		
		simpleAgents = GameObject.FindObjectsOfType(typeof(SimpleAgent)) as SimpleAgent[];
		
		// we want to do a global initialization of all counters in polygons 
		if (GlobalNavigator.usingDynamicNavigationMesh)
		{
			foreach(NonDeterministicObstacle nd in nonDeterministicObstacles)
			{
				uint polygonIndex = GlobalNavigator.recastSteeringManager.GetClosestPolygon(nd.transform.position);
				GlobalNavigator.recastSteeringManager.IncrNumObjectsInPolygon(polygonIndex);
			}
			
			foreach(AgentBrain a in agents)
			{
				uint polygonIndex = GlobalNavigator.recastSteeringManager.GetClosestPolygon(a.transform.position);
				GlobalNavigator.recastSteeringManager.IncrNumObjectsInPolygon(polygonIndex);
			}
			
			foreach(SimpleAgent a in simpleAgents)
			{
				uint polygonIndex = GlobalNavigator.recastSteeringManager.GetClosestPolygon(a.transform.position);
				GlobalNavigator.recastSteeringManager.IncrNumObjectsInPolygon(polygonIndex);
			}
		}
		
		
	}
		
	public static bool IsOffMeshLink ( Vector3 p)
	{
		return offMeshLinkDictionary.ContainsKey(p);
	}
	
	public static OffMeshLinkInformation GetOffMeshLinkInformation ( Vector3 p)
	{
		if ( IsOffMeshLink(p) == false)
			return null;
		
		return offMeshLinkDictionary[p];
		
	}
	
	public static uint UpdatePolygonDictionary (Vector3 currentPosition, uint currentPolygonIndex, NonDeterministicObstacle nonDeterministicObstacle)
	{		
		
		uint newPolygonIndex = GlobalNavigator.recastSteeringManager.GetClosestPolygon(currentPosition);
		
		if ( newPolygonIndex != currentPolygonIndex)
		{
			GlobalNavigator.recastSteeringManager.DecrNumObjectsInPolygon(currentPolygonIndex);
			GlobalNavigator.recastSteeringManager.IncrNumObjectsInPolygon(newPolygonIndex);
			
			if (polygonDictionary.ContainsKey(currentPolygonIndex))
			{
				_IncrementNumberOfChangesInPolygon(currentPolygonIndex);
				if ( nonDeterministicObstacle != null)
					polygonDictionary[currentPolygonIndex].nonDeterministicObstacles.Remove(nonDeterministicObstacle);
			}
				
			if (polygonDictionary.ContainsKey(newPolygonIndex))
			{
				_IncrementNumberOfChangesInPolygon(newPolygonIndex);
				if ( nonDeterministicObstacle != null)
					polygonDictionary[newPolygonIndex].nonDeterministicObstacles.Add(nonDeterministicObstacle);
			}
			
		}
		
		return newPolygonIndex;
	}
	
	private static void _IncrementNumberOfChangesInPolygon (uint polygonIndex)
	{
		if (polygonDictionary.ContainsKey(polygonIndex))
			polygonDictionary[polygonIndex].numberOfChanges = polygonDictionary[polygonIndex].numberOfChanges + 1; 
		
		if ( polygonDictionary[polygonIndex].numberOfChanges >= CentralizedManager.MAX_CHANGES )  
		{
			// time to replan for all agents in this polygon 
			foreach ( AgentBrain agent in polygonDictionary[polygonIndex].agents)
			{
				// a bit of a hack 
				agent.globalNavigationTask.notifyEvent(Event.GLOBAL_WORLD_CHANGED, null);
			}
			
			polygonDictionary[polygonIndex].numberOfChanges = 0; // reset counter because we took care of it 
		}
		
		
	}
		

}

