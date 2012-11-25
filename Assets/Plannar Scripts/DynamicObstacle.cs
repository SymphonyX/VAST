using UnityEngine;
using System.Collections;

public class DynamicObstacle : MonoBehaviour {
	
	
	// Use this for initialization
	void Start () {		
		gameObject.AddComponent<Rigidbody>();
		Rigidbody rigidbody = gameObject.GetComponent("Rigidbody") as Rigidbody;
		rigidbody.useGravity = false;
		rigidbody.isKinematic = true;		
	}
	
	/*
	// Update is called once per frame
	void Update () {
	
	}
	*/
	
	void OnTriggerEnter(Collider other)
	{					
		NeighbourObstacles no = other.transform.parent.gameObject.GetComponent("NeighbourObstacles") as NeighbourObstacles;
		if (no != null)
			no.HasDetected(this.gameObject);
	}
	
	void OnTriggerExit(Collider other)
	{
		NeighbourObstacles no = other.transform.parent.gameObject.GetComponent("NeighbourObstacles") as NeighbourObstacles;
		if (no != null)
			no.HasLostSight(this.gameObject);
	}
}
