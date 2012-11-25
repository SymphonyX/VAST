using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

//! Enum to determine Path Status
public enum PathStatus
{
	NoPath,
	Incomplete,
	SubOptimal,
	Optimal
};

//! Enum used for debugging purposes only
public enum ContainerType
{
	Open,
	Close,
	Incons,
	Plan,
	Visited
};

/**
 * This is the main class for ARAstar Planner
 * Handles all details related to the search.
 */
public class ARAstarPlanner : IPlannerInterface<Dictionary<DefaultState, ARAstarNode>>
{
	public static bool moved = false; 							//!< Flag to determine if agent moved
	public static bool goalMoved = false;						//!< Flag to determine if goal moved
	public static bool obstacleMoved = false;					//!< Flag to determine if obstacle moved
	public int _maxNumNodesToExpand;							//!< Maximum number of nodes to expand in a plan iteration
	public bool usingHeap = true;								//!< Flag to determine if we sort with a heap (testing)
	public float inflationFactor = 1.0f;						//!< Weight applied to heuristic
	public bool firstTime = true;								//!< Flag to determine if it's the first iteration
	public bool OneStep = false;								//!< Flag to determine if we run planner step by step
	public List<PlanningDomainBase> _planningDomain;			//!< List of possible domains
	PlanningDomainBase selectedPlanningDomain;					//!< Currently selected domain
	public Dictionary<DefaultState, ARAstarNode> Visited;		//!< Nodes visited so far
	DefaultState currentStart;									//!< Current agent position
	DefaultState goalState;										//!< Goal position
	bool OneStepUpdateNeeded = false;							//!< Determines when the planner starts a new iteration (step-by-step mode only)
	public CloseContainer Close;								//!< List of nodes expanded
	public OpenContainer Open;									//!< List of nodes to expand
	PlanContainer Plan;											//!< Return path
	public Incons Incons;										//!< List of inconsistent nodes
	ARAstarNode startNode;										//!< Agent node
	ARAstarNode goalNode;										//!< Goal node
	KeyValuePair<DefaultState, ARAstarNode> goalPair;			//!< Keyvalue pair for goal
	public bool plannerFinished = false;
	DefaultState stateReached;									//!< Closest state reached (if goal was reached, this will be the goal state)
	int nodesExpanded = 0;										//!< Number of nodes that already have been expanded
	PathStatus Status;											//!< Plan Status 
	
	
	/// <summary>
	/// Initializes a new instance of the <see cref="ARAstarPlanner"/> class.
	/// </summary>
	public ARAstarPlanner(){}
	
	/// <summary>
	/// Initializes required data for the planner.
	/// </summary>
	/// <param name='newPlanningDomain'>
	/// List of domains to be used
	/// </param>
	/// <param name='maxNumNodesToExpand'>
	/// Maximum number of nodes to expand.
	/// </param>
	public void init (ref List<PlanningDomainBase> newPlanningDomain, int maxNumNodesToExpand)
	{
		_planningDomain = new List<PlanningDomainBase> (newPlanningDomain.Capacity);
		_planningDomain = newPlanningDomain;
		DetermineDomain(ref selectedPlanningDomain, ref currentStart);
		_maxNumNodesToExpand = maxNumNodesToExpand;
	}
	
	/// <summary>
	/// Updates the vertex.
	/// </summary>
	/// <param name='node'>
	/// Node to be updated
	/// </param>
	void UpdateVertex(ARAstarNode node)
	{
		//[06]
		if(!currentStart.Equals(node.action.state)){
			float minCost = Mathf.Infinity;
			//List<DefaultState> neighbors = new List<DefaultState>();
			//domain.generateNeighbors(node.action.state, ref neighbors);
			List<DefaultAction> transitions = new List<DefaultAction>();
			selectedPlanningDomain.generatePredecessors(node.action.state, ref transitions);
			foreach(DefaultAction action in transitions){
				if(Visited.ContainsKey(action.state)){ //Was visited
					if(Visited[action.state].g + action.cost < minCost && !Visited[action.state].previousState.Equals(node.action.state) ){
						minCost = Visited[action.state].g + action.cost;
						node.previousState = action.state;
					}
				}
			}
			//node.action = new ARAstarAction(node.previousState, node.action.state);
			node.action = selectedPlanningDomain.generateAction(node.previousState,node.action.state);
			node.rhs = minCost;
		}
		if (selectedPlanningDomain.equals (node.action.state, goalState, false))
				goalPair = new KeyValuePair<DefaultState, ARAstarNode> (node.action.state, node);
		if(Open.ContainsState(node.action.state))
		   Open.Remove(node.action.state);
		if(node.g != node.rhs){
			if(!Close.Contains(node.action.state))	
				Open.Insert(node);
			else
				Incons.Insert(node);
		}
		
		Visited[node.action.state] = node;
	
	}
	
	/// <summary>
	/// Determines whether a complete path exists or not.
	/// </summary>
	/// <returns>
	/// The path status.
	/// </returns>
	bool completePathExists ()
	{
		
		Dictionary<DefaultState, ARAstarNode> tempDic = new Dictionary<DefaultState, ARAstarNode>();
		foreach(KeyValuePair<DefaultState, ARAstarNode> keyval in Visited)
		{
			tempDic[keyval.Key] = keyval.Value;	
		}
		
		DefaultState s = goalPair.Key;
		if(!stateReached.Equals(s))
			Status = PathStatus.Incomplete;
		while(!s.Equals(currentStart)){
			if(Visited[s].g == Mathf.Infinity){
				return false;
			}
			if(!tempDic.ContainsKey(s)){
				Status = PathStatus.NoPath;
				return false;	
			}
			else{
				DefaultState tempState = s;
				s = tempDic[s].previousState;
				tempDic.Remove(tempState);	
			}
		}
		
		if(selectedPlanningDomain.equals(stateReached,goalState,false)){
			if(inflationFactor == 1.0f){
				Status = PathStatus.Optimal;
			}
			else{
				Status = PathStatus.SubOptimal;	
			}
		}
		
		return true;
		
	}
	
	/// <summary>
	/// Expands a node.
	/// </summary>
	/// <param name='node'>
	/// Node to be expanded
	/// </param>
	private void expandNode(ARAstarNode node)
	{
		nodesExpanded++;
		Open.Remove(node.action.state);
		
		if(node.g > node.rhs) {
			node.g = node.rhs;
			Close.Insert(node);
		} else {
			node.g = Mathf.Infinity;
			UpdateVertex(node);
		}
		
		generateNodeSuccessors (ref node);
		
		if(Visited[stateReached].h > node.h)
			stateReached = node.action.state;
		
		if(!usingHeap)
			Open.Sort();
		
		if(node == null) {
			Status = PathStatus.NoPath;	
		}
	}
	
	/// <summary>
	/// Performs one step of the planner.
	/// </summary>
	public void PerformOneStep ()
	{
		
		ARAstarNode currentNode = Open.First ();	
		bool openListEmpty = false;
		if (((compareKey(currentNode, goalPair.Value)!=1) 
			&& goalPair.Value.rhs == goalPair.Value.g 
			&& currentNode.highPriority <= 0 && completePathExists() == true) || nodesExpanded == _maxNumNodesToExpand)
		{
			nodesExpanded = 0;
			if(firstTime)
				firstTime = false;
			Open.Sort();
			Close.Clear ();
			Open.clearHighPriority();	
			Debug.Log ("Updating weight. Plan again");
			return;
		}
		else
		{
			expandNode(currentNode);
		}
		Debug.Log ("number of nodes expanded : " + nodesExpanded);
		if(Incons.Elements().Count > 0)
			Incons.MoveToOpen (ref Open);
	}
	
	/// <summary>
	/// Computes or improves the plan.
	/// </summary>
	/// <param name='maxTime'>
	/// Maximum alloted time.
	/// </param>
	void ImprovePath (float maxTime)
	{
		
		nodesExpanded = 0;
		Close.Clear ();
		Open.Sort();
		ARAstarNode currentNode = Open.First ();
		float prevTime = Time.realtimeSinceStartup;
		
		bool openListEmpty = false;
		
		while (
			((compareKey(currentNode, goalPair.Value)==1) 
			|| goalPair.Value.rhs != goalPair.Value.g 
			|| currentNode.highPriority > 0 || completePathExists() == false) 
			&& (maxTime > 0)  
			&& (nodesExpanded < _maxNumNodesToExpand)) 
		{
			expandNode(currentNode);
			
			currentNode = Open.First();
			
			float actualTime = Time.realtimeSinceStartup;
			maxTime -= (actualTime-prevTime);
			prevTime = actualTime;
		}
		
		// TODO : use openListEmpty to send failure signal 
		if(firstTime)
			firstTime = false;
		else {
			Open.clearHighPriority();	
		}
		if(Incons.Elements().Count > 0)
			Incons.MoveToOpen (ref Open);
		Debug.Log ("number of nodes expanded : " + nodesExpanded);
	}
	
	/// <summary>
	/// Computes the plan.
	/// </summary>
	/// <returns>
	/// Plan status.
	/// </returns>
	/// <param name='currentState'>
	/// Agent state.
	/// </param>
	/// <param name='_goalState'>
	/// Goal state.
	/// </param>
	/// <param name='plan'>
	/// Dictionary where plan will be stored.
	/// </param>
	/// <param name='inflation'>
	/// Inflation factor.
	/// </param>
	/// <param name='maxTime'>
	/// Maximum alloted time.
	/// </param>
	public PathStatus computePlan (ref DefaultState currentState, ref DefaultState _goalState, 
		ref Dictionary<DefaultState, ARAstarNode> plan, ref float inflation, float maxTime)
	{
		DefaultState s = default(DefaultState);
		selectedPlanningDomain = default(PlanningDomainBase);
		goalState = _goalState;
		DetermineDomain (ref selectedPlanningDomain, ref currentState);
		currentStart = currentState;
		
		// introducing function that clears some temp data (e.g. tracked non det obstacles) from domain at the beginning 
		// of each plan iteration 
		//domain.clearAtBeginningOfEveryPlanIteration ();
			
		if (firstTime) {
			inflationFactor = inflation;
			InitializeValues (ref currentState, ref goalState, inflation);
			Plan = new PlanContainer(plan);
			if(OneStep)
				PerformOneStep();
			else {
				ImprovePath (maxTime);
			}
		} else {
			
			if(goalMoved){
				UpdateAfterGoalMoved(goalState);
			}
			if(moved) {
				UpdateAfterStartMoved(currentState);
			}
			
			if (inflationFactor < 1.0f)
				inflationFactor = 1.0f;
			
			//Check start node if it moved
			if(OneStep){
				PerformOneStep();
			} else {
				ImprovePath (maxTime);
			}
			
			if (inflationFactor == 1.0f)
				plannerFinished = true;
		}
		inflationFactor -= .5f;
		//TODO: please return Status here 
		//return true;
		return Status;
		//return (inflationFactor == 1.0F);
	}
	
	/// <summary>
	/// Fills the plan.
	/// </summary>
	/// <returns>
	/// Closest state reached.
	/// </returns>
	public DefaultState FillPlan()
	{
		Plan.Fill(ref Close, Visited, ref stateReached,  selectedPlanningDomain, ref currentStart, ref goalPair, inflationFactor);
		return stateReached;
	}
	/*********************************************************************************************************
	 ****************************** Helper Functions *********************************************************
	 ********************************************************************************************************/


	/*float DetermineNewInfFactor(ref ARAstarNode currentNode)
	{
		float min = Mathf.Infinity;
		foreach(ARAstarNode node in Open.Values)
		{
			float h = node.h; float g = node.g;
			if((h+g) < min)
				min = h+g;
		}
		foreach(ARAstarNode node in Incons.Values)
		{
			float h = node.h; float g = node.g;
			if((h+g) < min)
				min = h+g;
		}
		
		return Mathf.Min(inflationFactor, (currentNode.g/ min));
	}*/
	
	
	/// <summary>
	/// Compares two key.
	/// </summary>
	/// <returns>
	/// 1 is first node's key is greater than second's, 0 otherwise.
	/// </returns>
	/// <param name='firstNode'>
	/// First node.
	/// </param>
	/// <param name='secondNode'>
	/// Second node.
	/// </param>
	int compareKey(ARAstarNode firstNode, ARAstarNode secondNode)
	{
		float[] firstKey = firstNode.Key(inflationFactor);
		float[] secondKey = secondNode.Key(inflationFactor);
		if(firstKey[0] < secondKey[0])
			return 1;
		else if(firstKey[0] > secondKey[0])
			return 0;
		else{
			if(firstKey[1] < secondKey[1])
				return 1;
			else 
				return 0;
		}
	}
	
	/// <summary>
	/// Computes a node's f-value.
	/// </summary>
	/// <param name='node'>
	/// Node.
	/// </param>
	public float fvalue (ARAstarNode node)
	{
		return (node.g + inflationFactor * node.h);
	}
	
	/// <summary>
	/// Initializes start node, goal node and containers.
	/// </summary>
	/// <param name='currentState'>
	/// Current agent state.
	/// </param>
	/// <param name='goalState'>
	/// Goal state.
	/// </param>
	/// <param name='inflation'>
	/// Inflation factor.
	/// </param>
	void InitializeValues (ref DefaultState currentState, ref DefaultState goalState, float inflation)
	{
		InitializedArrays ();
		createStartNode (ref currentState, ref goalState);
		if(stateReached == null)
			stateReached = currentState;
		
		if(goalNode == null || !goalPair.Key.Equals(goalState))
			createGoalNode (ref goalState);
		
		if(!Close.Contains(currentState))
		{
			Open.Insert (startNode);
			Visited[currentState] = startNode;
			Open.startState = startNode.action.state;
		}
	}
	
	/// <summary>
	/// Creates the start node.
	/// </summary>
	/// <param name='currentState'>
	/// Current state.
	/// </param>
	/// <param name='goalState'>
	/// Goal state.
	/// </param>
	void createStartNode (ref DefaultState currentState, ref DefaultState goalState)
	{
		float new_h = selectedPlanningDomain.ComputeHEstimate (currentState, goalState);
		startNode = new ARAstarNode (Mathf.Infinity, new_h, currentState, currentState);
		startNode.rhs = 0.0f;
	}
	
	/// <summary>
	/// Creates the goal node.
	/// </summary>
	/// <param name='goalState'>
	/// Goal state.
	/// </param>
	void createGoalNode (ref DefaultState goalState)
	{
		goalNode = new ARAstarNode (Mathf.Infinity, 0.0f, goalState, goalState);
		goalNode.rhs = Mathf.Infinity;
		goalPair = new KeyValuePair<DefaultState, ARAstarNode> (goalState, goalNode);
	}
	
	/// <summary>
	/// Initializes the containers.
	/// </summary>
	void InitializedArrays ()
	{
		if(Close == null)
			Close = new CloseContainer();
		if(Open == null)
			Open = new OpenContainer (this, selectedPlanningDomain, usingHeap);
		if(Incons == null)
			Incons = new Incons ();
		if(Visited == null)
			Visited = new Dictionary<DefaultState, ARAstarNode> ();
	}
	
	/// <summary>
	/// Determines appropriate domain (testing).
	/// </summary>
	/// <param name='domain'>
	/// Domain.
	/// </param>
	/// <param name='startState'>
	/// Start state.
	/// </param>
	void DetermineDomain (ref PlanningDomainBase domain, ref DefaultState startState)
	{
		float score = 0.0f;
		foreach (PlanningDomainBase d in _planningDomain) {
			if (d.evaluateDomain (ref startState) > score) {
				score = d.evaluateDomain (ref startState);
				domain = d;
			}
		}
	}
	
	/// <summary>
	/// Generates a node's successors.
	/// </summary>
	/// <param name='currentNode'>
	/// Current node.
	/// </param>
	void generateNodeSuccessors (ref ARAstarNode currentNode)
	{
		List<DefaultAction> possibleTransitions = new List<DefaultAction> ();
		selectedPlanningDomain.generateTransitions (ref currentNode.action.state, ref currentNode.previousState, ref goalNode.action.state, ref possibleTransitions);
		ARAstarNode nextNode;
		foreach (DefaultAction successorAction in possibleTransitions) {
			DefaultAction nextAction = successorAction;
			//float newg = currentNode.g + nextAction.cost;
			float newh = selectedPlanningDomain.ComputeHEstimate (nextAction.state, goalNode.action.state);
			if(!Visited.ContainsKey(nextAction.state)){
				nextNode = new ARAstarNode (Mathf.Infinity, newh, currentNode.action.state, nextAction);
			}
			else{
				nextNode = Visited[nextAction.state];
			}
			nextNode.weightExpanded = inflationFactor;
			if(currentNode.highPriority > 0){
				if(!Plan.ContainsState(nextNode.action.state)){
					nextNode.highPriority = currentNode.highPriority-1;
				}
				
			}
			UpdateVertex(nextNode);
			//Visited[nextNode.action.state] = nextNode;
		}
	}
	

	//We don't need this function. only defined because its required by the interface
	public bool _computePlan (ref DefaultState startState, ref DefaultState idealGoalState, Dictionary<DefaultState, ARAstarNode> map, ref DefaultState actualStateReached, float maxTime)
	{
		return false;
	}
	
	/// <summary>
	/// Visualizes the containers for debugging.
	/// </summary>
	/// <param name='containerType'>
	/// Container type.
	/// </param>
	/// <param name='color'>
	/// Color used for visualization.
	/// </param>
	/// <param name='radius'>
	/// Radius of sphere.
	/// </param>
	public void VisualizeContainer(ContainerType containerType, Color color, float radius)
	{
		Gizmos.color = color;
		switch(containerType){
			case ContainerType.Close:
				showCloseList(radius, color);
				break;
			case ContainerType.Open:
				showOpenList(radius, color);
				break;
			case ContainerType.Incons:
				showInconsList(radius, color);
				break;
			case ContainerType.Plan:
				showPlan(radius, color);
				break;
			case ContainerType.Visited:
				showVisitedList(radius, color);
				break;
		}
	}
	
	
	/// <summary>
	/// Perfmors necessary updates after obstacle moved.
	/// </summary>
	/// <param name='prevObstacleState'>
	/// State were the obstacle was previously in.
	/// </param>
	/// <param name='currentObstacleState'>
	/// State where the obstacle is currently in.
	/// </param>
	public void UpdateAfterObstacleMoved(DefaultState prevObstacleState, DefaultState currentObstacleState)
	{
		inflationFactor = 2.5f;
		plannerFinished = false;
		float prevNodeH = selectedPlanningDomain.ComputeHEstimate(prevObstacleState, goalNode.action.state);
		float curNodeH = selectedPlanningDomain.ComputeHEstimate(prevObstacleState, goalNode.action.state);
		
		//ARAstarAction action = new ARAstarAction(prevObstacleState, currentObstacleState);
		//DefaultAction Daction = action as DefaultAction;
		DefaultAction Daction = selectedPlanningDomain.generateAction(prevObstacleState,currentObstacleState);
		
		DefaultState Dstate = default(DefaultState);
	
		
		DefaultAction DprevNodeAction = new DefaultAction();
		DprevNodeAction.cost = Mathf.Infinity;
		DprevNodeAction.state = prevObstacleState;
		
		
		ARAstarNode prevNode = new ARAstarNode(Mathf.Infinity, prevNodeH, Dstate, DprevNodeAction);
		
		if(Plan.ContainsState(currentObstacleState)) {
			prevNode.highPriority = 3;	
		}
		
		List<DefaultAction> possibleTransitions =  new List<DefaultAction>();
		selectedPlanningDomain.generatePredecessors(prevObstacleState, ref possibleTransitions);
		bool shouldUpdate = false;
		foreach(DefaultAction a in possibleTransitions)
		{
			if(Visited.ContainsKey(a.state))
			{
				shouldUpdate = true;
				break;
			}
		}
		if(shouldUpdate)
			UpdateVertex(prevNode);
		
		
		//Remove node if current obstacle breaks plan.
		if(Plan.ContainsState(currentObstacleState))
			Plan.Remove(currentObstacleState);	
		
		
		if(Visited.ContainsKey(currentObstacleState))
		{
			Visited[currentObstacleState].g = Mathf.Infinity;
			if(Close.Contains(currentObstacleState))
			{
				//List<DefaultState> neighborsList = new List<DefaultState>();
				//domain.generateNeighbors(currentObstacleState, ref neighborsList);
				List<DefaultAction> transitions = new List<DefaultAction>();
				selectedPlanningDomain.generatePredecessors(currentObstacleState, ref transitions);
				foreach(DefaultAction action in transitions)
				{
					if(Visited.ContainsKey(action.state))
					{
						UpdateVertex(Visited[action.state]);
					}
				}
			}
		}
		
	}
	
	public void setSelectedDomain(PlanningDomainBase domain)
	{
		selectedPlanningDomain = domain;	
	}
	
	/// <summary>
	/// Performs the necessary updates after the goal moved.
	/// </summary>
	/// <param name='currentGoalState'>
	/// Current goal state.
	/// </param>
	public void UpdateAfterGoalMoved(DefaultState currentGoalState)
	{
		
		if(Visited.ContainsKey(goalNode.action.state))
		{
			UpdateVertex(goalNode);
		}
		
		createGoalNode(ref currentGoalState);
		
		//CHECK IF WE ONLY NEED TO UPDATE OPEN -- debug this 
		//Plan.UpdateHeuristic(goalState);
		//Open.UpdateHeuristic(goalState);
		
		UpdateVisitedHeuristic();
		
		inflationFactor += 0.5f;
		//inflationFactor = 2.5f;
		
		goalMoved = false;
	}
	
	/// <summary>
	/// Performs the necessary updates after start moved.
	/// </summary>
	/// <param name='currentState'>
	/// Current state.
	/// </param>
	public void UpdateAfterStartMoved(DefaultState currentState)
	{
		
		if (Plan.ContainsState(currentState))
		{
			float actionCost = Plan.Node(currentState).action.cost;
			Plan.UpdateCosts(actionCost);
			Open.UpdateList (Plan.Node(currentState));
			moved = false;
		}
		else
		{
			// TODO : what if the current state is not part of the plan ? 
			// can we treat this like the obstacle movement splitting up the search graph 
			firstTime = true; 
			inflationFactor = 2.5f;
			//Open.CleanContainer ();
			Close.Clear ();
			Plan.Clear ();
			Incons.Clear ();
			
		}
	}
	
	/// <summary>
	/// Updates h-values for nodes in visited.
	/// </summary>
	void UpdateVisitedHeuristic()
	{
		foreach(ARAstarNode n in Visited.Values)
		{
			// TODO : use the heuristic function defined in the domain here !!
			//float newh = Vector3.Distance((n.action.state as ARAstarState).state, (goalState as ARAstarState).state);
			float newh = selectedPlanningDomain.ComputeHEstimate(n.action.state, goalState);
			n.h = newh;
		}	
	}
	
	void showOpenList(float radius, Color color)
	{
		if(Open.ListCount() == 0)
			Debug.LogWarning("Open List is Empty");
		else {
			foreach(ARAstarNode node in Open.Elements().Values)
			{
				Vector3 nodePosition = node.action.state.statePosition();
				Vector3 direction = node.action.state.statePosition() - node.previousState.statePosition();
				Gizmos.color = color;
				Gizmos.DrawSphere(nodePosition, radius);	
				DrawArrow.ForGizmo(node.previousState.statePosition(), direction, Color.black);
			}
		}
	}
	
	void showCloseList(float radius, Color color)
	{
		if(Close.Elements().Count == 0)
			Debug.LogWarning("Close List is Empty");
		else {
			foreach(ARAstarNode node in Close.Elements().Values)
			{
				Vector3 nodePosition = node.action.state.statePosition();
				Vector3 direction = node.action.state.statePosition() - node.previousState.statePosition();
				Gizmos.color = color;
				Gizmos.DrawSphere(node.action.state.statePosition(), radius);	
				DrawArrow.ForGizmo(node.previousState.statePosition(), direction, Color.black);
			}
		}
	}
	
	void showInconsList(float radius, Color color)
	{
		if(Incons.Elements().Count == 0)
			Debug.LogWarning("Inconsistent List is Empty");
		else {
			foreach(ARAstarNode node in Incons.Elements().Values)
			{
				Vector3 nodePosition = node.action.state.statePosition();
				Vector3 direction = node.action.state.statePosition() - node.previousState.statePosition();
				Gizmos.color = color;
				Gizmos.DrawSphere(node.action.state.statePosition(), radius);	
				DrawArrow.ForGizmo(node.previousState.statePosition(), direction, Color.black);
			}
		}
	}
	
	void showPlan(float radius, Color color)
	{
		if(Plan.Elements().Count == 0)
			Debug.LogWarning("Plan List is Empty");
		else {
			foreach(ARAstarNode node in Plan.Elements().Values)
			{
				Vector3 nodePosition = node.action.state.statePosition();
				Vector3 direction = node.action.state.statePosition() - node.previousState.statePosition();
				Gizmos.color = color;
				Gizmos.DrawSphere(node.action.state.statePosition(), radius);	
				DrawArrow.ForGizmo(node.previousState.statePosition(), direction, Color.black);
			}
		}
	}
	
	void showVisitedList(float radius, Color color)
	{
		if(Visited.Count == 0)
			Debug.LogWarning("Visited List is Empty");
		else{
			foreach(ARAstarNode node in Visited.Values)
			{
				Vector3 nodePosition = node.action.state.statePosition();
				Vector3 direction = node.action.state.statePosition() - node.previousState.statePosition();
				Gizmos.color = color;
				Gizmos.DrawSphere(node.action.state.statePosition(), radius);	
				DrawArrow.ForGizmo(node.previousState.statePosition(), direction, Color.black);
			}
		}
	}
}
