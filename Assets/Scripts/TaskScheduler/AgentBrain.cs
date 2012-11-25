using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class AgentBrain : MonoBehaviour, Observer, AnimationInterface {
	
	public State currentState; 
	public uint currentPolygonIndex; 
	
	public Goal goal; 
	public State goalState; // we dont really need this 
	public int passableMask;
	public bool goalReached; 
	
	[HideInInspector] public TaskManager taskManager; 
	
	// variables for dynamic nav mesh planning 
	public List<uint> globalPathPolygonIndices;
	
	
	public List<State> globalPath; // can be states except the current State 
	public int numGlobalPathWaypoints; 
	public List<List<State>> localPaths; // localPath[i] is path to globalPath[i] from globalPath[i-1] or currentState
	public List<List<State>> spaceTimePaths; // one to one mapping with every localPath
	
	public List<bool> offMeshLinkWayPoint; 
		
	public int currentPlanExecutionIndex; // index into which space time plan we are currently executing 
	
	// TODO GET THIS TO WORK 
	NeighbourObstacles neighborObstacles;
	NeighbourAgents neighborAgents; 
		
	public bool drawGlobalPath;
	public bool drawGridPaths; 
	public bool drawSpaceTimePaths;
	public bool drawSpaceTimeCurve;
	
	public bool drawVisited;
	public bool drawOpen;
	public bool drawClosed;
	public bool drawPlan;
	
	
	public GlobalNavigationtask globalNavigationTask;
	List<GridNavigationTask> gridNavigationTasks;
	
	AnimationCurve[] curves;
	float endTime = 0.0f;
	
	
	public SoldierLocomotion locomotionSystem;
	public bool moveAutomatically;
	public bool stop = false; 
	
	public GUIText text;
	public TextMesh textMesh;
	
	public Task GetCurrentGridTask ()
	{
		if (goalReached == true) return null;
		return gridNavigationTasks[currentPlanExecutionIndex];
	}
	
	////////////// ANIMATION INTERFACE FUNCTION DEFINITIONS  //////////////
	public float GetCurrentTime ()
	{
		return currentState._time;
	}
	
	public void SetStop ( bool t_stop )
	{
		stop = t_stop;
	}
		
	public void SetCurrentStatePosition (Vector3 position)
	{
		currentState.setPosition(position);
	}
	
	public void CompletedOffMeshLink ()
	{
		_MoveToNextTaskForExecution ();
	}
	
	public bool IsOnOffMeshLink ()
	{
		//Debug.Log ("is off mesh link " + CentralizedManager.IsOffMeshLink(currentState.getPosition()));
		//return offMeshLinkWayPoint[currentPlanExecutionIndex];
		
		if ( currentPlanExecutionIndex >= gridNavigationTasks.Count)
		{
			return false; 
		}
		
		Vector3 meshLinkStartPos = gridNavigationTasks[currentPlanExecutionIndex].goalState.getPosition();
		if ( CentralizedManager.IsOffMeshLink(meshLinkStartPos) == true )
			Debug.LogError("on off mesh link " + currentPlanExecutionIndex + " " + meshLinkStartPos);
		
		return CentralizedManager.IsOffMeshLink(meshLinkStartPos);
	}
	
	public OffMeshLinkInformation GetCurrentOffMeshLinkInformation ()
	{
		Vector3 meshLinkStartPos = gridNavigationTasks[currentPlanExecutionIndex].goalState.getPosition();
		return CentralizedManager.GetOffMeshLinkInformation(meshLinkStartPos);
	}
	
	public float GetCurrentSpeed ()
	{
		return currentState._speed;
	}
	
	public void SetCurrentSpeed (float t_currentSpeed)
	{
		currentState._speed = t_currentSpeed;	
	}
	
	public float RemainingDistance()
	{
		Vector3 toGoal = goalState.getPosition() - currentState.getPosition();
		float distanceToGoal = toGoal.magnitude;
		
		if (goalReached == true) distanceToGoal = 0.0f; 
		
		return distanceToGoal;
	}		
	
	public AnimationCurve[] GetPlanAnimationCurve()
	{
		return curves;
	}
	
	////////////////////////////////////////////////////////////////////////////////////
	
	
	
	////////////////////////////////////////////////////////////////
	// Use this for initialization
	public void InitializeAgentBrain () {
		
		
		// NAV MESH AGENT TEST 
		GetComponent<NavMeshAgent>().Stop ();
		GetComponent<NavMeshAgent>().updatePosition = false;
		GetComponent<NavMeshAgent>().updateRotation = false;
		
		locomotionSystem = GetComponent<SoldierLocomotion>();
		
		// initializing neighbor lists 
		neighborObstacles =  GetComponent<NeighbourObstacles>();
		neighborObstacles.Init ();
		
		neighborAgents = GetComponent<NeighbourAgents>();
		neighborAgents.Init();
		
		currentState = new State(transform.position);
		
		if (GlobalNavigator.usingDynamicNavigationMesh)
		{
			currentPolygonIndex = GlobalNavigator.recastSteeringManager.GetClosestPolygon(currentState.getPosition());
		}
				
		if ( goal == null ) Debug.LogError("Goal has not been initialized for this agent");
		goalState = goal.goalState; 
						
		passableMask = 1; // this could be part of the goal 
		
		globalPath = new List<State> ();
		numGlobalPathWaypoints = 0;
		
		globalPathPolygonIndices = new List<uint> ();
		
		localPaths = new List<List<State>> ();
		spaceTimePaths = new List<List<State>> ();
		currentPlanExecutionIndex = 0;
		
		offMeshLinkWayPoint = new List<bool>();
		
		taskManager = new TaskManager ();
		
		// creating global navigation task 
		 globalNavigationTask = new GlobalNavigationtask(currentState,goalState,passableMask, 
			TaskPriority.RealTime, taskManager);
		globalNavigationTask.taskName = "globalNavigationTask";

		// EVENT: AgentBrain is triggered by the globalNavigationTask when GLOBAL_PATH_CHANGED
		globalNavigationTask.registerObserver(Event.GLOBAL_PATH_CHANGED, this); 
		
		
		gridNavigationTasks = new List<GridNavigationTask>();
		
		// creating grid navigation and grid time tasks for each statically allocated global waypoint 
		// -- we can statically allocate how many ever we want 
		for (int i=0; i < 1; i ++ )
		{
			_addNewEmptyGlobalPathWayPointAtIndex(i);
		}
		
		
		curves = new AnimationCurve[4];
		curves[0] = new AnimationCurve(); // x 
		curves[1] = new AnimationCurve(); // y 
		curves[2] = new AnimationCurve(); // z 
		curves[3] = new AnimationCurve(); // speed 
						
	}
	
	private void _addNewEmptyGlobalPathWayPointAtIndex (int i)
	{
		
		// we are statically allocating one grid waypoint as we are creating one corresponding grid navigation task which will watch it 
		globalPath.Add(new State());
		
		globalPathPolygonIndices.Add(0); // adding dummy polygon index
		
		offMeshLinkWayPoint.Add(false); // adding off mesh link state defaults to false 
		
		// creating the grid task 
		List<State> gridPath = new List<State> ();
		localPaths.Add(gridPath);
		
		State gridStartState = (i == 0) ? currentState : globalPath[i-1];
		
		// tunnel planner stuff 
		GridTimeDomain gridTimeDomain = new GridTimeDomain (neighborObstacles, neighborAgents);
		List<State> spaceTimePath = new List<State> ();
		spaceTimePaths.Add(spaceTimePath);
		
		GridNavigationTask gridNavigationTask = new GridNavigationTask (gridStartState, globalPath[i],2.5F, ref gridPath,
			1.0f, 1.0f, ref spaceTimePath, gridTimeDomain,				
			TaskPriority.Inactive, taskManager);
		gridNavigationTask.taskName = "gridNavigationTask:" + i.ToString();
		
		globalPath[i].notifyObservers(Event.GOAL_INVALID); // currently the goal is invalid 
		
		gridNavigationTasks.Add(gridNavigationTask);
		
		if (i==0) // first grid navigation task 
		{
			neighborAgents.observable.registerObserver(Event.NEIGBHOR_AGENTS_CHANGED, gridNavigationTask);
			neighborObstacles.observable.registerObserver(Event.NEIGBHOR_DYNAMIC_OBSTACLES_CHANGED, gridNavigationTask);
			gridNavigationTask.currentlyExecutingTask = true;
		}
		
		/*
		// creating the grid time task 
		List<State> spaceTimePath = new List<State> ();
		spaceTimePaths.Add(spaceTimePath);
		
		GridTimeDomain gridTimeDomain = new GridTimeDomain (neigbhorObstacles);
		GridTimeTunnelNavigationTask tunnelNavigationTask  = 
			new GridTimeTunnelNavigationTask(gridStartState, globalPath[i], gridPath,1.0F,1.0F,ref gridTimeDomain, ref spaceTimePath, TaskPriority.Inactive,taskManager);
		tunnelNavigationTask.taskName = "tunnelNavigationTask" + i.ToString();
		
		// EVENT: gridNavigationTask will send Event.GRID_PATH_CHANGED to tunnelNavigationTask
		gridNavigationTask.registerObserver(Event.GRID_PATH_CHANGED,tunnelNavigationTask);
		*/
		
		
	}
	
	void OnDrawGizmos ()
	{
		if ( goalReached ) return;
		
		// drawing local path(s)
		if ( drawGridPaths &&  localPaths != null)
		{
			for (int i=currentPlanExecutionIndex; i< Mathf.Min (currentPlanExecutionIndex+ numGlobalPathWaypoints, localPaths.Count); i++)
				foreach (State state in localPaths[i])
					state.drawStateGizmo (Color.green);
		}
 		
		// drawing local path(s)
		if ( drawSpaceTimePaths &&  spaceTimePaths != null)
		{
		
			
			for (int i=currentPlanExecutionIndex; i< Mathf.Min (currentPlanExecutionIndex + numGlobalPathWaypoints, spaceTimePaths.Count); i++)
			{
				foreach (State state in spaceTimePaths[i])
					state.drawStateGizmo (Color.blue);
			}
			
			//Debug.LogWarning("end tiem " + endTime + " " + curves[0].length);
			
		}
		if ( drawSpaceTimeCurve &&  spaceTimePaths != null)
			AnimationCurveHelper.DrawAnimationCurveGizmos (curves, endTime, 0.1f, Color.red);
		
		
		if (drawGlobalPath)
		{
			for (int i=0; i< numGlobalPathWaypoints; i++)
			{
				//globalPath[i].drawStateGizmoWithTextDisplay(Color.red);
				globalPath[i].drawStateGizmo (Color.red);
			}
		}
		
		if ( drawOpen ) 
			gridNavigationTasks[currentPlanExecutionIndex].gridPlanner.VisualizeContainer(ContainerType.Open,Color.blue,0.1f);
		if ( drawClosed ) 
			gridNavigationTasks[currentPlanExecutionIndex].gridPlanner.VisualizeContainer(ContainerType.Close,Color.red,0.1f);
		if ( drawPlan ) 
			gridNavigationTasks[currentPlanExecutionIndex].gridPlanner.VisualizeContainer(ContainerType.Plan,Color.green,0.1f);
		if ( drawVisited ) 
			gridNavigationTasks[currentPlanExecutionIndex].gridPlanner.VisualizeContainer(ContainerType.Visited ,Color.grey,0.1f);
		
	}
	
	// Update is called once per frame
	void Update () {
				
		
		//Debug.LogError("Agent Brain Update " + currentPlanExecutionIndex + " " + numGlobalPathWaypoints);
		
		// TODO 
		// while time is remaining 
		// pick current highest priority task and execute it 
		// evaluate the status of the task -- and depending on status, evaluate new priority and add it back.
		// TODO : can exexcute multiple tasks ? 
		
		float maxTime = 1.0F;
		
		Task task = taskManager.getHighestPriorityTask();
		if (task != null)
		{
			Debug.Log("Executing task " + task.taskName);
			task.execute (maxTime);
		}
		else
		{
			//Debug.Log ("task manager does not have task");
		}
		
		// clearing curves 
		curves[0] = new AnimationCurve();
		curves[1] = new AnimationCurve();
		curves[2] = new AnimationCurve();
		curves[3] = new AnimationCurve();
		endTime = 0.0f;
		// assigning curves 
		
		//Debug.LogError("num global points " + numGlobalPathWaypoints + " num space time paths " + spaceTimePaths.Count + " grid tasks " + gridNavigationTasks.Count);
		for (int i=currentPlanExecutionIndex; i< Mathf.Min (currentPlanExecutionIndex + numGlobalPathWaypoints, spaceTimePaths.Count); i++)
		{
			// TODO CHECK HERE IF THE SPACE TIME PLAN IS CURRENTLY VALID OR NOT 
			if (offMeshLinkWayPoint[i] == true ) // this one is an off mesh link -- does not have a space time path 
			{
				spaceTimePaths[i].Clear();
				// TODO GET MOST RECENT POSITION FOR BETTER VIEWING 
				spaceTimePaths[i].Add(gridNavigationTasks[i].startState);
				spaceTimePaths[i].Add(gridNavigationTasks[i].goalState);
			}
			else if ( gridNavigationTasks[i].spaceTimePathStatus == false) // not an off mesh link and does not have a path 
				break;
			
			endTime = AnimationCurveHelper.GetPlanAnimationCurve(spaceTimePaths[i],curves);
			
			//Debug.LogError("we populated the curve till " + endTime);
		}
		
		
		// TODO CHECK HERE IF WE ARE CURRENTLY CONTROLLED BY THE MESH LINK 
		if (moveAutomatically == true)
		{
			if (stop == false) 
				ExecuteNextAction(Time.deltaTime);
			else
				currentState._time += Time.deltaTime; // THIS HAPPENS WHEN OFF MESH LINK HAS CONTROL 
		}
		else if (Input.GetKeyDown(KeyCode.A))
			ExecuteNextAction (0.2f);
		
		
		// monitoring positions of agents in the polygon dictionary 
		if (GlobalNavigator.usingDynamicNavigationMesh)
		{
			currentPolygonIndex = CentralizedManager.UpdatePolygonDictionary(currentState.getPosition(),currentPolygonIndex, null);
		}
		
		
		if ( text != null) 
		{
			string t = "";
			t = t + "global path " + numGlobalPathWaypoints.ToString() + "\n";
			t = t + "Current plan execution index : " + currentPlanExecutionIndex.ToString() + "\n";
			t  = t + "current state " + currentState.getPosition().ToString() + " " + currentState._time.ToString() + " " + currentState._speed.ToString() + " \n";
			for (int i=0; i < gridNavigationTasks.Count; i++)
			{
				t = t + offMeshLinkWayPoint[i].ToString() + " grid task " + i.ToString() + " " + gridNavigationTasks[i].taskPriority.ToString() + " " + 
					gridNavigationTasks[i].startState.getPosition().ToString() + " " + gridNavigationTasks[i].startState._time.ToString() + " " + 
					gridNavigationTasks[i].goalState.getPosition().ToString() + " " + gridNavigationTasks[i].goalState._time.ToString() + " " + 
					localPaths[i].Count.ToString() + " " + spaceTimePaths[i].Count.ToString() + "\n";
			}
			
		text.text = t;
		}
		
		// trying text mesh 
		int timeLeftForGlobalGoal = Mathf.RoundToInt(goalState._time - currentState._time);
		
		int timeLeftForCurrentWaypoint;
		if ( goalReached == false)
			timeLeftForCurrentWaypoint = Mathf.RoundToInt(gridNavigationTasks[currentPlanExecutionIndex].goalState._time - currentState._time);
		else
			timeLeftForCurrentWaypoint = 0;
		
		textMesh.text = timeLeftForGlobalGoal.ToString() + "/" + timeLeftForCurrentWaypoint.ToString();
		
	
	}
	
	public void ExecuteNextAction (float timeDelta)
	{
		// get next state to move to 
		/*
		 * here we can get the next state that is 'visible' that we can directly steer towards 
		 * but ... that would break our space-time path thing if we make a beeline for a path
		 * might need to follow space-time points --- man this is going to be a problem -- would be too jaggy (how do we keep ability to maintain momentum 
		 * 
		 * */
		
		// this should prob go in Update -- then u can monitor if the goal changes -- things restart 
		
		float currentTime = currentState._time;
		float nextTime = currentTime + timeDelta;
		
		currentState._time = nextTime; // MOVING TIME FORWARD WHENEVER THIS FUNCTION IS CALLED 
		
		if (goalReached) return; 
		
		// TODO -- FIRST FEW FRAMES IN AUTO UPDATE -- NO PLAN EXISTS 
		if (numGlobalPathWaypoints == 0) return;
				
		if ( offMeshLinkWayPoint[currentPlanExecutionIndex] == true )
			Debug.LogError(" WE ARE ON AN OFF MESH LINK");
		
		
		if ( nextTime > endTime ) 
		{
			Debug.LogError("CURVE DOES NOT EXIST");
			locomotionSystem.agentSpeed -= 0.1f;
			if ( locomotionSystem.agentSpeed < 0.0f) locomotionSystem.agentSpeed = 0.0f;
			return;
		}
		
		locomotionSystem.MoveAlongAnimationCurves(nextTime);
			
		Vector3 nextPos = transform.position;
		
		Debug.LogWarning("next state to move to " + nextPos + " " + nextTime);
		
		//currentState._speed = (nextPos - currentState.getPosition()).magnitude / 0.2f;

		currentState.setPosition(nextPos);
		
		transform.position = currentState.getPosition ();

		// checking if current task completed execution 
		if ( gridNavigationTasks[currentPlanExecutionIndex].taskPriority == TaskPriority.Complete )
		{
			_MoveToNextTaskForExecution ();
		}
		
		
	}
		
	private void _MoveToNextTaskForExecution ()
	{
		// we  finished executing current plan 
		currentPlanExecutionIndex = currentPlanExecutionIndex + 1;
		
		globalPath.RemoveAt(0); 
		// we can remove the grid task, the local path and the space time path 
		
		numGlobalPathWaypoints --;
		
		if (numGlobalPathWaypoints == 0)
		{
			Debug.LogWarning("REACHED GOAL or reached end of all calculated space time paths -- cannot execute any more");
			goalReached = true; 
			return; 
		}
		
		gridNavigationTasks[currentPlanExecutionIndex-1].currentlyExecutingTask = false;
		gridNavigationTasks[currentPlanExecutionIndex].currentlyExecutingTask = true;
		
		// WE UNREGISTER THE PREVIOUS TASK FROM THE NEIGHBOR AGENTS AND REGISTER THE CURRENT ONE 
		neighborAgents.observable.unregisterObserver(Event.NEIGBHOR_AGENTS_CHANGED, gridNavigationTasks[currentPlanExecutionIndex-1]);
		neighborObstacles.observable.unregisterObserver(Event.NEIGBHOR_DYNAMIC_OBSTACLES_CHANGED, gridNavigationTasks[currentPlanExecutionIndex-1]);
		// WE REGISTER THE CURRENT ONE 
		neighborAgents.observable.registerObserver(Event.NEIGBHOR_AGENTS_CHANGED, gridNavigationTasks[currentPlanExecutionIndex]);
		neighborObstacles.observable.registerObserver(Event.NEIGBHOR_DYNAMIC_OBSTACLES_CHANGED, gridNavigationTasks[currentPlanExecutionIndex]);
		
		// TODo: NOT SURE IF THIS IS CORREECT unregister with its old goal  for previous task
		globalPath[0].unregisterObserver(Event.STATE_POSITION_CHANGED,gridNavigationTasks[currentPlanExecutionIndex-1]);
		
		// JUST MANUALLY SENDING IT A START STATE CHANGED EVENT 
		Vector3[] data = new Vector3[2];
		data[0] = gridNavigationTasks[currentPlanExecutionIndex].startState.getPosition();
		data[1] = currentState.getPosition();
		gridNavigationTasks[currentPlanExecutionIndex].notifyEvent(Event.STATE_POSITION_CHANGED,data);
		
		// IMP :: we need to make the next task monitor the current start state now + unregister to previous start state 
		gridNavigationTasks[currentPlanExecutionIndex].startState = currentState;
		
		currentState.registerObserver(Event.STATE_POSITION_CHANGED, gridNavigationTasks[currentPlanExecutionIndex]);
		 
		gridNavigationTasks[currentPlanExecutionIndex].notifyEvent(Event.CURRENT_EXECUTING_TASK,null);
		
	}
	
	public void notifyEvent (Event ev, System.Object data)
	{
		switch (ev)
		{
		case Event.GLOBAL_PATH_CHANGED:
			
			Debug.Log ("the global path has changed, we must update the path");  
			
			// removing the agent from the polygons associated with the old path in the centralized manager 
			if ( GlobalNavigator.usingDynamicNavigationMesh )
			{
				for (int i=0; i < numGlobalPathWaypoints; i++)
				{
					CentralizedManager.polygonDictionary[globalPathPolygonIndices[i]].agents.Remove(this);
				}
			}
			
			// probably make this an event handler 
			if (GlobalNavigator.pathStatus != NavMeshPathStatus.PathInvalid)
			{
				// TODO :: do some checking how much plan has changed 
				// ALSO, we could do some filtering here maybe --- sometimes it gives points which are very close to each otehr 
				
				Debug.Log("number of points in nav mesh path " + GlobalNavigator.numberOfNodes);
				
				// we dont want to put in the start state, so starting from 1 
				int currentGlobalPathWaypoint = 0;
				
				
				for (int i=1; i < GlobalNavigator.numberOfNodes; i++)
				{
						
					// should we round off here ...  we prob want to round of when we give it to the discrete planner 
					// TODO this rounding off might sometimes place it on an obstacle 
					// either we make hte nav mesh more conservative (give it 0.5 boundary around obstracles) or do some check here
					
					Vector3 p = GlobalNavigator.path[i];
					
					// check for off mesh link before rounding off  -- only using x-z values 
					// using previous point to determin because the dictionary keys in using the start point -- NOT ANY MORE 
					bool offMeshLinkState = CentralizedManager.IsOffMeshLink(GlobalNavigator.path[i]);
															
					p.x = Mathf.Round(p.x);
					p.y = Mathf.Round(p.y); 
					p.z = Mathf.Round(p.z);
					
					// TODO : maybe off mesh link has its own time computation 
					float time =  (GlobalNavigator.pathCost[i] / GlobalNavigator.totalPathCost) * (goalState._time - currentState._time);
					
					// we ignore points that are the same as the previos point in the nav mesh path 
					if ( currentGlobalPathWaypoint !=0 && globalPath[currentGlobalPathWaypoint-1].positionEquals(p) )
						continue;
					
					
					// todo check for automatic offmesh link as well ... maybe we will just create all of them manually 
					
					
					if ( currentGlobalPathWaypoint < globalPath.Count ) 
					{
						// some global path nodes have been statically allocated 
						if ( currentGlobalPathWaypoint >= numGlobalPathWaypoints && offMeshLinkState == false)
						{
							// we are now introducing more waypoints than before -- we need to make these valid 
							globalPath[currentGlobalPathWaypoint].notifyObservers(Event.GOAL_VALID);
						}
						
					}
					else
					{
						_addNewEmptyGlobalPathWayPointAtIndex(currentGlobalPathWaypoint);
						
						if (offMeshLinkState == false)
							globalPath[currentGlobalPathWaypoint].notifyObservers(Event.GOAL_VALID);

					}
					
					// TODO: here check if the position actually changed -- else no need to call setPosition -- which triggers goal change event 
					globalPath[currentGlobalPathWaypoint].setPosition(p);
					
					// adding time computation 
					// TODO : REMOVE TIME.TIME 
					if ( currentGlobalPathWaypoint == 0 )
						globalPath[currentGlobalPathWaypoint]._time = currentState._time + time;
					else
						globalPath[currentGlobalPathWaypoint]._time = globalPath[currentGlobalPathWaypoint-1]._time + time;
					
					// TODO COMPUTE SPEED 
					
					
					// storing offmesh link state 
					if (offMeshLinkState == true) // this is an offlink
					{
						globalPath[currentGlobalPathWaypoint].notifyObservers(Event.GOAL_INVALID);
					}
					
					// setting off mesh link 
					offMeshLinkWayPoint[currentGlobalPathWaypoint] = offMeshLinkState;
					
					if (GlobalNavigator.usingDynamicNavigationMesh) 
					{
						globalPathPolygonIndices[currentGlobalPathWaypoint] = GlobalNavigator.recastSteeringManager.GetClosestPolygon(p);
						
						// adding agent to the respective polygon in the centralized manager 
						if (CentralizedManager.polygonDictionary.ContainsKey(globalPathPolygonIndices[currentGlobalPathWaypoint]) == false)
							CentralizedManager.polygonDictionary.Add(globalPathPolygonIndices[currentGlobalPathWaypoint], new PolygonData());
						
						CentralizedManager.polygonDictionary[globalPathPolygonIndices[currentGlobalPathWaypoint]].agents.Add(this);
						
					}					
					
					currentGlobalPathWaypoint ++;
					
				}
				
				Debug.LogWarning("current " + currentGlobalPathWaypoint + " prev " + numGlobalPathWaypoints );
				
				for ( int i= currentGlobalPathWaypoint; i < numGlobalPathWaypoints; i++)
				{
					// this loop is iterating through all states in globalPath which are currently inactive 
					globalPath[i].notifyObservers(Event.GOAL_INVALID);
				}
				
				numGlobalPathWaypoints = currentGlobalPathWaypoint;
								
			}
			else 
			{
				Debug.LogError("Path is invalid for agent"); // this does happen when the goal is on an obstacle 
			}
			
			break;
			
		}
	}	
}
