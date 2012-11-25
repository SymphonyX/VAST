using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Class representing the Footstep Action of the footstep domain

public class FootstepPlanningAction : DefaultAction
{
	public static float e_s = 2.23f;
	public static float e_w = 1.26f;
	
	public FootstepPlanningAction(FootstepPlanningState previousState, AnotatedAnimation anim, float s, float meanStepSize, float mass)
	{
		animInfo = anim;	
		speed = s;
		cost = ComputeCost(meanStepSize, mass);
		state = new FootstepPlanningState(previousState, anim, s, NextActionPreconditions());			
	}
	
	//Constructor for actions using dummytype for planningstate
	public FootstepPlanningAction(FootstepPlanningState previousState, AnotatedAnimation anim, float s, float meanStepSize, float mass, float dummyType)
	{
		animInfo = anim;	
		speed = s;
		cost = ComputeCost(meanStepSize,mass);
		state = new FootstepPlanningState(previousState, anim, s, NextActionPreconditions(), dummyType);			
	}
	
	public FootstepPlanningAction(GridPlanningAction gridAction, AnotatedAnimation anim, float sp)
	{		
		state = gridAction.state;		
		cost = gridAction.cost;	
		animInfo = anim;
		speed = sp;
	}
	
	public FootstepPlanningAction(GridTimeAction gridAction, AnotatedAnimation anim, float sp)
	{		
		state = gridAction.state;		
		cost = gridAction.cost;	
		animInfo = anim;
		speed = sp;
	}
	
	// The anotated animation of the represented action
	public AnotatedAnimation animInfo;
	
	// The speed at which the animation will be played or the action will be performed
	public float speed; 
	
	// The cost of executing the action
	//public float cost; 
	
	// The state after performing the action
	//public FootstepPlanningState state;	
	
	public string GetActionName(){return animInfo.name;}
	
	// Function that evaluates if the current action satisfies some preconditions
	public bool SatisfiesPreconditions(AnotatedAnimation precondition)
	{		
		if (precondition == null)
			return true;
		
		// TODO: evaluate if preconditions are satisfied
		if (animInfo.swing != precondition.swing)
			return false;		
		
		// if it passes all the precondition tests
		return true;
	}
	
	public float ComputeCost(float meanStepSize, float mass)
	{	
		/*
		
		//if (animInfo.type == LocomotionMode.Idle)
		//	return 0;
		
		float spaceCost = animInfo.distance / meanStepSize;
		//if (animInfo.type == LocomotionMode.Turn )
		//	spaceCost = 0;
		
		float timeCost = 0;		
		if (state != null)
		{			
			timeCost = (state as FootstepPlanningState).currentSpeed;			
			
			if (animInfo.type == LocomotionMode.Turn || animInfo.type == LocomotionMode.WalkTurn)
				timeCost = animInfo.rotationSpeed * speed;
		}
		
		//timeCost = animInfo.time*animInfo.totalLength;
		
		float angleCost = animInfo.angleCost;
				
		//return timeCost;
		//return spaceCost;		
		return  spaceCost + timeCost;		
		//return  spaceCost + timeCost + angleCost;		
		//return 1;
		
		*/
						
		int samples = animInfo.Root.Length;
								
		float v_0 = 0;
		
		if (state != null)
		{
			FootstepPlanningState previousState = (state as FootstepPlanningState).previousState;
		
			if (previousState != null)
				v_0 = previousState.currentSpeed;		
		}
		
		//v_0 = 0;
		
		CompleteAnimationCurve animCurve = animInfo.RootCurve;
		
		float timeInterval = 0.1f;
		
		float endTime = animInfo.time*animInfo.totalLength;
		
		float cost1 = (e_s) * timeInterval;
		float cost2 =  (e_w*v_0*v_0) * timeInterval;
		
		//Debug.Log(animInfo.name + " time = " + endTime);
		
		for (float t = 0.1f; t <= endTime; t += timeInterval)
		{
			float normT = t / animInfo.totalLength;			
			
			Vector3 displacement = Vector3.zero;
			
			displacement.x = animCurve.xPos.Evaluate(normT);	
			displacement.y = animCurve.yPos.Evaluate(normT);
			displacement.z = animCurve.zPos.Evaluate(normT);			
						
			float v2 = displacement.sqrMagnitude / (t*t);
			
			//Debug.Log(animInfo.name + " v2 = " + v2 + "(at time " + t + ")");
			
			cost1 += (e_s)*timeInterval;
			cost2 += (e_w * v2)*timeInterval;
		}		
		
		float cost = (cost1+cost2)*mass;
		
		cost += animInfo.angleCost / 180 * 5 * mass;
		
		if (animInfo.type == LocomotionMode.Run)
			cost *= 2;
		
		//Debug.Log("Cost1 of " + animInfo.name + " = " + cost1);
		//Debug.Log("Cost2 of " + animInfo.name + " = " + cost2);
		//Debug.Log("Cost of " + animInfo.name + " = " + cost);
		
		return cost;
	}
	
	// Computes an action that contains the preconditions of the selectable next actions
	public AnotatedAnimation NextActionPreconditions()
	{			
		AnotatedAnimation newAnimInfo = new AnotatedAnimation();
		
		if (animInfo.type != LocomotionMode.SideStep)
		{			
			if (animInfo.swing == Joint.LeftFoot)
			{
				newAnimInfo.swing = Joint.RightFoot;
				newAnimInfo.supporting = Joint.LeftFoot;
			}
			else
			{
				newAnimInfo.swing = Joint.LeftFoot;
				newAnimInfo.supporting = Joint.RightFoot;
			}
		}
		else
		{
			newAnimInfo.swing = animInfo.swing;
			newAnimInfo.supporting = animInfo.supporting;
		}
			
		return newAnimInfo;	
	}
	
}