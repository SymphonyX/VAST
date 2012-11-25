using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshCollider))]

public class NonDeterministicObject: MonoBehaviour {
	
	public SteeringManager steeringManager;
	
	private Vector3 pos;
	
	private uint polyRef;
	
	private Vector3 screenPoint;
	private Vector3 offset;
	
	/*
	void OnMouseDown() { 
		
		screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
		
		offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
	}
	*/
	
	void Awake () {
		
		if (!steeringManager.isInitialized())
			steeringManager.Initialize();		
	}
	
	// Use this for initialization
	void Start () {				
		
		pos = transform.position;
		
		polyRef = steeringManager.GetClosestPolygon(pos);
		steeringManager.IncrNumObjectsInPolygon(polyRef);
		
	}
	
	// Update is called once per frame
	void Update () {
	
		if (pos != transform.position)
		{
			//Debug.Log("Position = " + transform.position);
			Move();	
		}	
		
		pos = transform.position;
		
	}
	
	/*
	void OnMouseDrag() { 
		
		Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);

		Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
		transform.position = curPosition;
	}
	*/
	
	void Move(){
		
		Vector3 prevPos = pos;		
		Vector3 newPos = transform.position;
		
		/* DEBUG */
		//uint one = steeringManager.ReturnOne();
		//Debug.Log("At time " + Time.time + " print one: " + one);		
		/* END_DEBUG */
		
		uint newPol = steeringManager.GetClosestPolygon(newPos);
		
		if (newPol != polyRef)
		{
			steeringManager.DecrNumObjectsInPolygon(polyRef);
			steeringManager.IncrNumObjectsInPolygon(newPol);
			
			uint prevNum = steeringManager.GetNumOjbectsInPolygon(polyRef);
			uint newNum = steeringManager.GetNumOjbectsInPolygon(newPol);
			
			Debug.Log("At time " + Time.deltaTime + ", PrevPol = " + polyRef + " has " + prevNum + " objects, and NewPol = " + newPol + " has " + newNum + " objects");
			
			polyRef = newPol;			
		}	
		
	}
	
}
