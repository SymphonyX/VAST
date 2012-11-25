using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// class representing the footstep domain

class FootstepPlanningDomain : PlanningDomainBase
{
	private static float MAX_HUMAN_SPEED = 5.0f	;
	
	// The Domain needs to have access to the AnimationAnalyzer that has all the available
	// anotated animations, in order to generate the possible transitions
	private AnimationAnalyzer analyzer;
	
	private NeighbourAgents neighborhood;
	private NeighbourObstacles obstacles;
	
	public float W = 1.5f;
	
	public float window = 1.0f;	
	
	private int layer = (1 << LayerMask.NameToLayer("DeterministicObstacles"))
						| (1 << LayerMask.NameToLayer("Obstacles"))
						| (1 << LayerMask.NameToLayer("StaticWorld"));	
	
	public FootstepPlanningDomain(AnimationAnalyzer animAnalyzer, NeighbourAgents n, NeighbourObstacles o)
	{
		analyzer = animAnalyzer;	
		neighborhood = n;
		obstacles = o;
	}
	
	//public static float timeSum = 0.0f;
	//public static int numCalls = 0;
	
	public override bool equals (DefaultState s1, DefaultState s2, bool isStart)
	{
		//float startTime = Time.realtimeSinceStartup;
		
		bool b = false;
		
		FootstepPlanningState state1 = s1 as FootstepPlanningState;
		FootstepPlanningState state2 = s2 as FootstepPlanningState;
		
		if(isStart)
		{
			if((Mathf.Abs(state1.currentPosition.x - state2.currentPosition.x) < .5) &&
				(Mathf.Abs(state1.currentPosition.y - state2.currentPosition.y) < .5) &&
			   (Mathf.Abs(state1.currentPosition.z - state2.currentPosition.z) < .5))
				b = true; //return true;
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
				b = false; //return false;
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
		
		FootstepPlanningState state = Dstate as FootstepPlanningState;
		FootstepPlanningState idealGoalState = DidealGoalState as FootstepPlanningState;
		
		// A state is a goal one if it's really close to the goal
		Vector3 toGoal = idealGoalState.currentPosition - state.currentPosition;		
		
		//Debug.Log("distance to goal = " + toGoal.magnitude);

		float timeWindow = window*analyzer.maxActionDuration;
		
		bool inTime = (state.time <= (idealGoalState.time + timeWindow) ) 
			//&& (state.time >= (idealGoalState.time - timeWindow) )
			;
		//if (inTime)
		//	Debug.Log("Max action duration = " + analyzer.maxActionDuration + " and " + state.time + " -> In time!");

		/*
		if
		(
		        (toGoal.magnitude/analyzer.maxStepSize < 0.5) 
		        && inTime
				)
				Debug.Log("GoalState found");
		*/
		
		bool b = (
		        (toGoal.magnitude/analyzer.maxStepSize < 0.5) 
		        && inTime
				);
		
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
		
		FootstepPlanningState currentState = DcurrentState as FootstepPlanningState;
		FootstepPlanningState idealGoalState = DidealGoalState as FootstepPlanningState;
		
		//Debug.Log("Estimating cost");
		
		// For now we are just estimating the cost as the distance between states;
		Vector3 toGoal = idealGoalState.currentPosition - currentState.currentPosition;
		
		/*
		float spaceCost = toGoal.magnitude / analyzer.meanStepSize;
		float timeCost = Int32.MaxValue;
		if (idealGoalState.time - currentState.time > 0)
			timeCost = toGoal.magnitude / (idealGoalState.time - currentState.time);		
		
		float angle = -currentState.currentRotation.eulerAngles.y;
		Vector3 orientation = new Vector3(Mathf.Cos(angle*Mathf.Deg2Rad),0,Mathf.Sin(angle*Mathf.Deg2Rad));
		toGoal.y = 0;
		float toGoalAngle = Vector3.Angle(orientation,toGoal);
		
		//Debug.Log("ToGoalAngle = " + toGoalAngle);
		
		float angleCost = Mathf.Abs(toGoalAngle) / 180;
		
		//float h = timeCost;
		//float h = spaceCost;
		float h = spaceCost + timeCost;
		//float h = spaceCost + timeCost + angleCost;
		*/
		
						
		float h = 2 * analyzer.mass * toGoal.magnitude * Mathf.Sqrt(FootstepPlanningAction.e_s*FootstepPlanningAction.e_w); 
						
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
		FootstepPlanningState _from = Dfrom as FootstepPlanningState;
		FootstepPlanningState _to = Dto as FootstepPlanningState;
		
		if(estimateType == "g")
		{
			Vector3 toGoal = _to.currentPosition - _from.currentPosition;
			float spaceCost = toGoal.magnitude / analyzer.meanStepSize;
			float timeCost = Int32.MaxValue;
			if(_to.time - _from.time > 0)
				timeCost = toGoal.magnitude / (_to.time - _from.time);
			
			return (Mathf.Abs(Vector3.Distance(_to.currentPosition, _from.currentPosition)));
			//return (spaceCost + timeCost);
			//return spaceCost;
		}
		else if(estimateType == "h")
		{
			Vector3 fromStart = _to.currentPosition - _from.currentPosition;
			float spaceCost = fromStart.magnitude / analyzer.meanStepSize;
			float timeCost = Int32.MaxValue;
			if(_to.time - _from.time > 0)
				timeCost = fromStart.magnitude / (_to.time - _from.time);
			
			//return (Mathf.Abs(Vector3.Distance(_to.currentPosition, _from.currentPosition)));
			return (spaceCost + timeCost);
			//return spaceCost;
		}
		else
			return 0.0f;
	}
	
	public override float evaluateDomain (ref DefaultState state)
	{		
		return 3;
	}
	
	override public void generateTransitions(ref DefaultState DcurrentState, ref DefaultState DpreviousState,
	                                ref DefaultState DidealGoalState, ref List<DefaultAction> transitions)
	{
		FootstepPlanningState currentState = DcurrentState as FootstepPlanningState;
//		FootstepPlanningState previousState = DpreviousState as FootstepPlanningState;
		FootstepPlanningState idealGoalState = DidealGoalState as FootstepPlanningState;
		
		float timeLeft = idealGoalState.time - currentState.time;
		float timeWindow = window*analyzer.maxActionDuration;
		
		// If there is no time left
		if ( timeLeft + timeWindow < 0 )
		{
			//We don't generate transitions
			return;
		}
		
		//Debug.Log("Calling my generation");
		
		// The next action preconditions
		AnotatedAnimation preconditions = currentState.preconditions;				
		
		float meanStepSize = analyzer.meanStepSize;
		float mass = analyzer.mass;
		
		// The added transitions
		//int count = 0;
		
		/*
		//string[] names = {"WalkRightSlow","WalkRightSlow2","WalkNormal","WalkNormal2","WalkFast","WalkFast2","WalkLeft","WalkLeft2","WalkRight","WalkRight2","LeftStep","RightStep"};
		string[] names = {
			"WalkFast","WalkFast2","WalkNormal","WalkNormal2","WalkLeft","WalkLeft2","WalkRight","WalkRight2","LeftStep"
			,"RightStep","WalkLeftSlow","WalkLeftSlow2","WalkRightSlow","WalkRightSlow2"
			,"RightStepSlow","LeftStepSlow","WalkSlow","WalkSlow2"
			,"TurnRight360","TurnLeft360"
			,"WalkTurnLeft","WalkTurnLeft2"
			"WalkTurnRight","WalkTurnRight2"
			,"WalkSlowest","WalkSlowest2"
			,"Idle"
		};
		
		AnotatedAnimation[] anims = new AnotatedAnimation[names.Length];
		int aux=0;
		foreach (string name in names)
		{
			anims[aux] = analyzer.GetAnotatedAnimation(name);
			aux++;
		}
		*/
		
		// For each possible animation
		foreach( AnotatedAnimation anim in analyzer.analyzedAnimations.Values)
		//foreach (AnotatedAnimation anim in anims)
		{							
			// We compute the min and max possible speed of the animation
			// (depending on the previous state, the current state and the velocity of the new animation)
			float minAnimSpeed = 0.0f;
			float maxAnimSpeed = 1.0f;
			//ComputeMinAndMaxAnimSpeeds(currentState,previousState,anim.speed,ref minAnimSpeed,ref maxAnimSpeed);
			float animSpeedIncr = 0.1f;
			
			//minAnimSpeed = 0.2f; maxAnimSpeed = 1.0f; animSpeedIncr = 0.2f;
			minAnimSpeed = 1.0f; maxAnimSpeed = 1.0f; 
			//minAnimSpeed = 0.5f; maxAnimSpeed = 0.5f; 
			
			//int animCount = 0;
			
			//Debug.Log("Transitions: currentState.time = "+currentState.time+" realTime = "+Time.realtimeSinceStartup);
			
			// we check the preconditions of that action
			FootstepPlanningAction newAction = new FootstepPlanningAction(currentState,anim,minAnimSpeed,meanStepSize,mass);	
			if (newAction.state != null)
			{				
				if ( newAction.SatisfiesPreconditions(preconditions) ) 
				{					
					
					if (!CheckStateCollisions(newAction.state))
						transitions.Add(newAction);
					//count++;	
					//animCount++;
										
					// and we generate the other actions using the same animation but at different speeds
					for (float animSpeed = minAnimSpeed + animSpeedIncr; animSpeed <= maxAnimSpeed; animSpeed += animSpeedIncr)
					{
						newAction = new FootstepPlanningAction(currentState,anim,animSpeed,meanStepSize,mass);
						//transitions.Insert(count,newAction);
						
						if (!CheckStateCollisions(newAction.state))
							transitions.Add(newAction);
						//count++;
						
						//animCount++;
					}
									
				}
			}
			//Debug.Log("Animation " + anim.name + " transitions = " + animCount);
			
			//if (transitions.Count == 0)
			//	Debug.Log("No transitions!");
			
		}
	}
	
	
	public override void generatePredecesors (ref DefaultState DcurrentState, ref DefaultState DpreviousState, 
	                                          ref DefaultState DidealGoalState, ref List<DefaultAction> transitions)
	{
		
		FootstepPlanningState currentState =  DcurrentState as FootstepPlanningState;
		FootstepPlanningState idealGoalState = DidealGoalState as FootstepPlanningState;
		FootstepPlanningState previousState = DpreviousState as FootstepPlanningState;
		
		float timeLeft = idealGoalState.time - currentState.time;
		float timeWindow = window*analyzer.maxActionDuration;
		
		// If there is no time left
		if ( timeLeft + timeWindow < 0 )
		{
			//We don't generate transitions
			return;
		}
		
		AnotatedAnimation preconditions = currentState.preconditions;				
		
		float meanStepSize = analyzer.meanStepSize;
		
		foreach( AnotatedAnimation anim in analyzer.analyzedAnimations.Values)
		{
			float minAnimSpeed = 0.0f;
			float maxAnimSpeed = 1.0f;
			
			float animSpeedIncr = 0.1f;
			
			minAnimSpeed = 1.0f; maxAnimSpeed = 1.0f;
			
			FootstepPlanningAction newAction = new FootstepPlanningAction(previousState,anim,minAnimSpeed,meanStepSize, 0.0f);	
			if (newAction.state != null)
			{				
				if ( newAction.SatisfiesPreconditions(preconditions) ) 
				{					
					
					if (!CheckStateCollisions(newAction.state))
						transitions.Add(newAction);
										
					// and we generate the other actions using the same animation but at different speeds
					for (float animSpeed = minAnimSpeed + animSpeedIncr; animSpeed <= maxAnimSpeed; animSpeed += animSpeedIncr)
					{
						newAction = new FootstepPlanningAction(previousState,anim,animSpeed,meanStepSize, 0.0f);
						
						if (!CheckStateCollisions(newAction.state))
							transitions.Add(newAction);
						
					}
									
				}
			}
		}
		
		
	}
		
	// computes the min and max animatino speeds of an action that gives the character a speed of actionSpeed
	public void ComputeMinAndMaxAnimSpeeds(FootstepPlanningState currentState, FootstepPlanningState previousState,
			                                       float actionSpeed, ref float minAnimSpeed, ref float maxAnimSpeed)
	{
		if (actionSpeed == 0.0f)
		{
			minAnimSpeed = 1.0f;
			maxAnimSpeed = 1.0f;
			
			return;
		}
		else
		{
			float previousAcceleration = previousState.currentAcceleration;
			float currentAcceleration = currentState.currentAcceleration;
			
			float minAcceleration = 0.0f;
			float maxAcceleration = 0.0f;
			
			bool accelerating = currentAcceleration >= previousAcceleration;		
				
			// TODO: revise this part of the code
			if (accelerating)
			{
				minAcceleration = currentAcceleration - 0.2f;
				maxAcceleration = currentAcceleration + 0.4f;
			}
			else
			{
				minAcceleration = currentAcceleration - 0.4f;
				maxAcceleration = currentAcceleration + 0.2f;
			}
			
			float currentSpeed = currentState.currentSpeed;
			float minSpeed = currentSpeed + minAcceleration;
			float maxSpeed = currentSpeed + maxAcceleration;
			
			if ( minSpeed < 0 ) minSpeed = 0.01f;
			if ( maxSpeed > MAX_HUMAN_SPEED ) maxSpeed = MAX_HUMAN_SPEED;
			
			minAnimSpeed = minSpeed / actionSpeed;
			
			maxAnimSpeed = maxSpeed / actionSpeed;
			if (maxAnimSpeed > 1) maxAnimSpeed = 1;
		}
	}
	
	override public bool CheckStateCollisions(DefaultState Dstate)
	{
		//float startTime = Time.realtimeSinceStartup;
		
		FootstepPlanningState state = Dstate as FootstepPlanningState;
		
		//return CheckRootCollisions(state) || CheckAgentsCollisions(state);
		//return CheckJointsCollisions(state) || CheckAgentsCollisions(state);
		bool b =  (CheckJointsCollisions(state) || CheckAgentsCollisions(state) || CheckDynamicObstaclesCollisions(state));		
	
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
	
	// Collisions check with deterministic dynamic obstacles
	public bool CheckDynamicObstaclesCollisions(FootstepPlanningState state)
	{
		if (obstacles == null)
			return false;
		
		if (obstacles.closeObstacles == null)
			return false;
		
		List<DDObstacle> visibleObstacles = obstacles.visibleObstacles;
		
		if (visibleObstacles.Count <= 0)
			return false;
		
		//float[] actualTime = new float[visibleObstacles.Count];
		
		// Move obstacles
		//int i = 0;
		foreach(DDObstacle ddObstacle in visibleObstacles)
		{
			ObstaclesScript obstacleScript = ddObstacle.script;
			
			float timeWindow = 0;
			//for (float timeWindow = -0.5f; timeWindow <= 0.5f; timeWindow += 0.5f)
			{
				if (obstacleScript != null)					
				{
					//actualTime[i] = obstacleScript.animation["test"].time;
					float actualTime = obstacleScript.animation["test"].time;
					
					float time = state.time + timeWindow;
					
					obstacleScript.animation["test"].wrapMode = WrapMode.PingPong;
					obstacleScript.animation["test"].enabled = true;
					obstacleScript.animation["test"].time = time;					
					obstacleScript.animation.Play("test");
					obstacleScript.animation.Sample();						
					//obstacleScript.animation["test"].enabled = false;
					
					//Debug.DrawRay(pair.Value.obstacle.transform.position,4*Vector3.up,Color.blue);
						
					state.obstaclePos = ddObstacle.obstacle.transform.position;
			
					// Check collisions
					//bool collision = CheckJointsCollisions(state);
					//bool collision = CheckRootCollisions(state);		
					//Vector3 aux = state.obstaclePos - state.currentPosition;
					//bool collision = (aux.magnitude - 1 - analyzer.GetRadius()) < 0;
						
					bool collision = CheckObstacleCollision(state,obstacleScript);
		
					//if (collision)
					{
						//Debug.DrawRay(state.currentPosition,4*Vector3.up,Color.yellow);
						
						//Debug.Log("Possible collision detected at" + state.time);
						//Debug.Break ();
					}		
					
					obstacleScript.animation["test"].enabled = true;
					//obstacleScript.animation["test"].time = actualTime[i];					
					obstacleScript.animation["test"].time = actualTime;					
					obstacleScript.animation.Play("test");
					obstacleScript.animation.Sample();									
					
					if (collision)
						return true;
				}				
			}
		}
		
		return false;
	}
	
	public bool CheckObstacleCollision(FootstepPlanningState state, ObstaclesScript script)
	{
		//if (state == null || script == null)
		//	return false;
						
		if (script.ShapeChoice.Shape == Enum.Enumeration.Sphere)
		{
			Vector3 obstaclePos = script.gameObj.transform.position;
			Vector3 aux = obstaclePos - state.currentPosition;
			
			float sphereRadius = script.size.x;
			
		 	return (aux.magnitude - sphereRadius - analyzer.GetRadius()) <= 0;				
		}
		else if (script.ShapeChoice.Shape == Enum.Enumeration.Rectangle)
		{
			Bounds box = script.gameObj.collider.bounds;
			
			return box.SqrDistance(state.currentPosition) <= analyzer.GetRadius();
		}
				
		return false;		
	}
	
	
	
	// Collisions check with other agents
	public bool CheckAgentsCollisions(FootstepPlanningState state)
	{	
		if (neighborhood == null)
			return false;
		
		if (neighborhood.neighbors == null)		
			return false;
		
		Dictionary<int,Neighbor> neighbors = neighborhood.neighbors;
		
		if  (neighbors.Count <= 0)		
			return false;
		
		//if (neighbors.Count > 0)		
		//	Debug.Log("There are neighbors");
		//else
		//	Debug.Log("There are no neighbors");
		
		foreach(KeyValuePair<int,Neighbor> pair in neighbors)
		{
			if (pair.Value.planning != null && pair.Value.triggers == 2)
			{
				FootstepPlanningAction[] plan = pair.Value.planning.GetOutputPlan();			
	
				if (plan.Length > 0 && plan[0] != null)			
				{
					int i = 0;
					FootstepPlanningAction action = plan[i];															
						
					FootstepPlanningState prevNeighborState = action.state as FootstepPlanningState;	
					while (prevNeighborState == null && i < plan.Length)
					{
						action = plan[i];
						prevNeighborState = action.state as FootstepPlanningState;	
						i++;
					}					
						
					bool thereIsTime = true;
					while(i < plan.Length && plan[i] != null && action.state != null && thereIsTime)
					{
						FootstepPlanningState footstepState = action.state as FootstepPlanningState;						
						if ( footstepState != null && footstepState.time < state.time)
						{						
							action = plan[i];						
							i++;
						}
						else
							thereIsTime = false;
					}
					
					FootstepPlanningState neighborState = prevNeighborState;					
					if (action.state != null)					
					 	neighborState = action.state as FootstepPlanningState;					
					
					if (state != null && prevNeighborState != null && neighborState != null)
						return CheckAgentRootCollisions(state, prevNeighborState, neighborState);										
				}
				else
				{
					FootstepPlanningState neighborState = pair.Value.planning.currentState;
					
					return CheckAgentRootCollisions(state,null,neighborState);
				}
				
			}
		}
		
		return false;
		
	}
	
	public bool CheckAgentRootCollisions(FootstepPlanningState state, 
	             FootstepPlanningState prevNeighborState, FootstepPlanningState neighborState)
	{
		if (neighborState == null)
			return false;
		
		Vector3 neighborPosition = neighborState.currentPosition;
		
		if (prevNeighborState != null)
		{
			float stateTime = state.time;
			float interval = neighborState.time - prevNeighborState.time;		
			float a = 0;
			if (interval > 0)
				a = (stateTime - prevNeighborState.time)/interval;
			//float b = (neighborState.time - stateTime)/interval;
			
			Vector3 neighborTrajectory = neighborState.currentPosition - prevNeighborState.currentPosition;
			
			neighborPosition = prevNeighborState.currentPosition + a*(neighborTrajectory);
		}
		
		float distance = (state.currentPosition - neighborPosition).magnitude;
		
		//float distance = (state.currentPosition - prevNeighborState.currentPosition).magnitude;
		
		//Debug.Log("Distance = " + (distance - 4*analyzer.GetRadius()));
		
		//if (distance - 4*analyzer.GetRadius() < 0)
			//Debug.Log("Possible collision detected");
		
		return distance - 2*analyzer.GetRadius() < 0;		
	}
	
	// Collision check for the lower resolutions model
	public bool CheckRootCollisions(FootstepPlanningState state)
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
			AnotatedAnimation anim = analyzer.GetAnotatedAnimation(state.actionName);
			
			for (int i=1; i<samples-1; i++)
			{			
				start = state.previousState.currentPosition + anim.Root[i].position;
				end = start + new Vector3(0,analyzer.GetHeight()/2,0);
				
				if (Physics.CheckCapsule(start,end,radius,layer))
				//if (Physics.CheckSphere(state.previousState.currentPosition,radius,layer))
			    return true;		                         		
			}
		}
		
		
		return false;
	}
	
	// Collision check for the coarser resolution model
	public bool CheckJointsCollisions(FootstepPlanningState state)
	{		
		// Check right foot collisions
		//Debug.Log("RightFoot.y = " + state.rightFoot.y);	
		float radius = analyzer.footRadius;		
		Vector3 start = state.rightFoot - new Vector3(0,2*radius,0);
		Vector3 end = start + new Vector3(0,analyzer.GetHeight(),0);		
		if (Physics.CheckCapsule(start,end,radius,layer))
		//if (Physics.CheckCapsule(start,end,radius))
		//if (Physics.CheckSphere(start,radius))
		{			
			/*
			Vector3 direction = end - start;
			RaycastHit[] hits = Physics.CapsuleCastAll(start,end,radius,direction);	
					
			for (int i=0; i<hits.Length; i++)
			{
				Debug.Log("Hit " +i+ ".x: " + hits[i].point.x);
				Debug.Log("Hit " +i+ ".y: " + hits[i].point.y);
				Debug.Log("Hit " +i+ ".z: " + hits[i].point.z);
			}
			*/
			
			return true;		                         
		}
		
		
		// Check left foot collisions
		start = state.leftFoot - new Vector3(0,2*radius,0);
		end = start + new Vector3(0,analyzer.GetHeight(),0);		
		if (Physics.CheckCapsule(start,end,radius,layer))
		    return true;		                         
		
		
		
		// Check head collisions
		start = state.currentPosition;// - new Vector3(0,analyzer.GetHeight()/2,0);
		end = start + new Vector3(0,analyzer.GetHeight(),0);///2,0);
		radius = analyzer.headRadius;		
		if (Physics.CheckCapsule(start,end,radius,layer))
		    return true;		                         
		                         
		// Check left hand collision
		start = state.leftHand;
		radius = analyzer.handRadius;
		if (Physics.CheckSphere(start,radius,layer))
		    return true;
		    
		// Check right hand collision
		start = state.rightHand;
		if (Physics.CheckSphere(start,radius,layer))
		    return true;    
		
		
		if (state.previousState != null)
		{
			int samples = analyzer.samples;
			AnotatedAnimation anim = analyzer.GetAnotatedAnimation(state.actionName);
			
			for (int i=1; i<samples-1; i++)
			{			
				// Check right foot collisions
				//Debug.Log("RightFoot.y = " + state.rightFoot.y);	
				radius = analyzer.footRadius;		
				start = state.previousState.rightFoot + anim.RightFoot[i].position - new Vector3(0,2*radius,0);
				end = start + new Vector3(0,analyzer.GetHeight(),0);		
				if (Physics.CheckCapsule(start,end,radius,layer))
					return true;		                         
								
				// Check left foot collisions
				start = state.previousState.leftFoot + anim.LeftFoot[i].position - new Vector3(0,2*radius,0);
				end = start + new Vector3(0,analyzer.GetHeight(),0);		
				if (Physics.CheckCapsule(start,end,radius,layer))
				    return true;		                         
								
				
				// Check head collisions
				start = state.previousState.currentPosition + anim.Root[i].position;// - new Vector3(0,analyzer.GetHeight()/2,0);
				end = start + new Vector3(0,analyzer.GetHeight(),0);///2,0);
				radius = analyzer.headRadius;		
				if (Physics.CheckCapsule(start,end,radius,layer))
				    return true;		                         
				                         
				// Check left hand collision
				start = state.previousState.leftHand + anim.LeftHand[i];
				radius = analyzer.handRadius;
				if (Physics.CheckSphere(start,radius,layer))
				    return true;
				    
				// Check right hand collision
				start = state.previousState.rightHand + anim.RightHand[i];
				if (Physics.CheckSphere(start,radius,layer))
				    return true;
				
				
				
				
				
				
				
			}
		}
		
		
		return false;			
	}	
}