using UnityEngine;
using System.Collections;

public class RandomObstacleMovement : MonoBehaviour {
	
	private int layer =   (1 << LayerMask.NameToLayer("Agents"));
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		
		if ( Input.GetKeyDown(KeyCode.A)) 
			foreach( NonDeterministicObstacle nd in CentralizedManager.nonDeterministicObstacles)
			{
				if ( Random.value > 0.8f)
				{
					Vector3 newPos; 
					float val = Random.value;
					if ( val < 0.25f)
						newPos = nd.transform.position + new Vector3(1.0F,0.0f,0.0f);
					else if ( val < 0.5f)
						newPos = nd.transform.position + new Vector3(-1.0F,0.0f,0.0f);
					else if ( val < 0.75f)
						newPos = nd.transform.position + new Vector3(0.0F,0.0f,1.0f);
					else
						newPos = nd.transform.position + new Vector3(0.0F,0.0f,-1.0f);
				
					Collider[] c = Physics.OverlapSphere(newPos, 2.0F, layer);
					
					if (c.Length == 0)
						nd.transform.position = newPos;
				}
				
					
				
			}
		}	
	}
