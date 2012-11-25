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

//*******************************************************************//
//*******************   OPEN CONTAINER   ****************************//
//*******************************************************************//

public class OpenContainer
{
	List<ARAstarNode> openList;
	ARAstarHeap openHeap;
	Dictionary<DefaultState, ARAstarNode> openDictionary;
	PlanningDomainBase domain;
	ARAstarPlanner parentPlanner;
	bool useHeap;
	public DefaultState startState;
	int debug = 0;

	public OpenContainer (ARAstarPlanner planner, PlanningDomainBase _domain, bool _useHeap = true)
	{
		parentPlanner = planner;
		openList = new List<ARAstarNode> ();
		openHeap = new ARAstarHeap(planner.inflationFactor);
		openDictionary = new Dictionary<DefaultState, ARAstarNode> ();
		domain = _domain;
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


	public void UpdateList (ARAstarNode currentState)
	{
		startState = currentState.action.state;
		Queue<ARAstarNode> queue = new Queue<ARAstarNode> ();
		queue.Enqueue (currentState);
		while (queue.Count > 0) {
			ARAstarNode state = queue.Dequeue ();
			UpdateState (state, ref queue);
		}
		
		if(useHeap){
			foreach (ARAstarHeapNode heapNode in openHeap.heap) {
				ARAstarNode node = heapNode.node;
				node.isDirty = false;
				node.touched = false;
				node.updated = false;
				openDictionary[node.action.state].isDirty = false;
				openDictionary[node.action.state].touched = false;
				openDictionary[node.action.state].updated = false;
			}
		} else{
			foreach (ARAstarNode node in openList) {
				node.isDirty = false;
				node.touched = false;
				node.updated = false;
				openDictionary[node.action.state].isDirty = false;
				openDictionary[node.action.state].touched = false;
				openDictionary[node.action.state].updated = false;
			}
			
		}
	}

	void UpdateState (ARAstarNode state, ref Queue<ARAstarNode> queue)
	{
		List<DefaultState> neighborsList = new List<DefaultState>();
		domain.generateNeighbors(state.action.state, ref neighborsList);
		foreach (DefaultState successor in neighborsList) {
			if(openDictionary.ContainsKey(successor) && !openDictionary[successor].updated)
			{
				UpdateCost (state, successor);
				UpdateReference (successor);
				if(!openDictionary[successor].updated)
					openDictionary[successor].updated = true;
					queue.Enqueue (openDictionary[successor]);
			}
		}
	}

	void UpdateCost (ARAstarNode node, DefaultState successor)
	{
		if(!openDictionary[successor].touched)
		{
			openDictionary[successor].g = 
				node.g + Vector3.Distance((node.action.state as ARAstarState).state,(successor as ARAstarState).state);
			openDictionary[successor].touched = true;
		}
		else if(openDictionary[successor].g > 
		        node.g + Vector3.Distance((node.action.state as ARAstarState).state, (successor as ARAstarState).state))
		{
			openDictionary[successor].g = 
				node.g + Vector3.Distance((node.action.state as ARAstarState).state, (successor as ARAstarState).state);	
		}
		
		if(useHeap)
			openHeap.Remove(openDictionary[successor]);
		else
			openList.Remove(openDictionary[successor]);
		Insert(node);
	}

	void UpdateReference (DefaultState successor)
	{
		
		List<DefaultState> neighborsList = new List<DefaultState>();
		domain.generateNeighbors(successor, ref neighborsList);
		foreach (DefaultState neighbor in neighborsList) {
			if (isNeighborToStartAndPreviousStateIsNotStart (successor, neighbor)) {
				openDictionary[successor].previousState = startState;
				
				//openDictionary[successor].action = new ARAstarAction(neighbor, successor);
				openDictionary[successor].action = domain.generateAction(neighbor, successor);
				openDictionary[successor].isDirty = true;
				break;
			} else if (openDictionary.ContainsKey(neighbor) && openDictionary[neighbor].isDirty) {
				if (openDictionary[successor].previousState != null && predIsDirtyWithLeastCost (successor, neighbor)) {
					openDictionary[successor].previousState = neighbor;					
					
					//openDictionary[successor].action = new ARAstarAction(neighbor, successor);
					openDictionary[successor].action = domain.generateAction(neighbor, successor);
					openDictionary[successor].isDirty = true;
				}
			}
			
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

	bool isNeighborToStartAndPreviousStateIsNotStart (DefaultState successor, DefaultState neighbor)
	{
		return (neighbor.Equals(startState) && !openDictionary[successor].previousState.Equals(startState));
	}

	bool predIsDirtyWithLeastCost (DefaultState successor, DefaultState neighbor)
	{
		
		DefaultState prevState = openDictionary[successor].previousState;
		if(!openDictionary.ContainsKey(prevState)) return false;
		return (openDictionary[prevState].g > 
		        openDictionary[neighbor].g);
	}
	
	public bool ContainsState(DefaultState state)
	{
		return openDictionary.ContainsKey(state);
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