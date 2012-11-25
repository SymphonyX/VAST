using UnityEngine;
using System.Collections;


public interface AnimationInterface 
{
	AnimationCurve [] GetPlanAnimationCurve ();
	float GetCurrentSpeed ();
	void SetCurrentSpeed (float t_currentSpeed);
	
	float GetCurrentTime ();
	
	float RemainingDistance();
	
	bool IsOnOffMeshLink ();
	
	void SetCurrentStatePosition (Vector3 position);
	
	void CompletedOffMeshLink ();
	
	OffMeshLinkInformation GetCurrentOffMeshLinkInformation ();
	
	void SetStop ( bool t_stop );
	
	
	
}
