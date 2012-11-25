using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


class GridPlanningDomain : PlanningDomainBase
{
	public float W = 1.5f;
	
	public float window = 1.0f;	
	
	private AnimationAnalyzer analyzer;
	
	private int layer =   (1 << LayerMask.NameToLayer("Obstacles"))
						| (1 << LayerMask.NameToLayer("StaticWorld"));	
	
	
	private List<Vector3> movDirections;	
	public List<Vector3> getMovDirections(){ return movDirections; }
	
	public GridPlanningDomain(AnimationAnalyzer animAnalyzer)
	{
		analyzer = animAnalyzer;
					
		Vector3 dir1 = new Vector3(1,0,0);		
		Vector3 dir2 = new Vector3(-1,0,0);
		Vector3 dir3 = new Vector3(0,0,1);
		Vector3 dir4 = new Vector3(0,0,-1);
						
		Vector3 dir5 = new Vector3(-1,0,-1);
		dir5.Normalize();
		Vector3 dir6 = new Vector3(-1,0,1);
		dir6.Normalize();
		Vector3 dir7 = new Vector3(1,0,-1);
		dir7.Normalize();
		Vector3 dir8 = new Vector3(1,0,1);
		dir8.Normalize();

		movDirections = new List<Vector3>();
		
		/*
		if (analyzer != null)
		{
			movDirections.Add(dir1*analyzer.meanStepSize);
			movDirections.Add(dir2*analyzer.meanStepSize);
			movDirections.Add(dir3*analyzer.meanStepSize);
			movDirections.Add(dir4*analyzer.meanStepSize);
			
			
			movDirections.Add(dir5*analyzer.meanStepSize);
			movDirections.Add(dir6*analyzer.meanStepSize);
			movDirections.Add(dir7*analyzer.meanStepSize);
			movDirections.Add(dir8*analyzer.meanStepSize);
		}		
		else
		*/
		{
			float meanStepSize = 1.0f;
			
			movDirections.Add(dir1*meanStepSize);
			movDirections.Add(dir2*meanStepSize);
			movDirections.Add(dir3*meanStepSize);
			movDirections.Add(dir4*meanStepSize);
						
			movDirections.Add(dir5*meanStepSize);
			movDirections.Add(dir6*meanStepSize);
			movDirections.Add(dir7*meanStepSize);
			movDirections.Add(dir8*meanStepSize);
		}		
	}
	
	//public static float timeSum = 0.0f;
	//public static int numCalls = 0;
	
	public override bool equals (DefaultState s1, DefaultState s2, bool isStart)
	{
		//float startTime = Time.realtimeSinceStartup;
		
		
		
		bool b = false;
		
		GridPlanningState state1 = s1 as GridPlanningState;
		GridPlanningState state2 = s2 as GridPlanningState;
		
		if(isStart)
		{
			if((Mathf.Abs(state1.currentPosition.x - state2.currentPosition.x) < .5) &&
				(Mathf.Abs(state1.currentPosition.y - state2.currentPosition.y) < .5) &&
			   (Mathf.Abs(state1.currentPosition.z - state2.currentPosition.z) < .5))
				b = true;//return true;
			else 
				b = false; //return false;
		}
		else
		{
			if((Mathf.Abs(state1.currentPosition.x - state2.currentPosition.x) < .1) &&
				(Mathf.Abs(state1.currentPosition.y - state2.currentPosition.y) < .1) &&
			   (Mathf.Abs(state1.currentPosition.z - state2.currentPosition.z) < .1))
				b = true; //return true;
			else
				b = false; // return false;
		}
		
		
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
		
		return b;
	}
	
	
	override public bool isAGoalState(ref DefaultState Dstate, ref DefaultState DidealGoalState)
	{	
		//float startTime = Time.realtimeSinceStartup;
		
		GridPlanningState state = Dstate as GridPlanningState;
		GridPlanningState idealGoalState = DidealGoalState as GridPlanningState;
		
		if (state == null)
		{
			FootstepPlanningState fsState = Dstate as FootstepPlanningState;
			if (fsState != null)
				state = new GridPlanningState(fsState);
			else
			{
				GridTimeState gridTimeState = Dstate as GridTimeState;
				state = new GridPlanningState(gridTimeState);
			}
		}
		
		Vector3 toGoal;
		if (idealGoalState != null)
			// A state is a goal one if it's really close to the goal
		 	toGoal = idealGoalState.currentPosition - state.currentPosition;	
		else
		{
			FootstepPlanningState fsIdealGoalState = DidealGoalState as FootstepPlanningState;	
			if (fsIdealGoalState != null)
				toGoal = fsIdealGoalState.currentPosition - state.currentPosition;				
			else
			{
				GridTimeState gtIdealGoalState = DidealGoalState as GridTimeState;
				toGoal = gtIdealGoalState.currentPosition - state.currentPosition;
			}
		}
		
		//bool b = toGoal.magnitude/analyzer.maxStepSize < 0.5;
		bool b = toGoal.magnitude/2.0 < 0.5;
		
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
				
		return b;
	}
	
	override public float estimateTotalCost(ref DefaultState DcurrentState, ref DefaultState DidealGoalState, float currentg)
	{
		//float startTime = Time.realtimeSinceStartup;
		
		GridPlanningState currentState = DcurrentState as GridPlanningState;
		GridPlanningState idealGoalState = DidealGoalState as GridPlanningState;
		
		if (currentState == null)
		{
			FootstepPlanningState fsState = DcurrentState as FootstepPlanningState;
			currentState = new GridPlanningState(fsState);
		}
		
		//Debug.Log("Estimating cost");
		
		Vector3 toGoal;
		if (idealGoalState != null)
			// A state is a goal one if it's really close to the goal
		 	toGoal = idealGoalState.currentPosition - currentState.currentPosition;	
		else
		{
			FootstepPlanningState fsIdealGoalState = DidealGoalState as FootstepPlanningState;	
			toGoal = fsIdealGoalState.currentPosition - currentState.currentPosition;				
		}
		
		float h = toGoal.magnitude/analyzer.meanStepSize;
						
		float f = currentg + W*h;		
		
		//Debug.Log("Estimated cost = " + f);
		
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
		
		return f;
		
	}
	
	public override float ComputeEstimate (ref DefaultState Dfrom, ref DefaultState Dto, string estimateType)
	{			
		GridPlanningState _from = Dfrom as GridPlanningState;
		GridPlanningState _to = Dto as GridPlanningState;
		
		
		if (_from == null)
		{
			FootstepPlanningState fsState = Dfrom as FootstepPlanningState;
			_from = new GridPlanningState(fsState);
		}
		
		if(estimateType == "g")
			return (Mathf.Abs(Vector3.Distance(_to.currentPosition, _from.currentPosition)));
		else if(estimateType == "h")
		{
			
			Vector3 fromStart;
			if (_to != null)
				// A state is a goal one if it's really close to the goal
			 	fromStart = _to.currentPosition - _from.currentPosition;	
			else
			{
				FootstepPlanningState fsIdealGoalState = Dto as FootstepPlanningState;	
				fromStart = fsIdealGoalState.currentPosition - _from.currentPosition;				
			}
			
			float spaceCost = fromStart.magnitude/analyzer.meanStepSize;
			return spaceCost;
		}
		else
			return 0.0f;
	}
	
	public override float evaluateDomain (ref DefaultState state)
	{
		return 1;
	}
	
	override public void generateTransitions(ref DefaultState DcurrentState, ref DefaultState DpreviousState,
	                                ref DefaultState DidealGoalState, ref List<DefaultAction> transitions)
	{
		GridPlanningState currentState = DcurrentState as GridPlanningState;
		GridPlanningState idealGoalState = DidealGoalState as GridPlanningState;
		
		if (currentState == null)
		{
			FootstepPlanningState fsState = DcurrentState as FootstepPlanningState;
			if (fsState != null)
				currentState = new GridPlanningState(fsState);
			else
				currentState = new GridPlanningState(DcurrentState as GridTimeState);			
		}
		
		
		if (idealGoalState == null)
		{
			FootstepPlanningState fsIdealGoalState = DidealGoalState as FootstepPlanningState;
			if (fsIdealGoalState != null)
				idealGoalState = new GridPlanningState(fsIdealGoalState);
			else		
				idealGoalState = new GridPlanningState(DidealGoalState as GridTimeState);
		}
			
		foreach ( Vector3 mov in movDirections )
		{
			GridPlanningAction newAction = new GridPlanningAction(currentState,mov);
			
			//if (!CheckTransitionCollisions(newAction.state,DcurrentState))
			if (!CheckStateCollisions(newAction.state))
			{
				//Debug.Log(Time.time + ": grid successor generated");
				transitions.Add(newAction);
			}
		}
		
		//Debug.Log(transitions.Count + " grid transitions generated at time " + Time.time);
		
	}
	
	
	public override void generatePredecesors (ref DefaultState DcurrentState, ref DefaultState DpreviousState, 
	                                          ref DefaultState DidealGoalState, ref List<DefaultAction> transitions)
	{
		
		GridPlanningState currentState = DcurrentState as GridPlanningState;
		GridPlanningState idealGoalState = DidealGoalState as GridPlanningState;
		GridPlanningState previousState = DpreviousState as GridPlanningState;
		
		foreach ( Vector3 mov in movDirections )
		{
			GridPlanningAction newAction = new GridPlanningAction(previousState,mov,0.0f);
			
			if (!CheckStateCollisions(newAction.state))
				transitions.Add(newAction);			
		}
		
		
	}

	override public bool CheckStateCollisions(DefaultState Dstate)
	{
		//float startTime = Time.realtimeSinceStartup;
		
		GridPlanningState state = Dstate as GridPlanningState;
		
		bool b = false;
		
		// If we are checking a state of this domain
		if (state != null)		
			b = CheckRootCollisions(state);//return CheckRootCollisions(state);		
		else // If not
		{
			// We need to transform the state of the coarser domain 
			// to our low resolution domain
			FootstepPlanningState footstepState = Dstate as FootstepPlanningState;
			if (footstepState != null)
			{
				state = new GridPlanningState(footstepState);
				b = CheckRootCollisions(state); //return CheckRootCollisions(state);
			}
			else
				b = false;//return false;
		}
		
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
		
		return b;
	}
	
	
	
	// Collision check for the lower resolutions model
	public bool CheckRootCollisions(GridPlanningState state)
	{			
		Vector3 start = state.currentPosition - new Vector3(0,10*analyzer.GetHeight()/2,0);
		Vector3 end = start + new Vector3(0,10*analyzer.GetHeight()/2,0);
		float radius = analyzer.GetRadius();
		
		if (Physics.CheckCapsule(start,end,radius,layer))
		//if (Physics.CheckSphere(state.currentPosition,radius,layer))
			return true;		                         
		
		
		if (state.previousState != null)
		{
			int samples = analyzer.samples;
			Vector3 mov = state.actionMov;
			
			for (int i=1; i<samples-1; i+=2) // we sample less in this domain the collision check
			{			
				start = (state.previousState as GridPlanningState).currentPosition + i/(samples-1)*mov;
				end = start + new Vector3(0,analyzer.GetHeight()/2,0);
				
				if (Physics.CheckCapsule(start,end,radius,layer))
				//if (Physics.CheckSphere(state.previousState.currentPosition,radius,layer))
			    return true;		                         		
			}
		}
		
		
		return false;
	}
	
	
		
}