using UnityEngine;
using System.Collections;

public class MoveWithKeyboard : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
		if ((Input.GetKeyDown(KeyCode.A)))
			this.transform.Translate(new Vector3(-1.0f,0.0f,0.0f));
		if ((Input.GetKeyDown(KeyCode.S)))
			this.transform.Translate(new Vector3(0.0f,0.0f,-1.0f));
		if ((Input.GetKeyDown(KeyCode.D)))
			this.transform.Translate(new Vector3(1.0f,0.0f,0.0f));
		if ((Input.GetKeyDown(KeyCode.W)))
			this.transform.Translate(new Vector3(0.0f,0.0f,1.0f));
	}
}
