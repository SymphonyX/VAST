using UnityEngine;
using System.Collections;


public class Goal : MonoBehaviour{
	
	public State goalState; 
	
	// Use this for initialization
	void Awake () {
	
		if (goalState == null) // not initiazed in inspector
			goalState = new State (transform.position);
		else
			goalState.setPosition(transform.position);
	}
	
	// Update is called once per frame
	void Update () {
		
		float distance = Vector3.SqrMagnitude(transform.position - goalState.getPosition());
		if ( distance > 1.0F)
		{
			goalState.setPosition(transform.position);
		}
	}
}


