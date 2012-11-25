using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPicker : MonoBehaviour {
	
	public Transform grabbed;
	public float grabDistance = 10.0f;
	
	public string[] pickableTags; 
	
	private Dictionary<string,int> pickableTagsDictionary;
	
	public bool useToggleDrag;
	public bool useKeyBoardEvents; 
	
	// Use this for initialization
	void Start () {
	
		pickableTagsDictionary = new Dictionary<string, int> ();
		
		for (int i =0; i < pickableTags.Length; i++)
			pickableTagsDictionary.Add(pickableTags[i],i);
			
	}
	
	void  Update () 
	{
	    if (useToggleDrag)
	        UpdateToggleDrag();
	    else
	        UpdateHoldDrag();
		
		if (useKeyBoardEvents)
			KeyBoardEventHandler ();
	}
	
	// Toggles drag with mouse click
	void UpdateToggleDrag () {
	    if (Input.GetMouseButtonDown(0)) 
	        Grab();
	    else if (grabbed)
	        Drag();
	}
	
	// Drags when user holds down button
	void UpdateHoldDrag () {
	    if (Input.GetMouseButton(0)) 
	        if (grabbed)
	            Drag();
	        else 
	            Grab();
	    else
	        grabbed = null;
	}

	void Grab() {
	    if (grabbed) 
	       grabbed = null;
	    else {
	        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
	        
	        RaycastHit[] hits = Physics.RaycastAll(ray);
	        for ( int i =0; i < hits.Length; i++ )
	        {
				if ( pickableTagsDictionary.ContainsKey(hits[i].transform.tag) )
	            {
	            	grabbed = hits[i].transform;
	            	break;
	        	}
	        }
	    }
	    
	}
	
	void Drag() 
	{
		// if using keyboard dont drag 
		if (useKeyBoardEvents) return;
		
	    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
	    Vector3 position = transform.position + transform.forward * grabDistance;
	    Plane plane = new Plane(-transform.forward, position);
	    float distance; 
		
	    if (plane.Raycast(ray, out distance)) {
	    	//grabbed.position = ray.origin + ray.direction * distance;
	    	Vector3 newPosition = ray.origin + ray.direction * distance;
	    	
	    	// clamping to X and Z movement only 
	    	
	    	grabbed.position = new Vector3(Mathf.CeilToInt(newPosition.x), grabbed.position.y, Mathf.CeilToInt(newPosition.z));        
	        //grabbed.rotation = transform.rotation;
	    }
	}
	
	void KeyBoardEventHandler ()
	{
		if ( grabbed == null) return;
		
		if(Input.GetKeyDown(KeyCode.LeftArrow)){
			grabbed.Translate(-1.0f, 0.0f, 0.0f);
		}
		else if(Input.GetKeyDown(KeyCode.RightArrow)){
			grabbed.Translate(1.0f, 0.0f, 0.0f);
		}
		else if(Input.GetKeyDown(KeyCode.UpArrow)){
			grabbed.Translate(0.0f, 0.0f, 1.0f);
		}
		else if(Input.GetKeyDown(KeyCode.DownArrow)){
			grabbed.Translate(0.0f, 0.0f, -1.0f);
		}
		
	}
	
	
	
	void OnDrawGizmos()
	{
		if(grabbed != null){
			Gizmos.color = Color.yellow;
			if(grabbed.collider is BoxCollider)
				Gizmos.DrawWireCube(grabbed.position, (grabbed.collider as BoxCollider).size);	
			else if(grabbed.collider is SphereCollider)
				Gizmos.DrawWireSphere(grabbed.transform.position, (grabbed.collider as SphereCollider).radius);	
		}
	}	
}
