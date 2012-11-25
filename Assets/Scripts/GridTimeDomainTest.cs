using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class GridTimeDomainTest : MonoBehaviour {
	
	Dictionary<DefaultState,ARAstarNode> plan;
	Stack<DefaultState> outputPlan;
	public bool BestFirstSearchPlanner;
	ARAstarPlanner planner;
	BestFirstSearchPlanner BestFirstPlanner;
	
	// Use this for initialization
	void Start () {
	
		
		planner = new ARAstarPlanner();
		BestFirstPlanner = new BestFirstSearchPlanner();
		
		GetComponent<NeighbourObstacles>().Init ();
		
		GridTimeDomain domain = new GridTimeDomain(GetComponent<NeighbourObstacles>(), GetComponent<NeighbourAgents>());
		List<PlanningDomainBase> dl = new List<PlanningDomainBase>();
		dl.Add(domain);
		
		
		
		DefaultState startState = new GridTimeState(transform.position) as DefaultState;
		DefaultState goalState = new GridTimeState(new Vector3(5,0.5f,0),5.0f)  as DefaultState;
		plan = new Dictionary<DefaultState, ARAstarNode>();
		outputPlan = new Stack<DefaultState>();
		
		if(BestFirstSearchPlanner) {
			BestFirstPlanner.init(ref dl, 200);	
			bool completed = BestFirstPlanner.computePlan(ref startState, ref goalState, ref outputPlan, 10.0f);
			Debug.Log("BestFirst Plan " + outputPlan.Count);
			Debug.Log("Plan found: " + completed);
			
		} else {
			planner.init(ref dl,200);
			float inflationFactor = 2.5f;
			PathStatus status = planner.computePlan(ref startState ,ref goalState ,ref plan,ref inflationFactor, 10.0f);
			planner.FillPlan();
			Debug.Log ("ARA Plan " + plan.Count);
			Debug.Log("Nodes Expanded: " + planner.Close.Elements().Count);
			Debug.Log("Status: " + status); 
		}
		
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void OnDrawGizmos()
	{
		//Gizmos.color = Color.red;
		//Gizmos.DrawSphere(new Vector3(0,0,0),.25f);
		
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(new Vector3(5,0,0),.25f);
		Gizmos.color = Color.blue;
		
		if(BestFirstSearchPlanner){
			if(outputPlan == null) return;
			if(outputPlan.Count > 0){
				BestFirstPlanner.VisualizeContainer(ContainerType.Plan, Color.blue, .1f);
			}
			BestFirstPlanner.VisualizeContainer(ContainerType.Visited, Color.magenta, .1f);
			
		} else {
			if(plan == null) return;
			if(plan.Count > 0){
				planner.VisualizeContainer(ContainerType.Plan, Color.blue, .1f);
			}
			
			planner.VisualizeContainer(ContainerType.Visited, Color.magenta, .1f);
		}
	}
}


