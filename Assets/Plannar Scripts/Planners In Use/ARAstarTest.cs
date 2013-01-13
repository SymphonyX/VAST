using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

public class NeighborMap
{
	public Dictionary<DefaultState, List<DefaultState>> neighborMap;
}

public class GridNeighbors : NeighborMap
{
	public GridNeighbors()
	{
		neighborMap = new Dictionary<DefaultState, List<DefaultState>>();	
		for(float i=0; i < 20; ++i)
		{
			for(float j=0; j < 20; ++j)
			{
				float x = -10.0f+j; float z = -10.0f +i;
				Vector3 newState = new Vector3(x, 0.5f, z);
				List<DefaultState> neighborStates = new List<DefaultState>();
				
				if(newState.x+1 < 10.0f) neighborStates.Add(new ARAstarState(new Vector3(x+1, 0.5f, z)));
				if(newState.x-1 >= -10.0f) neighborStates.Add(new ARAstarState(new Vector3(x-1, 0.5f, z))); 
				if(newState.z+1 < 10.0f) neighborStates.Add(new ARAstarState(new Vector3(x, 0.5f, z+1)));
				if(newState.z-1 >= -10.0f) neighborStates.Add(new ARAstarState(new Vector3(x, 0.5f, z-1)));
				if(newState.x+1 < 10.0f && newState.z+1 < 10.0f)
					neighborStates.Add(new ARAstarState(new Vector3(x+1, 0.5f, z+1)));
				if(newState.x+1 < 10.0f && newState.z-1 >= -10.0f)
					neighborStates.Add(new ARAstarState(new Vector3(x+1, 0.5f, z-1)));
				if(newState.x-1 >= -10.0f && newState.z+1 < 10.0f)
					neighborStates.Add(new ARAstarState(new Vector3(x-1, 0.5f, z+1)));
				if(newState.x-1 >= -10.0f && newState.z-1 >= -10.0f)
					neighborStates.Add(new ARAstarState(new Vector3(x-1, 0.5f, z-1)));
				
				DefaultState DState = new ARAstarState(newState) as DefaultState;
				neighborMap[DState] = neighborStates;
			}
		}
	}
}

public class ARAstarState : DefaultState, IEquatable<ARAstarState>
{
	public ARAstarState(Vector3 st)
	{ state = st; }
	
	public override int GetHashCode ()
	{
		return 1;
	}
	
	public override bool Equals(object obj)
	{
		return Equals(obj as ARAstarState);
	}
	
	public bool Equals(ARAstarState obj)
	{
		return obj!=null && obj.state == this.state;
	}
	
	public override Vector3 statePosition()
	{
		return state;	
	}
	
	public Vector3 state;
}

class ARAstarAction : DefaultAction 
{ 
	public ARAstarAction(){}
	public ARAstarAction(DefaultState _from, DefaultState _to) 
	{ 
		ARAstarState Afrom = _from as ARAstarState; 
		ARAstarState Ato = _to as ARAstarState;
		
		Vector3 dir = new Vector3(Ato.state.x - Afrom.state.x, Ato.state.y - Afrom.state.y, Ato.state.z - Afrom.state.z);
		direction = dir; 
		cost = Vector3.Distance(Afrom.state, Ato.state);
		state = Ato;
	}
	
	public Vector3 direction;
}

class ARAstarDomain : PlanningDomainBase
{
	private int layer =   (1 << LayerMask.NameToLayer("Obstacles"));
					//| (1 << LayerMask.NameToLayer("StaticWorld"));	
	private List<NonDeterministicObstacle> _currentObservedNonDeterministicObstacles; 
	private Task planningTask;
	
	MovableObstacleManager obstacleManager;
	static List<Vector3> transitionsList; 
		
	public ARAstarDomain(){
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
		return new ARAstarAction(previousState,nextState);
		
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
		ARAstarState currentState = state as ARAstarState;
		ARAstarState goalState = idealGoalState as ARAstarState;
		return (Vector3.Distance(currentState.state, goalState.state) < 1);	
	}
	
	public override float evaluateDomain (ref DefaultState state)
	{
		return 1.0f;
	}
	
	public override float ComputeHEstimate (DefaultState _from, DefaultState _to)
	{
		return (Vector3.Distance((_from as ARAstarState).state, (_to as ARAstarState).state));	
	}
	
	public override float ComputeGEstimate (DefaultState _from, DefaultState _to)
	{
		return (Vector3.Distance((_from as ARAstarState).state, (_to as ARAstarState).state));	
	}
	
	public override bool equals (DefaultState s1, DefaultState s2, bool isStart)
	{
		ARAstarState ARAstate1 = s1 as ARAstarState;
		ARAstarState ARAstate2 = s2 as ARAstarState;
		
		if(ARAstate1.Equals(ARAstate2))
			return true;
		else 
			return false;
	}
	
	
	public override void generatePredecessors(DefaultState currentState, ref List<DefaultAction> actionList)
	{
		ARAstarState ACurrentState = currentState as ARAstarState;
				
		bool[] transitionsPossible = new bool[8];
		
		// doing non-diagonals first 
		for (int i =0; i < transitionsList.Count; i+=2)
		{
			Collider [] colliders = Physics.OverlapSphere(ACurrentState.state + transitionsList[i], 0.25F, layer);
			
			//if (! Physics.CheckSphere(ACurrentState.state + transitionsList[i],0.5F,layer))
			if (colliders.Count()== 0)
			{
				transitionsPossible[i] = true;
				ARAstarAction action = new ARAstarAction(ACurrentState, new ARAstarState(ACurrentState.state+transitionsList[i]));
				actionList.Add(action);
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
							//nonDeterministicObstacle.observable.registerObserver(Event.NON_DETERMINISTIC_OBSTACLE_CHANGED,planningTask);
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
					ARAstarAction action = new ARAstarAction(ACurrentState, new ARAstarState(ACurrentState.state + transitionsList[i]));
					actionList.Add(action);
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
							//nonDeterministicObstacle.observable.registerObserver(Event.NON_DETERMINISTIC_OBSTACLE_CHANGED,planningTask);
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
		ARAstarState ACurrentState = currentState as ARAstarState;
				
		bool[] transitionsPossible = new bool[8];
		
		// doing non-diagonals first 
		for (int i =0; i < transitionsList.Count; i+=2)
		{
			Collider [] colliders = Physics.OverlapSphere(ACurrentState.state + transitionsList[i], 0.25F, layer);
			
			//if (! Physics.CheckSphere(ACurrentState.state + transitionsList[i],0.5F,layer))
			if (colliders.Count()== 0)
			{
				transitionsPossible[i] = true;
				
				ARAstarAction action =  new ARAstarAction();
				action.cost = Vector3.Distance(ACurrentState.state, ACurrentState.state+transitionsList[i]);
				action.direction = transitionsList[i];
				ARAstarState st = new ARAstarState(ACurrentState.state + transitionsList[i]);
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
							Debug.LogError("NON DET OBSTACLE  " + collider.name);
							//nonDeterministicObstacle.observable.registerObserver(Event.NON_DETERMINISTIC_OBSTACLE_CHANGED,planningTask);
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
					ARAstarAction action =  new ARAstarAction();
					action.cost = Vector3.Distance(ACurrentState.state, ACurrentState.state+transitionsList[i]);
					action.direction = transitionsList[i];
					ARAstarState st = new ARAstarState(ACurrentState.state + transitionsList[i]);
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
							//nonDeterministicObstacle.observable.registerObserver(Event.NON_DETERMINISTIC_OBSTACLE_CHANGED,planningTask);
							_currentObservedNonDeterministicObstacles.Add(nonDeterministicObstacle);
						}
					}
					
				}
			}
			
		}
		
		
	}	
	
		
}

public class ARAstarTest : MonoBehaviour {
	
	public GameObject startObject, goalObject, selectedGameObject;
	public bool usingHeap;
	ARAstarAction action;
	int index = 0;
	PathStatus PlanStatus;
	ARAstarAction move;
	bool moving = false;
	public static Dictionary<DefaultState, ARAstarNode> outputPlan;
	ARAstarPlanner planner;
	bool finished = false;
	List<PlanningDomainBase> domainList;
	DefaultState DStartState, DGoalState, DPrevGoalState;
	float inflationFactor = 2.5f;
	Stack<ARAstarAction> actions;
	GridNeighbors neighborMap;
	DefaultState previousState, currentState;
	PathStatus PlannerStatus;
	int i = 0;
	public bool showOpen, showClose, showIncons, showVisited;
	
	ARAstarState startState, goalState, prevGoalState;
	void Awake() {
	}
	
	
	// Use this for initialization
	void Start () {
		
		
		actions = new Stack<ARAstarAction>();
		outputPlan = new Dictionary<DefaultState, ARAstarNode>();
		
		domainList = new List<PlanningDomainBase>();
		domainList.Add(new ARAstarDomain());
		
		planner = new ARAstarPlanner();
		planner.init(ref domainList, 100);
		planner.usingHeap = usingHeap;
		
		startState = new ARAstarState(startObject.transform.position);
		goalState = new ARAstarState(goalObject.transform.position);
		prevGoalState = goalState;
		DStartState = startState as DefaultState;
		DGoalState = goalState as DefaultState;
		neighborMap = new GridNeighbors();
			
		
	}
	
	void fillActionStack(DefaultState reachedState){
		ARAstarState state = reachedState as ARAstarState;
		while(!state.Equals(DStartState as ARAstarState)){
			actions.Push(outputPlan[state].action as ARAstarAction);
			state = outputPlan[state].previousState as ARAstarState;
		}
	}
	
	
	
	// Update is called once per frame
	void Update () {
		
		if(Input.GetKeyDown(KeyCode.A)){
			Debug.Log("Planning");
			DStartState = new ARAstarState(startObject.transform.position) as DefaultState;
			DGoalState = new ARAstarState(goalObject.transform.position) as DefaultState;
			DPrevGoalState = prevGoalState as DefaultState;
			actions.Clear();
			planner.OneStep = false;
			//planner.computePlan(ref DStartState, ref DGoalState, DPrevGoalState, ref outputPlan, ref inflationFactor, 1.0f);
			PlanStatus = planner.computePlan(ref DStartState, ref DGoalState, ref outputPlan, ref inflationFactor, 10.0f);
			Debug.Log("Status: " + PlanStatus);
			if(PlanStatus == PathStatus.NoPath){
				Debug.LogWarning("NO PLAN. PLANNING AGAIN");
				//inflationFactor = 2.5f;
				PlanStatus = planner.computePlan(ref DStartState, ref DGoalState, ref outputPlan, ref inflationFactor, 10.0f);
			}
			new WaitForEndOfFrame();
		}
		
		if(Input.GetKeyDown(KeyCode.Z)){
			showOpen = !showOpen;	
		}
		
		if(Input.GetKeyDown(KeyCode.C)){
			showClose = !showClose;	
		}
		
		if(Input.GetKeyDown(KeyCode.X)){
			showVisited = !showVisited;	
		}
		
		if(Input.GetKeyDown(KeyCode.V)){
			showIncons = !showIncons;	
		}
		
		if(Input.GetKeyDown(KeyCode.Q)) {
			DStartState = new ARAstarState(startObject.transform.position) as DefaultState;
			PlanStatus = PathStatus.NoPath;
			planner.restartPlanner();	
		}
		
		if(Input.GetKeyDown(KeyCode.S))
		{
			Debug.Log("Planning");
			DStartState = new ARAstarState(startObject.transform.position) as DefaultState;
			DGoalState = new ARAstarState(goalObject.transform.position) as DefaultState;
			DPrevGoalState = prevGoalState as DefaultState;
			actions.Clear();
			//planner.computePlan(ref DStartState, ref DGoalState, DPrevGoalState, ref outputPlan, ref inflationFactor, 1.0f);
			planner.OneStep = true;
			PlanStatus = planner.computePlan(ref DStartState, ref DGoalState, ref outputPlan, ref inflationFactor, 10.0f);
			Debug.Log("Status: " + PlanStatus);
			new WaitForEndOfFrame();

		}
		
		if(Input.GetKeyDown(KeyCode.Return)){
			if (actions.Count > 0) {
				ARAstarAction action = actions.Pop();
				Debug.Log("Direction: " + action.direction);
				Vector3 prevPost = startObject.transform.position;
				startObject.transform.position = new Vector3(prevPost.x+action.direction.x, prevPost.y+action.direction.y, prevPost.z+action.direction.z);
			}
			ARAstarPlanner.moved = true;
		}
		if(selectedGameObject != null){ 
			if(Input.GetKeyDown(KeyCode.RightArrow)){
				moveSelectedObjectWithDirection(1.0f, 0.0f, 0.0f);
			}
			else if(Input.GetKeyDown(KeyCode.LeftArrow)){
				moveSelectedObjectWithDirection(-1.0f, 0.0f, 0.0f);
			}
			else if(Input.GetKeyDown(KeyCode.UpArrow)){
				moveSelectedObjectWithDirection(0.0f, 0.0f, 1.0f);
			}
			else if(Input.GetKeyDown(KeyCode.DownArrow)){
				moveSelectedObjectWithDirection(0.0f, 0.0f, -1.0f);
			}
		}
		
		if(Input.GetMouseButtonDown(0)){
			RaycastHit hit;
			Debug.Log("Mouse Down");
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if(Physics.Raycast(ray, out hit, 100.0f)){
				Debug.Log("Selected");
				selectedGameObject = hit.transform.gameObject;
				Debug.Log("Position: " + selectedGameObject.transform.position);
			}
			else{
				Debug.Log("Nothing selected");	
			}
		}
		
		if(Input.GetKeyDown(KeyCode.Space)){
			Debug.Log("Obstacle Moved Update");
			Debug.Log("Prev: " + (previousState as ARAstarState).state);
			Debug.Log("Curret: " + (currentState as ARAstarState).state);
			planner.UpdateAfterObstacleMoved(previousState, currentState);
		}
		if(Input.GetKeyDown(KeyCode.P)){
			DefaultState stateReached = planner.FillPlan();	
			fillActionStack(stateReached);
		}
	}
	
	void moveSelectedObjectWithDirection(float x, float y, float z)
	{
		previousState = new ARAstarState(selectedGameObject.transform.position) as DefaultState;
		Vector3 prevPos = selectedGameObject.transform.position;
		selectedGameObject.transform.position = new Vector3(prevPos.x+x, prevPos.y+y, prevPos.z+z);
		currentState = new ARAstarState(selectedGameObject.transform.position) as DefaultState;
		//inflationFactor = 2.5f;
		if (selectedGameObject == goalObject) {
			goalState = new ARAstarState(goalObject.transform.position);
			prevGoalState = new ARAstarState(prevPos);
			ARAstarPlanner.goalMoved = true;
			planner.plannerFinished = false;
		} else if (selectedGameObject == startObject) {
			ARAstarPlanner.moved = true;
		}
	}
	
	void OnDrawGizmos()
	{
		if(selectedGameObject != null){
			Gizmos.color = Color.yellow;
			if(selectedGameObject.collider is BoxCollider)
				Gizmos.DrawWireCube(selectedGameObject.transform.position, (selectedGameObject.collider as BoxCollider).size);	
			else if(selectedGameObject.collider is SphereCollider)
				Gizmos.DrawWireSphere(selectedGameObject.transform.position, (selectedGameObject.collider as SphereCollider).radius);	
		}
		
		
		
		if(planner == null){
			Debug.LogWarning("Planner not initialized");
			return;
		}
		
		if(showVisited)
		{
			planner.VisualizeContainer(ContainerType.Visited, Color.magenta, .20f);	
		}
		
		if(showOpen){
			planner.VisualizeContainer(ContainerType.Open, Color.green, .20f);
		}
		
		if(showClose)
		{
			planner.VisualizeContainer(ContainerType.Close, Color.red, .20f);
		}
		
		if(actions != null && actions.Count != 0)
		{
			planner.VisualizeContainer(ContainerType.Plan, Color.blue, .20f);
		}
			
		if(showIncons)
		{
			planner.VisualizeContainer(ContainerType.Incons, Color.yellow, .20f);
		}
		
	
			
	}
	
	
	
		

}
