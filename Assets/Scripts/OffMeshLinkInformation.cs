using UnityEngine;
using System.Collections;

public enum LinkType 
{
	JumpLink,
	LadderLink
}
public class OffMeshLinkInformation : MonoBehaviour {
	
	public OffMeshLink offMeshLink;
	public Vector3 originalEndPosition; // used for dictionary  -- not used any more 
	public Vector3 originalStartPosition;
	
	// set these in inspector 
	public Transform start;
	public Transform end;
	
	public LinkType linkType;
	public float timeToTraverseLink;
	
	
	// Use this for initialization
	void Awake () {
		
		
		originalEndPosition = end.transform.position;
		originalStartPosition = start.transform.position;
	
	}
	
	
	void OnDrawGizmos ()
	{
		if (start == null || end == null) return;
		
		if (offMeshLink.activated == true)
			Gizmos.color = Color.green;
		else
			Gizmos.color = Color.red;
		
		Gizmos.DrawWireSphere(start.position,0.3f);
		Gizmos.DrawWireSphere(end.position,0.3f);
		Gizmos.DrawLine(start.position,end.position);
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	public Vector3 GetPredictedEndPositionAtEndOfJump ( float currentTime )
	{
		Vector3 predictedEndPosition;
		MovingPlane movingPlane = end.GetComponent<MovingPlane>();
		
		if ( movingPlane != null)
			predictedEndPosition = movingPlane.GetPosition(currentTime + timeToTraverseLink);
		else
			predictedEndPosition = end.transform.position;
		
		return predictedEndPosition;
		
	}
}
