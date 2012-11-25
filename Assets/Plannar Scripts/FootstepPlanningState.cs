using UnityEngine;
using System.Collections;

// State of the Footstep domain

public class FootstepPlanningState : DefaultState
{
	// Construction function for an initial state
	public FootstepPlanningState(Vector3 gameObjectPosition, Quaternion gameObjectRotation, float t)
	{
		previousState = null;
		
		actionName = null;
		actionSpeed = 0;
		
		currentPosition = gameObjectPosition;				
		
		currentSpeed = 0.0f;
		currentAcceleration = 0.0f;
		
		float initAngle = gameObjectRotation.eulerAngles.y;
		currentRotation = Quaternion.AngleAxis(initAngle,new Vector3(0,1,0));		
		
		preconditions = null; 
		
		time = t;
	}
		
	//public static float timeSum = 0.0f;
	//public static int numCalls = 0;
	
	// Construction function for a state that comes from a previous state with an action	
	public FootstepPlanningState(FootstepPlanningState prevState, AnotatedAnimation anim, float speed, AnotatedAnimation pre)
	{
		//float startTime = Time.realtimeSinceStartup;
		
		previousState = prevState;
		
		actionName = anim.name;
		actionSpeed = speed;
		
		time = prevState.time;
		
		ComputeActualState(anim);				
		
		preconditions = pre;
		
		
		/*
		float endTime = Time.realtimeSinceStartup;
		
		timeSum += (endTime - startTime);
		numCalls++;
		
		float meanTime = 0.0f;
		if (numCalls == 100)
		{
			meanTime = timeSum / numCalls;
			Debug.Log("At time " + Time.time + " MeanTime = " + meanTime);
			numCalls = 0;
			timeSum = 0;
		}
		*/
		
	}
	
	//This constructor creates a previous state given a current state and an action.
	//The dummyType is to differentiate it from the constructor used to create successors states.
	public FootstepPlanningState(FootstepPlanningState currentState, AnotatedAnimation anim,
	                             float speed, AnotatedAnimation pre, float dummyType)
	{
		previousState = currentState;
		
		actionName = anim.name;
		actionSpeed = speed;
		
		time = currentState.time;
		
		ComputePreviousState(anim);
		
		preconditions = pre;
	}
	
	
	
	
	
	public FootstepPlanningState(FootstepPlanningState original)
	{
		this.actionName = original.actionName;
		this.actionSpeed = original.actionSpeed;
		this.currentAcceleration = original.currentAcceleration;
		this.currentPosition = original.currentPosition;
		this.currentRotation = original.currentRotation;
		this.currentSpeed = original.currentSpeed;
		this.leftFoot = original.leftFoot;
		this.leftHand = original.leftHand;
		this.obstaclePos = original.obstaclePos;
		this.preconditions = original.preconditions;
		this.previousState = original.previousState;
		this.rightFoot = original.rightFoot;
		this.rightHand = original.rightHand;
		this.time = original.time;	
	}
	
	public FootstepPlanningState(GridPlanningState gridState)
	{
		//float startTime = Time.realtimeSinceStartup;
		
		this.actionName = "GridAction";
		//this.actionSpeed = 1.0f;
		//this.currentAcceleration = 1.0f;
		this.previousState = gridState.previousState as FootstepPlanningState;
		this.currentPosition = gridState.currentPosition;
		//this.currentRotation = gridState.previousState.currentRotation;		
		//this.currentSpeed = 1.0f;
		//this.leftFoot = gridState.currentPosition;
		//this.leftHand = gridState.currentPosition;
		//this.obstaclePos = original.obstaclePos;
		//this.preconditions = original.preconditions;		
		//this.rightFoot = gridState.currentPosition;
		//this.rightHand = gridState.currentPosition;
		//this.time = original.time;			
		
		/*
		float endTime = Time.realtimeSinceStartup;
		
		timeSum += (endTime - startTime);
		numCalls++;
		
		float meanTime = 0.0f;
		if (numCalls == 100)
		{
			meanTime = timeSum / numCalls;
			Debug.Log("At time " + Time.time + " MeanTime = " + meanTime);
			numCalls = 0;
			timeSum = 0;
		}
		*/
	}
	
	// The previous state
	public FootstepPlanningState previousState;
	
	/*********************************************/
	// TODO: define parameters to define a state
		
	public string actionName; // the name of the action played to get to the state
	public float actionSpeed; // the speed at which actionId is to be played
	
	public Vector3 currentPosition;	// current position of the Root
	public float currentSpeed; // current speed of the Root
	public float currentAcceleration; // current acceleration of the Root
	public Quaternion currentRotation; // current root rotation
	
	public Vector3 leftFoot;
	public Vector3 rightFoot;
	
	public Vector3 leftHand;
	public Vector3 rightHand;
	
	public float time; // added time since the computation of the plan for the character to get to that state
	
	// DEBUG PARAMETER
	public Vector3 obstaclePos;
	
	/*********************************************/
	
	// The Next Action Preconditions
	public AnotatedAnimation preconditions;
	
	// Computes the actual state based on the effect of the lastAction over the previous state
	// PRE: lastAction != null && previousState != null
	private void ComputeActualState(AnotatedAnimation anim)
	{		
		if (previousState != null)
		{				
			int endSample = anim.Root.Length-1;
			
			/*
				Vector3 forward = Vector3.zero;
				
				float currentTime = anim.time/anim.totalLength;
					
				CompleteAnimationCurve animCurve = anim.RootCurve;					
			
				forward.x = animCurve.xRot.Evaluate(currentTime);
				forward.y = animCurve.yRot.Evaluate(currentTime);
				forward.z = animCurve.zRot.Evaluate(currentTime);		
								
				Quaternion rotation = Quaternion.FromToRotation(animCurve.initForward,forward);
				currentRotation = previousState.currentRotation * rotation;
			*/	
			
			//currentRotation = previousState.currentRotation * Quaternion.AngleAxis(anim.rotationAngle,new Vector3(0,1,0));						
			float yRot = anim.rotationAngle;//anim.RootCurve.yRot.Evaluate(anim.time);
			currentRotation = previousState.currentRotation * Quaternion.Euler(new Vector3(0,yRot,0));
			
			leftFoot = previousState.currentPosition + previousState.currentRotation * ( anim.LeftFoot[endSample].position );
			rightFoot = previousState.currentPosition + previousState.currentRotation * ( anim.RightFoot[endSample].position );
			
			leftHand = previousState.currentPosition + previousState.currentRotation * ( anim.LeftHand[endSample] );
			rightHand = previousState.currentPosition + previousState.currentRotation * ( anim.RightHand[endSample] );
			
			currentPosition = previousState.currentPosition + previousState.currentRotation * anim.rootDisplacement;
			currentSpeed = anim.speed * actionSpeed;
			currentAcceleration = currentSpeed - previousState.currentSpeed;							
			
			time += anim.time * anim.totalLength / actionSpeed;			
			
		}
	}
	
	
	//Compute previous state based on the effect of the action over the current state
	//Meant to be used with backtracking algorithms (ADA*)
	private void ComputePreviousState(AnotatedAnimation anim)
	{
		if(previousState != null)
		{
			int endSample = anim.Root.Length-1;
			
			//currentRotation = previousState.currentRotation * Quaternion.AngleAxis(anim.rotationAngle, new Vector3(0,1,0));
			float yRot = anim.rotationAngle;//anim.RootCurve.yRot.Evaluate(anim.time);
			currentRotation = previousState.currentRotation * Quaternion.Euler(new Vector3(0,yRot,0));
			
			leftFoot = previousState.currentPosition - previousState.currentRotation * (anim.LeftFoot[endSample].position);
			rightFoot = previousState.currentPosition - previousState.currentRotation * (anim.RightFoot[endSample].position);
			
			leftHand = previousState.currentPosition - previousState.currentRotation * (anim.LeftHand[endSample]);
			rightHand = previousState.currentPosition - previousState.currentRotation * (anim.RightHand[endSample]);
			
			currentPosition = previousState.currentPosition - previousState.currentRotation * anim.rootDisplacement;
			currentSpeed = anim.speed * actionSpeed;
			currentAcceleration = currentSpeed - previousState.currentSpeed;
			
			time -= anim.time * anim.totalLength / actionSpeed;
		}
	}
	
}
