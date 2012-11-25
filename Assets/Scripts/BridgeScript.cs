using UnityEngine;
using System.Collections;

public class BridgeScript : MonoBehaviour {
	
	
	public NavMeshAgent[] team1;
	public NavMeshAgent[] team2;
	public NavMeshAgent[] team3;
	
	public Transform bridge1;
	public Transform bridge2;
	public Transform bridge3;
	public Transform common;
	
	// Use this for initialization
	void Start () {
		
		foreach(NavMeshAgent a in team3)
			a.SetDestination(bridge3.position);
		
		foreach(NavMeshAgent a in team1)
			a.SetDestination(bridge1.position);
	
		foreach(NavMeshAgent a in team2)
			a.SetDestination(bridge2.position);
		
	}
	
	// Update is called once per frame
	void Update () {
	
		foreach(NavMeshAgent a in team1)
			if (a.remainingDistance < 0.3f)
				a.SetDestination(common.position);
	
		foreach(NavMeshAgent a in team2)
			if (a.remainingDistance < 0.3f)
				a.SetDestination(common.position);
		
		foreach(NavMeshAgent a in team3)
		{
			if (a.remainingDistance < 0.3f)
			{
				a.SetDestination(common.position);
			}
		}
		
	}
}
