using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ADAFootstepTest : Planner {
	
	private AnimationAnalyzer analyzer;
	public bool useThisPlanner = false;
	public static bool ADA_PLANNER_IN_USE = false;
	
	public  Reproduction REPRODUCTION_MODE;
		
	//public int actionsToRecompute = 3;
	
	public float inflationFactor;
	
	private MeshRenderer goalMeshRenderer;
	
	private Stack<DefaultAction> outputPlan;
	private List<FootstepPlanningAction> storedPlan;
		
	//private BestFirstSearchPlanner<PlanningDomainBase, FootstepPlanningState> planner;
	private ADAstarPlanner planner;
	
	private FootstepPlanningState goalState;
	
	private bool planInitialized = false; 
	
	//private List<FootstepPlanningDomain> domainList;
	//private List<PlanningDomainBase<FootstepPlanningState>> domainList;	
	private List<PlanningDomainBase> domainList;
	
	
	public Transform leftFoot; public Transform leftHand;
	public Transform rightFoot; public Transform rightHand;	
	
	
	void Awake(){
		initialized = false;			
		ADA_PLANNER_IN_USE = useThisPlanner;
	}
	
	// Use this for initialization
	public override void Init (AnimationAnalyzer animAnalyzer, NeighbourAgents neighborhood, NeighbourObstacles obstacles) {				
		
		analyzer = animAnalyzer;
		if (analyzer != null && !analyzer.initialized)
			analyzer.Init();
				
		storedPlan = new List<FootstepPlanningAction>();
		
		outputPlan = new Stack<DefaultAction>();
		
		domainList = new List<PlanningDomainBase>();
		domainList.Add(new FootstepPlanningDomain(analyzer, neighborhood, obstacles));

		planner = new ADAstarPlanner();
		
		planner.init(ref domainList, MaxNumberOfNodes);			
		
		initialized = true;	
		
		currentState = null;	
		
		isPlanComputed = false;
		
		planChanged = false;
		
		goalMeshRenderer = goalStateTransform.gameObject.GetComponent("MeshRenderer") as MeshRenderer;	
		
		
		currentGoal = goalStateTransform.position;
		
	}
	
	public override void RecomputePlan()
	{
		if (!initialized)
			return;
		if(ADAstarPlanner.Closed != null
		   && outputPlan != null && ADAstarPlanner.startFound)
		{
		//ADAstarPlanner.startFound = false;
		//ADAstarPlanner.Closed.Clear();
		//outputPlan.Clear();
		}
		
		isPlanComputed = ComputePlan();
		planChanged = true;
	}
	
	public override int GetNumberOfDomains()
	{
		return domainList.Count;	
	}
	
	public override bool CheckDomainStateCollisions(int d, FootstepPlanningState state)
	{
		return domainList.ElementAt(d).CheckStateCollisions(state);	
	}
		
	
	public FootstepPlanningState InitState()
	{
		analyzer.RemoveAnimations(animation);
		
		//FootstepPlanningState initState = new FootstepPlanningState(root.position, currentStateTransform.rotation,0);
		FootstepPlanningState initState = new FootstepPlanningState(root.position, root.rotation,Time.time);
		initState.leftFoot = leftFoot.position;
		initState.rightFoot = rightFoot.position;
		initState.leftHand = leftHand.position;
		initState.rightHand = rightHand.position;
		
		initState.previousState = initState;
		
		return initState;
	}
	
	public override bool ComputePlan()
	{	
		if (currentState == null)
			currentState = InitState();		
		
		goalState = new FootstepPlanningState(goalStateTransform.position, Quaternion.identity,goalTime);
		currentGoal = goalStateTransform.position;

		if(!planInitialized){
			outputPlan = new Stack<DefaultAction>();
			planInitialized = true;
		}
		
		
		currentState.time = Time.time;
		
		DefaultState DcurrentState = currentState as DefaultState;
		DefaultState DgoalState = goalState as DefaultState;
		
		//Debug.Log("START: " + currentState.currentPosition);
		//Debug.Log("START TRANSFORM: " + currentStateTransform.position);
		//Debug.Log("GOAL: " + goalState.currentPosition);
		Debug.Log("GOAL TRANSFORM: " + goalStateTransform.position);
		
		
		bool planComputed = planner.computePlan(ref DcurrentState, ref DgoalState, ref outputPlan, inflationFactor, maxTime);
				
		if (goalMeshRenderer!= null && goalStateTransform.position == currentGoal)
		{
			if(planComputed && goalCompletedMaterial != null)
				goalMeshRenderer.material = goalCompletedMaterial; 
			else
				goalMeshRenderer.material = goalMaterial; 
		}	
		
		return (planComputed && outputPlan.Count > 0);
	}

	
	public override FootstepPlanningAction getFirstAction()
	{
		FootstepPlanningAction firstAction = null;
		
		int i=0;
		Debug.Log("OUTPUT PLAN: " + outputPlan.Count);
		while(i < outputPlan.Count && firstAction == null)
		{				
			firstAction = outputPlan.Pop() as FootstepPlanningAction;			
			i++;						
		}
	
		if ( firstAction != null )
			currentState = firstAction.state as FootstepPlanningState;
	
		return firstAction;
	}
	
	
	public override FootstepPlanningAction[] GetOutputPlan(){
		
		FootstepPlanningAction[] actionList = new FootstepPlanningAction[outputPlan.Count];
		int i=0;
		int count=0;
		while(i < outputPlan.Count)
		{				
			FootstepPlanningAction fsAction = outputPlan.ElementAt(i) as FootstepPlanningAction;
			if (fsAction != null)
			{				
				actionList[count] = fsAction;				
				count++;
				if(REPRODUCTION_MODE.REPRODUCTION_MODE.Equals(Reproduction.MODE.RECORD))
				{
					storedPlan.Add(fsAction);
					//Debug.Log("Recording");
				}
			}
			i++;
						
		}
		
		return actionList;
		
	}
	
	public override bool IsEmpty()
	{
		return outputPlan.Count == 0;	
	}
}
