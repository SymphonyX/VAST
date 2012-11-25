using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;




public class ADAstarNode {
	
	public ADAstarNode() { }
	public ADAstarNode(float _g, float _h, ref DefaultState _previousStateRef, ref DefaultAction _nextActionRef) 
		{ g = _g; h = _h; previousState = _previousStateRef; action = _nextActionRef; alreadyExpanded = false; predecessors = new List<ADAstarNode>();	}

	public ADAstarNode(float _g, float _h, float _rhs, ref DefaultState _previousStateRef, ref DefaultState _nextStateRef) 
			//: g(_g), f(_f), previousState(_previousState), alreadyExpanded(false)
		{
            g = _g; h = _h; previousState = _previousStateRef; 
			alreadyExpanded = false;
			action = new DefaultAction();
			action.cost = 0.0f;
			action.state = _nextStateRef;
			predecessors = new List<ADAstarNode>();
		}
	

	public float g;
	public float rhs;
	public float h;
	public float key1, key2;
	public DefaultState previousState;
	public List<ADAstarNode> predecessors;
	public DefaultAction action;
	public bool alreadyExpanded;	
	};


	/**
	 * @brief A functor class used to compare the costs of two BestFirstSearchNodes.
	 *
	 * @see
	 *   - Documentation of the BestFirstSearchPlanner class, which describes how states and actions are used.
	 */
	/*public class ADACompareCosts  {
	    ADAstarNode node1, node2;
 
        public static int CompareCost(BestFirstSearchNode n1, BestFirstSearchNode n2)
        { 
			if (n1.f != n2.f) 
            	 if(n1.f > n2.f)
					return 1;
				else  
					return -1;
			else
				if(n1.g > n2.g) return 1;
				else if(n1.g < n2.g) return -1;
				else return 0;
        }
	};*/

  public class ADAstartCopareCost  {
	    KeyValuePair<DefaultState, ADAstarNode> node1, node2;
 
        public static int CompareCost(KeyValuePair<DefaultState, ADAstarNode> n1, KeyValuePair<DefaultState, ADAstarNode> n2)
        { 
			if (n1.Value.key1 != n2.Value.key1) 
            	 if(n1.Value.key1 > n2.Value.key1)
					return 1;
				else  
					return -1;
			else
				if(n1.Value.key2 > n2.Value.key2) return 1;
				else if(n1.Value.key2 < n2.Value.key2) return -1;
				else return 0;
        }
	};



 public class Edge{
	
		public Edge(DefaultState _u, DefaultState _v, float _c)
		{ u = _u; v = _v; cost = previousCost = _c;} 
		
		public DefaultState u;
		public DefaultState v;
		public float cost;
		public float previousCost;
	};

public class PlannerMode{
	public enum PLANNING_MODE {IncreaseFactor, FromScratch};
	public PLANNING_MODE MODE;
}

class ADAstarPlanner : IPlannerInterface<Dictionary<DefaultState, ADAstarNode>>{
	
        public float edgeChangeThreshold;
	    public int _maxNumNodesToExpand;
	    public List<PlanningDomainBase> _planningDomain;
		float inflationFactor;
		public static Dictionary<DefaultState, ADAstarNode> Closed, Incons;
		public static List<KeyValuePair<DefaultState, ADAstarNode>> Open; 
		List<KeyValuePair<DefaultState, ADAstarNode>> BackUp;
		public bool changeInEdgeCost = false;
		ADAstarNode startNode, goalNode;
		public Dictionary<DefaultState[], Edge> edgeList;
		int maxNodes = 0;
		bool firstTime = true;
		bool startPreviouslyFound = false;
		
		public static bool startFound = false;
		public PlannerMode PLANNER_MODE;
	

		/// Initializes the planner to use the specified instance of the planning domain, and sets the search horizon limit.
	public void init(ref List<PlanningDomainBase> newPlanningDomain, int maxNumNodesToExpand) {
			_maxNumNodesToExpand = maxNumNodesToExpand;
			_planningDomain = new List<PlanningDomainBase>(newPlanningDomain.Capacity);
			_planningDomain = newPlanningDomain;
			edgeList = new Dictionary<DefaultState[], Edge>();
			maxNodes = maxNumNodesToExpand;
			BackUp = new List<KeyValuePair<DefaultState, ADAstarNode>>();
		}
	
	float[] GetKey(ADAstarNode node)
	{
		float[] returnValue = new float[2];
		//Distance node to goal
		if(node.g>node.rhs)	{
			 returnValue[0] = node.rhs + inflationFactor*node.h;
			 returnValue[1] = node.rhs;}
		else{
			returnValue[0] = node.g + node.h;
			returnValue[1] = node.g;
		}
		
		return returnValue;
	}
	
	int CompareKey(float[] first, float[] second)
	{
		if(first[0] > second[0]) return 1;
		else if(first[0] < second[0]) return -1;
		else {
			if(first[1] > second[1]) return 1;
			else if(first[1] < second[1]) return -1;
			else return 0;
		}
		
	}
	
	void UpdateState(ref ADAstarNode currentNode)
	{
		PlanningDomainBase domain = default(PlanningDomainBase);
		List<DefaultAction> possibleTransitions = new List<DefaultAction>();
		
		float score = 0;
		foreach(PlanningDomainBase d in _planningDomain)
		{
			if(d.evaluateDomain(ref startNode.action.state) > score){
				score = d.evaluateDomain(ref startNode.action.state);
				domain = d;
			}
		}
		
		if(!currentNode.alreadyExpanded)
			currentNode.g = Mathf.Infinity;
		if(!domain.isAGoalState(ref currentNode.action.state, ref goalNode.action.state))
		{
			possibleTransitions.Clear();
			domain.generateTransitions(ref currentNode.action.state, ref currentNode.previousState, ref goalNode.action.state, ref possibleTransitions);			
			
			// Determine min(c(s,s')+g(s')) for rhs for every successor
			float min_rhs = Mathf.Infinity;
			foreach(DefaultAction action in possibleTransitions)
			{
				DefaultAction nextAction = action;
               	float newh = domain.ComputeEstimate(ref startNode.action.state, ref nextAction.state, "h");
				float newg = domain.ComputeEstimate(ref nextAction.state, ref goalNode.action.state, "g");
				//g is calculated as the distance to the goal, just use a dummy value -1.0 and calculate the distance next
               	ADAstarNode nextNode = new ADAstarNode(newg, newh, ref currentNode.action.state, ref nextAction);
			
				if((nextAction.cost + nextNode.g) < min_rhs)
					min_rhs = nextAction.cost + nextNode.g;		
			}			
			currentNode.rhs = min_rhs;
			float[] keys = GetKey(currentNode);
			currentNode.key1 = keys[0]; currentNode.key2 = keys[1];
		}
		Debug.Log("A");
		//If open contains node, remove it.
		//foreach(KeyValuePair<DefaultState, ADAstarNode> keyval in Open)
		for(int i=0; i < Open.Count; ++i)	
		{	if(Open[i].Key != null)
			{
			if(domain.equals(Open[i].Key,currentNode.action.state, false)){
				Open.RemoveAt(i); currentNode.alreadyExpanded = true;	
			}		
			}
		}
		//Open = BackUp;
		//KeyValuePair<DefaultState, ADAstarNode> keyval = new KeyValuePair<DefaultState, ADAstarNode>(currentNode.action.state, currentNode);
		//if(Open.Contains(keyval)) {Open.Remove(keyval); currentNode.alreadyExpanded = true;}
		
		if(currentNode.g != currentNode.rhs){
			bool containsNode = false;
			//foreach(DefaultState key in Closed.Keys)
			//{
				//if(domain.equals(key, currentNode.action.state))
				//if(domain.equals(key, currentNode.action.state, false))
				//{ containsNode = true; break; }
			//}
			if(Closed.ContainsKey(currentNode.action.state)) containsNode = true;
			if(!containsNode){
				
				//Generate all predecessors to keep expanding the open list
				generateNodePredecessors(ref domain, ref currentNode); 
				
				Open.Add(new KeyValuePair<DefaultState, ADAstarNode>(currentNode.action.state, currentNode));
				//Sort by priority keys
				Open.Sort(ADAstartCopareCost.CompareCost);
				}
			else{
				Incons.Add(currentNode.action.state, currentNode);	
			}
		}		
	}
	
	void generateNodePredecessors(ref PlanningDomainBase domain, ref ADAstarNode currentNode)
	{
			
		List<DefaultAction> possibleTransitions = new List<DefaultAction>();
		domain.generatePredecesors(ref currentNode.action.state, ref currentNode.previousState, ref goalNode.action.state, ref possibleTransitions);
		
		foreach(DefaultAction predecessorAction in possibleTransitions)
		{
			DefaultAction previousAction = predecessorAction;
			float newg = domain.ComputeEstimate(ref previousAction.state, ref goalNode.action.state, "g");
			float newh = domain.ComputeEstimate(ref startNode.action.state, ref previousAction.state, "h");
			ADAstarNode previousNode = new ADAstarNode(newg, newh, ref predecessorAction.state, ref previousAction);
			
			currentNode.predecessors.Add(previousNode);
					
			//If edge does not exist in the list, add it
			
			if(!edgeList.ContainsKey(new DefaultState[]{previousNode.action.state, currentNode.action.state}))
				edgeList.Add(new DefaultState[]{previousNode.action.state, currentNode.action.state}, 
							new Edge(previousNode.action.state, currentNode.action.state, previousNode.action.cost));
			else {//If it exists update cost value if there was any change
					if(edgeList[new DefaultState[]{previousNode.action.state, currentNode.action.state}].cost != currentNode.action.cost)
					{
						//If there was a change in the cost, set bool to true and update the current cost
						changeInEdgeCost = true;
						edgeList[new DefaultState[]{previousNode.action.state, currentNode.action.state}].cost = currentNode.action.cost;
					}
						
			}
		}
	}
	

	
	void ComputeorImprovePath(ref PlanningDomainBase domain, float maxTime)
	{
		int i =0;
		int nodesExpanded = 0;
		Debug.Log("BLLLLLLLAAAAAAAAAAA");
		float prevTime = Time.realtimeSinceStartup;
		Debug.Log("OPEN: " + Open.Count);
		//Open is sorted with lowest key first, so first element always has the smallest key
		//while((ADAstarPlanner.startFound==false) && (nodesExpanded < maxNodes) && (maxTime > 0))
	    while((CompareKey(GetKey(Open.First().Value), GetKey(startNode))==-1 || startNode.rhs != startNode.g) 
		      && (nodesExpanded < maxNodes) && (maxTime > 0))
		{
			i++;
			Debug.Log("Planning: " + i);
			if( i == 48)
			{
				i=i;	
			}
			
			
			if(domain.equals(Open.First().Key, startNode.action.state, true) || startNode.rhs == startNode.g)
			{ADAstarPlanner.startFound = true; Debug.Log("FOUND"); break;}
			
				
			KeyValuePair<DefaultState, ADAstarNode> currentPair = Open.First();
			ADAstarNode currentNode = currentPair.Value;
			
			
			Open.Remove(currentPair);
			currentNode.alreadyExpanded = true;
			nodesExpanded++;
			
			if(currentNode.g > currentNode.rhs) {
				currentNode.g = currentNode.rhs;
				Closed.Add(currentNode.action.state, currentNode);
								
				
				//For all s in pred(s) updateState
				foreach(ADAstarNode predecessor in currentNode.predecessors){
					ADAstarNode pred = predecessor;
					UpdateState(ref pred);
				}
			}
			else {
				currentNode.g = Mathf.Infinity;
				
				//For all s in pred(s) U s updateState
				UpdateState(ref currentNode);
				foreach(ADAstarNode predecessor in currentNode.predecessors)
				{
					ADAstarNode pred = predecessor;
					UpdateState(ref pred);
				}
			}
			float actualTime = Time.realtimeSinceStartup;
			maxTime -= (actualTime-prevTime);
			prevTime = actualTime;
		}
	}
	
	

public void InitializeValues(ref DefaultState startState, ref DefaultState goalState, float _inflationFactor, ref Stack<DefaultAction> plan, PlannerMode.PLANNING_MODE mode, float threshold, float maxTime)
{
//		PLANNER_MODE.MODE = mode;
		PlanningDomainBase domain = default(PlanningDomainBase);
		edgeChangeThreshold = threshold;

		float score = 0;
		foreach(PlanningDomainBase d in _planningDomain)
		{
			if(d.evaluateDomain(ref startState) > score){
				score = d.evaluateDomain(ref startState);
				domain = d;
			}
		}
		
		
		Open = new List<KeyValuePair<DefaultState, ADAstarNode>>();
		Closed = new Dictionary<DefaultState, ADAstarNode>();
		Incons = new Dictionary<DefaultState, ADAstarNode>();
		Open.Clear(); Closed.Clear(); Incons.Clear();
		
		// h estimate from start to start is 0
		float starth = domain.ComputeEstimate(ref startState, ref startState, "h");
        startNode = new ADAstarNode(Mathf.Infinity, starth, Mathf.Infinity, ref startState, ref startState);
		startNode.key1 = Mathf.Infinity; startNode.key2 = Mathf.Infinity;
		
		float goalh = domain.ComputeEstimate(ref startState, ref goalState, "h");
			
		goalNode = new ADAstarNode(Mathf.Infinity, goalh, 0.0f, ref goalState, ref goalState);
		goalNode.key1 = GetKey(goalNode)[0]; goalNode.key2 = GetKey(goalNode)[1];
		inflationFactor = _inflationFactor;
		generateNodePredecessors(ref domain, ref goalNode);
		
		Open.Add(new KeyValuePair<DefaultState, ADAstarNode>(goalNode.action.state, goalNode));
		
		ComputeorImprovePath(ref domain, maxTime);

}
	
	//An edge is just a node using previous state, action cost as the edge cost, and action state
public void InifiniteUpdate(ref Dictionary<DefaultState[], Edge> edges, ref Dictionary<DefaultState, ADAstarNode> nodes, ref Stack<DefaultAction> plan, float maxTime, ref DefaultState startState)
{
		float score = 0.0f;
		PlanningDomainBase domain = default(PlanningDomainBase);
		foreach(PlanningDomainBase d in _planningDomain)
		{
			if(d.evaluateDomain(ref startState) > score){
				score = d.evaluateDomain(ref startState);
				domain = d;
			}
		}
		
		
		/*float maxChange = 0.0f;
		if(changeInEdgeCost || inflationFactor != 1.0f)
		{
		
			foreach(Edge edge in edges.Values)
			{
				if(edge.previousCost != edge.cost)
				{
					maxChange = Mathf.Abs(edge.cost - edge.previousCost);
					edge.previousCost = edge.cost;
					ADAstarNode n = nodes[edge.u];
					UpdateState(ref n);
				}
			}
		}
			
		if(maxChange > edgeChangeThreshold) //Decide either increase inflation factor or replan from scratch
		{
			if(PLANNER_MODE.MODE.Equals(PlannerMode.PLANNING_MODE.IncreaseFactor)){
				inflationFactor += .1f;
			}
			else if(PLANNER_MODE.MODE.Equals(PlannerMode.PLANNING_MODE.FromScratch)){
				ComputeorImprovePath(ref domain, maxTime );
			}
					//	inflationFactor += .5; // Search for a good value to increase 
					//OR
					//ComputeorImprovePath();
		}			*/
		//else 
		if(inflationFactor > 1.0f)
		{
			inflationFactor -= .2f; //Decide decrease amount
		}
				//Decrease inflationFactor by some amount
			
			//Move states from incons to open
		foreach(KeyValuePair<DefaultState, ADAstarNode> keyVal in Incons)
		{
			Open.Add(keyVal);
		}
		Incons.Clear();
		foreach(KeyValuePair<DefaultState, ADAstarNode> keyval in Open)
		{
			keyval.Value.key1 = GetKey(keyval.Value)[0];
			keyval.Value.key2 = GetKey(keyval.Value)[1];
		}
		Open.Sort(ADAstartCopareCost.CompareCost);
		//Closed.Clear();
			
			//computeimprove path
		ComputeorImprovePath(ref domain, maxTime);
			//publish solution					
}
		
	
	
	//We don't need this function. only defined because its required by the interface
	public bool _computePlan(ref DefaultState startState, ref DefaultState idealGoalState, Dictionary<DefaultState, ADAstarNode> map, ref DefaultState actualStateReached, float maxTime)
	{
		//ComputeorImprovePath();	
		return false; //dummy return value, the loop is infinite
	}
	
	public bool computePlan(ref DefaultState currentState, ref DefaultState goalState, ref Stack<DefaultAction> plan, float inflation, float maxTime)
	{
	
		if(!startFound)
		{
		if(firstTime){
			InitializeValues(ref currentState, ref goalState, inflation, ref plan, PlannerMode.PLANNING_MODE.IncreaseFactor, .5f, maxTime);
			firstTime = false;
			Debug.Log("GOAL: " + (ADAstarPlanner.Closed.First().Value.action.state as FootstepPlanningState).currentPosition);

		}
		else {
			InifiniteUpdate(ref edgeList, ref Closed, ref plan, maxTime, ref currentState);
		}
		}
			
		Debug.Log("Plan: " + plan.Count);
		Debug.Log("CLOSED: " + Closed.Count);
		
		if(startFound && !startPreviouslyFound)
		{
			//plan.Clear();
			//ADAstarNode n = Closed.First().Value;
			   
			foreach(KeyValuePair<DefaultState, ADAstarNode> keyval in Closed)
			{
				plan.Push(keyval.Value.action);	
			}
			//plan.Reverse();
			startPreviouslyFound = true;
			Closed.Clear();
			return true;
		}
		//If we have previously found it
		else if(startFound)
			return true;
		else
			return false;
	}
	
public	void publishPlan(ref Stack<DefaultAction> plan, ref PlanningDomainBase domain)
	{		
			
		
	}
	
	//Sequence of function to call
	//In Start()
	//InitializeValues
	//Get plan from stack
	//In Update()
	//inifiteUpdate()
	//Get plan from stack
	
	};



	


