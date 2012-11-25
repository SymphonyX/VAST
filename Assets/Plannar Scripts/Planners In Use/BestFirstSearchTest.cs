using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

public class BestFirstState : DefaultState, IEquatable<BestFirstState>
{
	public BestFirstState(Vector3 st)
	{ state = st; }
	
	public override int GetHashCode ()
	{
		return 1;
	}
	
	public override bool Equals(object obj)
	{
		return Equals(obj as BestFirstState);
	}
	
	public bool Equals(BestFirstState obj)
	{
		return obj!=null && obj.state == this.state;
	}
	
	public override Vector3 statePosition()
	{
		return state;	
	}
	
	public Vector3 state;
	
}

class BestFirstAction : DefaultAction 
{ 
	public BestFirstAction(){}
	public BestFirstAction(DefaultState _from, DefaultState _to) 
	{ 
		BestFirstState Afrom = _from as BestFirstState; 
		BestFirstState Ato = _to as BestFirstState;
		
		Vector3 dir = new Vector3(Ato.state.x - Afrom.state.x, Ato.state.y - Afrom.state.y, Ato.state.z - Afrom.state.z);
		direction = dir; 
		cost = Vector3.Distance(Afrom.state, Ato.state);
		state = Ato;
	}
	
	public Vector3 direction;
}

class BestFirstDomain : PlanningDomainBase
{
	private int layer =   (1 << LayerMask.NameToLayer("Obstacles"));
					//| (1 << LayerMask.NameToLayer("StaticWorld"));	
	private List<NonDeterministicObstacle> _currentObservedNonDeterministicObstacles; 
	private Task planningTask;
	
	MovableObstacleManager obstacleManager;
	static List<Vector3> transitionsList; 
		
	public BestFirstDomain(){
		obstacleManager = new MovableObstacleManager();	
		transitionsList = new List<Vector3>();
		// changed ordering so that it is in anti-clockwise direction 
		// even are non-diagonal and odd ore diagonal 
		transitionsList.Add(new Vector3(1.0f, 0.0f, 0.0f)); 
		transitionsList.Add(new Vector3(1.0f, 0.0f, 1.0f));
		transitionsList.Add(new Vector3(0.0f, 0.0f, 1.0f));
		transitionsList.Add(new Vector3(-1.0f, 0.0f, 1.0f));
		transitionsList.Add(new Vector3(-1.0f, 0.0f, 0.0f));
		transitionsList.Add(new Vector3(-1.0f, 0.0f, -1.0f));
		transitionsList.Add(new Vector3(0.0f, 0.0f, -1.0f));
		transitionsList.Add(new Vector3(1.0f, 0.0f, -1.0f));
		
		_currentObservedNonDeterministicObstacles = new List<NonDeterministicObstacle> ();
	}
	
	public void setPlanningTask ( Task t_planningTask )
	{
		planningTask = t_planningTask;
	}
	
	public override DefaultAction generateAction (DefaultState previousState, DefaultState nextState)
	{
		return new BestFirstAction(previousState,nextState);
		
	}
	
	public override float estimateTotalCost (ref DefaultState currentState, ref DefaultState idealGoalState, float currentg)
	{
		float h = Vector3.Distance(currentState.statePosition(), idealGoalState.statePosition());
	    float f = currentg + h;  
	    return f;
	}
	
	// call this at the beginning of ever plan iteration 
	private void unregisterPlanningTaskAndClearNonDeterministicObstacles ()
	{
		if (planningTask != null)
		{
			foreach (NonDeterministicObstacle nd in _currentObservedNonDeterministicObstacles)
			{
				nd.observable.unregisterObserver(Event.NON_DETERMINISTIC_OBSTACLE_CHANGED,planningTask);
			}
			
			_currentObservedNonDeterministicObstacles.Clear ();		
		}
	}
	
	// this function is called at the beginning of every plan iteration 
	public override void clearAtBeginningOfEveryPlanIteration ()
	{
		unregisterPlanningTaskAndClearNonDeterministicObstacles ();
	}
	
	
	public override bool isAGoalState (ref DefaultState state, ref DefaultState idealGoalState)
	{
		BestFirstState currentState = state as BestFirstState;
		BestFirstState goalState = idealGoalState as BestFirstState;
		return (Vector3.Distance(currentState.state, goalState.state) < 1);	
	}
	
	public override float evaluateDomain (ref DefaultState state)
	{
		return 1.0f;
	}
	
	public override float ComputeHEstimate (DefaultState _from, DefaultState _to)
	{
		return (Vector3.Distance((_from as BestFirstState).state, (_to as BestFirstState).state));	
	}
	
	public override float ComputeGEstimate (DefaultState _from, DefaultState _to)
	{
		return (Vector3.Distance((_from as BestFirstState).state, (_to as BestFirstState).state));	
	}
	
	public override bool equals (DefaultState s1, DefaultState s2, bool isStart)
	{
		BestFirstState ARAstate1 = s1 as BestFirstState;
		BestFirstState ARAstate2 = s2 as BestFirstState;
		
		if(ARAstate1.Equals(ARAstate2))
			return true;
		else 
			return false;
	}
	
	public override void generateNeighbors(DefaultState currentState, ref List<DefaultState> neighbors)
	{
		neighbors.Clear();
		BestFirstState ACurrentState = currentState as BestFirstState;
				
		bool[] transitionsPossible = new bool[8];
		
		// doing non-diagonals first 
		for (int i =0; i < transitionsList.Count; i+=2)
		{
			Collider [] colliders = Physics.OverlapSphere(ACurrentState.state + transitionsList[i], 0.25F, layer);
			
			//if (! Physics.CheckSphere(ACurrentState.state + transitionsList[i],0.5F,layer))
			if (colliders.Count()== 0)
			{
				transitionsPossible[i] = true;
				BestFirstState neighborState = new BestFirstState(ACurrentState.state + transitionsList[i]);
				neighbors.Add(neighborState);
			}
			else 
			{
				transitionsPossible[i] = false;
				// TODO: register the non deterministic obstacle here 	
				foreach (Collider collider in colliders)
				{
					NonDeterministicObstacle nonDeterministicObstacle = collider.GetComponent<NonDeterministicObstacle>();
					if (nonDeterministicObstacle == null)
					{}	//Debug.LogWarning("planner collided with something that is not a non deterministic obtacle " + collider.name) ;
					else
					{
						if ( planningTask != null && 
							_currentObservedNonDeterministicObstacles.Contains(nonDeterministicObstacle) == false)  // not using a planning task, no need to register 
						{
							Debug.Log("NON DET OBSTACLE  " + collider.name);
							nonDeterministicObstacle.observable.registerObserver(Event.NON_DETERMINISTIC_OBSTACLE_CHANGED,planningTask);
							_currentObservedNonDeterministicObstacles.Add(nonDeterministicObstacle);
						}
					}
				}

			}
			
		}
		// diagonals 
		for (int i =1; i < transitionsList.Count; i+=2)
		{
			Collider [] colliders = Physics.OverlapSphere(ACurrentState.state + transitionsList[i], 0.25F, layer);
			//if (! Physics.CheckSphere(ACurrentState.state + transitionsList[i],0.5F,layer))
			if (colliders.Count() == 0)
			{
				if  ( transitionsPossible[i-1] == true || transitionsPossible[(i+1)%transitionsList.Count] == true )
				{
					transitionsPossible[i] = true;
					BestFirstState neighborState = new BestFirstState(ACurrentState.state + transitionsList[i]);
					neighbors.Add(neighborState);
				}
				else transitionsPossible[i] = false;
			}
			else 
			{
				transitionsPossible[i] = false;
				// TODO: register the non deterministic obstacle here 	
				foreach (Collider collider in colliders)
				{
					NonDeterministicObstacle nonDeterministicObstacle = collider.GetComponent<NonDeterministicObstacle>();
					if (nonDeterministicObstacle == null)
					{}//	Debug.LogWarning("planner collided with something that is not a non deterministic obtacle " + collider.name);
					else
					{
						if ( planningTask != null && 
							_currentObservedNonDeterministicObstacles.Contains(nonDeterministicObstacle) == false)  // not using a planning task, no need to register 
						{
							Debug.Log("NON DET OBSTACLE  " + collider.name);
							nonDeterministicObstacle.observable.registerObserver(Event.NON_DETERMINISTIC_OBSTACLE_CHANGED,planningTask);
							_currentObservedNonDeterministicObstacles.Add(nonDeterministicObstacle);
						}
					}
					
				}
			}
			
		}
		
		
	}
			
	public override void generateTransitions (ref DefaultState currentState, ref DefaultState previousState, ref DefaultState idealGoalState, ref List<DefaultAction> transitions)
	{
		transitions.Clear();
		BestFirstState ACurrentState = currentState as BestFirstState;
				
		bool[] transitionsPossible = new bool[8];
		
		// doing non-diagonals first 
		for (int i =0; i < transitionsList.Count; i+=2)
		{
			Collider [] colliders = Physics.OverlapSphere(ACurrentState.state + transitionsList[i], 0.25F, layer);
			
			//if (! Physics.CheckSphere(ACurrentState.state + transitionsList[i],0.5F,layer))
			if (colliders.Count()== 0)
			{
				transitionsPossible[i] = true;
				
				BestFirstAction action =  new BestFirstAction();
				action.cost = Vector3.Distance(ACurrentState.state, ACurrentState.state+transitionsList[i]);
				action.direction = transitionsList[i];
				BestFirstState st = new BestFirstState(ACurrentState.state + transitionsList[i]);
				action.state = st;
				transitions.Add(action);
				
			}
			else 
			{
				transitionsPossible[i] = false;
				// TODO: register the non deterministic obstacle here 	
				foreach (Collider collider in colliders)
				{
					NonDeterministicObstacle nonDeterministicObstacle = collider.GetComponent<NonDeterministicObstacle>();
					if (nonDeterministicObstacle == null)
					{}//	Debug.LogWarning("planner collided with something that is not a non deterministic obtacle " + collider.name) ;
					else
					{
						if ( planningTask != null && 
							_currentObservedNonDeterministicObstacles.Contains(nonDeterministicObstacle) == false)  // not using a planning task, no need to register 
						{
							Debug.Log("NON DET OBSTACLE  " + collider.name);
							nonDeterministicObstacle.observable.registerObserver(Event.NON_DETERMINISTIC_OBSTACLE_CHANGED,planningTask);
							_currentObservedNonDeterministicObstacles.Add(nonDeterministicObstacle);
						}
					}
				}

			}
			
		}
		// diagonals 
		for (int i =1; i < transitionsList.Count; i+=2)
		{
			Collider [] colliders = Physics.OverlapSphere(ACurrentState.state + transitionsList[i], 0.25F, layer);
			//if (! Physics.CheckSphere(ACurrentState.state + transitionsList[i],0.5F,layer))
			if (colliders.Count() == 0)
			{
				if  ( transitionsPossible[i-1] == true || transitionsPossible[(i+1)%transitionsList.Count] == true )
				{
					transitionsPossible[i] = true;
					BestFirstAction action =  new BestFirstAction();
					action.cost = Vector3.Distance(ACurrentState.state, ACurrentState.state+transitionsList[i]);
					action.direction = transitionsList[i];
					BestFirstState st = new BestFirstState(ACurrentState.state + transitionsList[i]);
					action.state = st;
					transitions.Add(action);
				}
				else transitionsPossible[i] = false;
			}
			else 
			{
				transitionsPossible[i] = false;
				// TODO: register the non deterministic obstacle here 	
				foreach (Collider collider in colliders)
				{
					NonDeterministicObstacle nonDeterministicObstacle = collider.GetComponent<NonDeterministicObstacle>();
					if (nonDeterministicObstacle == null)
					{}//	Debug.LogWarning("planner collided with something that is not a non deterministic obtacle " + collider.name);
					else
					{
						if ( planningTask != null && 
							_currentObservedNonDeterministicObstacles.Contains(nonDeterministicObstacle) == false)  // not using a planning task, no need to register 
						{
							Debug.Log("NON DET OBSTACLE  " + collider.name);
							nonDeterministicObstacle.observable.registerObserver(Event.NON_DETERMINISTIC_OBSTACLE_CHANGED,planningTask);
							_currentObservedNonDeterministicObstacles.Add(nonDeterministicObstacle);
						}
					}
					
				}
			}
			
		}
		
		
	}	
	
		
}

public class BestFirstSearchTest : MonoBehaviour {
	
	
	public GameObject startObject, goalObject, selectedGameObject;
	public Stack<DefaultState> outputPlan;
	BestFirstSearchPlanner planner;
	List<PlanningDomainBase> domainList;
	DefaultState DStartState, DGoalState, DPrevGoalState;
	float inflationFactor = 2.5f;
	Stack<BestFirstAction> actions;
	DefaultState previousState, currentState;
	int i = 0;
	public bool showOpen, showVisited;
	
	BestFirstState startState, goalState, prevGoalState;
	// Use this for initialization
	void Start () {
		
		
		actions = new Stack<BestFirstAction>();
		outputPlan = new Stack<DefaultState>();
		
		domainList = new List<PlanningDomainBase>();
		domainList.Add(new BestFirstDomain());
		
		planner = new BestFirstSearchPlanner();
		planner.init(ref domainList, 1000);
		
		startState = new BestFirstState(startObject.transform.position);
		goalState = new BestFirstState(goalObject.transform.position);
		prevGoalState = goalState;
		DStartState = startState as DefaultState;
		DGoalState = goalState as DefaultState;
	}
	
	
	// Update is called once per frame
	void Update () {
		
		if(Input.GetKeyDown(KeyCode.A)){
			Debug.Log("Planning");
			DStartState = new BestFirstState(startObject.transform.position) as DefaultState;
			DGoalState = new BestFirstState(goalObject.transform.position) as DefaultState;
			planner.oneStep = false;
			planner.computePlan(ref DStartState, ref DGoalState, ref outputPlan, 10.0f);
		}
		
		if(Input.GetKeyDown(KeyCode.S)){
			Debug.Log("Planning");
			DStartState = new BestFirstState(startObject.transform.position) as DefaultState;
			DGoalState = new BestFirstState(goalObject.transform.position) as DefaultState;
			planner.oneStep = true;
			planner.computePlan(ref DStartState, ref DGoalState, ref outputPlan, 10.0f);
		}
		
		if(Input.GetKeyDown(KeyCode.Z)){
			showOpen = !showOpen;	
		}
		
		if(Input.GetKeyDown(KeyCode.X)){
			showVisited = !showVisited;	
		}
	}
	
	void OnDrawGizmos() {
		
		
		if(planner == null)
			return;
		
		if(showVisited)
			planner.VisualizeContainer(ContainerType.Visited, Color.magenta, .20f);
		
		if(showOpen)
			planner.VisualizeContainer(ContainerType.Open, Color.green, .20f);
		
		
		if(outputPlan != null)
			planner.VisualizeContainer(ContainerType.Plan, Color.blue, .20f);
		
	}
}
