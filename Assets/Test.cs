using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour {
	
	public GameObject sphere;
	
	public Vector3 collisionPos = new Vector3(0,0,0);
	public float collisionRadius = 1;
	
	private bool moved;
	
	private SphereCollider collider;
	
	// Use this for initialization
	void Start () {
	
		moved = false;
		
		collider = sphere.GetComponent("SphereCollider") as SphereCollider;
		
	}
	
	void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(collisionPos,collisionRadius);
	}
	
	// Update is called once per frame
	void Update () {
	
		if (!moved)
		{		
			Vector3 prevPos = sphere.transform.position;
			
			Debug.Log("Collider center before: "  + collider.center);
			Debug.Log("Rigid body position before: "  + sphere.rigidbody.position);
			
			sphere.transform.position = collisionPos;			
			
			Debug.Log("Collider center after: "  + collider.center);
			Debug.Log("Rigid body position after: "  + sphere.rigidbody.position);
			
			if (Physics.CheckSphere(collisionPos, collisionRadius))
				Debug.Log("Collision detected");
			
			sphere.transform.position = prevPos;
			
			moved = true;
		}
	}
}
