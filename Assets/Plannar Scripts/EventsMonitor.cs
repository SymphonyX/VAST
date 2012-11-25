using UnityEngine;
using System.Collections;

// Component charged of monitoring other components
// and other variables like time, in order to decide
// wheter to recompute or not the plan
// It also checks the goal and changes the waypoints
// obtained from the NavMesh 

public class EventsMonitor : MonoBehaviour {
	
	//private FootstepPlanningTest planning;	
	private Planner planning;
	private AnimationEngine engine;
	private CollisionReaction collision;
	private NavMeshWayPoints waypoints;
	private NeighbourAgents neighborhood;
	private NeighbourObstacles nObstacles;
	private PlaceFootSteps footsteps;
	
	// maximum number of actions to execute from the same
	// plan before recomputing the plan
	public int actionsToRecompute = 3;
	
	// maximum number of seconds we allow a plan to be executed
	// without recomputing it 
	public int timeToRecompute = 10;
	
	private bool initialized;
	
	private float timeSinceLastPlan;
	private float timeSinceLastWaypoints;
	
	private Vector3 previousGoalPosition;
	private Vector3 previousCurrentGoalPosition;
	
	private int previousActionsSinceLastPlan;
	
	private bool newWaypoint;
	
	public bool useWaypoints = true;
	
	void Awake()
	{
		initialized = false;
	}	
	
	//public void Init(AnimationAnalyzer analyzer, FootstepPlanningTest planner, 
	public void Init(AnimationAnalyzer analyzer, Planner planner, 
	                 AnimationEngine animEngine, CollisionReaction collisionReact, 
	                 NavMeshWayPoints navMeshWaypoints, SteeringManager steering,
	                 NeighbourAgents agents, NeighbourObstacles obstacles,
	                 PlaceFootSteps steps)
	{		
		planning = planner;	
		if (planning != null && !planning.initialized)
			planning.Init(analyzer,agents,obstacles);
		
		engine = animEngine;
		if (engine != null && !engine.initialized)
			engine.Init(analyzer,planning,agents,obstacles);
		
		collision = collisionReact;
		if (collision != null && !collision.initialized)
			collision.Init(analyzer,planner,engine,agents,obstacles);
		
		waypoints = navMeshWaypoints;
		//if (waypoints != null && !waypoints.initialized)
		//	waypoints.Init(planning,steering,analyzer,agents,obstacles);
		
		neighborhood = agents;
		if (neighborhood != null && !neighborhood.initialized)
			neighborhood.Init();
		
		footsteps = steps;
		if (footsteps != null && !footsteps.initiated)
			footsteps.Init(analyzer,planner,engine,neighborhood,obstacles);
		
		nObstacles = obstacles;
		if (nObstacles != null && !nObstacles.initialized)
			nObstacles.Init();
		
		timeSinceLastPlan = 0;
		timeSinceLastWaypoints = 0;
		
		previousGoalPosition = planning.goalStateTransform.position;
		previousCurrentGoalPosition = planning.currentGoal;
		
		previousActionsSinceLastPlan = -1;
		
		newWaypoint = false;
		
		initialized = true;
	}
	
	
	// Update is called once per frame
	void Update () {
	
		if (!initialized)
			return;
			
		timeSinceLastPlan += Time.deltaTime;
		timeSinceLastWaypoints += Time.deltaTime;
		
		bool thereIsCollision = false; //CollisionDetected();		
		
		// If we are not reacting to a collision and there is a collision detected		
		if (!collision.reacting && thereIsCollision)
		{
			//Debug.Log("Collision detected at time " + Time.time);
			
			// We react and recompute the plan
			collision.React();
			RecomputePlan();			
		}
		else // if there is not a collision	and we are not allready reacting to one	
		{		
				// We perform different checks 
			
				bool endGoalMoved = EndGoalMoved();
				bool currentGoalMoved = CurrentGoalMoved();

			
			if (useWaypoints)
			{
				if ( 
				    (waypoints.numPoints == 0 && !planning.isPlanComputed)
				    || (LimitedSecondsElapsed(timeSinceLastWaypoints) || endGoalMoved)
				    )
					RecomputeWaypoints();
				else if (!LastWayPoint() && (NextWayPointVisible() ||  WaypointReached()) )
					NextWaypoint();	
			}
			
					bool lastWayPoint = LastWayPoint();
					
					bool limitedActionsPlayed = LimitedActionsPlayed();
					
					bool hasNewNeighbor = HasNewNeighbor();
					bool hasNewObstacle = HasNewObstacle();
						
			if (
			    (planning.IsEmpty() || planning.HasExpired() && planning.ThereStillTime() ) && !planning.isPlanComputed || //if the plan is empty (or executed) or not computed
			    ( useWaypoints && newWaypoint ) || // or if we changed the waypoint
			    //CollisionDetected() || // or if we have detected a collision
			    limitedActionsPlayed // or if we have executed the maximum number of actions without changing the plan
			    || LimitedSecondsElapsed(timeSinceLastPlan) // or if the maximum number of seconds has elapsed
			    || hasNewNeighbor // or if we have detected a new agent
			    || hasNewObstacle // or if we have detected a new dynamic obstacle
			    || (useWaypoints && lastWayPoint && currentGoalMoved) // or if we are going to the final goal and it has moved
			    || (!useWaypoints && (endGoalMoved || currentGoalMoved))	 // or if the current goal has moved
			)
			{
				if (!useWaypoints)
					planning.currentGoal = planning.goalStateTransform.position;
				
				// If any of the previous conditions is true, we recompute the plan
				RecomputePlan();
			}
			else 
			{				
				if (collision.reacting && !thereIsCollision) // if we are reacting to a collision and there is not a collision anymore
				{
					// we stop reacting and recompute the plan
					
					collision.reacting = false;	
					RecomputePlan();					
				}				
				else // if we are reacting and there still a collision we do nothing (we keep reacting)
					planning.planChanged = false;
				
			}
			
			
		
		}
		
		if (engine != null)
			/// we call the update of the animation engine, in order to execute the action and play the animations
			engine.AnimationEngineUpdate();
		
	}	
	
	void LateUpdate(){
		
		if (engine != null)
			engine.AnimationEngineLateUpdate();
		
	}
	
	/*
	void LateUpdate()
	{
		footsteps.ExecutePlaceFootSteps();	
	}
	*/
	
	private void RecomputeWaypoints()
	{
		waypoints.ComputeWayPoints();
		
		timeSinceLastWaypoints = 0;
			
		newWaypoint = true;
	}		
		
	private void RecomputePlan()
	{		
		planning.RecomputePlan();
		
		timeSinceLastPlan = 0;
		
		if (engine != null)
			engine.actionsSinceLastPlan = 0;	
		
		newWaypoint = false;
		
		SendMessage("RetrieveAnimationCurves");
	}
	
	private void NextWaypoint()
	{
		waypoints.NextWaypoint();
		newWaypoint = true;
	}
	
	private bool HasNewNeighbor()
	{
		bool b = neighborhood.newNeighbor;
		neighborhood.newNeighbor = false;
		
		return b;
	}	
	
	private bool HasNewObstacle()
	{
		bool b = nObstacles.newObstacle;
		nObstacles.newObstacle = false;
		
		return b;
	}
	
	private bool LastWayPoint()
	{
		return planning.currentGoal == planning.goalStateTransform.position;		
	}
		
	private bool WaypointReached()
	{
		return !newWaypoint && planning.isPlanComputed && planning.IsEmpty();			
	}
	
	private bool NextWayPointVisible()
	{
		return waypoints.NextWayPointVisible();
	}
	
	private bool LimitedActionsPlayed()
	{
		bool actionsPlayed = false;
		
		if (engine != null)
		{
			if (engine.actionsSinceLastPlan != previousActionsSinceLastPlan)
			{			
				if ( planning.IsEmpty() && !planning.isPlanComputed || engine.actionsSinceLastPlan > 0 && (engine.actionsSinceLastPlan % actionsToRecompute) == 0)
					actionsPlayed = true;
			}
		
			previousActionsSinceLastPlan = engine.actionsSinceLastPlan;
		}
		
		return actionsPlayed;
	}	
	
	private bool LimitedSecondsElapsed(float time)
	{
		return ( time > timeToRecompute );
	}	
	
	private bool EndGoalMoved()
	{
		bool moved = false;
		
		if (planning.goalStateTransform.position != previousGoalPosition)
			moved = true;
		
		previousGoalPosition = planning.goalStateTransform.position;	
		
		return moved;
	}
	
	private bool CurrentGoalMoved()
	{
		bool moved = false;
		
		if (planning.currentGoal != previousCurrentGoalPosition)
			moved = true;
		
		previousCurrentGoalPosition = planning.currentGoal;		
		
		return moved;
	}
	
	private bool CollisionDetected()
	{		
		return (
		        collision.FrameCollisionCheck() 
		        ||
		        collision.CurrentActionCollisionCheck()			 	
			 );
	}
}
