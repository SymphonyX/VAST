using UnityEngine;
using System.Collections;
using System;



public class GridTimeState : DefaultState, IEquatable<GridTimeState>
{
	
	public override int GetHashCode()
	{
		return 1;	
	}
	
	public GridTimeState(){}
	
	// Construction function for an initial state
	public GridTimeState(Vector3 gameObjectPosition, float t = 0, float s = 0)
	{
		currentPosition = gameObjectPosition;		
		
		actionMov = new Vector3(0,0,0);
		
		time = t;
		
		speed = s;
		
	}

	//public static float timeSum = 0.0f;
	//public static int numCalls = 0;
	
	// Construction function for a state that comes from a previous state with a movement of mov	
	public GridTimeState(GridTimeState prevState, Vector3 mov, float s)
	{
		//float startTime = Time.realtimeSinceStartup;
		
		speed = s;
				
		ComputeActualState(prevState, mov,s);					
		
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
	public GridTimeState(GridTimeState currentState, Vector3 mov, float s, float dummyType)
	{
		speed = s;
		
		ComputePreviousState(currentState,mov,s);		
	}
		
	
	public GridTimeState(GridTimeState original)
	{
		this.currentPosition = original.currentPosition;
		this.time = original.time;
		this.speed = original.speed;
	}
	
	public GridTimeState(GridPlanningState gridState)
	{
		currentPosition = gridState.currentPosition;
		time = 0;
		speed = 0; // ¿?
	}
	
	public override Vector3 statePosition ()
	{
		return currentPosition;
	}
	
	/*
	public GridTimeState(FootstepPlanningState footstepState)
	{
		//float startTime = Time.realtimeSinceStartup;
		
		currentPosition = footstepState.currentPosition;
				
		previousState = footstepState.previousState;
		
		if (previousState != null)
			actionMov = currentPosition - footstepState.previousState.currentPosition;
		else
			actionMov = new Vector3(0,0,0);		
		
		time = footstepState.time;
		speed = footstepState.currentSpeed;
	}
	*/

	// The previous state
	//public GridTimeState previousState;	
	//public DefaultState previousState;
	
	public Vector3 currentPosition;	// current position of the Root
	
	public Vector3 actionMov;
	
	public float time;
	public float speed;
	
	// DEBUG PARAMETER
	public Vector3 obstaclePos;
	
	
	/*********************************************/
	
	
	// Computes the actual state based on the effect of the lastAction over the previous state
	// PRE: lastAction != null && previousState != null
	private void ComputeActualState(GridTimeState previousState, Vector3 mov, float s)
	{		
		mov.Normalize();		
		
		Vector3 actionMov = mov*s* GridTimeDomain.DELTA_TIME;
		
		if (previousState != null)
		{
			currentPosition = previousState.currentPosition + actionMov;
			time = previousState.time;
		}
		
		/*
		else
		{
			FootstepPlanningState prevFootstepState	 = previousState as FootstepPlanningState;
			if (prevFootstepState != null)
			{
				currentPosition = prevFootstepState.currentPosition + mov * s * deltaTime;			
				time = prevFootstepState.time;	
			}
		}
		*/		
	
		time += GridTimeDomain.DELTA_TIME;	
	}
	
	
	//Compute previous state based on the effect of the action over the current state
	//Meant to be used with backtracking algorithms (ADA*)
	public void ComputePreviousState(GridTimeState previousState, Vector3 mov, float s)
	{
		mov.Normalize();
		
		Vector3 actionMov = mov*s*GridTimeDomain.DELTA_TIME;
		
		if (previousState != null)
		{
			currentPosition = previousState.currentPosition - actionMov;
			time = previousState.time;
		}
		/*
		else
		{
			FootstepPlanningState prevFootstepState	 = previousState as FootstepPlanningState;
			if (prevFootstepState != null)
			{
				currentPosition = prevFootstepState.currentPosition - mov * s * deltaTime;				
				time = prevFootstepState.time;	
			}
		}
		*/
	
		time -= GridTimeDomain.DELTA_TIME;
	}
	
	public override bool Equals(object obj)
	{
		return Equals(obj as GridTimeState);
	}
	
	public bool Equals (GridTimeState obj)
	{
		
		bool b = false;
		
		GridTimeState s = obj;
		
		if((Mathf.Abs(this.currentPosition.x - s.currentPosition.x) < .2) &&
			(Mathf.Abs(this.currentPosition.y - s.currentPosition.y) < .2) &&
		   (Mathf.Abs(this.currentPosition.z - s.currentPosition.z) < .2))
			b = true; //return true;
		else
			b = false; // return false;
		
		bool inTime = Mathf.Abs(this.time - s.time) < 0.5;
		
		return b && inTime;
		
	}
	
	
}