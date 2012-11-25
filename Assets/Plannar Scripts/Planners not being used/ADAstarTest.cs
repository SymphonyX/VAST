using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


public class ADAstarState : DefaultState
{
	public ADAstarState(Vector3 st)
	{ state = st; }
	
	public Vector3 state;
}

class ADAstarAction : DefaultAction
{ 
	public Vector3 direction;
}

class ADAstarDomain : PlanningDomainBase
{
	public override bool isAGoalState (ref DefaultState state, ref DefaultState idealGoalState)
	{
		ADAstarState currentState = state as ADAstarState;
		ADAstarState goalState = idealGoalState as ADAstarState;
		return (Vector3.Distance(currentState.state, goalState.state) < 1);	
	}
	
	public override float evaluateDomain (ref DefaultState state)
	{
		return 1.0f;
	}
	
	public override float ComputeEstimate (ref DefaultState _from, ref DefaultState _to, string estimateType)
	{
		
		//The could could be implemented differently. For testing, using the same logis for both the difference would be
		//in the parameters given
		if(estimateType == "g")
		{
			return (Vector3.Distance((_from as ADAstarState).state, (_to as ADAstarState).state));
		}
		else if(estimateType == "h")
		{
			return (Vector3.Distance((_from as ADAstarState).state, (_to as ADAstarState).state));
		}
		else 
			return 0.0f;
	}
	
	public override void generateTransitions (ref DefaultState currentState, ref DefaultState previousState, ref DefaultState idealGoalState, ref List<DefaultAction> transitions)
	{
		List<Vector3> moves = new List<Vector3>();
		moves.Add(new Vector3(1.0f, 0.0f, 0.0f));
		moves.Add(new Vector3(-1.0f, 0.0f, 0.0f));
		moves.Add(new Vector3(0.0f, 0.0f, 1.0f));
		moves.Add(new Vector3(0.0f, 0.0f, -1.0f));
		moves.Add(new Vector3(1.0f, 0.0f, 1.0f));
		moves.Add(new Vector3(-1.0f, 0.0f, 1.0f));
		moves.Add(new Vector3(1.0f, 0.0f, -1.0f));
		moves.Add(new Vector3(-1.0f, 0.0f, -1.0f));
		
		
		ADAstarState curState = currentState as ADAstarState;
		
		Vector3 obs = new Vector3(3.0f, 0.5f, 0.0f);
		
		
		foreach(Vector3 move in moves)
		{
			if(!(curState.state+move).Equals(obs))
			{
			ADAstarAction action =  new ADAstarAction();
			action.cost = 1;
			action.direction = move;
			ADAstarState st = new ADAstarState(curState.state + move);
			action.state = st;
			transitions.Add(action);
			}
			
		
		}	
	}
	
	
	public override void generatePredecesors (ref DefaultState currentState, ref DefaultState previousState, ref DefaultState idealGoalState, ref List<DefaultAction> transitions)
	{
		
		List<Vector3> moves = new List<Vector3>();
		moves.Add(new Vector3(1.0f, 0.0f, 0.0f));
		moves.Add(new Vector3(-1.0f, 0.0f, 0.0f));
		moves.Add(new Vector3(0.0f, 0.0f, 1.0f));
		moves.Add(new Vector3(0.0f, 0.0f, -1.0f));
		moves.Add(new Vector3(1.0f, 0.0f, 1.0f));
		moves.Add(new Vector3(-1.0f, 0.0f, 1.0f));
		moves.Add(new Vector3(1.0f, 0.0f, -1.0f));
		moves.Add(new Vector3(-1.0f, 0.0f, -1.0f));
		
		
		ADAstarState curState = currentState as ADAstarState;
		
		Vector3 obs = new Vector3(3.0f, 0.5f, 0.0f);
		
		foreach(Vector3 move in moves)
		{
			if(!(curState.state-move).Equals(obs))
			{
				ADAstarAction action =  new ADAstarAction();
				action.cost = 1;
				action.direction = -move;
				ADAstarState st = new ADAstarState(curState.state - move);
				action.state = st;
				transitions.Add(action);	
			}
			
			
		}
	}
	
	public override bool equals(DefaultState s1, DefaultState s2, bool isStart)
	{
		if((s1 as ADAstarState).state == (s2 as ADAstarState).state)
			return true;
		else
			return false;	
	}
	
		
}

public class ADAstarTest : MonoBehaviour {
	
	public GameObject startObject, goalObject;
	ADAstarAction action;
	//ADAstarState startState, goalState;
	bool moving = false;
	
	public static Stack<DefaultAction> outputPlan;
	ADAstarPlanner planner;
	bool finished = false;
	List<PlanningDomainBase> domainList;
	DefaultState DStartState;
	DefaultState DGoalState;
	
	void Awake() {
		
		//startState = new ADAstarState(startObject.transform.position);
		//goalState = new ADAstarState(goalObject.transform.position);

	}
	
	
	// Use this for initialization
	void Start () {
		
		
		outputPlan = new Stack<DefaultAction>();
		
		domainList = new List<PlanningDomainBase>();
		domainList.Add(new ADAstarDomain());
		
		planner = new ADAstarPlanner();
		planner.init(ref domainList, 100);
		
		DStartState = ADAexecution.startState as DefaultState;
		DGoalState = ADAexecution.goalState as DefaultState;
		
		
		planner.InitializeValues(ref DStartState, ref DGoalState, 2.5f, ref outputPlan, 
		                         PlannerMode.PLANNING_MODE.IncreaseFactor, .2f, 0.0f);
		
		
		Debug.Log(ADAstarPlanner.Closed.Count);
		
		Debug.Log("Finished");
		
	//	foreach(Edge edge in planner.edgeList.Values)
	//	{
	//		Debug.Log("Edge: u: " + (edge.u as ADAstarState).state);
	//		Debug.Log("Edge: v: " + (edge.v as ADAstarState).state);	
	//	}
	
	}

	
	// Update is called once per frame
	void Update () {

		if(ADAexecution.startState.state != startObject.transform.position){
			ADAstarPlanner.startFound = false;
			ADAexecution.startState.state = startObject.transform.position;
			DStartState = ADAexecution.startState as DefaultState;
		}
		if(ADAexecution.goalState.state != goalObject.transform.position){
			ADAstarPlanner.startFound = false;
			ADAexecution.goalState.state = goalObject.transform.position;
			DGoalState = ADAexecution.goalState as DefaultState;
		}
		
			
		if(ADAstarPlanner.startFound == false){
			planner.InitializeValues(ref DStartState, ref DGoalState, 2.5f, ref outputPlan, 
			                         PlannerMode.PLANNING_MODE.IncreaseFactor, .2f, 0.0f);			
			}
		//if(!ADAexecution.startState.state.Equals(ADAexecution.goalState.state)){
		//	PlanningDomainBase domain = planner._planningDomain[0];
		//	planner.inifiniteUpdate(ref planner.edgeList, ref domain, 
		//	                        ref planner.Closed,ref outputPlan);
		//}
	}
}
