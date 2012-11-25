using UnityEngine;
using System.Collections;

public class TestColliderScript : MonoBehaviour {

	// Use this for initialization
	void Start () {	
		//Debug.Log("start");
		
		
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
    void OnTriggerEnter() {
		Debug.Log("Trigger enter " + Time.time)	;			        
	}
}
