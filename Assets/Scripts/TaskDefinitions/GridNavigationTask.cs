using UnityEngine;
using System.Collections.Generic;

class GridNavigationTask : Task
{
	
	public State startState;
	public State goalState; 
	
	public ARAstarPlanner gridPlanner; 
	Dictionary<DefaultState, ARAstarNode> outputPlan;
	
	public float inflationFactor; 
	List<State> path; 
	
	// TODO : CHECK IF WE NEED THIS ANYMORE 
	bool initialized = false;
	bool firstPlan = false; 
	
	bool startChanged = false;
	
	bool goalChanged = false; 
	
	bool obstacleChanged;
	List<System.Object> obstacleChangedData; 
	
	
	// tunnel planner variables 
	public float tunnelDistanceThreshold; 
	public float tunnelTimeThreshold; 
	public GridTimeDomain gridTimeDomain;
	public List<State> spaceTimePath; 
	
	public bool spaceTimePathStatus; 
	
	PathStatus gridPathStatus; 
	
	public bool dynamicChange;
	
	public bool currentlyExecutingTask; 
	
	public GridNavigationTask (State t_startState, State t_goalState, float t_inflationFactor, ref List<State> t_path,
		float t_tunnelDistanceThreshold, float t_tunnelTimeThreshold, ref List<State> t_spaceTimePath, GridTimeDomain t_gridTimeDomain,
		TaskPriority t_taskPriority, TaskManager taskManager)
		: base (taskManager, t_taskPriority) // defaults to a real-time task -- now what happens when both this and Global are first put in queue ?? 
	{
		taskType = TaskType.GridNavigationTask;
		
		startState = t_startState;
		goalState = t_goalState;
		
		// EVENT : startState will trigger GridNavigationTask that STATE_CHANGED
		startState.registerObserver(Event.STATE_POSITION_CHANGED, this);
		
		// EVENT : goalState will trigger GridNavigationTask that STATE_CHANGED
		goalState.registerObserver(Event.STATE_POSITION_CHANGED, this);
		
		// EVENT : goalState will trigger GridNavigationTask that it is GOAL_INVALID or VALID
		goalState.registerObserver(Event.GOAL_INVALID, this);
		goalState.registerObserver(Event.GOAL_VALID, this);
		
		
		inflationFactor = t_inflationFactor;
		
		//planner = t_planner;
		outputPlan = new Dictionary<DefaultState, ARAstarNode>();
		path = t_path;
		
		
		// TODO : deprecate these lists 
		List<PlanningDomainBase> domainList = new List<PlanningDomainBase>();
		ARAstarDomain araStarDomain = new ARAstarDomain ();
		araStarDomain.setPlanningTask(this);
		domainList.Add(araStarDomain);
		
		gridPlanner = new ARAstarPlanner();
		gridPlanner.init(ref domainList, 100);
		
		initialized = true;
		
		obstacleChanged = false;
		obstacleChangedData = new List<object> ();
		
		
		// tunnel planner variables
		tunnelDistanceThreshold = t_tunnelDistanceThreshold;
		tunnelTimeThreshold = t_tunnelTimeThreshold;
		gridTimeDomain = t_gridTimeDomain;
		spaceTimePath = t_spaceTimePath;
		
		spaceTimePathStatus = false;
		
		dynamicChange = false;
		
		currentlyExecutingTask = false;
		
		gridPathStatus = PathStatus.NoPath;
						
	}
	
	void generatePlanList (DefaultState stateReached)
	{
		// here we are clearing the plan list 
		path.Clear();
		
		// TODO : what if we want someone to monitor the states in this plan 
		
		// TODO : this is unnecessary -- make planner use State 
		ARAstarState currentState = stateReached as ARAstarState;
		ARAstarState starttState = new ARAstarState(startState.getPosition());
		
		Debug.Log ("generating plan to state " + currentState.state);
		while(!currentState.Equals(starttState)){
	
			path.Add(new State(currentState.state));
			currentState = outputPlan[currentState].previousState as ARAstarState;
		}
		// making sure start state enters as well
		path.Add(new State(currentState.state));
		
		//notifyObservers(Event.GRID_PATH_CHANGED,path);
		
	}	
	
	bool doTunnelTask ()
	{
		Stack<DefaultAction> convertedTunnel = new Stack<DefaultAction> ();
		// convert tunnel 
		for(int i = 0; i < path.Count; i++)
		{
			GridPlanningState gs = new GridPlanningState(path[i].getPosition());
			GridPlanningAction g = new GridPlanningAction (gs, new Vector3());
			convertedTunnel.Push(g);
			
		}
		
		GridTunnelSearch gridTunnelSearch = 
			new GridTunnelSearch(convertedTunnel, 
				this.startState.getPosition(),startState._time, goalState._time,gridTimeDomain,300,2.0f,1.0f,1.0f,startState._speed);
			//new GridTunnelSearch(convertedTunnel, startState.getPosition(), 0.0F, 10.0F, gridTimeDomain, 250, 2.0F, 1.0F, 1.0F);
			
		
		
		//gridTunnelSearch.tryARAStarPlanner(spaceTimePath);
		//Debug.Log ("grid time plan using ara star planner " + spaceTimePath.Count);
		
		//return true;
		
		//// generating plan for now 
		spaceTimePath.Clear();
		
		/*
		// this is the state plan 
		Debug.Log ("grid tunnel plan using best first planner " + gridTunnelSearch.newStatePlan.Count);
		while (gridTunnelSearch.newStatePlan.Count != 0)
		{
			GridTimeState state = gridTunnelSearch.newStatePlan.Pop() as GridTimeState;
			//Debug.LogWarning ("space time state " + state.time);
			spaceTimePath.Add(new State(state.currentPosition, state.time));
		}
		*/
		
		// this is the action plan 
		Debug.Log ("grid tunnel plan using best first planner " + gridTunnelSearch.newPlan.Count);
		while (gridTunnelSearch.newPlan.Count != 0)
		{
			DefaultAction action = gridTunnelSearch.newPlan.Pop();
			GridTimeState state = action.state as GridTimeState;
			//Debug.LogWarning ("space time state " + state.time + " " + state.currentPosition);
			spaceTimePath.Add(new State(state.currentPosition, state.time, state.speed));
		}
		
		bool planComputed = gridTunnelSearch.goalReached;	
		
		//Debug.LogError("grid tunnel plan status " + planComputed);
		
		return planComputed;		
	}
	
	public override TaskStatus execute (float maxTime)
	{
		// TODO : make sure start and goal are set to integers -- else this planner will crash, never return 
		// TODO : make sure the 'y' value is consistent throughout -- causes big problems 
		// as soon as u see the planner expanding max nodes -- u know its prob y value .
		
		bool doWeNeedToGridPlan = false;
		if (firstPlan == false) doWeNeedToGridPlan = true;

		if (startChanged == true && firstPlan == true )
		{
			Debug.LogError("HANDLING START CHANGE " );
			DefaultState newStartState = new ARAstarState( startState.getPosition() ) as DefaultState;
			gridPlanner.UpdateAfterStartMoved(newStartState);
			startChanged = false;
			
			doWeNeedToGridPlan = true;
		}
		
		// handle obstacle changes here 
		if (obstacleChanged == true && firstPlan == true)
		{
			
			Debug.LogError("HANDLING OBSTACLE CHANGE");
			foreach (object data in obstacleChangedData)
			{
				Vector3[] pc = (Vector3[]) data; // {oldPos, newPos}
				
				DefaultState ps = new ARAstarState( pc[0] ) as DefaultState;
				DefaultState ns = new ARAstarState( pc[1] ) as DefaultState;
				
				gridPlanner.UpdateAfterObstacleMoved(ps, ns);
			}
			
			obstacleChanged = false;
			obstacleChangedData.Clear ();
			
			doWeNeedToGridPlan = true;
			
		}
		
		if (goalChanged == true && firstPlan == true) // update goal is called only if we have already planned once 
		{
			Debug.LogError("HANDLING GOAL CHANGE");
			DefaultState newGoalState = new ARAstarState( goalState.getPosition() ) as DefaultState;
			gridPlanner.UpdateAfterGoalMoved(newGoalState);
			
			doWeNeedToGridPlan = true;
		}
		
		// SINISTER BUG THAT GOALCHANGED WAS NOT BEING SET TO FALSE IF firstPlan was false -- always set it when we come here 
		goalChanged = false; 
		
	
		// grid planning only if start, goal, or obstracle changed OR first time 
		if (doWeNeedToGridPlan)
		{
			
			Debug.Log("grid Planning from " + startState.getPosition() + "to " + goalState.getPosition() + " " + doWeNeedToGridPlan + " " + firstPlan);
			DefaultState DStartState = new ARAstarState(startState.getPosition()) as DefaultState;
			DefaultState DGoalState = new ARAstarState(goalState.getPosition()) as DefaultState;
			
			gridPathStatus = gridPlanner.computePlan(ref DStartState, ref DGoalState, ref outputPlan, ref inflationFactor, 1.0f);
			Debug.Log("grid Plan result : " + gridPathStatus);
			
			// filling plan -- made separate operations now 
			DefaultState stateReached = gridPlanner.FillPlan();
			generatePlanList (stateReached);
			
			if ( firstPlan == false ) firstPlan = true; 	
		}
		
		// space time planning only if grid plan chagned or DYNAMIC CHANGE 
		if ( doWeNeedToGridPlan || dynamicChange || firstPlan)
		{
			
			Debug.LogError("SPACE TIME PLANNING");
			spaceTimePathStatus = false;
			if (gridPathStatus == PathStatus.NoPath || gridPathStatus == PathStatus.Incomplete )
			{
				Debug.LogError("GRID PLAN IS INCOMPLETE OR NO PATH EXISTS -- CANNOT DO TUNNEL PLANNING");
			}
			else
				spaceTimePathStatus = doTunnelTask ();
			
			dynamicChange = false; // just assuming it was handled here for now 
		}
		
		
		// TODO : accomodate tunnel status in this 
		//if ( finished != PathStatus.Optimal)
		if ( gridPathStatus == PathStatus.Incomplete || spaceTimePathStatus == false)
			setTaskPriority(TaskPriority.RealTime);
		else
			setTaskPriority(TaskPriority.Inactive);
		
		return TaskStatus.Success;		
				
	}
	
	public override void notifyEvent (Event ev, System.Object data)
	{
		if (initialized == false) return; 
		
		// dont handle any events other than goal validation when task is asleep 
		if (taskPriority == TaskPriority.Asleep && ev != Event.GOAL_VALID)
			return; 
		
		
		switch (ev)
		{
		case Event.GOAL_INVALID : 
			Debug.LogWarning ("goal is INVALID for " + taskName);
			setTaskPriority(TaskPriority.Asleep);
			break;
			
		case Event.GOAL_VALID : 
			Debug.LogWarning ("goal is VALID for " + taskName);
			setTaskPriority(TaskPriority.Inactive);
			break;
			
		case Event.STATE_POSITION_CHANGED:
			
			// in this case, either the start must have changed or the goal must have changed 
			
			Vector3[] posChange = (Vector3[]) data; // {oldPos, newPos}
			Debug.Log (startState.getPosition() + " " + posChange[0] + " " + posChange[1] + " " + goalState.getPosition()) ;
			
			DefaultState prevState = new ARAstarState( posChange[0] ) as DefaultState;
			DefaultState newState = new ARAstarState( posChange[1] ) as DefaultState;
			
			if (Vector3.Distance(startState.getPosition(),posChange[1]) < 0.1F) // this should always be exactly the same if hit 
			{
				//Debug.Log ("start state has changed in grid task " + taskName);
			
				// TODO : this comes either from the global nav task OR the by executing the tunnel task 
				// second case will return a float value -- problem 
				if ( startState.ApproximatelyEquals(goalState,1.0f) ) // this is being compared to the grid task goal -- not the tunnel task goal 
				{
					Debug.LogWarning("REACHED GOAL in " + taskName);
					
					path.Clear ();
					spaceTimePath.Clear ();
					
					startState.unregisterObserver(Event.STATE_POSITION_CHANGED,this);
					goalState.unregisterObserver(Event.GOAL_INVALID, this);
					goalState.unregisterObserver(Event.GOAL_VALID, this);
					goalState.unregisterObserver(Event.STATE_POSITION_CHANGED, this);
					
					// TODO : unregister the non determinstic obstacles -- we really just need to keep a UNIQUE list (set) here 
				
					// task is complete 
					taskCompleted ();
					
					break;
				}
				else if (Vector3.Distance(posChange[0],posChange[1]) > 1.0F) // NOT A MAJOR CHANGE IN START POSITION (ASSUMING ALWAYS ALONG THE PATH 
				{
					setTaskPriority(TaskPriority.RealTime);
				}
				
				// always set this -- make sure you do call UpdateAfterStartMoved if this task is executed 
				startChanged = true; 
				
			}
			else if (Vector3.Distance(goalState.getPosition(),posChange[1]) < 0.1F)
			{
				Debug.LogError ("goal state has changed in grid task");				
				goalChanged = true;
				setTaskPriority(TaskPriority.RealTime);
			}			
			
			
			break;
			
		case Event.NON_DETERMINISTIC_OBSTACLE_CHANGED:
			
			if ( currentlyExecutingTask )
			{
				obstacleChanged = true;
				// TODO : what if the same obstacle changes ? we need to check for that 
				obstacleChangedData.Add(data);
				Debug.LogError("grid task has recieved non deterministic obstacle change event ");
				setTaskPriority(TaskPriority.RealTime);
			}
			
			break;
			
		case Event.NEIGBHOR_AGENTS_CHANGED :
			Debug.LogError("NEIGHBOR AGENTS HAVE CHANGED WE NEED TO REPLAN SPACE TIME");
			dynamicChange = true;
			setTaskPriority(TaskPriority.RealTime);
			return;
  			
		case Event.NEIGBHOR_DYNAMIC_OBSTACLES_CHANGED :
			Debug.LogError("NEIGHBOR OBSTACLES HAVE CHANGED WE NEED TO REPLAN SPACE TIME");
			dynamicChange = true; 
			setTaskPriority(TaskPriority.RealTime);
			return;
			
		case Event.CURRENT_EXECUTING_TASK :
			Debug.LogError("I AM NOW THE CURRENT EXECUTING TASK");
			setTaskPriority(TaskPriority.RealTime);
			return;
			
			
		}
	}
	
}
