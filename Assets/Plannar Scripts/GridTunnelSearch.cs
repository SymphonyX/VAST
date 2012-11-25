using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Structure defining the tunnel
public struct Tunnel{
	
	// An array of GridStates computed if we travel at mid velocity
	public GridTimeState[] tunnelMidVelocity;
	
	// Two arrays of floats with the time to get to the states at 
	// min and max velocities
	public float[] tunnelMinVelocity;
	public float[] tunnelMaxVelocity;
	
	//public float thresholdX;
	//public float thresholdZ;
	public float thresholdD;
	public float thresholdT;
	
}

public class GridTunnelSearch{
	
	// Creation function
	//public GridTunnelSearch(Stack<DefaultAction> plan, float startTime, float idealGoalTime, GridTimeDomain gtd, int MaxNumberOfNodes, float MaxTime, float T_x, float T_z, float T_t)
	public GridTunnelSearch(Stack<DefaultAction> plan, Vector3 startPos, float startTime, float idealGoalTime, GridTimeDomain gtd, int MaxNumberOfNodes, float MaxTime, float T_d, float T_t, float startSpeed = 0)
	{
		currentPlan = new DefaultAction[plan.Count];
		plan.CopyTo(currentPlan,0);	
		
		this.T_d = T_d;
		//this.T_x = T_x;
		//this.T_z = T_z;
		this.T_t = T_t;
		
		this.maxTime = MaxTime;
		
		int startIndex = 0;
		//GridPlanningState startPlanningState = currentPlan[startIndex].state as GridPlanningState;
		//while (startPlanningState == null)
		//{
		//	startIndex++;
		//	startPlanningState = currentPlan[startIndex].state as GridPlanningState;
		//}
				
		//startState = new GridTimeState(startPlanningState.currentPosition, startTime, startSpeed);
		startState = new GridTimeState(startPos, startTime, startSpeed);
		lastState = new GridTimeState((currentPlan[currentPlan.Length-1].state as GridPlanningState).currentPosition, idealGoalTime);
		
		gridTimeDomain = gtd;
		
		float dist = (startState.currentPosition - lastState.currentPosition).magnitude;
		// TODO: what to do if there is no time???
		float minSpeed = dist / (idealGoalTime-startTime);
		if (minSpeed < 0)
			minSpeed = GridTimeDomain.MIN_SPEED;
		
		//tunnel = ConvertPlan(gtd.minSpeed, gtd.midSpeed, gtd.maxSpeed);
		tunnel = ConvertPlan(startTime, minSpeed, GridTimeDomain.MID_SPEED, GridTimeDomain.MAX_SPEED);
		
		//for(int aux = 0; aux < tunnel.tunnelMinVelocity.Length; aux++)
		//	Debug.Log("Tunnel "+aux+":"+tunnel.tunnelMinVelocity[aux]);
				
	 	tunnelPlanner = new BestFirstSearchPlanner();
		
		
		List<PlanningDomainBase> domainList = new List<PlanningDomainBase>();		
		domainList.Add(gridTimeDomain);
		
		tunnelPlanner.init(ref domainList, MaxNumberOfNodes);			
		
		// MUBBASIR TESTING ARA STAR PLANNER HERE 
		araStarPlanner = new ARAstarPlanner ();
		araStarPlanner.init(ref domainList, MaxNumberOfNodes);
		
		
		goalReached = false;
		
		ComputeTunnelSearch();	
		
		//ComputeTunnelSearchStatePlan ();
	}
		
	// The current plan with actions of different resolutions
	private DefaultAction[] currentPlan;
		
	private GridTimeState startState;
	private GridTimeState lastState;
	
	private GridTimeDomain gridTimeDomain;
	
	private Tunnel tunnel;
	
	private static BestFirstSearchPlanner tunnelPlanner;
	
	private ARAstarPlanner araStarPlanner; 
	
	// The new plan, hopefully it will just have HighLevelActions
	public Stack<DefaultAction> newPlan;
	
	public Stack<DefaultState> newStatePlan;
	
	public float T_d;
	//public float T_x;
	//public float T_z;
	public float T_t;
	
	public float maxTime;
	
	public bool goalReached;
	
	private float ComputeEstimatedTime(GridPlanningState state, float velocity)
	{
		float distance = (state.currentPosition - startState.currentPosition).magnitude;
		
		float time = distance / velocity;
		
		return time;
	}
		
	private float[] ComputeNodeDistances(DefaultAction[] plan)
	{
		float[] distances = new float[plan.Length];
		int i = 0;
		GridPlanningAction action = null;
		GridPlanningState state = null;
		while( action == null && i < plan.Length)
		{	
			action = plan[i] as GridPlanningAction;
			if (action != null)
				state = action.state as GridPlanningState;
			distances[i] = 0;
			i++;
		}
		
		GridPlanningState nextState;
		while(i+1 < plan.Length)
		{
			nextState = (plan[i+1] as GridPlanningAction).state as GridPlanningState;
			
			distances[i+1] = ( state.currentPosition - nextState.currentPosition ).magnitude;
			
			state = nextState;
			i++;
		}		
		
		return distances;
	}
	
	private float ComputeTotalDistance(float[] distances)
	{
		float sum = 0;
		foreach (float d in distances)
			sum += d;
		return sum;
	}
	
	/*
	private Tunnel ConvertPlan(float minVelocity, float midVelocity, float maxVelocity)
	{
		Tunnel tunnel;
		tunnel.tunnelMidVelocity = new GridTimeState[currentPlan.Length];
		tunnel.tunnelMinVelocity = new float[currentPlan.Length];
		tunnel.tunnelMaxVelocity = new float[currentPlan.Length];
		
		tunnel.thresholdD = T_d;
		//tunnel.thresholdX = T_x;
		//tunnel.thresholdZ = T_z;
		tunnel.thresholdT = T_t;
		
		int i = 0;		
		foreach ( DefaultAction action in currentPlan )
		{
			GridPlanningState state = action.state as GridPlanningState;
			
			if (state != null)
			{
				float midTime = ComputeEstimatedTime(state, midVelocity);
				
				GridTimeState gridTimeState = new GridTimeState(state.currentPosition,midTime);
				
				tunnel.tunnelMidVelocity[i] = gridTimeState;
				
				tunnel.tunnelMinVelocity[i] = ComputeEstimatedTime(state, minVelocity);
				tunnel.tunnelMaxVelocity[i] = ComputeEstimatedTime(state, maxVelocity);
			}
			
			i++;
		}
		
		return tunnel;
	}
	*/
	
	
	private Tunnel ConvertPlan(float startTime, float minVelocity, float midVelocity, float maxVelocity)
	{
		Tunnel tunnel;
		tunnel.tunnelMidVelocity = new GridTimeState[currentPlan.Length];
		tunnel.tunnelMinVelocity = new float[currentPlan.Length];
		tunnel.tunnelMaxVelocity = new float[currentPlan.Length];
		
		tunnel.thresholdD = T_d;
		//tunnel.thresholdX = T_x;
		//tunnel.thresholdZ = T_z;
		tunnel.thresholdT = T_t;

		GridPlanningState lastGridPlanningState = currentPlan[currentPlan.Length-1].state as GridPlanningState;
		
		float maxGoalTime = ComputeEstimatedTime(lastGridPlanningState,minVelocity);
		float midGoalTime = ComputeEstimatedTime(lastGridPlanningState,midVelocity);
		float minGoalTime = ComputeEstimatedTime(lastGridPlanningState,maxVelocity);
		
		//Debug.Log("minGoalTime = " + minGoalTime);
		//Debug.Log("midGoalTime = " + midGoalTime);
		//Debug.Log("maxGoalTime = " + maxGoalTime);
		
		float[] distances = ComputeNodeDistances(currentPlan);
		
		//for (int aux=0; aux<distances.Length; aux++)
		//	Debug.Log("distance "+aux+": "+distances[aux]);
		
		float totalDistance = ComputeTotalDistance(distances);
		
		int i = 0;	
		float minSum = startTime;
		float midSum = startTime;
		float maxSum = startTime;
		while( i < currentPlan.Length )
		{
			GridPlanningState state = currentPlan[i].state as GridPlanningState;
			if (state != null)
			{
				float midTime = midSum + midGoalTime * distances[i] / totalDistance;
				midSum = midTime;
				GridTimeState gridTimeState = new GridTimeState(state.currentPosition,midTime);				
				tunnel.tunnelMidVelocity[i] = gridTimeState;			
			}
			else
				tunnel.tunnelMidVelocity[i] = null;			
				
			tunnel.tunnelMaxVelocity[i] = minSum + minGoalTime * distances[i] / totalDistance;
			tunnel.tunnelMinVelocity[i] = maxSum + maxGoalTime * distances[i] / totalDistance;
			
			minSum = tunnel.tunnelMaxVelocity[i];
			maxSum = tunnel.tunnelMinVelocity[i];
			
			i++;
		}
		
		return tunnel;
	}

	
	private void ComputeTunnelSearch()
	{
		gridTimeDomain.UseTunnelSearch(tunnel);
		
		
		DefaultState defStartState = startState as DefaultState;
		DefaultState defLastState = lastState as DefaultState;
		
		newPlan = new Stack<DefaultAction>();
		goalReached = tunnelPlanner.computePlan(ref defStartState,ref defLastState,ref newPlan, maxTime);
		
		gridTimeDomain.DisableTunnelSearch();
	}
	
	private void ComputeTunnelSearchStatePlan()
	{
		gridTimeDomain.UseTunnelSearch(tunnel);
		
		DefaultState defStartState = startState as DefaultState;
		DefaultState defLastState = lastState as DefaultState;
		
		newStatePlan = new Stack<DefaultState>();
		goalReached = tunnelPlanner.computePlan(ref defStartState,ref defLastState,ref newStatePlan, maxTime);
		
		gridTimeDomain.DisableTunnelSearch();
	}

	
	
	public void tryARAStarPlanner ( List<State> spaceTimePath )
	{
		gridTimeDomain.UseTunnelSearch(tunnel);
		
		DefaultState defStartState = startState as DefaultState;
		DefaultState defLastState = lastState as DefaultState;
				
		Dictionary<DefaultState,ARAstarNode> araStarPlan = new Dictionary<DefaultState, ARAstarNode>();
		float inflationFactor = 2.5f;
		PathStatus status = araStarPlanner.computePlan(ref defStartState, ref defLastState,ref araStarPlan,ref inflationFactor, maxTime);
		DefaultState stateReached = araStarPlanner.FillPlan ();
		Debug.Log("TUNNEL ARA SEARCH TEST " + araStarPlan.Count + " status " + status.ToString());		
		
		generateSpaceTimePlanStack(defStartState, stateReached, araStarPlan, spaceTimePath);
		
		gridTimeDomain.DisableTunnelSearch();
	}
	
	void generateSpaceTimePlanStack (DefaultState startState, DefaultState stateReached, Dictionary<DefaultState,ARAstarNode> araStarPlan, List<State> spaceTimePath)
	{
		// here we are clearing the plan list 
		spaceTimePath.Clear();
		
		GridTimeState currentState = stateReached as GridTimeState;
		GridTimeState starttState = startState as  GridTimeState;
		
		while(!currentState.Equals(starttState)){
	
			spaceTimePath.Add(new State(currentState.currentPosition, currentState.time));
			currentState = araStarPlan[currentState].previousState as GridTimeState;
		}
		
		spaceTimePath.Add(new State(currentState.currentPosition, currentState.time));
		
		//notifyObservers(Event.GRID_PATH_CHANGED,path);
		
	}	
	
	
	
	// Returns the perpendicular distance from the state "state" to the vector/path/edge
	// formed by the states s1 and s2
	private static float ComputeDistance(GridTimeState state, GridTimeState s1, GridTimeState s2)
	{
		Vector3 A = s1.currentPosition;
		Vector3 B = s2.currentPosition;
		Vector3 C = state.currentPosition;
		
		Vector3 AB = B - A;
		Vector3 AC = C - A;
		
		//float ABdotAC = Vector3.Dot(AB,AC); 
				
		//float distance = Mathf.Sqrt(1 - (ABdotAC*ABdotAC)/(AC.sqrMagnitude * AB.sqrMagnitude)) * AC.magnitude;
		
		// P is the projection of C over AB 
		Vector3 AP = Vector3.Project(AC,AB);
		Vector3 P = A + AP;		
		Vector3 BP = P - B;
		
		//Debug.DrawLine(A,(A+Vector3.up),Color.red);
		//Debug.DrawLine(P,(P+Vector3.up),Color.blue);
		//Debug.DrawLine(A,P,Color.green);
		
		float APsqrMagnitude = AP.sqrMagnitude;
		float BPsqrMagnitude = BP.sqrMagnitude;
		float ABsqrMagnitude = AB.sqrMagnitude;
		
		float distance;
		if ( APsqrMagnitude <= ABsqrMagnitude && BPsqrMagnitude <= ABsqrMagnitude)
			// if the projection is between A and B (in the segment)
			distance = ( P - C ).magnitude;
		else
		{
			float ACmagnitude = AC.magnitude;
			float BCmagnitude = (C-B).magnitude;
			
			if (ACmagnitude < BCmagnitude)
				distance = ACmagnitude;
			else
				distance = BCmagnitude;			
		}
		
		return distance;
	}
	
	// Performs a binary search for the indexes in the tunnel t corresponding to the portion of the path 
	// where the time values are in the valid range of the state we are looking at
	private static void SearchTimeRanges(Tunnel t, GridTimeState state, 
		ref int startIndexMinV, ref int endIndexMinV,
		ref int startIndexMaxV, ref int endIndexMaxV)
	{
		startIndexMinV = -1;
		endIndexMinV = -1;
		startIndexMaxV = -1;
		endIndexMaxV = -1;
				
		float minTime = state.time - t.thresholdT;
		if (minTime < 0 ) minTime = 0;
		float maxTime = state.time + t.thresholdT;
		
		int i = 0;
		int j = t.tunnelMidVelocity.Length-1;		
		int k = -1;		
		while (i < j)
		{
			k = i + (int)(( j - i ) / 2);
			
			float minTimeK = t.tunnelMaxVelocity[k];
			
			if (minTime == minTimeK)
			{
				i = k;
				j = k;
			}
			else
			{
				if (minTime < minTimeK)
				{
					k--;
					j = k;
				}
				else // minTim > minTimeK
				{
					k++;
					i = k;					
				}
			}
		}		
		startIndexMaxV = k;
				
		float aux;
		
		if (k >= 0 && k < t.tunnelMidVelocity.Length)
		{
			aux = t.tunnelMaxVelocity[k];
			if (aux < minTime)
				startIndexMaxV++;
		}
				
		i = 0;
		j = t.tunnelMidVelocity.Length-1;		
		k = -1;		
		while (i < j)
		{
			k = i + (int)(( j - i ) / 2);
			
			float minTimeK = t.tunnelMaxVelocity[k];
			
			if (maxTime == minTimeK)
			{
				i = k;
				j = k;
			}
			else
			{
				if (maxTime < minTimeK)
				{
					k--;
					j = k;
				}
				else // maxTim > minTimeK
				{
					k++;
					i = k;					
				}
			}
		}
		endIndexMaxV = k;
		
		if (k >= 0 && k < t.tunnelMidVelocity.Length)
		{
			aux = t.tunnelMaxVelocity[k];
			if (aux > maxTime)
				endIndexMaxV--;
		}
		
		
		i = 0;
		j = t.tunnelMidVelocity.Length-1;		
		k = -1;		
		while (i < j)
		{
			k = i + (int)(( j - i ) / 2);
			
			float maxTimeK = t.tunnelMinVelocity[k];
			
			if (minTime == maxTimeK)
			{
				i = k;
				j = k;
			}
			else
			{
				if (minTime < maxTimeK)
				{
					k--;
					j = k;
				}
				else // minTim > maxTimeK
				{
					k++;
					i = k;					
				}
			}
		}
		startIndexMinV = k;
		
		if (k >= 0 && k < t.tunnelMidVelocity.Length)
		{
			aux = t.tunnelMaxVelocity[k];
			if (aux < minTime)
				startIndexMinV--;
		}
		
		i = 0;
		j = t.tunnelMidVelocity.Length-1;		
		k = -1;		
		while (i < j)
		{
			k = i + (int)(( j - i ) / 2);
			
			float maxTimeK = t.tunnelMinVelocity[k];
			
			if (maxTime == maxTimeK)
			{
				i = k;
				j = k;
			}
			else
			{
				if (maxTime < maxTimeK)
				{
					k--;
					j = k;
				}
				else // maxTim > maxTimeK
				{
					k++;
					i = k;					
				}
			}
		}
		endIndexMinV = k;
		
		if (k >= 0 && k < t.tunnelMidVelocity.Length)
		{
			aux = t.tunnelMaxVelocity[k];
			if (aux > maxTime)
				endIndexMinV--;
		}
		
		
		/*
		for(int aux2 = 0; aux2 < t.tunnelMaxVelocity.Length; aux2++)
			Debug.Log("Tunnel "+aux2+":"+t.tunnelMaxVelocity[aux2]+ " y "+t.tunnelMinVelocity[aux2]);
		Debug.Log("minTime = "+minTime);
		Debug.Log("maxTime = "+maxTime);
		Debug.Log("startIndexMaxV = "+startIndexMaxV);
		Debug.Log("endIndexMaxV= "+endIndexMaxV);
		Debug.Log("startIndexMinV = "+startIndexMinV);
		Debug.Log("endIndexMinV= "+endIndexMinV);
		*/
				
	}
	
	public static float ComputeDistanceToTunnel(GridTimeState state, Tunnel t)
	{
		int startIndexMaxV = -1;
		int endIndexMaxV = -1;
		int startIndexMinV = -1;
		int endIndexMinV = -1;
		SearchTimeRanges(t,state,ref startIndexMinV, ref endIndexMinV, ref startIndexMaxV, ref endIndexMaxV);
		
		Vector3 statePos = state.currentPosition;
			
		float minDistance = float.MaxValue;
		
		if ( 
			(startIndexMinV >=0 && startIndexMinV < t.tunnelMidVelocity.Length) || 
			(endIndexMinV >=0 && endIndexMinV < t.tunnelMidVelocity.Length)
		)
		{
			if (startIndexMinV >=0 || startIndexMinV < t.tunnelMidVelocity.Length)
				startIndexMinV = 0;
			if (endIndexMinV >=0 || endIndexMinV < t.tunnelMidVelocity.Length)
				endIndexMaxV = t.tunnelMidVelocity.Length-1;
				
			for (int i = startIndexMinV; i < endIndexMinV; i++)		
			{
				GridTimeState s1 = t.tunnelMidVelocity[i];
				GridTimeState s2 = t.tunnelMidVelocity[i+1];
				
				float distance = float.MaxValue;
				if (s1 != null && s2 != null)			
					distance = ComputeDistance(state,s1,s2);
				
				if (distance < minDistance)
					minDistance = distance;
			}
		}
			
		if ( 
			(startIndexMaxV >=0 && startIndexMaxV < t.tunnelMidVelocity.Length) || 
			(endIndexMaxV >=0 && endIndexMaxV < t.tunnelMidVelocity.Length)
		)
		{
			if (startIndexMaxV >=0 || startIndexMaxV < t.tunnelMidVelocity.Length)
				startIndexMaxV = 0;
			if (endIndexMaxV >=0 || endIndexMaxV < t.tunnelMidVelocity.Length)
				endIndexMaxV = t.tunnelMidVelocity.Length-1;
		
		
			for (int i = startIndexMaxV; i < endIndexMaxV; i++)
			{
				GridTimeState s1 = t.tunnelMidVelocity[i];
				GridTimeState s2 = t.tunnelMidVelocity[i+1];
				
				float distance = float.MaxValue;
				if (s1 != null && s2 != null)			
					distance = ComputeDistance(state,s1,s2);
				
				if (distance < minDistance)
					minDistance = distance;
			}
		}	
			
		return minDistance;
	}
}


