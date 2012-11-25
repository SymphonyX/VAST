using UnityEngine;
using System.Collections;

public class WorldRotation : MonoBehaviour {
	public float dAngle = 0.1f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		gameObject.transform.Rotate(0,dAngle,0);
	}
	
}
