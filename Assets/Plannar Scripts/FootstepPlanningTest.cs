using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;


[System.Serializable]
public class Reproduction
{
	public enum MODE {NORMAL, RECORD, PLAYBACK};
	public MODE REPRODUCTION_MODE;
}



public class FootstepPlanningTest : Planner, AnimationInterface{
	
	/*
	public AnimationCurve curveX = new AnimationCurve();
	public AnimationCurve curveY = new AnimationCurve();
	public AnimationCurve curveZ = new AnimationCurve();
	*/
	
	private AnimationAnalyzer analyzer;
	
	public  Reproduction REPRODUCTION_MODE;
		
	//public int actionsToRecompute = 3;
	
	
	private MeshRenderer goalMeshRenderer;
	
	private Stack<DefaultAction> outputPlan;
	private Stack<DefaultAction> outputPlanBeforeTunnel;
	private List<FootstepPlanningAction> storedPlan;
	
	
	//private BestFirstSearchPlanner<PlanningDomainBase, FootstepPlanningState> planner;
	private BestFirstSearchPlanner planner;
	
	private FootstepPlanningState goalState;
	
	//private List<FootstepPlanningDomain> domainList;
	//private List<PlanningDomainBase<FootstepPlanningState>> domainList;	
	private List<PlanningDomainBase> domainList;
	private GridPlanningDomain gpd;
	private GridTimeDomain gtd;
	private FootstepPlanningDomain fpd;
	
	public float currentSpeed = 0.0f;
	
	public bool useFootstepDomain = false;
	public bool useGridTimeDomain = false;
	public bool useGridDomain = true;
	
	public bool TunnelSearch = true;
	
	public float tunnelThresholdD = 1.0f;
	//public float tunnelThresholdX = 1.0f;
	//public float tunnelThresholdZ = 1.0f;
	public float tunnelThresholdT = 1.0f;
	
	public Transform leftFoot; public Transform leftHand;
	public Transform rightFoot; public Transform rightHand;		
	
	
	void Awake(){
		initialized = false;			
	}
	
	// Use this for initialization
	public override void Init (AnimationAnalyzer animAnalyzer, NeighbourAgents neighborhood, NeighbourObstacles obstacles) {				
		
		analyzer = animAnalyzer;
		if (analyzer != null && !analyzer.initialized)
			analyzer.Init();
				
		storedPlan = new List<FootstepPlanningAction>();
		
		outputPlan = new Stack<DefaultAction>();
		
		domainList = new List<PlanningDomainBase>();
		
		gpd = new GridPlanningDomain(analyzer);
		//gtd = new GridTimeDomain(analyzer, obstacles);
		gtd = new GridTimeDomain(obstacles,neighborhood);
		fpd = new FootstepPlanningDomain(analyzer, neighborhood, obstacles);
		
		if (useGridDomain)
		{
			domainList.Add(gpd);		
			//domainList.Add(gpd);
		}	
		if (useGridTimeDomain)
		{
			domainList.Add(gtd);
			//domainList.Add(gtd);		
		}
		if (useFootstepDomain)
		{
			domainList.Add(fpd);		
			//domainList.Add(fpd);		
		}
		
		planner = new BestFirstSearchPlanner();
		
		planner.init(ref domainList, MaxNumberOfNodes);			
		
		initialized = true;	
		
		currentState = null;	
		
		isPlanComputed = false;
		
		planChanged = false;
		
		goalMeshRenderer = goalStateTransform.gameObject.GetComponent("MeshRenderer") as MeshRenderer;	
			
		currentGoal = goalStateTransform.position;
		
		goalReached = false;
		
	}
	
	/*
	void Update(){
		
		if (!initialized)
			return;
		
		// Track goal movement
		if (previousGoalPosition != goalStateTransform.position)
		{
			RecomputePlan();			
			previousGoalPosition = goalStateTransform.position;
		}		
		else
			if (newPlanRequired && !isPlanComputed)
			RecomputePlan();
		else
			planChanged = false;		
		
	}
	*/
	
	public override void RecomputePlan()
	{
		if (!initialized)
			return;
	
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
		if (analyzer != null)
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
			
	
	public static float timeSum = 0.0f;
	public static int numCalls = 0;
	
	public override bool ComputePlan()
	{	
		bool stateWasNull = false;
		
		if (currentState == null)
		{
			currentState = InitState();	
			stateWasNull = true;	
		}
		
		goalState = new FootstepPlanningState(currentGoal, Quaternion.identity,goalTime);
		
		outputPlan = new Stack<DefaultAction>();
		
		if (stateWasNull || goalReached && !stateWasNull)
		{			
			if (currentState.previousState != null)
			{
				float actionLength = currentState.time - currentState.previousState.time;				
				currentState.previousState.time = Time.time - actionLength;
			}					
			currentState.time = Time.time;		
		}		
		
		DefaultState DcurrentState = currentState as DefaultState;
		DefaultState DgoalState = goalState as DefaultState;	
		
		
		if (useGridDomain)
		{
			GridPlanningState currentGridState = new GridPlanningState(root.position);
			DcurrentState = currentGridState as DefaultState;
			
			GridPlanningState goalGridState = new GridPlanningState(currentGoal);
			DgoalState = goalGridState as DefaultState;
		}
		
		
		if (useGridTimeDomain)
		{
			GridTimeState currentTimeState = new GridTimeState(root.position, Time.time, currentSpeed);
			DcurrentState = currentTimeState as DefaultState;
			
			GridTimeState goalTimeState = new GridTimeState(currentGoal,goalTime);
			DgoalState = goalTimeState as DefaultState;
			
			//Debug.Log("current time = " + currentTimeState.time);
		}
		
		//float startTime = Time.realtimeSinceStartup;
		
		bool planComputed = planner.computePlan(ref DcurrentState, ref DgoalState, ref outputPlan, maxTime);
		
		/*************************/
		/* TUNNEL SEARCH TESTING */		
		//List<Vector3> movDirections = (domainList[0] as GridPlanningDomain).getMovDirections();
		//TunnelSearch tunnelSearch = new TunnelSearch(outputPlan, 3, analyzer, movDirections);
		
		if (TunnelSearch)
		{
			if (planComputed)
			{
				GridTunnelSearch gridTunnelSearch = new GridTunnelSearch(outputPlan, root.position, 
				Time.time, goalState.time, gtd, 
				MaxNumberOfNodes, maxTime, 
				//tunnelThresholdX, tunnelThresholdZ, tunnelThresholdT);
				tunnelThresholdD, tunnelThresholdT,
				currentSpeed);
			
				outputPlanBeforeTunnel = outputPlan;				
				outputPlan = gridTunnelSearch.newPlan;
				
				planComputed = gridTunnelSearch.goalReached;
				
				//Debug.Log("At time " + Time.time + " tunnel plan computed? " + planComputed);
			}
		}
		/*************************/
		
		/*
		float endTime = Time.realtimeSinceStartup;
		
		timeSum += (endTime - startTime);
		numCalls++;
		
		float meanTime = 0.0f;
		if (numCalls == 100)
		{
			meanTime = timeSum / numCalls;
			Debug.Log("At time " + Time.time + " Mean Computing Plan Time = " + meanTime);
			numCalls = 0;
			timeSum = 0;
		}
		*/
		
		if (goalMeshRenderer!= null && goalStateTransform.position == currentGoal)
		{
			if(planComputed && goalCompletedMaterial != null)
				goalMeshRenderer.material = goalCompletedMaterial; 
			else
				goalMeshRenderer.material = goalMaterial; 
		}	
		
		//Debug.Log("new plan! at time: " + Time.time + " with " + outputPlan.Count + " actions");
		
		
		
		goalReached = false;
		
		/*
		if (planComputed)
		{
			AnimationCurve[] curves = GetPlanAnimationCurve();	
			curveX = curves[0];
			curveY = curves[1];
			curveZ = curves[2];
		}
		*/
		
		return (planComputed && outputPlan.Count > 0);
	}

	
	public override FootstepPlanningAction getFirstAction()
	{
		FootstepPlanningAction firstAction = null;
		
		int plannedActions = outputPlan.Count;
		
		int i=0;
		bool isGridAction = false;
		while(i < plannedActions && firstAction == null && !isGridAction)
		{				
			DefaultAction defAction = outputPlan.Pop();
			firstAction = defAction as FootstepPlanningAction;
			
			if (firstAction == null)
			{								
				GridPlanningAction gridAction = defAction as GridPlanningAction;
				if (gridAction != null)
				{
					outputPlan.Push(defAction);
					isGridAction = true;	
				}
				else
				{
					GridTimeAction gridTimeAction = defAction as GridTimeAction;
					if (gridTimeAction != null)
					{
						outputPlan.Push(defAction);
						isGridAction = true;
					}
				}				
			}
			
			i++;						
		}
	
		if ( firstAction != null )
			currentState = firstAction.state as FootstepPlanningState;		
	
		return firstAction;
	}
	
	public AnimationCurve[] GetPlanAnimationCurve(Stack<DefaultAction> outputPlan)
	{
		int numKeys = outputPlan.Count + 1;
		
		AnimationCurve curveX = new AnimationCurve();
		AnimationCurve curveY = new AnimationCurve();
		AnimationCurve curveZ = new AnimationCurve();
		AnimationCurve curveSpeed = new AnimationCurve();
		
		
		float time = Time.time;
		Vector3 pos = currentStateTransform.position;
		float speed = currentSpeed;
		
		curveX.AddKey(time,pos.x);
		curveY.AddKey(time,pos.y);
		curveZ.AddKey(time,pos.z);
		curveSpeed.AddKey(time,speed);
		
		for(int i = 0; i < numKeys; i++)
		{
			DefaultAction action = outputPlan.ElementAt(i);
			if (action != null && action.state != null)
			{					
				GridTimeState gridTimeState = action.state as GridTimeState;
				if (gridTimeState != null)
				{
					time = gridTimeState.time;
					pos = gridTimeState.currentPosition;
					speed = gridTimeState.speed;
				}
				
				curveX.AddKey(time,pos.x);
				curveY.AddKey(time,pos.y);
				curveZ.AddKey(time,pos.z);
				curveSpeed.AddKey(time,speed);
			}					
		}
		
		AnimationCurve[] curves = new AnimationCurve[4];
		curves[0] = curveX;
		curves[1] = curveY;
		curves[2] = curveZ;
		curves[3] = curveSpeed;
		
		return curves;		
	}
	
	////////////// ANIMATION INTERFACE FUNCTION DEFINITIONS  //////////////
	
	public float GetCurrentTime ()
	{
		return Time.time;
	}
		
	public void SetStop ( bool t_stop )
	{
		
	}
	public void CompletedOffMeshLink ()
	{
	}	
	
	public void SetCurrentStatePosition (Vector3 position)
	{
	}
	
	public bool IsOnOffMeshLink ()
	{
		return false; 
	}
	
	public OffMeshLinkInformation GetCurrentOffMeshLinkInformation ()
	{
		return null;
	}
	
	public float GetCurrentSpeed ()
	{
		return currentSpeed;
	}
	
	public void SetCurrentSpeed (float t_currentSpeed)
	{
		currentSpeed = t_currentSpeed;		
	}
	
	public float RemainingDistance()
	{
		Vector3 toGoal = goalStateTransform.position - gameObject.transform.position;
		float distanceToGoal = toGoal.magnitude;
		return distanceToGoal;
	}		
	public AnimationCurve[] GetPlanAnimationCurve()
	{
		if (outputPlan == null)
			return null;
		
		int numKeys = outputPlan.Count;
		
		AnimationCurve curveX = new AnimationCurve();
		AnimationCurve curveY = new AnimationCurve();
		AnimationCurve curveZ = new AnimationCurve();
		AnimationCurve curveSpeed = new AnimationCurve();
		
		
		
		
		float time = Time.time;
		Vector3 pos = currentStateTransform.position;
		float speed = currentSpeed;
		
		curveX.AddKey(time,pos.x);
		curveY.AddKey(time,pos.y);
		curveZ.AddKey(time,pos.z);
		curveSpeed.AddKey(time,speed);
		
		for(int i = 0; i < numKeys; i++)
		{
			DefaultAction action = outputPlan.ElementAt(i);
			if (action != null && action.state != null)
			{					
				FootstepPlanningState fsState = action.state as FootstepPlanningState;
				if (fsState != null)
				{
					time = fsState.time;
					pos = fsState.currentPosition;					
					speed = fsState.currentSpeed;
				}
				else
				{
					GridPlanningState gridState = action.state as GridPlanningState;
					if (gridState != null)
					{
						time += 1.0f;
						pos = gridState.currentPosition;						
					}
					else
					{
						GridTimeState gridTimeState = action.state as GridTimeState;
						if (gridTimeState != null)
						{
							time = gridTimeState.time;
							pos = gridTimeState.currentPosition;
							speed = gridTimeState.speed;
						}
					}
				}
				
				curveX.AddKey(time,pos.x);
				curveY.AddKey(time,pos.y);
				curveZ.AddKey(time,pos.z);
				curveSpeed.AddKey(time,speed);
			}					
		}		
		
		AnimationCurve[] curves = new AnimationCurve[4];
		curves[0] = curveX;
		curves[1] = curveY;
		curves[2] = curveZ;
		curves[3] = curveSpeed;
		
		return curves;		
	}
	
	////////////////////////////////////////////////////////////////////////////////////
	
	
	public override FootstepPlanningAction[] GetOutputPlan(){

		if (outputPlan == null)
			return null;
		
		FootstepPlanningAction[] actionList = new FootstepPlanningAction[outputPlan.Count];
		int i=0;
		int count=0;
		
//		Debug.Log("OutputPlan: " + outputPlan.Count);
		
		while(i < outputPlan.Count)
		{	
			DefaultAction action = outputPlan.ElementAt(i);
			
			//Debug.Log("we have an action " + i);
			
			FootstepPlanningAction fsAction = action as FootstepPlanningAction;
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
			
			else
			{				
				GridPlanningAction gridAction = action as GridPlanningAction;
				if (gridAction != null)
				{
					fsAction = new FootstepPlanningAction(gridAction,analyzer.GetAnotatedAnimation("Idle"),1.5f);
					
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
					
					//Debug.Log("OutputPlan at time " + Time.time + " has " + outputPlan.Count + " actions.");					
					//Debug.Log("Grid Action added at time: " + Time.time + " at pos " + count);					
				}
				
				else
				{
					//Debug.Log("We might add a GridTimeAction");
					
					GridTimeAction gridTimeAction = action as GridTimeAction;
					if (gridTimeAction != null)
					{
						if (analyzer != null)
							fsAction = new FootstepPlanningAction(gridTimeAction,analyzer.GetAnotatedAnimation("Idle"),1.0f);		
												
						if (fsAction != null)
						{
							//Debug.Log("Adding a gridTimeAction");
							
							actionList[count] = fsAction;				
							count++;
							if(REPRODUCTION_MODE.REPRODUCTION_MODE.Equals(Reproduction.MODE.RECORD))
							{								
								storedPlan.Add(fsAction);
								//Debug.Log("Recording");						
							}	
						}
					}
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
		
	public override bool HasExpired()
	{
		bool expired = false;
		
		GridTimeAction lastAction = outputPlan.Last() as GridTimeAction;
		if (lastAction != null)
		{
			GridTimeState lastState = lastAction.state as GridTimeState;
			if (lastState != null)
			{
				float lastTime = lastState.time;
				if (Time.time > lastTime)
					expired = true;
			}
		}
		else
		{
			FootstepPlanningAction lastFsAction = outputPlan.Last () as FootstepPlanningAction;
			if (lastFsAction != null)
			{
				FootstepPlanningState lastFsState = lastFsAction.state as FootstepPlanningState;
				if (lastFsState != null)
				{
					float lastTime = lastFsState.time;
					if (Time.time > lastTime)
						expired = true;
				}
			}
		}
		
		return expired;
	}
	
	public override bool ThereStillTime()
	{
		return Time.time < goalState.time;	
	}
	
	public void SwitchPlans()
	{
		Stack<DefaultAction> auxPlan = outputPlan;
		outputPlan = outputPlanBeforeTunnel;
		outputPlanBeforeTunnel = auxPlan;
	}
	

	
	/*
	// Update is called once per frame
	void Update () {
	
		
	}
	*/
}

