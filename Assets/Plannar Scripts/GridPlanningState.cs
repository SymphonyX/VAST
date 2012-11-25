using UnityEngine;
using System.Collections;


public class GridPlanningState : DefaultState
{
	// Construction function for an initial state
	public GridPlanningState(Vector3 gameObjectPosition)
	{
		previousState = null;
		
		currentPosition = gameObjectPosition;		
		
		actionMov = new Vector3(0,0,0);
		
	}

	//public static float timeSum = 0.0f;
	//public static int numCalls = 0;
	
	// Construction function for a state that comes from a previous state with a movement of mov	
	public GridPlanningState(GridPlanningState prevState, Vector3 mov)
	{
		//float startTime = Time.realtimeSinceStartup;
		
		previousState = prevState;
		
		actionMov = mov;		
		
		ComputeActualState(mov);					
		
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
	public GridPlanningState(GridPlanningState currentState, Vector3 mov, float dummyType)
	{
		previousState = currentState;
		
		actionMov = mov;
		
		ComputePreviousState(mov);		
	}
		
	
	public GridPlanningState(GridPlanningState original)
	{
		this.currentPosition = original.currentPosition;
		this.actionMov = original.actionMov;
		this.previousState = original.previousState;
	}
	
	public GridPlanningState(GridTimeState gridTimeState)
	{
		currentPosition = gridTimeState.currentPosition;
		actionMov = gridTimeState.actionMov;
		previousState = null; //gridTimeState.previousState;
	}
	
	public GridPlanningState(FootstepPlanningState footstepState)
	{
		//float startTime = Time.realtimeSinceStartup;
		
		currentPosition = footstepState.currentPosition;
				
		previousState = footstepState.previousState;
		
		if (previousState != null)
			actionMov = currentPosition - footstepState.previousState.currentPosition;
		else
			actionMov = new Vector3(0,0,0);		
		
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
	
	public override Vector3 statePosition ()
	{
		return currentPosition;
	}
	
	// The previous state
	//public GridPlanningState previousState;	
	public DefaultState previousState;
	
	public Vector3 currentPosition;	// current position of the Root
	
	public Vector3 actionMov;
	
	/*********************************************/
	
	
	// Computes the actual state based on the effect of the lastAction over the previous state
	// PRE: lastAction != null && previousState != null
	private void ComputeActualState(Vector3 mov)
	{		
		GridPlanningState prevGridState = previousState as GridPlanningState;		
		if (prevGridState != null)
			currentPosition = prevGridState.currentPosition + mov;
		else
		{
			FootstepPlanningState prevFootstepState	 = previousState as FootstepPlanningState;
			if (prevFootstepState != null)
				currentPosition = prevFootstepState.currentPosition + mov;			
		}
	}
	
	
	//Compute previous state based on the effect of the action over the current state
	//Meant to be used with backtracking algorithms (ADA*)
	private void ComputePreviousState(Vector3 mov)
	{
		GridPlanningState prevGridState = previousState as GridPlanningState;		
		if (prevGridState != null)
			currentPosition = prevGridState.currentPosition - mov;
		else
		{
			FootstepPlanningState prevFootstepState	 = previousState as FootstepPlanningState;
			if (prevFootstepState != null)
				currentPosition = prevFootstepState.currentPosition - mov;				
		}
	}
	
}