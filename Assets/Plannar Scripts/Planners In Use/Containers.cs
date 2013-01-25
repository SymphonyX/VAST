using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

//************************************************************//
//************	CLOSE CONTAINER   ****************************//
//************************************************************//

public class CloseContainer
{
	Dictionary<DefaultState, ARAstarNode> close;
	public CloseContainer()
	{
		close = new Dictionary<DefaultState, ARAstarNode>();
	}
	
	public void Insert(ARAstarNode node)
	{
		if(close.ContainsKey(node.action.state)){
			if(close[node.action.state].g > node.g)
				close[node.action.state] = node;
		}
		else{
			close[node.action.state] = node;	
		}
	}
	
	public List<DefaultState> keys()
	{
		return close.Keys.ToList();	
	}
	
	public bool Contains(DefaultState state)
	{
		return close.ContainsKey(state);	
	}
	
	public Dictionary<DefaultState, ARAstarNode> Elements()
	{
		return close;	
	}
	
	public void Clear()
	{
		close.Clear();	
	}
	
	public void UpdateReferences(float inflationFactor, PlanningDomainBase domain)
	{
		List<DefaultState> neighborsList = new List<DefaultState>();
		foreach(KeyValuePair<DefaultState, ARAstarNode> node in close)
		{
			if(node.Value.weightExpanded > inflationFactor)
			{
				float best_g = Mathf.Infinity;
				node.Value.weightExpanded = inflationFactor;
				domain.generateNeighbors(node.Key, ref neighborsList);
				foreach(DefaultState neighbor in neighborsList)
				{
					if(close.ContainsKey(neighbor))
					{
						if(close[neighbor].g < best_g){
							best_g = close[neighbor].g;
							close[neighbor].weightExpanded = inflationFactor;
							close[node.Key].previousState = neighbor;
							//close[node.Key].action = new ARAstarAction(neighbor, node.Key);
							close[node.Key].action = domain.generateAction(neighbor, node.Key);
						}
					}
				}
			}
		}
	}
}

public class VisitedContainer
{
	public Dictionary<DefaultState, ARAstarNode> dictionary;
	PlanningDomainBase domain;
	DefaultState startState;
	
	public VisitedContainer(PlanningDomainBase d)
	{
		dictionary = new Dictionary<DefaultState, ARAstarNode>();
		domain = d;
	}
	
	public bool ContainsState(DefaultState state)
	{
		return dictionary.ContainsKey(state);	
	}
	
	public ARAstarNode nodeForState(DefaultState state)
	{
		return dictionary[state];	
	}
	
	public void insertNode(ref ARAstarNode node)
	{
		dictionary[node.action.state] = node;
	}
		
	public void UpdateList (ARAstarNode currentState)
	{
		startState = currentState.action.state;
		Queue<ARAstarNode> queue = new Queue<ARAstarNode> ();
		queue.Enqueue (currentState);
		int n = 0;
		while (queue.Count > 0) {
			ARAstarNode state = queue.Dequeue ();
			UpdateState (ref state, ref queue);
			n++;
		}
		
		foreach (ARAstarNode node in dictionary.Values) {
			node.isDirty = false;
			node.touched = false;
			node.updated = false;
			dictionary[node.action.state].isDirty = false;
			dictionary[node.action.state].touched = false;
			dictionary[node.action.state].updated = false;
		}
	}

	void UpdateState (ref ARAstarNode state, ref Queue<ARAstarNode> queue)
	{
		List<DefaultAction> neighborsList = new List<DefaultAction>();
		domain.generatePredecessors(state.action.state, ref neighborsList);
		foreach (DefaultAction action in neighborsList) {
			if(dictionary.ContainsKey(action.state) && !dictionary[action.state].updated)
			{
				UpdateCost (ref state, action.state);
				UpdateReference (action.state);
				if(!dictionary[action.state].updated)
					dictionary[action.state].updated = true;
					queue.Enqueue (dictionary[action.state]);
			}
		}
	}

	void UpdateCost (ref ARAstarNode node, DefaultState successor)
	{
		if(!dictionary[successor].touched)
		{
			dictionary[successor].g = 
				node.g + Vector3.Distance((node.action.state as ARAstarState).state,(successor as ARAstarState).state);
			dictionary[successor].touched = true;
			dictionary[successor].g = dictionary[successor].rhs;
		}
		else if(dictionary[successor].g > 
		        node.g + Vector3.Distance((node.action.state as ARAstarState).state, (successor as ARAstarState).state))
		{
			dictionary[successor].g = 
				node.g + Vector3.Distance((node.action.state as ARAstarState).state, (successor as ARAstarState).state);
			dictionary[successor].g = dictionary[successor].rhs;
		}
		
	}

	void UpdateReference (DefaultState successor)
	{
		
		List<DefaultAction> neighborsList = new List<DefaultAction>();
		domain.generatePredecessors(successor, ref neighborsList);
		foreach (DefaultAction action in neighborsList) {
			if (isNeighborToStartAndPreviousStateIsNotStart (successor, action.state)) {
				dictionary[successor].previousState = startState;
				
				//openDictionary[successor].action = new ARAstarAction(neighbor, successor);
				dictionary[successor].action = domain.generateAction(action.state, successor);
				dictionary[successor].isDirty = true;
				break;
			} else if (dictionary.ContainsKey(action.state) && dictionary[action.state].isDirty) {
				if (dictionary[successor].previousState != null && predIsDirtyWithLeastCost (successor, action.state)) {
					dictionary[successor].previousState = action.state;					
					
					//openDictionary[successor].action = new ARAstarAction(neighbor, successor);
					dictionary[successor].action = domain.generateAction(action.state, successor);
					dictionary[successor].isDirty = true;
				}
			}
			
		}
	}
	
	bool predIsDirtyWithLeastCost (DefaultState successor, DefaultState neighbor)
	{
		
		DefaultState prevState = dictionary[successor].previousState;
		if(!dictionary.ContainsKey(prevState)) return false;
		return (dictionary[prevState].g > dictionary[neighbor].g);
	}
	
	bool isNeighborToStartAndPreviousStateIsNotStart (DefaultState successor, DefaultState neighbor)
	{
		return (neighbor.Equals(startState) && !dictionary[successor].previousState.Equals(startState));
	}
}

//*******************************************************************//
//*******************   OPEN CONTAINER   ****************************//
//*******************************************************************//

public class OpenContainer
{
	List<ARAstarNode> openList;
	ARAstarHeap openHeap;
	Dictionary<DefaultState, ARAstarNode> openDictionary;
	ARAstarPlanner parentPlanner;
	bool useHeap;
	public DefaultState startState;
	int debug = 0;

	public OpenContainer (ARAstarPlanner planner, bool _useHeap = true)
	{
		parentPlanner = planner;
		openList = new List<ARAstarNode> ();
		openHeap = new ARAstarHeap(planner.inflationFactor);
		openDictionary = new Dictionary<DefaultState, ARAstarNode> ();
		useHeap = _useHeap;
		Debug.Log("Using Heap: " + useHeap);
	}

	public List<DefaultState> keys()
	{
		return openDictionary.Keys.ToList();	
	}
	
	public Dictionary<DefaultState, ARAstarNode> Elements()
	{
		return openDictionary;	
	}
	
	int CompareCost (ARAstarNode n1, ARAstarNode n2)
	{
		float[] n1Key = n1.Key(parentPlanner.inflationFactor);
		float[] n2Key = n2.Key(parentPlanner.inflationFactor);
		if(n1.highPriority > 0 && n2.highPriority <= 0)
			return -1;
		else if(n1.highPriority <= 0 && n2.highPriority > 0)
			return 1;
		else{ 
			if (n1Key[0] < n2Key[0])
				return -1;
			else if(n1Key[0] > n2Key[0])
				return 1;
			else {
				if(n1Key[1] < n2Key[1])
					return -1;
				else
					return 1;
			}
		}
	}
	
	public void clearHighPriority()
	{
		if(useHeap){
			//Find an efficient way to traverse the tree and break when a node high priority is already false
			foreach(ARAstarHeapNode heapNode in openHeap.heap)
			{
				heapNode.node.highPriority = 0;	
			}
		} else {
			foreach(ARAstarNode node in openList)
			{
				if(node.highPriority == 0)
					return;
				node.highPriority = 0;	
			}
		}
	}
	
	public ARAstarNode Node(DefaultState state)
	{
		return openDictionary[state];	
	}
	
	public ARAstarNode First ()
	{
		int count = useHeap ? openHeap.Count() : openList.Count;
		if ( count == 0 )
		{
			Debug.LogWarning("open list is empty -- still trying to access first element");
			return null;
		}
		else{
			DefaultState state = useHeap ? openHeap.HeapMaximum().action.state : openList.First().action.state;
			return openDictionary[state];
		}
			
	}
	
	public int ListCount()
	{
		if(useHeap)
			return openHeap.Count();
		else
			return openList.Count();
	}

	public void Remove (DefaultState state)
	{
		if(useHeap)
			openHeap.Remove(openDictionary[state]);
		else
			openList.Remove(openDictionary[state]);
		openDictionary.Remove (state);
	}
	

	public void Insert (ARAstarNode node)
	{
		
		if (openDictionary.ContainsKey (node.action.state)) {
			if (openDictionary[node.action.state].g > node.g) {
				if(useHeap){
					openHeap.Remove(openDictionary[node.action.state]);
					openHeap.Insert (node);
				} else {
					openList.Remove(openDictionary[node.action.state]);
					openList.Add(node);
				}
				openDictionary[node.action.state] = node;
			}
		} else {
			openDictionary[node.action.state] = node;
			if(useHeap) 
				openHeap.Insert (node);
			else
				openList.Add(node);
		}
	}

	public void Sort ()
	{
		if(useHeap){
			openHeap.currentWeight = parentPlanner.inflationFactor;
			openHeap.Heapify();
		} else {
			openList.Sort (CompareCost);
		}
	}


	
	
	public void UpdateHeuristic(DefaultState goal)
	{
		if(useHeap){
			foreach(ARAstarHeapNode heapNode in openHeap.heap)
			{
				ARAstarNode n = heapNode.node;
				float newh = Vector3.Distance((n.action.state as ARAstarState).state, (goal as ARAstarState).state);
				n.h = newh;
				openDictionary[n.action.state].h = newh;
			}
		} else {
			foreach(ARAstarNode n in openList)
			{
				float newh = Vector3.Distance((n.action.state as ARAstarState).state, (goal as ARAstarState).state);
				n.h = newh;
				openDictionary[n.action.state].h = newh;
			}	
		}
	}
	
	public bool ContainsState(DefaultState state)
	{
		return openDictionary.ContainsKey(state);
	}
	
	public void Clear()
	{
		openList.Clear();
		openDictionary.Clear();
	}
	
}

//**********************************************************************//
//*********************   PLAN CONTAINER   *****************************//
//**********************************************************************//

class PlanContainer
{
	Dictionary<DefaultState, ARAstarNode> plan;
	public PlanContainer(Dictionary<DefaultState, ARAstarNode> _plan)
	{
		plan = _plan;	
	}
	
	public ARAstarNode Node(DefaultState st)
	{
		return plan[st];	
	}
	
	public void Remove(DefaultState state)
	{
		plan.Remove(state);
	}
	
	public void InsertNode(ref DefaultState st, ref ARAstarNode node)
	{
		plan[st] = node;	
	}
	
	public void UpdateCosts(float cost)
	{
		foreach(ARAstarNode n in plan.Values)
		{
			n.g -= cost;	
		}	
	}
	
	public void UpdateHeuristic(DefaultState goal)
	{
		foreach(ARAstarNode n in plan.Values)
		{
			n.h = Vector3.Distance((n.action.state as ARAstarState).state, (goal as ARAstarState).state);	
		}
	}
	
	public Dictionary<DefaultState, ARAstarNode> Elements()
	{
		return plan;	
	}
	
	public void Fill (ref CloseContainer Close, Dictionary<DefaultState, ARAstarNode> Visited, ref DefaultState stateReached, PlanningDomainBase domain, ref DefaultState current, ref KeyValuePair<DefaultState, ARAstarNode> goalPair, float inflationFactor)
	{
		//DefaultState s = goalPair.Key;
		//Close.Insert(goalPair.Value);
		plan.Clear();
		DefaultState s;
		if(Visited.ContainsKey(goalPair.Key))
			s = stateReached = goalPair.Key;
		else
			s = stateReached; 
		DefaultAction a;
		bool done = false;
		/*foreach(ARAstarNode planNode in plan.Values)
		{
			Close.Insert(planNode);	
		}
		plan.Clear();
		
		// TODO : check if we still need this function 
		Close.UpdateReferences(inflationFactor, domain);*/	
		do {
			if (domain.equals (s, current, false))
					done = true;
			if(Visited.ContainsKey(s)){
				plan[s] = Visited[s];
				s = Visited[s].previousState;
			}
			else{
				break;	
			}
			
		} while (!done);
		//updatePlanReference(domain);
		
	}
	
	void updatePlanReference(PlanningDomainBase domain)
	{
		List<DefaultState> neighborsList = new List<DefaultState>();
		foreach(DefaultState state in plan.Keys)
		{
			float ming = Mathf.Infinity;
			domain.generateNeighbors(state, ref neighborsList);
			foreach(DefaultState neighbor in neighborsList)
			{
				if(plan.ContainsKey(neighbor))
				{
					if(plan[neighbor].g < ming)
					{
						ming = plan[neighbor].g;
						plan[state].previousState = neighbor;
						
						//plan[state].action = new ARAstarAction(neighbor, state);
						plan[state].action = domain.generateAction(neighbor, state);
					}
				}
			}
		}
	}
	
	public bool ContainsState(DefaultState state)
	{
		return plan.ContainsKey(state);	
	}
	
	
	public void Clear ()
	{
		plan.Clear ();
	}
}

//*********************************************************//
//*************   INCONSISTENT CONTAINER   ****************//
//*********************************************************//

public class Incons
{
	Dictionary<DefaultState, ARAstarNode> incons;

	public Incons ()
	{
		incons = new Dictionary<DefaultState, ARAstarNode> ();
	}

	public void Insert (ARAstarNode node)
	{
		if (incons.ContainsKey (node.action.state)) {
			if (incons[node.action.state].g > node.g) {
				incons[node.action.state] = node;
			}
		} else {
			incons[node.action.state] = node;
		}
	}
	
	public Dictionary<DefaultState, ARAstarNode> Elements()
	{
		return incons;	
	}
	
	public List<DefaultState> keys()
	{
		return incons.Keys.ToList();	
	}

	public void MoveToOpen (ref OpenContainer open)
	{
		List<ARAstarNode> templist = new List<ARAstarNode> ();
		templist = incons.Values.ToList ();
		foreach (ARAstarNode node in templist) {
			open.Insert (node);
			incons.Remove (node.action.state);
		}
		open.Sort ();
	}
	
	public void Clear ()
	{
		incons.Clear ();
	}
}