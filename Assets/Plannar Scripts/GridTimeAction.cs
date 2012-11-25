using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridTimeAction : DefaultAction
{
	//public GridTimeAction(Vector3 mov, float s)
	//{
	//	cost = s*s + (s-GridTimeDomain.DESIRED_SPEED)*(s-GridTimeDomain.DESIRED_SPEED);
	//}
	
	public GridTimeAction(GridTimeState previousState, Vector3 mov, float s)
	{
		state = new GridTimeState(previousState, mov, s);
		
		cost = (previousState.speed - s)*(previousState.speed - s) + (s-GridTimeDomain.DESIRED_SPEED)*(s-GridTimeDomain.DESIRED_SPEED);
		
		//cost = Vector3.Distance(previousState.statePosition(), state.statePosition()); 
		//cost = _calculateCost(previousState, state as GridTimeState);
		
		//Debug.Log ("old cost " + cost + " new cost " + cost1);
		
	}
	
	//Constructor for actions using dummytype for planningstate
	// dummy type is used to create predecessor instead of successor 
	public GridTimeAction(GridTimeState previousState, Vector3 mov, float s, float dummyType)
	{
		state = new GridTimeState(previousState, mov, s, dummyType);			
		
		cost = (previousState.speed - s)*(previousState.speed - s) + (s-GridTimeDomain.DESIRED_SPEED)*(s-GridTimeDomain.DESIRED_SPEED);
		
		//cost = Vector3.Distance(previousState.statePosition(), state.statePosition()); 
		//cost = _calculateCost(previousState, state as GridTimeState);
		//Debug.Log ("old cost " + cost + " new cost " + cost1);
	}
	
	public GridTimeAction (GridTimeState previousState, GridTimeState nextState)
	{
		state = nextState;
		
		cost = (previousState.speed - nextState.speed)*(previousState.speed - nextState.speed) + (nextState.speed-GridTimeDomain.DESIRED_SPEED)*(nextState.speed-GridTimeDomain.DESIRED_SPEED);
		
		//cost = Vector3.Distance(previousState.statePosition(), nextState.statePosition());
		//cost = _calculateCost(previousState, state as GridTimeState);
		//Debug.Log ("old cost " + cost + " new cost " + cost1);
	}
	
	private float _calculateCost (GridTimeState previousState, GridTimeState currentState) 
	{
		
		// cost for acceleration 
		float e1 = Mathf.Abs (previousState.speed * previousState.speed - currentState.speed * currentState.speed);
		// cost for deviation from desired speed
		float e2 = Mathf.Abs(GridTimeDomain.DESIRED_SPEED_SQUARE - currentState.speed * currentState.speed);
		// cost for traveling distance 
		float e3 = Vector3.SqrMagnitude(currentState.currentPosition - previousState.currentPosition);
		//Debug.Log("e1 " + e1 + " e2 " + e2 + "e3 " + e3);
		return e1+e2+e3;
			
		//return _calculateKineticEnergy (previousState, currentState) + _calculatePotentialEnergy (currentState);
	}
	
	/*
	private float _calculateKineticEnergy (GridTimeState previousState, GridTimeState currentState) 
	{
		float distance = Vector3.Distance(currentState.currentPosition,previousState.currentPosition);
		// work = a.d = (v1-v2) * d / t 
		float ke = Mathf.Abs(currentState.speed - previousState.speed) * distance / GridTimeDomain.DELTA_TIME; 
		return ke;
	}
	
	// the potential energy at a particular state that penalizes deviation from desired speed 
	private float _calculatePotentialEnergy (GridTimeState currentState) 
	{
		
		// this can be optimized -- independent of max acceleration ? 
		// v2 = u2 + 2 a s => ..
		float distance = Mathf.Abs((GridTimeDomain.DESIRED_SPEED_SQUARE - currentState.speed * currentState.speed) / ( 2 * GridTimeDomain.MAX_ACCELERATION ));
		
		// PE = g * h 
		float pe = GridTimeDomain.MAX_ACCELERATION * distance;
		
		return pe;
	}
	*/
}