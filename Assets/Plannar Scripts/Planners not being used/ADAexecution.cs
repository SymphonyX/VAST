using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ADAexecution : MonoBehaviour {
	
	public GameObject startObject, goalObject;
	ADAstarAction action;
	bool can_i_plan=true, planExecuted=false;
	int iteration = 0;
	bool moving = false;
	public static ADAstarState startState, goalState;
	Stack<DefaultAction> plan;
	
	void fillPlan()
	{				
		if(startState.state!=goalState.state)
		{
			foreach(KeyValuePair<DefaultState, ADAstarNode> keyval in ADAstarPlanner.Closed)
			{
				plan.Push(keyval.Value.action);
			}
		
			foreach(ADAstarNode closedNode in ADAstarPlanner.Closed.Values)
			{
				if((closedNode.action.state as ADAstarState).state != goalState.state){
					Debug.Log((closedNode.action.state as ADAstarState).state);
					Debug.Log((closedNode.action as ADAstarAction));
				}
			}
		
			planExecuted = false;
			can_i_plan = false;
		}		
		
	}
	
	void moveAgent()
	{
		if(startState.state!=goalState.state)
		{
			if(!moving && plan.Count > 0){	
				action = plan.Pop() as ADAstarAction;
				moving = true;
			}
			else if(moving && plan.Count > 0)
			{				
				startObject.transform.Translate(-action.direction);
				startState.state = startObject.transform.position;
					//_moveAgent((-action.direction)/10);
				iteration++;
				if(iteration==10)
				{ iteration=0; moving=false;}
			}
		}
		else
		{
			moving = false;
			planExecuted=true;	
			can_i_plan = true;
			plan.Clear();
		}
		
	}
	
	// Use this for initialization
	void Start () {
		startState = new ADAstarState(startObject.transform.position);
		goalState = new ADAstarState(goalObject.transform.position);
		plan = new Stack<DefaultAction>();
	
	}
	
	
	
	// Update is called once per frame
	void Update () {
	//	if(goalState.state!=goalObject.transform.position)
	//		goalState.state = goalObject.transform.position;
	//	if(startState.state!=startObject.transform.position)
	//		startState.state = startObject.transform.position;
		if(ADAstarPlanner.startFound && can_i_plan)
			fillPlan();
		if(!planExecuted)
			moveAgent();
		
		
	}
}
