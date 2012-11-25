using UnityEngine;
using System.Collections;
using System.Collections.Generic;


// Class managing the analysis of all the animations
public class AnimationAnalyzer : MonoBehaviour {
	
	// set this to true if we want to analyze and use all the animations of the agent
	// set to false if we want just to use the animations in the list animationNames
	public bool allAnimations; 	
	public string[] animationNames;
	
	public int rootSamples; // the number of samples we want for the rootAnimationCurve
	public int samples; // the number of samples we want to analyze of each animation clip
	
	// Joint Transforms
	public Transform root;
	public Transform leftFoot; public Transform leftToe;
	public Transform rightFoot; public Transform rightToe;	
	
	public float circleRadius = 0.5f;
	
	public float height = 0.23f;
	
	public float footRadius = 0.25f;	
	//public Transform head;
	public float headRadius = 0.3f;
	public Transform leftHand;
	public Transform rightHand;
	public float handRadius = 0.2f;
	
	public bool debugDraw = false;
	
	public bool log = false;
	
	// General extracted info
	[HideInInspector] public float maxStepSize;
	private float sumStepsSizes;
	[HideInInspector] public float meanStepSize;
	[HideInInspector] public int walkingAnimations;
	[HideInInspector] public float maxActionDuration;
	[HideInInspector] public float meanActionDuration;
	private float sumActionDuration;
	
	public SerializableDictionary<string,AnotatedAnimation> analyzedAnimations;	
	
	[HideInInspector] public bool initialized;
	
	
	public float mass = 75;
	/*
	public float RMassProportion = 1;
	private float R;
	public float w = 0.35f;
	*/
	
	void Awake(){
		initialized = false;		
	}
	
	// Use this for initialization
	public void Init () {
		
		if (allAnimations)
		{
			int num = 0;
			foreach ( AnimationState anim in animation )
				if (anim.name != "TPose")
					num++;
			
			animationNames = new string[num];
			int index = 0;
			foreach ( AnimationState anim in animation )				
			{
				if (anim.name != "TPose")
				{
					animationNames[index] = anim.name;
					index++;
				}
			}
		}
		
		walkingAnimations = 0;
		maxStepSize = 0;
		sumStepsSizes = 0;
		meanStepSize = 0;
		meanActionDuration = 0;
		maxActionDuration = 0;
		
		Vector3 startPosition = transform.position;
		Quaternion startRotation = transform.rotation;
		
		transform.position = new Vector3(0,0,0);
		transform.rotation = Quaternion.identity; //new Quaternion(0,0,0,0);		
		//transform.forward = new Vector3(0,0,1);
		//transform.right = new Vector3(1,0,0);
		
		// We ensure that no animation is played
		RemoveAnimations(animation);
		 
		// We create and fill a Dictionary of AnotatedAnimations
		analyzedAnimations = new SerializableDictionary<string,AnotatedAnimation>();		
			
		int id=0;
		foreach ( string anim in animationNames )				
		{
			if (!analyzedAnimations.ContainsKey(anim) && !anim.Contains("TPose") )
			{				
				analyzedAnimations.Add( anim, AnalyzeAnimation(animation[anim],id));					
				id++;
			}
		}			
		
		transform.position = startPosition;
		transform.rotation = startRotation;
		
		
		UpdateGlobalInfo();
	
		
		initialized = true;
		
		
		//R = mass*RMassProportion;
	}
		
	
	public void UpdateGlobalInfo()
	{
		meanStepSize = sumStepsSizes / walkingAnimations;
		meanActionDuration = sumActionDuration / analyzedAnimations.Count;	
	}
	
	// Stops all animations
	public void RemoveAnimations(Animation agentAnimation)
	{
		foreach ( AnimationState anim in agentAnimation)		
		{
			anim.enabled = false;			
			animation.Stop();
			animation.Sample();
		}
		
		
		animation["TPose"].enabled = true;
		animation["TPose"].wrapMode = WrapMode.Loop;
		animation.Play("TPose");
		animation.Sample();
		animation.Stop();
		animation.Sample();
		
	
	}
	
	// Detect the foot on the floor based on a height comparison
	private Joint DetectPlantedFoot()
	{
		if (leftFoot.position[1] < rightFoot.position[1])
			return Joint.LeftFoot;
		else
			return Joint.RightFoot;
	}
	
	// Struct to define a foot plant in the animation
	// with normalized times
	private struct FootPlant
	{
		public float start; 
		public float end;
		public float lenght;
	};
	
	// Returns the normalized time of the first foot plant of the animation anim
	// considering the supporting foot
	// and the normalized time length of that foot plant
	private void SeekFootplantTime(AnimationState anim, Joint supporting, ref float footPlantTime, ref float footPlantLength)
	{
		float numberOfSamples = 100;
		float timeStep = 1 / numberOfSamples;
		float auxTime = 0;
		
		List<FootPlant> footPlants = new List<FootPlant>();
		
		FootPlant auxFootPlant;
		auxFootPlant.start = -1;
		auxFootPlant.end = -1;
		auxFootPlant.lenght = 0;
		
		// We sample the animation and check each pose
		for (auxTime = 0; auxTime < 1; auxTime += timeStep)
		{
			anim.normalizedTime = auxTime;
			animation.Play(anim.name);		
			animation.Sample();
			
			// if the foot on the floor is not the supporting foot anymore
			// it means that the swing foot could be planting
			if (DetectPlantedFoot() != supporting)
			{
				// we set the star time of the foot plant
				if (auxFootPlant.start == -1)
					auxFootPlant.start = auxTime;				
			}
			// if the foot on the floor is the supporting foot
			// but previously we detected a foot plant starting
			else if (auxFootPlant.start != -1)
			{
				// We set the end time of the foot plant to the previous sample
				auxFootPlant.end = auxTime - timeStep;
				auxFootPlant.lenght = auxFootPlant.end - auxFootPlant.start;
				
				footPlants.Add(auxFootPlant);
				
				auxFootPlant.start = -1;
				auxFootPlant.end = -1;
				auxFootPlant.lenght = 0;
			}
		}
		
		auxFootPlant.lenght = -1;
		
		// To ensure that we are finding the right foot plant
		// we look for the foot plant that lasted more time
		foreach (FootPlant fp in footPlants)
			if (auxFootPlant.lenght < fp.lenght)
				auxFootPlant = fp;
				
		// TODO: we might want to store the whole fase of the foot plant
		footPlantTime = auxFootPlant.start; // + auxFootPlant.lenght / 2;
		footPlantLength = auxFootPlant.lenght; 
		
		animation.Stop();
		animation.Sample();		
	}	
		     
		     
	// The function that analyzes an animation and returns the AnotatedAnimation
	private AnotatedAnimation AnalyzeAnimation(AnimationState anim, int id)
	{		
		AnotatedAnimation analyzedAnim = new AnotatedAnimation();
		
		////////
		
		analyzedAnim.Init(samples);
		
		analyzedAnim.id = id;
		analyzedAnim.name = anim.name;			
		analyzedAnim.totalLength = anim.length;
		
		if (analyzedAnim.name.Contains("WalkTurn") 
		    || ( analyzedAnim.name.Contains("Walk") &&  analyzedAnim.name.Contains("Turn"))
		    || ( analyzedAnim.name.Contains("Turn") &&  analyzedAnim.name.Contains("Turn"))
		    )
			analyzedAnim.type = LocomotionMode.WalkTurn;
		else if (analyzedAnim.name.Contains("Walk"))
			analyzedAnim.type = LocomotionMode.Walk;
		else if (analyzedAnim.name.Contains("Run"))
			analyzedAnim.type = LocomotionMode.Run;
		else if (analyzedAnim.name.Contains("Idle"))
			analyzedAnim.type = LocomotionMode.Idle;
		else if (analyzedAnim.name.Contains("Jump"))
			analyzedAnim.type = LocomotionMode.Jump;		
		else if (analyzedAnim.name.Contains("Turn"))
			analyzedAnim.type = LocomotionMode.Turn;
		else if (analyzedAnim.name.Contains("Side"))
			analyzedAnim.type = LocomotionMode.SideStep;
		else //if (analyzedAnim.name.Contains("Action"))
			analyzedAnim.type = LocomotionMode.Action;
				
		// Load Animation
		anim.normalizedTime = 0.0f;
		anim.enabled = true;
		anim.wrapMode = WrapMode.Loop;
		
		animation.Play(analyzedAnim.name);
		animation.Sample();
		
		// Detect supporting and swing foot
		analyzedAnim.SetSupportingFoot( DetectPlantedFoot() );
		
		
		if (
		    analyzedAnim.type == LocomotionMode.Walk 
		    || analyzedAnim.type == LocomotionMode.Run 
		    || analyzedAnim.type == LocomotionMode.WalkTurn		    
		    || analyzedAnim.type == LocomotionMode.Jump
		    )
		{
			// We look the time where the action is completed
			// e.g. the swing foot is planted	
			SeekFootplantTime(anim, analyzedAnim.supporting, ref analyzedAnim.time, ref analyzedAnim.footPlantLenght);		
		}
		else 
		{
			analyzedAnim.time = 1.0f-0.001f;
			analyzedAnim.footPlantLenght = analyzedAnim.time/2;	
		}		
		
		//////////
		Vector3 initPos = gameObject.transform.position;
		Quaternion initRot = gameObject.transform.rotation;
		
		// Load Animation
		anim.normalizedTime = 0.0f;		
		animation.Play(analyzedAnim.name);
		animation.Sample();
				
		// Analyze Initial State
		analyzeInitState(analyzedAnim);
		
		// For every sample we want
		for (int i=1; i<samples; i++)
		{		
			// Sample the Animation until the end of our action
			anim.normalizedTime = i*analyzedAnim.time/(samples-1);
			animation.Play(analyzedAnim.name);		
			animation.Sample();
						
			// Analyze State
			analyzeState(analyzedAnim,i,anim.normalizedTime);			
		}	
		
		for (int i=1; i<rootSamples; i++)
		{
			// Sample the Animation until the end of our action
			anim.normalizedTime = i*analyzedAnim.time/(rootSamples-1);
			animation.Play(analyzedAnim.name);		
			animation.Sample();
						
			// Analyze State
			copyRootCurve(analyzedAnim,i,anim.normalizedTime);			
		}
			
		// Compute Action Movement Attributes
		analyzeMovement(analyzedAnim);
		
		////////
		
		// We remove the animation
		anim.normalizedTime = 0.0f;
		anim.enabled = false;
		animation.Stop();		
		animation.Sample();
		
		
		animation["TPose"].enabled = true;
		animation["TPose"].wrapMode = WrapMode.Loop;
		animation.Play("TPose");
		animation.Sample();
		animation.Stop();
		animation.Sample();
		
		
		gameObject.transform.position = initPos;
		gameObject.transform.rotation = initRot;
	
		updateGlobalInfo(analyzedAnim);
		
		
		return analyzedAnim;
	}
	
	
	public void updateGlobalInfo(AnotatedAnimation analyzedAnim)
	{			
		// We update some global information
		if ( analyzedAnim.type == LocomotionMode.Walk || analyzedAnim.type == LocomotionMode.WalkTurn 
		    || analyzedAnim.type == LocomotionMode.SideStep )
		{
			walkingAnimations++;
			
			float displ = analyzedAnim.rootDisplacement.magnitude;
			if ( displ > maxStepSize)
				maxStepSize = displ;
			
			sumStepsSizes += displ;
		}
		
		float duration = analyzedAnim.time*analyzedAnim.totalLength;
		
		//Debug.Log(analyzedAnim.name + " duration: " + duration);
		
		if ( analyzedAnim.type != LocomotionMode.Idle )
		{			
			if (duration > maxActionDuration)
				maxActionDuration = duration;
			sumActionDuration += duration;
		}		
		
		// For debugging we print the resulting analysis
		if (log)
			analyzedAnim.LogAnotations();
	}
	
	public float computeForwardRotY()
	{
		Vector3 initForwardProj = new Vector3(-root.right.x,0,-root.right.z);
		Vector3 initZVector = new Vector3(0,0,1);
		return CalculateAngleOf2Vectors(initZVector,initForwardProj);							
	}
	
	public void analyzeInitState( AnotatedAnimation anotatedAnim)
	{
		anotatedAnim.RootCurve.initRootPos = root.position;
		
		anotatedAnim.RootCurve.initRotY = computeForwardRotY();

		anotatedAnim.RootCurve.initHeight = root.position.y;	
			
		analyzeState(anotatedAnim,0,0);
	}
	
	public void copyRootCurve( AnotatedAnimation anotatedAnim , int sample, float normalizedTime)
	{
		Vector3 rootDisplacement = root.position - anotatedAnim.RootCurve.initRootPos;
				
		// if it is the last sample we want to set back the height
		// TODO: not do this if we want animations where we want the end position higher.
		if (sample == rootSamples-1)
			rootDisplacement.y = 0;
		
		anotatedAnim.RootCurve.xPos.AddKey(normalizedTime,rootDisplacement.x);
		anotatedAnim.RootCurve.yPos.AddKey(normalizedTime,rootDisplacement.y);
		anotatedAnim.RootCurve.zPos.AddKey(normalizedTime,rootDisplacement.z);
		
		float rotY = computeForwardRotY() - anotatedAnim.RootCurve.initRotY;
		
		anotatedAnim.RootCurve.yRot.AddKey(normalizedTime,rotY);		
		
	}
	
	public void analyzeState( AnotatedAnimation anotatedAnim , int sample, float normalizedTime)
	{			
		Vector3 rootDisplacement = root.position - anotatedAnim.RootCurve.initRootPos;
				
		// if it is the last sample we want to set back the height
		// TODO: not do this if we want animations where we want the end position higher.
		if (sample == samples-1)
			rootDisplacement.y = 0;
		
		//anotatedAnim.RootCurve.xPos.AddKey(normalizedTime,rootDisplacement.x);
		//anotatedAnim.RootCurve.yPos.AddKey(normalizedTime,rootDisplacement.y);
		//anotatedAnim.RootCurve.zPos.AddKey(normalizedTime,rootDisplacement.z);
		
		float rotY = computeForwardRotY() - anotatedAnim.RootCurve.initRotY;
		
		//anotatedAnim.RootCurve.yRot.AddKey(normalizedTime,rotY);
				
		anotatedAnim.Root[sample].position = root.position;
		//anotatedAnim.Root[sample].angle = root.localRotation.eulerAngles.y;
		//anotatedAnim.Root[sample].orientation = new Vector3(Mathf.Cos(anotatedAnim.Root[sample].angle*Mathf.Deg2Rad),0,Mathf.Sin(anotatedAnim.Root[sample].angle*Mathf.Deg2Rad));
		
		anotatedAnim.Root[sample].orientation = -root.right; 
		anotatedAnim.Root[sample].orientation.Normalize();
		anotatedAnim.Root[sample].angle = CalculateAngle(anotatedAnim.Root[sample].orientation.x,anotatedAnim.Root[sample].orientation.z);
		
		anotatedAnim.LeftFoot[sample].position = leftFoot.position;
		anotatedAnim.RightFoot[sample].position = rightFoot.position;
				
		anotatedAnim.LeftFoot[sample].orientation = leftToe.position - leftFoot.position;
		anotatedAnim.LeftFoot[sample].orientation[1] = 0;
		anotatedAnim.RightFoot[sample].orientation = rightToe.position - rightFoot.position;
		anotatedAnim.RightFoot[sample].orientation[1] = 0;
		
		anotatedAnim.LeftFoot[sample].angle = Vector3.Angle(new Vector3(1,0,0), anotatedAnim.LeftFoot[sample].orientation);
		anotatedAnim.RightFoot[sample].angle = Vector3.Angle(new Vector3(1,0,0), anotatedAnim.RightFoot[sample].orientation);
		
		anotatedAnim.LeftHand[sample] = leftHand.position;
		anotatedAnim.RightHand[sample] = rightHand.position;
	}
	
	public void analyzeMovement( AnotatedAnimation anotatedAnim )
	{		
		int endSample = samples-1;
		
		anotatedAnim.RootCurve.xPos.postWrapMode = WrapMode.Clamp;
		anotatedAnim.RootCurve.xPos.preWrapMode = WrapMode.Clamp;
		anotatedAnim.RootCurve.yPos.postWrapMode = WrapMode.Clamp;
		anotatedAnim.RootCurve.yPos.preWrapMode = WrapMode.Clamp;
		anotatedAnim.RootCurve.zPos.postWrapMode = WrapMode.Clamp;
		anotatedAnim.RootCurve.zPos.preWrapMode = WrapMode.Clamp;
		
		anotatedAnim.RootCurve.yRot.postWrapMode = WrapMode.Clamp;
		anotatedAnim.RootCurve.yRot.preWrapMode = WrapMode.Clamp;
		
		Vector3 origin = anotatedAnim.Root[0].position;
//		
		float rootRefAngle = anotatedAnim.Root[0].angle;
					
		//anotatedAnim.rotationAngle =  -(anotatedAnim.Root[endSample].angle - anotatedAnim.Root[0].angle);
		anotatedAnim.rotationAngle = anotatedAnim.RootCurve.yRot.Evaluate(anotatedAnim.time);
		anotatedAnim.rotationSpeed = anotatedAnim.rotationAngle / (anotatedAnim.time*anotatedAnim.totalLength);				
		
		
		for (int i=0; i<samples; i++)
		{
		
			anotatedAnim.Root[i].position -= origin;		
			anotatedAnim.RightFoot[i].position -= origin;
			anotatedAnim.LeftFoot[i].position -= origin;
			
			anotatedAnim.LeftHand[i] -= origin;
			anotatedAnim.RightHand[i] -= origin;		
						
			//anotatedAnim.Root[i].angle = 0;
			anotatedAnim.Root[i].angle -= rootRefAngle;
			
			anotatedAnim.RightFoot[i].angle -= rootRefAngle;
			anotatedAnim.LeftFoot[i].angle -= rootRefAngle;			
		}			
			
		anotatedAnim.movement_RightFoot.position = anotatedAnim.RightFoot[endSample].position - anotatedAnim.RightFoot[0].position; 
		//anotatedAnim.movement_RightFoot.position[1] = 0;
		anotatedAnim.movement_LeftFoot.position = anotatedAnim.LeftFoot[endSample].position - anotatedAnim.LeftFoot[0].position;
		//anotatedAnim.movement_LeftFoot.position[1] = 0;
		
		anotatedAnim.movement_LeftHand = anotatedAnim.LeftHand[endSample] - anotatedAnim.LeftHand[0];
		anotatedAnim.movement_RightHand = anotatedAnim.RightHand[endSample] - anotatedAnim.RightHand[0];
		
		anotatedAnim.rootDisplacement = anotatedAnim.Root[endSample].position - anotatedAnim.Root[0].position;
		anotatedAnim.rootDisplacement[1] = 0;
		anotatedAnim.distance = anotatedAnim.rootDisplacement.magnitude;
		anotatedAnim.speed = anotatedAnim.distance / (anotatedAnim.time*anotatedAnim.totalLength);
				
		//anotatedAnim.angle = Vector3.Angle(new Vector3(1,0,0), anotatedAnim.rootDisplacement) - rootRefAngle;				
		
		anotatedAnim.angle = -CalculateAngleOf2Vectors(anotatedAnim.Root[0].orientation,anotatedAnim.rootDisplacement);		
		
			
		if (anotatedAnim.type == LocomotionMode.WalkTurn || anotatedAnim.type == LocomotionMode.Turn )
			anotatedAnim.angleCost = Mathf.Abs( Mathf.Abs( anotatedAnim.angle ) - Mathf.Abs( anotatedAnim.rotationAngle ) ) / 180;		
		else
			anotatedAnim.angleCost = Mathf.Abs( anotatedAnim.angle );// / 90;	
			
		
		
	}
	
	public int GetNumberOfAnimations()
	{
		return analyzedAnimations.Count;
	}
	
	public AnotatedAnimation GetAnotatedAnimation(string aName)
	{
		if (aName == null)
			return null;		
		else if (analyzedAnimations.ContainsKey(aName))
			return analyzedAnimations[aName];
		else
			return null;
	}
		

	public float GetRadius(){
		return circleRadius;	
	}
	
	public float GetHeight(){
		return height;
	}
	
	void OnDrawGizmosSelected()
	{
		if (debugDraw)
		{			
			// Check right foot collisions
			Vector3 start = rightFoot.position;
			Vector3 end = start + new Vector3(0,0.23f,0);
			float radius = footRadius;					
			Gizmos.DrawLine(start,end);
			Gizmos.DrawWireSphere(start,radius);
			
			// Check left foot collisions
			start = leftFoot.position;
			end = start + new Vector3(0,0.23f,0);		
			Gizmos.DrawLine(start,end);
			Gizmos.DrawWireSphere(start,radius);
			
			// Check head collisions
			start = transform.position;
			end = start + new Vector3(0,0.23f,0);///2,0);
			radius = headRadius;		
			Gizmos.DrawLine(start,end);
			Gizmos.DrawWireSphere(start,radius);
			
			
			// Check left hand collision
			start = leftHand.position;
			radius = handRadius;
			Gizmos.DrawWireSphere(start,radius);
						    
			// Check right hand collision
			start = rightHand.position;
			Gizmos.DrawWireSphere(start,radius);						
		}
	}
	
	// Returns the angle given a cos and a sin
	public static float CalculateAngle(float cosi, float sinu)
	{
		float angle = Mathf.Acos(cosi);
		if (sinu < 0)
			angle = -angle;
		
		if ( Mathf.Abs(angle) < 0.0001) 
			angle = 0;
			
		return angle*Mathf.Rad2Deg;
	}
	
	// Returns the signed angle between two vectors
	public static float CalculateAngleOf2Vectors(Vector3 vec1, Vector3 vec2)
	{
		if (vec1.magnitude == 0 || vec2.magnitude == 0)
			return 0;
				
		float cosi = Vector3.Dot(vec1,vec2)/(vec1.magnitude*vec2.magnitude);
		float sinu = 0;
		if (cosi != 1)
			sinu = Mathf.Sqrt(1-cosi*cosi);
		if ( (Vector3.Cross(vec1,vec2)).y < 0 ) sinu = -sinu;
		
		float angle =  CalculateAngle(cosi,sinu);
	
		return angle;
	}
	
	public void ReadAnalysisFromFile(string fileName)
	{
		//reading an object from file 
		ObjectSerializer os1 = new ObjectSerializer();
		os1.serializedObject = new SerializableDictionary<string,AnotatedAnimation>();
		os1.readObjectFromFile(fileName);
		analyzedAnimations = (SerializableDictionary<string,AnotatedAnimation>) os1.serializedObject;		
		
		foreach (KeyValuePair<string,AnotatedAnimation> anim in analyzedAnimations)
		{
			updateGlobalInfo(anim.Value);
		}
		UpdateGlobalInfo();
		
		if (log)
		{
			foreach (KeyValuePair<string,AnotatedAnimation> anim in analyzedAnimations)
			{
				anim.Value.LogAnotations();	
			}
			
			Debug.Log("Mean duration: " + meanActionDuration);
			Debug.Log("Mean step size: " + meanStepSize);
		}		
		
	}
	
}

