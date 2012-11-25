using UnityEngine;
using System.Collections;

public class PlaybackRootControl : MonoBehaviour {
	
	public Transform root;
	
	private Vector3 lastRootPos;
	private Vector3 lastGameObjectPos;
	
	private Vector3 rootDisplacement;
	private Vector3 rootInit;
	private Vector3 rootLocalInit;
	
	// Use this for initialization
	void Start () {
			
		lastRootPos = root.localPosition;
		
		lastGameObjectPos = transform.position;
		
		rootInit = root.position;
		rootLocalInit = root.localPosition;
		
	}
	
	// Update is called once per frame
	void Update () {
	
		AnimationState state = animation[animation.clip.name];
		
		Debug.Log("Playing " + state.name);
		
		if (lastRootPos != root.localPosition)
			Debug.Log("Root cambia!");
		
		if (lastGameObjectPos != transform.position)
			Debug.Log("GameObject cambia!");
		
		lastRootPos = root.localPosition;
		
		lastGameObjectPos = transform.position;
		
		if (state.time >= state.length-0.02f)
		{
			state.time = state.length-0.02f;
			animation.Sample();
			animation.Stop();				
			
			rootDisplacement = root.position - rootInit;
			
			transform.position += rootDisplacement;
			
			root.localPosition = rootLocalInit;			
			
			//animation.Play();
			
			//rootInit = root.position;
		}
		
				
	}
}
