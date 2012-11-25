//#define USE_ANGLE_MOV

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GridTimeDomain : PlanningDomainBase
{
	// constants 
	
	public static float DESIRED_SPEED = 1.5f;
	public static float DESIRED_SPEED_SQUARE = DESIRED_SPEED * DESIRED_SPEED;
	public static float MAX_ACCELERATION = 1.5f; // this constant is effectively not needed (only used in calculatePe where it cancels out 
	
	public static float SPEED_DELTA = 0.5f;
	public static float DELTA_TIME = 0.5f;
	public static float MAX_SPEED = 4.5f;
	public static float MID_SPEED = 1.5f;
	public static float MIN_SPEED = 0.1f;
		
	public bool useTimeConstraint = true;
	
	public float W = 1.5f;
	
	public float window = 1.0f;		
	
	//private AnimationAnalyzer analyzer;
	
	private NeighbourAgents neighborhood;
	private NeighbourObstacles obstacles;
	
	// MUBBASIR CHANGE -- SHOULD ROOT COLLISION HAVE TO CHECK DETERMINISTIC OBSTACLES ??? 
	private int layer = (1 << LayerMask.NameToLayer("Obstacles"))
						| (1 << LayerMask.NameToLayer("StaticWorld"));	

	/*
	private int layer = (1 << LayerMask.NameToLayer("DeterministicObstacles"))
						| (1 << LayerMask.NameToLayer("Obstacles"))
						| (1 << LayerMask.NameToLayer("StaticWorld"));	
	*/
	
	private List<float> possibleSpeedIncrements;

#if USE_ANGLE_MOV
	private List<float> movAngles;
	public List<float> getMovAngles(){return movAngles;}
#else	
	private List<Vector3> movDirections;		
	public List<Vector3> getMovDirections(){ return movDirections; }
#endif
	
	private Tunnel tunnel;
	bool useTunnel;
		
	public float timeWindow = 2.0f;
	public float agentRadius = 0.5f; //0.5f;
	public float maxStepSize = 2.0f;
	public float meanStepSize = 1.0f;
	public float agentHeight = 0.23f;
	public int numSamples = 3;
	
	
	
	//public GridTimeDomain(AnimationAnalyzer animAnalyzer,  NeighbourObstacles o)
	public GridTimeDomain(NeighbourObstacles o, NeighbourAgents n)
	{
		//analyzer = animAnalyzer;
		
		neighborhood = n;
		obstacles = o;
		
#if USE_ANGLE_MOV

		movAngles = new List<float>();
		movAngles.Add(0.0f);
		movAngles.Add(22.5f);
		movAngles.Add(-22.5f);
		movAngles.Add(45.0f);
		movAngles.Add(-45.0f);
		movAngles.Add(180.0f);
		movAngles.Add(157.5f);
		movAngles.Add(-157.5f);

#else	
		
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
		
		movDirections.Add(dir1);
		movDirections.Add(dir2);
		movDirections.Add(dir3);
		movDirections.Add(dir4);		
		
		movDirections.Add(dir5);
		movDirections.Add(dir6);
		movDirections.Add(dir7);
		movDirections.Add(dir8);
				
#endif		
		possibleSpeedIncrements = new List<float>();
		
		possibleSpeedIncrements.Add(0.0f);
		possibleSpeedIncrements.Add(SPEED_DELTA);
		possibleSpeedIncrements.Add(-SPEED_DELTA);
		
		useTunnel = false;
	}
	
	public void UseTunnelSearch(Tunnel t)
	{
		tunnel = t;	
		useTunnel = true;
	}
	
	public void DisableTunnelSearch()
	{
		useTunnel = false;
	}
	
	public override DefaultAction generateAction (DefaultState previousState, DefaultState nextState )
	{
		return new GridTimeAction(previousState as GridTimeState,nextState as GridTimeState);
	}
	
	//public static float timeSum = 0.0f;
	//public static int numCalls = 0;
	
	public override bool equals (DefaultState s1, DefaultState s2, bool isStart)
	{
		//float startTime = Time.realtimeSinceStartup;
		
		//Debug.Log("hi, " +Time.time);
		
		bool b = false;
		
		GridTimeState state1 = s1 as GridTimeState;
		GridTimeState state2 = s2 as GridTimeState;
		
		// MUBBASIR TODO : WHY DONT U CONSIDER TIME HERE ?? 
		
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
	
	
	// this function is used by the best first search  -- prob not by the ara star
	override public bool isAGoalState(ref DefaultState Dstate, ref DefaultState DidealGoalState)
	{	
		//float startTime = Time.realtimeSinceStartup;
		
		GridTimeState state = Dstate as GridTimeState;
		GridTimeState idealGoalState = DidealGoalState as GridTimeState;
		
		/*
		if (state == null)
		{
			FootstepPlanningState fsState = Dstate as FootstepPlanningState;
			state = new GridTimeState(fsState);			
		}
		*/
		
		bool inTime = false;						
		
		Vector3 toGoal;
		
		//if (idealGoalState != null)
		{	// A state is a goal one if it's really close to the goal
		 	toGoal = idealGoalState.currentPosition - state.currentPosition;		
			inTime = (state.time <= (idealGoalState.time + timeWindow) ); 
		}
		/*
		else
		{
			FootstepPlanningState fsIdealGoalState = DidealGoalState as FootstepPlanningState;	
			toGoal = fsIdealGoalState.currentPosition - state.currentPosition;
			inTime = (state.time <= (fsIdealGoalState.time + timeWindow) ); 
		}
		*/
		//Debug.Log(Time.time + " distance to goal = " + toGoal.magnitude);
				
		
		bool b = (
		        (toGoal.magnitude/maxStepSize < 0.5) 
		        && (!useTimeConstraint || inTime)
				);
		
		// this is not being called --- check ara star planner -- it must use this -- and the grid domain should define this funciton 
				
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
	
	public override bool equals (float value1, float value2)
	{
		return false;
	}
	
	// used by ara star + used by evaluateTotalCost which is used by best first search 
	public override float ComputeHEstimate (DefaultState _from, DefaultState _to)
	{
		GridTimeState from = _from as GridTimeState;
		GridTimeState to = _to as GridTimeState;
		
		/*
		float distanceToGoal = Vector3.Distance(to.currentPosition, from.currentPosition);
		float timeToGoal = to.time - from.time;
		float acceleration = Mathf.Abs( 2 * ( distanceToGoal - from.speed * timeToGoal ) / (timeToGoal * timeToGoal)) ;
		float velocityRequiredToReachGoal = from.speed + acceleration * timeToGoal;
		
		float e1 = Mathf.Abs (from.speed * from.speed - velocityRequiredToReachGoal * velocityRequiredToReachGoal);
		if (velocityRequiredToReachGoal > GridTimeDomain.MAX_SPEED )
		{
			//Debug.Log ("e1 is infinity");
			e1 = Mathf.Infinity;
		}
		*/
		
		//float e2 = Mathf.Abs(GridTimeDomain.DESIRED_SPEED_SQUARE - velocityRequiredToReachGoal * velocityRequiredToReachGoal);
		//float e3 = distanceToGoal;
		
		
		Vector3 fromStart;
	 	fromStart = to.currentPosition - from.currentPosition;	
		float spaceCost = fromStart.magnitude/meanStepSize;
		//Debug.Log("old h = " + spaceCost + " HEURISTIC = " + (e1+e2+e3) + " e1 " + e1 + " e2 " + e2 + "e3 " + e3 );

		return spaceCost;
		//return (spaceCost + e1);
		//return e1+e2+e3;
		
	}
	
	// used by ara star only -- 
	public override float ComputeGEstimate (DefaultState _from, DefaultState _to)
	{
		//Debug.Log ("compute g estimate");
		GridTimeState from = _from as GridTimeState;
		GridTimeState to = _to as GridTimeState;
		return (Mathf.Abs(Vector3.Distance(to.currentPosition, from.currentPosition)));
	}
	
	
	override public float estimateTotalCost(ref DefaultState DcurrentState, ref DefaultState DidealGoalState, float currentg)
	{
		
		//Debug.Log ("estimate total cost" );
		
		//float startTime = Time.realtimeSinceStartup;
		
		GridTimeState currentState = DcurrentState as GridTimeState;
		GridTimeState idealGoalState = DidealGoalState as GridTimeState;
		
		/*
		if (currentState == null)
		{
			FootstepPlanningState fsState = DcurrentState as FootstepPlanningState;
			currentState = new GridTimeState(fsState);
		}
		*/
		
		//Debug.Log("Estimating cost");
		
		Vector3 toGoal;
		//if (idealGoalState != null)
			// A state is a goal one if it's really close to the goal
		 	toGoal = idealGoalState.currentPosition - currentState.currentPosition;	
		/*
		else
		{
			FootstepPlanningState fsIdealGoalState = DidealGoalState as FootstepPlanningState;	
			toGoal = fsIdealGoalState.currentPosition - currentState.currentPosition;				
		}
		*/
		
		float availableTime = idealGoalState.time - currentState.time;// + timeWindow;
		float necessarySpeed = toGoal.magnitude / availableTime;
		//float h = (currentState.speed - necessarySpeed)*(currentState.speed-necessarySpeed) + (necessarySpeed - 1.5f)*(necessarySpeed - 1.5f);
				
		// MUBBASIR CHANGING HEURISTIC ESTIMATE 
		//float h = toGoal.magnitude/meanStepSize;
						
		float h = ComputeHEstimate(currentState, idealGoalState);
		
		/*
		float timeCost = necessarySpeed - currentState.speed;
	
		bool negative = timeCost < 0;
		if (negative)
			timeCost = 0;
		else
			timeCost *= timeCost;
		
		float f = currentg + timeCost + W*h;		
		*/
		
		float f = currentg +  W*h;		
		
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
	
	// does not look like it is being used 
	public override float ComputeEstimate (ref DefaultState Dfrom, ref DefaultState Dto, string estimateType)
	{			
		GridTimeState _from = Dfrom as GridTimeState;
		GridTimeState _to = Dto as GridTimeState;
		
		/*
		if (_from == null)
		{
			FootstepPlanningState fsState = Dfrom as FootstepPlanningState;
			_from = new GridTimeState(fsState);
		}
		*/
		
		Debug.Log ("compute estimate " + estimateType);
		
		if(estimateType == "g")
			return (Mathf.Abs(Vector3.Distance(_to.currentPosition, _from.currentPosition)));
		else if(estimateType == "h")
		{
			
			Vector3 fromStart;
			//if (_to != null)
				// A state is a goal one if it's really close to the goal
			 	fromStart = _to.currentPosition - _from.currentPosition;	
			/*
			else
			{
				FootstepPlanningState fsIdealGoalState = Dto as FootstepPlanningState;	
				fromStart = fsIdealGoalState.currentPosition - _from.currentPosition;				
			}
			*/
			float spaceCost = fromStart.magnitude/meanStepSize;
			return spaceCost;
		}
		else
			return 0.0f;
	}
	
	public override float evaluateDomain (ref DefaultState state)
	{
		return 2;
	}
	
	public override void generatePredecessors(DefaultState DcurrentState, ref List<DefaultAction> actionList)
	{
		
		GridTimeState currentState = DcurrentState as GridTimeState;
		
		
		GridTimeAction stopAction = new GridTimeAction(currentState,Vector3.zero,0);
		if (!CheckTransitionCollisions(currentState, stopAction.state, false))
		{
			if (!useTunnel || isInsideTunnel(stopAction.state) )
				actionList.Add(stopAction);		
		}
		
#if USE_ANGLE_MOV
		foreach( float angle in movAngles)
		{
			Vector3 mov = currentState.actionMov;
			mov =  Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(angle,Vector3.up),new Vector3(1,1,1)) * mov;
			mov.Normalize();			
#else		
		foreach ( Vector3 mov in movDirections )				
		{	
#endif				
			foreach( float speedIncr in possibleSpeedIncrements )
			{			
				float newSpeed = currentState.speed + speedIncr;
				if (newSpeed > MAX_SPEED) 
					newSpeed = MAX_SPEED;
				else if (newSpeed < MIN_SPEED)
					newSpeed = MIN_SPEED;
				
				GridTimeState prevState = new  GridTimeState();
				prevState.ComputePreviousState(currentState,mov,newSpeed);
				GridTimeAction newAction = new GridTimeAction(currentState,prevState);
				
				
				if (!CheckTransitionCollisions(currentState, newAction.state))
				{
					// If we are not using a tunnel 
					// or if we are using one and the stata falls inside it
					if (!useTunnel || isInsideTunnel(newAction.state) )
					{
						//Debug.Log(Time.time + ": grid successor generated");
						actionList.Add(newAction); // we add the action as a transition
					}
				}
			}
		}
		
		//Debug.Log(transitions.Count + " grid transitions generated at time " + Time.time);
		
	}
	
	override public void generateTransitions(ref DefaultState DcurrentState, ref DefaultState DpreviousState,
	                                ref DefaultState DidealGoalState, ref List<DefaultAction> transitions)
	{
		GridTimeState currentState = DcurrentState as GridTimeState;
		GridTimeState idealGoalState = DidealGoalState as GridTimeState;
		
		/*
		if (currentState == null)
			currentState = new GridTimeState(DcurrentState as FootstepPlanningState);
		
		if (idealGoalState == null)
			idealGoalState = new GridTimeState(DidealGoalState as FootstepPlanningState);
		*/
		
		GridTimeAction stopAction = new GridTimeAction(currentState,Vector3.zero,0);
		if (useTimeConstraint)
		{
			GridTimeState stopState = stopAction.state as GridTimeState;
			Vector3 toGoal = idealGoalState.currentPosition - stopState.currentPosition;
			float availableTime = idealGoalState.time - stopState.time;// + timeWindow;
			float necessarySpeed = toGoal.magnitude / availableTime;
		
			float timeCost = necessarySpeed - stopState.speed;
	
			bool negative = timeCost < 0;
			if (negative)
				timeCost = 0;
			else
				timeCost *= timeCost;
				
			stopAction.cost += timeCost;			
		}
			
		if (!CheckTransitionCollisions(currentState, stopAction.state, false))
		{
			if (!useTunnel || isInsideTunnel(stopAction.state) )
				transitions.Add(stopAction);		
		}
				
#if USE_ANGLE_MOV
		foreach( float angle in movAngles)
		{
			Vector3 mov = currentState.actionMov;
			mov =  Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(angle,Vector3.up),new Vector3(1,1,1)) * mov;
			mov.Normalize();			
#else		
		foreach ( Vector3 mov in movDirections )				
		{	
#endif
			
			foreach( float speedIncr in possibleSpeedIncrements )
			{			
				float newSpeed = currentState.speed + speedIncr;
				if (newSpeed > MAX_SPEED) 
					newSpeed = MAX_SPEED;
				else if (newSpeed < MIN_SPEED)
					newSpeed = MIN_SPEED;
				
				GridTimeAction newAction = new GridTimeAction(currentState,mov,newSpeed);
				if (useTimeConstraint)
				{
					GridTimeState newState = newAction.state as GridTimeState;		
					Vector3 toGoal = idealGoalState.currentPosition - newState.currentPosition;
					float availableTime = idealGoalState.time - newState.time; // + timeWindow;
					float necessarySpeed = toGoal.magnitude / availableTime;
				
					float timeCost = necessarySpeed - newState.speed;
			
					bool negative = timeCost < 0;
					if (negative)
						timeCost = 0;
					else
						timeCost *= timeCost;
						
					newAction.cost += timeCost;			
				}
						
						
				if (!CheckTransitionCollisions(currentState, newAction.state))
				{
					// If we are not using a tunnel 
					// or if we are using one and the stata falls inside it
					if (!useTunnel || isInsideTunnel(newAction.state) )
					{
						//Debug.Log(Time.time + ": grid successor generated");
						transitions.Add(newAction); // we add the action as a transition
					}
				}
			}
		}
		
		//Debug.Log(transitions.Count + " grid transitions generated at time " + Time.time);
		
	}
	
	/*
	private bool isInsideTunnel(DefaultState state)
	{
		GridTimeState gridTimeState = state as GridTimeState;
		if (gridTimeState == null)
			return false;
		
		float x = gridTimeState.currentPosition.x;
		float z = gridTimeState.currentPosition.z;
		float t = gridTimeState.time;
		
		int i = 0;		
		while(i < tunnel.tunnelMidVelocity.Length)
		{
			GridTimeState tunnelState = tunnel.tunnelMidVelocity[i];
			if (tunnelState != null)
			{
				float tunnelX = tunnelState.currentPosition.x;
				float tunnelZ = tunnelState.currentPosition.z;
								
				if (Mathf.Abs(tunnelX - x) < tunnel.thresholdX)
				{				
					if (Mathf.Abs(tunnelZ - z) > tunnel.thresholdZ)
					{					
						float minTime = tunnel.tunnelMinVelocity[i];
						float maxTime = tunnel.tunnelMaxVelocity[i];			
						
						if ( t < minTime || t > maxTime )
							return true;
					}
				}
			}
			
			i++;	
		}
		
		return false;
	}
	*/
	
	private bool isInsideTunnel(DefaultState state)
	{
		GridTimeState gridTimeState = state as GridTimeState;
		if (gridTimeState == null)
			return false;
		
		float distanceToTunnel = GridTunnelSearch.ComputeDistanceToTunnel(gridTimeState,tunnel);	
			
		//Debug.Log("distanceToTunnel = " + distanceToTunnel + " and threshold = " + tunnel.thresholdD);		
		//gridTimeState.time = distanceToTunnel;
		
		return distanceToTunnel <= tunnel.thresholdD;
	}
	
	public override void generatePredecesors (ref DefaultState DcurrentState, ref DefaultState DpreviousState, 
	                                          ref DefaultState DidealGoalState, ref List<DefaultAction> transitions)
	{
		
		GridTimeState currentState = DcurrentState as GridTimeState;
		GridTimeState idealGoalState = DidealGoalState as GridTimeState;
		GridTimeState previousState = DpreviousState as GridTimeState;
		
		GridTimeAction stopAction = new GridTimeAction(currentState,Vector3.zero,0);
		transitions.Add(stopAction);
		

#if USE_ANGLE_MOV
		foreach( float angle in movAngles)
		{
			Vector3 mov = currentState.actionMov;
			mov =  Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(angle,Vector3.up),new Vector3(1,1,1)) * mov;
			mov.Normalize();			
#else		
		foreach ( Vector3 mov in movDirections )				
		{	
#endif
			
			foreach ( float speedIncr in possibleSpeedIncrements )
			{
				float newSpeed = previousState.speed - speedIncr;
				if (newSpeed > MAX_SPEED) 
					newSpeed = MAX_SPEED;
				else if (newSpeed < MIN_SPEED)
					newSpeed = MIN_SPEED;				
				
				GridTimeAction newAction = new GridTimeAction(previousState,mov,newSpeed,0.0f);
				
				if (!CheckTransitionCollisions(currentState, newAction.state))
					transitions.Add(newAction);			
			}
		}
		
		
	}

	override public bool CheckTransitionCollisions(DefaultState Dstate, DefaultState DprevState, bool sampling = true)
	{
		//float startTime = Time.realtimeSinceStartup;
		
		GridTimeState state = Dstate as GridTimeState;
		GridTimeState prevState = DprevState as GridTimeState;
		
		bool b = false;
		
		// If we are checking a state of this domain
		if (state != null)		
		{
			b = (
				CheckRootCollisions(state, prevState, sampling) || 
				CheckAgentsCollisions(state, prevState, sampling) || 
				CheckDynamicObstaclesCollisions(state,prevState, sampling)
				);
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
	public bool CheckRootCollisions(GridTimeState state, GridTimeState previousState, bool sampling)
	{			
		Vector3 start = state.currentPosition - new Vector3(0,10*agentHeight/2,0);
		Vector3 end = start + new Vector3(0,10*agentHeight/2,0);
		float radius = agentRadius;
		
		if (Physics.CheckCapsule(start,end,radius,layer))
		//if (Physics.CheckSphere(state.currentPosition,radius,layer))
			return true;		                         
		
		
		if (previousState != null)
		{
			int samples = numSamples;
			if (!sampling) 
				samples = 2;
			Vector3 mov = state.currentPosition - previousState.currentPosition;
			
			for (int i=1; i<samples-1; i+=1) // we sample less in this domain the collision check
			{			
				start = (previousState as GridTimeState).currentPosition + i/(samples-1)*mov;
				end = start + new Vector3(0,agentHeight/2,0);
				
				if (Physics.CheckCapsule(start,end,radius,layer))
				//if (Physics.CheckSphere(state.previousState.currentPosition,radius,layer))
			    return true;		                         		
			}
		}
		
		
		return false;
	}
	
	
	// Collisions check with deterministic dynamic obstacles
	public bool CheckDynamicObstaclesCollisions(GridTimeState state, GridTimeState previousState, bool sampling)
	{
		if (obstacles == null)
			return false;
		
		if (obstacles.closeObstacles == null)
			return false;
		
		List<DDObstacle> visibleObstacles = obstacles.visibleObstacles;
		
		if (visibleObstacles.Count <= 0)
			return false;
		
		if (previousState != null)
		{
			int samples = numSamples;
			if (!sampling) 
				samples = 2;
			Vector3 mov = state.currentPosition - previousState.currentPosition;
			
			float timeStep = (state.time - previousState.time) / (samples-1);
			float time = previousState.time;			
			
			for (int i=1; i<samples; i+=1) 
			{			
				Vector3 pos = previousState.currentPosition + i/(samples-1)*mov;
				time += timeStep;		
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
							
							//float time = state.time + timeWindow;
							
							obstacleScript.animation["test"].wrapMode = WrapMode.PingPong;
							obstacleScript.animation["test"].enabled = true;
							obstacleScript.animation["test"].time = time;					
							obstacleScript.animation.Play("test");
							obstacleScript.animation.Sample();						
							//obstacleScript.animation["test"].enabled = false;
							
							//Debug.DrawRay(pair.Value.obstacle.transform.position,4*Vector3.up,Color.blue);
								
							state.obstaclePos = obstacleScript.gameObj.transform.position;//ddObstacle.obstacle.transform.position;
					
							// Check collisions
							//bool collision = CheckJointsCollisions(state);
							//bool collision = CheckRootCollisions(state);		
							//Vector3 aux = state.obstaclePos - state.currentPosition;
							//bool collision = (aux.magnitude - 1 - analyzer.GetRadius()) < 0;
								
							bool collision = CheckObstacleCollision(pos,obstacleScript);
				
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
		
			}
		}
				
		return false;
	}
	
	public bool CheckObstacleCollision(Vector3 pos, ObstaclesScript script)
	{
		//if (state == null || script == null)
		//	return false;
						
		if (script.ShapeChoice.Shape == Enum.Enumeration.Sphere)
		{
			Vector3 obstaclePos = script.gameObj.transform.position;
			Vector3 aux = obstaclePos - pos;
			
			float sphereRadius = script.size.x;
			
		 	return (aux.magnitude - sphereRadius - agentRadius) <= 0;				
		}
		else if (script.ShapeChoice.Shape == Enum.Enumeration.Rectangle)
		{
			Bounds box = script.gameObj.collider.bounds;
			
			return box.SqrDistance(pos) <= agentRadius;
		}
				
		return false;		
	}	
	
	// Collisions check with other agents
	public bool CheckAgentsCollisions(GridTimeState state, GridTimeState previousState, bool sampling)
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
		
		if (previousState != null)
		{
			int samples = numSamples;
			if (!sampling) 
				samples = 2;
			Vector3 mov = state.currentPosition - previousState.currentPosition;
			
			float timeStep = (state.time - previousState.time) / (samples-1);
			float time = previousState.time;			
			
			for (int i=1; i<samples; i+=1) 
			{			
				Vector3 pos = previousState.currentPosition + i/(samples-1)*mov;
				time += timeStep;		
	
			
				foreach(KeyValuePair<int,Neighbor> pair in neighbors)
				{
					if (pair.Value.animationInterface != null && pair.Value.triggers == 2)
					{
						AnimationCurve[] curves = pair.Value.animationInterface.GetPlanAnimationCurve();
						
						if (curves != null)
						{
							float x = curves[0].Evaluate(time);
							float y = curves[1].Evaluate(time);
							float z = curves[2].Evaluate(time);
							
							Vector3 npos = new Vector3(x,y,z);
							
							//Debug.Log("neighbour pos = " + pos);
							
							if ( (npos - pos).magnitude < 2*agentRadius )
								return true;				
						}
					}
				}
					
			}
		}
			
		return false;
		
	}
	

}