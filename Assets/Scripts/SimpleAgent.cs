using UnityEngine;
using System.Collections;

public class SimpleAgent : MonoBehaviour {
	
	
	uint polygonIndex; 
	public NavMeshAgent agentToFollow;
	
	
	// Use this for initialization
	void Start () {
		
		if ( GlobalNavigator.usingDynamicNavigationMesh)
		{
			polygonIndex = GlobalNavigator.recastSteeringManager.GetClosestPolygon(transform.position);
			GlobalNavigator.recastSteeringManager.IncrNumObjectsInPolygon(polygonIndex);
		}
	
	}
	
	// Update is called once per frame
	void Update () {
		
		//transform.position = agentToFollow.transform.position;
		
		//Vector3 target = agentToFollow.steeringTarget;
		//float distance = Vector3.Distance(transform.position,target);
		
		
		
		if ( GlobalNavigator.usingDynamicNavigationMesh)
			polygonIndex = CentralizedManager.UpdatePolygonDictionary(transform.position,polygonIndex,null);
	
	}
}
