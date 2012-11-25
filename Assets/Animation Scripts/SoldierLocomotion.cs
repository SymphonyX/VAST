using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SoldierLocomotion : MonoBehaviour {
	
	
	public float blendTime = 0.1f;
	
	public bool I_AM_ALEJANDRO;
	
	public bool isADAPTMan_ = false; 

	public float blendingTime_ = 0.1f;
	public float jumpBlendingTime = 0.2f;
	public float ladderBlendingTime = 0.2f;
	public float parabolFactor_ = 0.4f;
	public float jumpSpeed_ = 2.0f;
	public float ladderSpeed_ = 2.0f;
	
	public SteeringManager steeringManager;
	
	private uint polyRef;
	
	string locoState_ = "Locomotion_Stand";
	AnimationInterface agent_;
	
	Animation anim_;
	Vector3 linkStart_;
	Vector3 linkEnd_;
	Quaternion linkRot_;
	
	public AnimationCurve[] curves;
	
	private Vector3 prevPos;
	
	public float agentSpeed = 0.0f;	
	
	OffMeshLinkInformation currentLinkInformation;
	
	void Awake () {
		
		if (steeringManager != null && !steeringManager.isInitialized())
			steeringManager.Initialize();		
		
		prevPos = transform.position;
		if (steeringManager != null)
		{
			polyRef = steeringManager.GetClosestPolygon(prevPos);
			steeringManager.IncrNumObjectsInPolygon(polyRef);
		}
	}
		
	IEnumerator Start() {
	
		if ( I_AM_ALEJANDRO)
			agent_ = GetComponent("FootstepPlanningTest") as AnimationInterface;
		else
			agent_ = GetComponent("AgentBrain") as AnimationInterface;
		
		//agent_.autoTraverseOffMeshLink = false;
		AnimationSetup();
	
		while(Application.isPlaying) {
			yield return StartCoroutine(locoState_);
		}
	}
	
	
	IEnumerator Locomotion_Stand() {	
		do {
			agentSpeed -= 0.1f; 
			if ( agentSpeed < 0.0f) agentSpeed = 0.0f; 
			
			UpdateAnimationBlend();
			yield return null;
		} while(agent_.RemainingDistance() < 0.1f );
	
		locoState_ = "Locomotion_Move";
		
		
	}
	
		
	IEnumerator Locomotion_Move() {
		do {
			UpdateAnimationBlend();
			yield return null;
	
			if(agent_.IsOnOffMeshLink()) 
			{
				//Debug.LogWarning("SOLIDER ON OFF MESH LINK");
				locoState_ = SelectLinkAnimation();
				return true;
				
			}
		} while(agent_.RemainingDistance() > 0.1);

		locoState_ = "Locomotion_Stand";
		
	}
	
	IEnumerator Locomotion_JumpAnimation() {
		
		string linkAnimationName = "RunJump";
	
		agent_.SetStop(true);
		
		float currentTime = agent_.GetCurrentTime();
		Vector3 currentStartPosition = currentLinkInformation.start.transform.position; // we are on this right now 
		Vector3 predictedEndPosition = currentLinkInformation.GetPredictedEndPositionAtEndOfJump(currentTime);
		
		float jumpDistance = Vector3.Distance( currentStartPosition, predictedEndPosition);
		
		while (jumpDistance > CentralizedManager.MAX_JUMP_DISTANCE)
		{
			//anim_.CrossFade("Idle", blendTime, PlayMode.StopAll);
			
			// SETTING CURRENT STATE 
			agent_.SetCurrentStatePosition(currentLinkInformation.start.transform.position);
			
			if (Vector3.Distance(transform.position,currentLinkInformation.start.transform.position) > 0.3f)
				anim_.CrossFade("Walk", blendTime, PlayMode.StopAll);
			else anim_.CrossFade("Idle", blendTime, PlayMode.StopAll);
			
			transform.position = Vector3.Lerp(transform.position, currentLinkInformation.start.transform.position,0.02f);
			transform.forward = currentLinkInformation.end.transform.position-transform.position;
			
			
			yield return null;
			
			currentTime = agent_.GetCurrentTime();
			
			currentStartPosition = currentLinkInformation.start.transform.position; // we are on this right now 
			predictedEndPosition = currentLinkInformation.GetPredictedEndPositionAtEndOfJump(currentTime);
			jumpDistance = jumpDistance = Vector3.Distance( currentStartPosition, predictedEndPosition);

			Debug.LogError("JUMP DISTANCE" + jumpDistance + " time " + currentTime);
		
		}
		
		// THIS IS BASICALLY NOT ACCURATE -- THE PREDICTION IS OFF 
		anim_[linkAnimationName].speed = 1.3f / currentLinkInformation.timeToTraverseLink;
		anim_.CrossFade(linkAnimationName, jumpBlendingTime, PlayMode.StopAll);
		
		// TODO we should monitor this signal -- because it may evoke replans 
		currentLinkInformation.offMeshLink.activated = false;
		
		Debug.LogError("STARTING TO JUMP" + agent_.GetCurrentTime());
		transform.rotation = linkRot_;
		Vector3 posStartAnim  = transform.position;
		Vector3 posEndAnim = currentLinkInformation.GetPredictedEndPositionAtEndOfJump(currentTime);
		do {
			float tlerp = anim_[linkAnimationName].normalizedTime;
			// MAJOR TODO : WHAT IF THE START LINK POS IS MOVING -- HOW DOES AGENT MOVE WITH IT ? 
			
			Vector3 newPos  = Vector3.Lerp(posStartAnim, posEndAnim, tlerp);
			newPos.y += parabolFactor_*Mathf.Sin(3.14159f*tlerp);
			transform.position = newPos;
			Debug.LogWarning("jumping .... " + posStartAnim + " " + posEndAnim + " " + transform.position);
	
			yield return null;
		} while(anim_[linkAnimationName].normalizedTime < 1);
	
		//anim_.CrossFade("Idle",0.1f);
		anim_.Play("Idle");
		
		Debug.LogError("JUMP ENDED" + agent_.GetCurrentTime());
		
		currentLinkInformation.offMeshLink.activated = true;
		agent_.CompletedOffMeshLink ();
		
		agent_.SetStop(false);
		
		//
		//transform.position = linkEnd_;
		//transform.position = 
		
		// TODO NEED TO HANDLE SPEED STILL 
		// TIME IS ALWAYS SET IN AGENT BRAIN 
		agent_.SetCurrentStatePosition(transform.position);
		
		locoState_ = "Locomotion_Stand";
	}

	IEnumerator Locomotion_LadderAnimation() {
		
		Vector3 linkCenter = 0.5f *(linkEnd_ + linkStart_);
		string linkAnimationName;
		
		if(transform.position.y > linkCenter.y) {
			linkAnimationName = "Ladder_Down";
		} else {
			linkAnimationName = "Ladder_Up";
		}
	
	 	agent_.SetStop(true);
	
		Quaternion startRot = transform.rotation;
		Vector3 startPos  = transform.position;
		float blendTime  = 0.2f;
		float tblend  = 0.0f;
		
		do {
			transform.position = Vector3.Lerp(startPos, linkStart_, tblend/blendTime);
			transform.rotation = Quaternion.Slerp(startRot, linkRot_, tblend/blendTime);
			yield return null;
			tblend += Time.deltaTime;
		} while(tblend < blendTime);
		transform.position = linkStart_;
	
		if (!isADAPTMan_)
		{
			currentLinkInformation.offMeshLink.activated = false;
			anim_.CrossFade(linkAnimationName, ladderBlendingTime, PlayMode.StopAll);
			do {
				yield return null;
			} while(anim_[linkAnimationName].normalizedTime < 1);
			
		}
		else
		{
			if (linkAnimationName == "Ladder_Up")
			{
				anim_.CrossFade(linkAnimationName, ladderBlendingTime, PlayMode.StopAll);
				currentLinkInformation.offMeshLink.activated = false;
				do {
					yield return null;
				} while(anim_[linkAnimationName].normalizedTime < 1);	
				
										
				anim_.CrossFade("Walk_Up", blendingTime_*2, PlayMode.StopAll);
				do {
					yield return null;
				} while(anim_["Walk_Up"].normalizedTime < 0.5f);	
							
			}
			else
			{			
				anim_.CrossFade("Turn180", ladderBlendingTime, PlayMode.StopAll);
				currentLinkInformation.offMeshLink.activated = false;
				do {
					yield return null;
				} while(anim_["Turn180"].normalizedTime < 1);		
				
				anim_.CrossFade(linkAnimationName, blendingTime_*2, PlayMode.StopAll);
				do {
					yield return null;
				} while(anim_[linkAnimationName].normalizedTime < 1);		
				
				anim_.CrossFade("Turn180-down", blendingTime_*2, PlayMode.StopAll);
				do {
					yield return null;
				} while(anim_["Turn180-down"].normalizedTime < 1);		
			}
		}
		

		currentLinkInformation.offMeshLink.activated = true;
	
		//anim_.CrossFade("Idle",ladderBlendingTime);
		anim_.Play("Idle");
		
		transform.position = linkEnd_;
		
		agent_.CompletedOffMeshLink ();
		agent_.SetCurrentStatePosition(transform.position);
		
		agent_.SetStop(false);
		
	
		locoState_ = "Locomotion_Stand";
	}
	
	private string SelectLinkAnimation() {
		
		currentLinkInformation = agent_.GetCurrentOffMeshLinkInformation ();
		
		Vector3 startPos = currentLinkInformation.start.transform.position;
		Vector3 endPos = currentLinkInformation.end.transform.position;
		
		float distS  = (transform.position - startPos).magnitude;
		float distE  = (transform.position - endPos).magnitude;
		if(distS < distE) {
			linkStart_ = startPos;
			linkEnd_ = endPos;
		} else {
			linkStart_ = endPos;
			linkEnd_ = startPos;
		}
	
		Vector3 alignDir  = linkEnd_ - linkStart_;
		alignDir.y = 0;
		linkRot_ = Quaternion.LookRotation(alignDir);
	
		string linkAnimation = ""; 
		
		switch( currentLinkInformation.linkType)
		{
		case LinkType.JumpLink :
			linkAnimation = "Locomotion_JumpAnimation"; break;
			
		case LinkType.LadderLink :
			linkAnimation = "Locomotion_LadderAnimation"; break;
			
		default :
			Debug.LogWarning("Should never comhe here");
			linkAnimation = "Locomotion_LadderAnimation"; break;
		}
		
		//Debug.LogWarning("link animation " + linkAnimation);
		return linkAnimation;
	}
	
	private void AnimationSetup() {
		anim_  = GetComponent("Animation") as Animation;
	
		// loop in sync
		anim_["Walk"].layer = 1;
		anim_["Run"].layer = 1;
		anim_.SyncLayer(1);
		
		// speed up & play once
		anim_["RunJump"].wrapMode = WrapMode.ClampForever;
		anim_["RunJump"].speed = 2;
		anim_["Ladder_Up"].wrapMode = WrapMode.ClampForever;
		anim_["Ladder_Up"].speed = 2;
		anim_["Ladder_Down"].wrapMode = WrapMode.ClampForever;
		anim_["Ladder_Down"].speed = 2;
		
		
		if ( isADAPTMan_ )
		{
			anim_["Turn180"].wrapMode = WrapMode.ClampForever;
			anim_["Turn180"].speed = ladderSpeed_;
			anim_["Turn180-down"].wrapMode = WrapMode.ClampForever;
			anim_["Turn180-down"].speed = ladderSpeed_;
			anim_["Walk_Up"].wrapMode = WrapMode.ClampForever;
			anim_["Walk_Up"].speed = ladderSpeed_;
		}
		
		anim_.CrossFade("Idle", blendTime, PlayMode.StopAll);
	}
 	
	
	private void UpdateAnimationBlend() {
		
		float walkAnimationSpeed  = 1.5f;
		float runAnimationSpeed = 4.0f;
		float speedThreshold = 0.1f;
	
		//Vector3 velocityXZ = agent_.velocity; // Vector3(agent_.velocity.x, 0.0f , agent_.velocity.z);
		float speed = agentSpeed; //3.0f; //velocityXZ.magnitude;
		anim_["Run"].speed = speed / runAnimationSpeed;
		anim_["Walk"].speed = speed / walkAnimationSpeed;
	
		if(speed > (walkAnimationSpeed+runAnimationSpeed)/2.0f) {
			anim_.CrossFade("Run");
		}
		else if(speed > speedThreshold) {
			anim_.CrossFade("Walk");
		} else {
			anim_.CrossFade("Idle", blendTime, PlayMode.StopAll);
		}
	}
	
	
	void RetrieveAnimationCurves()
	{
		curves = agent_.GetPlanAnimationCurve();		
	}
	
	void OnDrawGizmos()
	{	
		//AnimationCurveHelper::DrawAnimationCurveGizmos(curves,10,1,Color.red);
	}
	
	void Update()
	{
		if (I_AM_ALEJANDRO)
			MoveAlongAnimationCurves (Time.time);
		
	}
	
	public void MoveAlongAnimationCurves (float time)
	{
		if (I_AM_ALEJANDRO)
		{
			if (curves == null)
				return;
		}
		else
			curves = agent_.GetPlanAnimationCurve ();
		
		float x = curves[0].Evaluate(time);
		float y = curves[1].Evaluate(time);
		float z = curves[2].Evaluate(time);
		agentSpeed = curves[3].Evaluate(time);
		
		Vector3 newPos = new Vector3(x,y,z);
		
		//Debug.Log("At time " + time + " pos = " + newPos);
		
		prevPos = transform.position;
		
		transform.position = newPos;				
		
		Vector3 mov = newPos - prevPos;	
		
		if (mov.magnitude > 0.0001f)
		{
			Vector2 movPlane = new Vector2(mov.x,mov.z);
			Vector2 forwardPlane = new Vector2(transform.forward.x,transform.forward.z);
			float angle = Vector2.Angle(movPlane,forwardPlane);
			if (agentSpeed >= 1.75f && Mathf.Abs(angle) <= 100 && locoState_ != "Locomotion_LadderAnimation" )
				transform.forward = mov;				
			else
				transform.forward = Vector3.Slerp(transform.forward,mov,Time.deltaTime);
				
			agent_.SetCurrentSpeed(agentSpeed);
			
			if (steeringManager != null)
			{
				uint newPol = steeringManager.GetClosestPolygon(newPos);
			
				if (newPol != polyRef)
				{
					steeringManager.DecrNumObjectsInPolygon(polyRef);
					steeringManager.IncrNumObjectsInPolygon(newPol);
					
					uint prevNum = steeringManager.GetNumOjbectsInPolygon(polyRef);
					uint newNum = steeringManager.GetNumOjbectsInPolygon(newPol);
					
					//Debug.Log("At time " + Time.deltaTime + ", PrevPol = " + polyRef + " has " + prevNum + " objects, and NewPol = " + newPol + " has " + newNum + " objects");
					
					polyRef = newPol;			
				}
			}
		}
		else
		{
			agentSpeed = 0;
		}
		
	}
	
}