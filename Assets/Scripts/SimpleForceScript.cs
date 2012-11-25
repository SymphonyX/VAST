using UnityEngine;
using System.Collections;

public class SimpleForceScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		
		float x = 0.0f,z = 0.0f;
		bool force = false;
		if (Input.GetKeyDown(KeyCode.A))
		{
			x = -1.0f; z = 0.0f;
			force = true;
		}
		else if (Input.GetKeyDown(KeyCode.S))
		{
			x = 0.0f; z = -1.0f;
			force = true;
		}
		if (Input.GetKeyDown(KeyCode.W))
		{
			x = 0.0f; z = 1.0f;
			force = true;
		}
		if (Input.GetKeyDown(KeyCode.D))
		{
			x = 1.0f; z = 0.0f;
			force = true;
		}
		
		if (force)
		{
			foreach( NonDeterministicObstacle nd in CentralizedManager.nonDeterministicObstacles)
			{
				nd.rigidbody.AddForce(x,0.0f,z,ForceMode.Impulse);
			}
		}
	
	}
}
