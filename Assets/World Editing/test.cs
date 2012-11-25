using UnityEngine;
using System.Collections;

public class test : MonoBehaviour {
	
	public Transform A;
	public Transform B;
	public Transform C;

	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
		Vector3 AB = B.position - A.position;
		Vector3 AC = C.position - A.position;
		
		//float ABdotAC = Vector3.Dot(AB,AC); 
				
		//float distance = Mathf.Sqrt(1 - (ABdotAC*ABdotAC)/(AC.sqrMagnitude * AB.sqrMagnitude)) * AC.magnitude;
		
		// P is the projection of C over AB 
		Vector3 AP = Vector3.Project(AC,AB);
		Vector3 P = A.position + AP;		
		Vector3 BP = P - B.position;
		
		//Debug.DrawLine(A,(A+Vector3.up),Color.red);
		//Debug.DrawLine(P,(P+Vector3.up),Color.blue);
		//Debug.DrawLine(A,P,Color.green);
				
		
	}
}
