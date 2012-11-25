using UnityEngine;
using System.Collections;

// Component to compute the waypoints to reach the goal using the Navmesh and ADAPT

public class NavMeshWayPoints : MonoBehaviour {
			
	public int MaxNumberOfWaypoints = 32;	
	
	//private FootstepPlanningTest planning;
	private Planner planning;
	
	public bool debugDraw = false;
	
	[HideInInspector] public bool initialized;	
	
	private SteeringManager steering;
	
	private Vector3[] path = new Vector3[32];				
	[HideInInspector] public int numPoints = 0;
	
	private int currentWaypoint = 0;
	
	private Vector3 start;
	private Vector3 end;
	
	private int navMeshLayer;
	
	void Awake(){
		initialized = false;		
	}
	
	// Use this for initialization
	//public void Init (FootstepPlanningTest planner, SteeringManager steeringManager, AnimationAnalyzer analyzer, NeighbourAgents agents, NeighbourObstacles obstacles, RootMotionComputer computer) {
	public void Init (Planner planner, SteeringManager steeringManager, AnimationAnalyzer analyzer, NeighbourAgents agents, NeighbourObstacles obstacles) {
		
		steering = steeringManager;
		
		planning = planner;
		if (planning != null && !planning.initialized)
			planning.Init(analyzer, agents, obstacles);
			
		navMeshLayer = 1 << LayerMask.NameToLayer("StaticWorld");
		
		initialized = true;
		
	}
	
	public void ComputeWayPoints()
	{
		path = new Vector3[32];
		numPoints = 0;
		
		if (steering != null)
		{
			start = planning.currentStateTransform.position;
			end = planning.goalStateTransform.position;							
			numPoints = steering.FindPath(start,end,path,MaxNumberOfWaypoints);
			//Debug.Log("Path found? " + numPoints);							
		}
				
		if (numPoints > 0)
		{
			if (numPoints == 1)
				return;
			else
			{
				currentWaypoint = 1;
				planning.currentGoal = path[currentWaypoint];	
			}			
		}
	}
	
	public bool NextWayPointVisible()
	{
		Vector3 nextWaypoint;
		if (currentWaypoint+1 < numPoints-1)
			nextWaypoint = path[currentWaypoint+1];			
		else
			nextWaypoint = planning.goalStateTransform.position;
		
		Vector3 toNextWaypoint = nextWaypoint - planning.currentStateTransform.position;
		
		bool hit = Physics.Raycast(planning.currentStateTransform.position,toNextWaypoint,toNextWaypoint.magnitude,navMeshLayer);
		
		return !hit;
	}
	
	public void NextWaypoint()
	{
		currentWaypoint++;
		if (currentWaypoint < numPoints-1)
			planning.currentGoal = path[currentWaypoint];	
		else
			planning.currentGoal = planning.goalStateTransform.position;
	}	
	
	// Update is called once per frame
	void Update () {
	
		if (debugDraw)
			DrawPath();
		
	}
	
	private void DrawPath()
	{
		if (numPoints < 2)
			return;		
			
		Vector3 posA = start;//path[0];
		Vector3 posB = path[1];
		
		//float length = 0.0f;
		
		for(int i=2; i<numPoints; i++)
		{
			if (currentWaypoint == i-1)
				Debug.DrawLine(posA,posB,Color.yellow);
			else if (currentWaypoint > i-1)
				Debug.DrawLine(posA,posB,Color.green);
			else
				Debug.DrawLine(posA,posB,Color.red);
			
			//length += (posB-posA).magnitude;
			
			posA = posB;
			posB = path[i];
		}
		
		if (currentWaypoint == numPoints-1)
			Debug.DrawLine(posA,end,Color.yellow);	
		else if (currentWaypoint < numPoints-1)
			Debug.DrawLine(posA,end,Color.red);
		else
			Debug.DrawLine(posA,end,Color.green);
		
		//length += (posB-posA).magnitude;	
		//Debug.Log("At time " + Time.time + " Waypoints path length = " + length);
	}	
	
}
