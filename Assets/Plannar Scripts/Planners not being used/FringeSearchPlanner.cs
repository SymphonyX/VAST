using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class FringeSearchNode {
	
	public FringeSearchNode() { }
	public FringeSearchNode(float _g, float _f, ref DefaultState _previousStateRef, ref DefaultAction _nextActionRef) 
		{ g = _g; f = _f; previousState = _previousStateRef; action = _nextActionRef; parent = null;}

	public FringeSearchNode(float _g, float _f, ref DefaultState _previousStateRef, ref DefaultState _nextStateRef) 
			//: g(_g), f(_f), previousState(_previousState), alreadyExpanded(false)
		{
            g = _g; f = _f; previousState = _previousStateRef;
			parent = null;
			action = new DefaultAction();
			action.cost = 0.0f;
			action.state = _nextStateRef;
		}

	public float g;
	public float f;
	public DefaultState previousState;
	public DefaultAction action;
	public FringeSearchNode parent;
	};


class FringeSearchPlanner : IPlannerInterface<Dictionary<DefaultState, FringeSearchNode>>{
	
	public int _maxNumNodesToExpand;
	public List<PlanningDomainBase> _planningDomain;
	public Dictionary<DefaultState, FringeSearchNode> Cache;
	public int _capacity;
	
	public FringeSearchPlanner(int capacity)
	{ _capacity = capacity;}

		/// Initializes the planner to use the specified instance of the planning domain, and sets the search horizon limit.
	public void init(ref List<PlanningDomainBase> newPlanningDomain, int maxNumNodesToExpand ) {
			_maxNumNodesToExpand = maxNumNodesToExpand;
			_planningDomain = new List<PlanningDomainBase>(newPlanningDomain.Capacity);
			_planningDomain = newPlanningDomain;
		}
	
	public bool _computePlan(ref DefaultState startState, ref DefaultState idealGoalState, Dictionary<DefaultState, FringeSearchNode> Cache, ref DefaultState actualStateReached, float maxTime)
	{
		//Make sure Cache is empty when starting to compute plan
		
		PlanningDomainBase domain = default(PlanningDomainBase);

		float score = 0;
		foreach(PlanningDomainBase d in _planningDomain)
		{
			if(d.evaluateDomain(ref startState) > score){
				score = d.evaluateDomain(ref startState);
				domain = d;
			}
		}
		
		float newf = domain.estimateTotalCost(ref startState, ref idealGoalState, 0.0f);
		FringeSearchNode rootNode = new FringeSearchNode(0.0f, newf, ref startState, ref startState);
		rootNode.parent = null;
		
		LinkedList<FringeSearchNode> fringe =  new LinkedList<FringeSearchNode>();
		fringe.AddFirst(rootNode);
			
		
		
		foreach(KeyValuePair<DefaultState, FringeSearchNode> keyval in Cache)
		{
			if((keyval.Key as FringePlanningState).state.Equals((startState as FringePlanningState).state))
			{	Cache[keyval.Key] = rootNode; 
				break;}
		}
		
		
		
		//flimit = h(start)
		float flimit = rootNode.f - rootNode.g;
		
		while(fringe.Count > 0)
		{
			float fmin = Mathf.Infinity;
			
			LinkedListNode<FringeSearchNode> fringeNode = fringe.First;
			
			while(fringeNode != null)
			{
				FringeSearchNode node = fringeNode.Value;	
			
				FringeSearchNode tempNode = default(FringeSearchNode);
				foreach(DefaultState key in Cache.Keys)
				{
					if((key as FringePlanningState).state.Equals((node.action.state as FringePlanningState).state))
					{tempNode = Cache[key]; break;}
				}
				//FringeSearchNode tempNode = Cache[node.action.state];
				
			
				
				if(tempNode.f > flimit){
					fmin = Mathf.Min(tempNode.f,fmin);
					fringeNode = fringeNode.Next;
					continue;
				}
				
				if(domain.isAGoalState(ref tempNode.action.state, ref idealGoalState)){
					actualStateReached = tempNode.action.state;
					return true;
				}
				
				List<DefaultAction> possibleActions = new List<DefaultAction>();
            	possibleActions.Clear();
				
				domain.generateTransitions(ref tempNode.action.state, ref tempNode.previousState, ref idealGoalState, ref possibleActions);
				
				foreach(DefaultAction action in possibleActions)
				{
					float newg = tempNode.g + action.cost;
					newf = domain.estimateTotalCost(ref action.state, ref idealGoalState, newg);
					
					DefaultAction nextAction = action;
					FringeSearchNode successor = new FringeSearchNode(newg, newf, ref tempNode.action.state, ref nextAction);
					successor.parent = tempNode;
				 
					FringeSearchNode fn = default(FringeSearchNode);
					bool exists = false;
					foreach(KeyValuePair<DefaultState, FringeSearchNode> sn in Cache)
					{
						if((sn.Key as FringePlanningState).state.Equals((successor.action.state as FringePlanningState).state))
							if(sn.Value != null){
							{fn = sn.Value; exists = true; break;} 
						}
						
					}
					
					//if(Cache[successor.action.state] != null)
					if(exists){
						if(successor.g >= fn.g)
							continue;
					}
	
					//If fringe contains successor
					//if(fringe.Contains(successor))
					//	fringe.Remove(successor);
					foreach(FringeSearchNode s in fringe)
					{	
						if(s.action.state.Equals(successor.action.state))
							fringe.Remove(s);
					}
					
					
					fringe.AddAfter(fringe.Find(node), successor);
					//C[s]<-(gs, n) already added when node created
					
					foreach(KeyValuePair<DefaultState, FringeSearchNode> sn in Cache)
					{
						if((sn.Key as FringePlanningState).state.Equals((successor.action.state as FringePlanningState).state))
						{Cache[sn.Key] = successor; break;}
					}
					//Cache[successor.action.state].g = newg;
					//Cache[successor.action.state].parent = node; 
					
				}
				
				fringeNode = fringeNode.Next;
				fringe.Remove(tempNode);
			}
		flimit = fmin;
		}
		return false;
	}
	
	public	bool computePlan(ref DefaultState startState, ref DefaultState goalState, ref Stack<DefaultAction> plan, float maxTime )
        {		

		DefaultState s = default(DefaultState);
		DefaultAction a;

		bool isPlanComplete = _computePlan(ref startState,ref goalState, Cache, ref s, maxTime);
		
		//Debug.Log("Vector S:" + s);
		//Debug.Log("Start:" + startState);
		//Debug.Log("StateMap:" + stateMap.Count);
	
		
		
		// reconstruct path here
		foreach(KeyValuePair<DefaultState, FringeSearchNode> n in Cache)
		{
			if((n.Key as FringePlanningState).state.Equals((s as FringePlanningState).state))
			{
				FringeSearchNode parent = n.Value;
				while(parent != null)
				{
					plan.Push(parent.action);
					parent = parent.parent;
					
				}
			}
		}
		
           
		//Debug.Log("Plan:" + plan.Count);

		return isPlanComplete;
	}
	
	
	
}
