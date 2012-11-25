using UnityEngine;
using System.Collections;
using System.Collections.Generic;

class FringePlanningAction : DefaultAction
{
	public Vector3 direction;
}

class FringePlanningState : DefaultState
{
	public FringePlanningState(Vector3 st)
	{ state = st;}
	public Vector3 state;
}

class FringePlanningDomain : PlanningDomainBase
{
	public float x_min, x_max, z_min, z_max;
	public void setBoundaries(float _x_min, float _x_max, float _z_min, float _z_max)
	{
		x_min=_x_min; x_max=_x_max; z_min=_z_min; z_max=_z_max;
	}
	
	public override bool isAGoalState (ref DefaultState state, ref DefaultState idealGoalState)
	{
		return (Vector3.Distance((state as FringePlanningState).state, (idealGoalState as FringePlanningState).state) < .5);	
	}
	
	public override float evaluateDomain (ref DefaultState state)
	{
		return 1;
	}
	
	public override float estimateTotalCost (ref DefaultState currentState, ref DefaultState idealGoalState, float currentg)
	{
		float h = Vector3.Distance((currentState as FringePlanningState).state, (idealGoalState as FringePlanningState).state);
		float f = currentg + h;
		return f;
	}
	
	public override void generateTransitions (ref DefaultState currentState, ref DefaultState previousState, ref DefaultState idealGoalState, ref List<DefaultAction> transitions)
	{
		List<Vector3> moves = new List<Vector3>();
		moves.Add(new Vector3(1.0f, 0.0f, 0.0f));
		moves.Add(new Vector3(-1.0f, 0.0f, 0.0f));
		moves.Add(new Vector3(0.0f, 0.0f, 1.0f));
		moves.Add(new Vector3(0.0f, 0.0f, -1.0f));
		
		FringePlanningState curState = currentState as FringePlanningState;
		
		foreach(Vector3 move in moves)
		{
			if((move + curState.state).x < x_max && (move + curState.state).x >= x_min &&
			   (move + curState.state).z < z_max && (move + curState.state).z >= z_min)
			{
				FringePlanningAction action =  new FringePlanningAction();
				action.cost = 1;
				action.direction = move;
				FringePlanningState st = new FringePlanningState(curState.state + move);
				action.state = st;
				transitions.Add(action);	
			}
		}
	}
}

class FringeSearchTest : MonoBehaviour {
	
	public GameObject startObject, goalObject;
	Vector3 start, goal;
	FringePlanningState startState, goalState;
	public Stack<DefaultAction> outputPlan;
	public FringeSearchPlanner planner;
	List<PlanningDomainBase> domainList;
	float xmin,xmax,zmin,zmax;
	
	Stack<FringePlanningAction> st;
	
	// Use this for initialization
	void Start () {
		start =  startObject.transform.position;
		goal = goalObject.transform.position;
		
		startState = new FringePlanningState(start);
		goalState = new FringePlanningState(goal);
		
		
		
		outputPlan = new Stack<DefaultAction>();
		planner = new FringeSearchPlanner(400);
		
		planner.Cache = new Dictionary<DefaultState, FringeSearchNode>(planner._capacity);
		initCache(ref planner.Cache);
		domainList = new List<PlanningDomainBase>();
		
		FringePlanningDomain domain = new FringePlanningDomain();
		domain.setBoundaries(xmin,xmax,zmin,zmax);
		domainList.Add(domain);
		
		planner.init( ref domainList, -1);
		
		DefaultState DcurrentState = startState as DefaultState;
		DefaultState DgoalState = goalState as DefaultState;
		
		bool planComputed = planner.computePlan(ref DcurrentState, ref DgoalState, ref outputPlan, -1);
		
		Debug.Log(planComputed);
		Debug.Log(outputPlan.Count);	
		
		st = new Stack<FringePlanningAction>();
		
		while(outputPlan.Count != 0)
		{
			st.Push(outputPlan.Pop() as FringePlanningAction);
		}
		
		Debug.Log(st.Count);
		
	}
	
	// Update is called once per frame
	void Update () {
		
		if(st.Count != 0)
			startObject.transform.Translate(st.Pop().direction);
	
	}
	
	void initCache(ref Dictionary<DefaultState, FringeSearchNode> _cache)
	{
		//Dummy Vector3
		Vector3 statePosition = new Vector3(0.0f, start.y, 0.0f);
		
		xmin = start.x-10; xmax = start.x+10;
		zmin = start.z-10; zmax = start.z+10;
		
		for(int i=0; i < 20; ++i)
		{
			statePosition.x = (start.x-10)+i; 
			for(int j=0; j < 20; ++j)
			{
				statePosition.z = (start.z-10)+j;
				FringePlanningState state = new FringePlanningState(statePosition);
				//DefaultState st = state as DefaultState;
				_cache.Add(state, null);
			}
		}
		
		
	}
}
