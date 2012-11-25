using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NonDeterministicObstacle : MonoBehaviour {
	
	
	public Observable observable;
	private Vector3 prevPosition;
	
	uint polygonIndex; // the recast polygon i currently am in 
	
	// Use this for initialization
	void Start () {
	
		
		// making them rigid bodies so they can recieve triggers 
		//gameObject.AddComponent<Rigidbody>();
		//Rigidbody rigidbody = gameObject.GetComponent("Rigidbody") as Rigidbody;
		//rigidbody.useGravity = false;
		//rigidbody.isKinematic = true; // if we want to do physics -- this will have to change 
		
		observable = new Observable();
		prevPosition = transform.position;
		
		if ( GlobalNavigator.navigationMeshChoice == NavigationMeshChoice.USE_RECAST )
		{
			polygonIndex = GlobalNavigator.recastSteeringManager.GetClosestPolygon(transform.position);
		}
		
	}
		
	// Update is called once per frame
	void FixedUpdate () {
	
		Vector3 newPosition = transform.position;
		
		
		if ( prevPosition.Equals(newPosition) == false )
		{
			Debug.LogError ("obstacle has moved" );
			Vector3[] posChange = new Vector3[2]; // {oldPos, newPos}
			posChange[0] = prevPosition; posChange[1] = transform.position;
			
			// this event goes to all grid navigation tasks that have encountered this obstacle during its planning 
			observable.notifyObservers(Event.NON_DETERMINISTIC_OBSTACLE_CHANGED, posChange);
			
			if ( GlobalNavigator.usingDynamicNavigationMesh == true )
			{
				polygonIndex = CentralizedManager.UpdatePolygonDictionary(transform.position,polygonIndex,this);
			}
			
			prevPosition = newPosition;
			transform.position = newPosition;
		}
		
		//transform.position = newPosition;
		
	}
	
	void OnTriggerEnter(Collider other)
	{	
		AgentBrain agent = other.transform.parent.GetComponent<AgentBrain>();
		if ( agent != null)
		{
			Task planningTask = agent.GetCurrentGridTask();
			if ( planningTask != null)
			{
				observable.registerObserver(Event.NON_DETERMINISTIC_OBSTACLE_CHANGED,planningTask);
				planningTask.setTaskPriority(TaskPriority.RealTime); // first time we saw this guy 
			}
			
		}
		
	}
	
	void OnTriggerExit(Collider other)
	{
		AgentBrain agent = other.transform.parent.GetComponent<AgentBrain>();
		if ( agent != null)
		{
			Task planningTask = agent.GetCurrentGridTask();
			if ( planningTask != null)
				observable.unregisterObserver(Event.NON_DETERMINISTIC_OBSTACLE_CHANGED,planningTask);
		}
		
	}	
	
}
