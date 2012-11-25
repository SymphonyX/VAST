using UnityEngine;
using System.Collections;
using System.Linq;

// Component for debug purposes
// It instantiates footsteps representing the plan of the agent
// It also instantiate a footstep for where the agent is going to place the foot
// at his current step

public class PlaceFootSteps : MonoBehaviour {
	
	//private FootstepPlanningTest planning;
	private Planner planning;
	private AnimationAnalyzer analyzer;
	private AnimationEngine engine;
	
	public int numberOfFootSteps = 10;
	public bool draw = true; 
	public bool debugDraw = false;
	public bool debugOnlyCurrent = false;
	public bool drawTimes = false;
	
	public TextMesh debugText;
	
	public GameObject leftFootStep;
	public GameObject rightFootStep;	
	public GameObject redFootStep;
	public GameObject sphere;
	public GameObject gridTimeSphere;
	
	// DEBUG
	private Vector3 lastObstacle;
	private Vector3 lastPos;
	
	private Object[] texts;
	private Object[] footSteps;
	private Object[] steps;
	
	private Object[] texts2;
	private Object[] steps2;
	private Vector3[] steps2Pos;
	
	
	private Vector3[] predictedStepsPositions;
	private Quaternion[] predictedStepsRotations;
	private Quaternion[] rootRotations;
	private Vector3[] rootOrientations;
	
	private int counter;	
	
	public Transform root;
	public Transform rightFoot;
	public Transform leftFoot;
		
	[HideInInspector] public bool footstepsPlaced;
	
	private float auxHeight;
	
	private int numberOfActions;
	
	[HideInInspector] public bool initiated;	
	
	public bool drawTunnelPath = false;
	
	void Awake(){
		initiated = false;
	}
	
	//public void Init(AnimationAnalyzer animAnalyzer, FootstepPlanningTest planner, AnimationEngine animEngine,
	public void Init(AnimationAnalyzer animAnalyzer, Planner planner, AnimationEngine animEngine,
	                 NeighbourAgents agents, NeighbourObstacles obstacles)
	{
		analyzer = animAnalyzer;
		if (analyzer != null && !analyzer.initialized)
			analyzer.Init();
		
		planning = planner;	
		if (planning != null && !planning.initialized)
			planning.Init(analyzer, agents, obstacles);
		
		engine = animEngine;
		if (engine != null && !engine.initialized)
			engine.Init(analyzer,planning,agents,obstacles);
		
		if (rightFoot.position[1] < leftFoot.position[1])
			auxHeight = rightFoot.position[1];	
		else
			auxHeight = leftFoot.position[1];	
		
		counter = 0;
		
		footSteps = new Object[numberOfFootSteps];
		
		initiated = true;
	}
	
	void Start(){
		
	}
	
	void OnDrawGizmos() 
	{
		
		
		if (!debugDraw)
			return;
		
		if (planning == null)
			return;
		
		/*
		//if(firstTime)
		//{
		foreach(ADAstarNode node in ADAstarPlanner.Closed.Values)		
		{
			if(node.action.GetType() == typeof(FootstepPlanningAction)){
			Vector3 center = ((node.action as FootstepPlanningAction).state as FootstepPlanningState).currentPosition;
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(center, .25f);
			}
		}
		//	firstTime = false;
		//}
		*/
		
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(lastPos,analyzer.GetRadius());
		Gizmos.DrawWireSphere(lastObstacle,1.0f);
		
		//if (debugOnlyCurrent)
		//	return;
		
		FootstepPlanningAction[] plan = planning.GetOutputPlan();
		
		numberOfActions = plan.Length;
				
		for ( int i=0; i<numberOfActions; i++)
		{
			if (plan[i] != null)
			{
				FootstepPlanningState state = plan[i].state as FootstepPlanningState;
								
				if (state != null)
				{					
					AnotatedAnimation animInfo = analyzer.GetAnotatedAnimation(state.actionName);
					
					//Quaternion rotation = state.currentRotation;
					
					Vector3 position;
					
					if ( animInfo.swing == Joint.LeftFoot )
					{					
						position = state.leftFoot;					
						position[1] = auxHeight;					
						Gizmos.color = Color.green;
					}
					else
					{						
						position = state.rightFoot;					
						position[1] = auxHeight;
						Gizmos.color = Color.blue;
					}
					
					Gizmos.DrawWireSphere(state.currentPosition,analyzer.GetRadius());
					Gizmos.DrawWireSphere(state.obstaclePos,1.0f);				
				}
				else
				{
					GridTimeState gtState = plan[i].state as GridTimeState;
					
					if (gtState != null)
					{
						Vector3 position = gtState.currentPosition;
						if (i % 2 == 0)
							Gizmos.color = Color.green;
						else
							Gizmos.color = Color.blue;
												
						if (!debugOnlyCurrent || (debugOnlyCurrent && Mathf.Abs(Time.time - gtState.time) < 0.5f) )
						{
							Gizmos.DrawWireSphere(gtState.currentPosition,analyzer.GetRadius());
						
							Gizmos.color = Color.red;
							Gizmos.DrawWireSphere(gtState.obstaclePos,1.0f);				
						}
					}
				}
			}			
		}		
	}
	
	//public void ExecutePlaceFootSteps(){	
	void LateUpdate(){
		
		if (!initiated)
			return;
		
		if (debugDraw)
			//DebugDraw(root);
			DebugDraw(gameObject.transform);
		
		if (planning.planChanged)
		{
			if (footstepsPlaced)
				DeleteSequenceFootSteps();
				
			if (draw && !footstepsPlaced)
			{
				PlaceSequenceFootSteps();
				
				if (drawTunnelPath)
				{					
					(planning as FootstepPlanningTest).SwitchPlans();					
					PlaceSequenceFootSteps(true);
					(planning as FootstepPlanningTest).SwitchPlans();
				}
			}
		}
		
		if (engine != null)
		{
			if (draw && engine.changed)
				PlaceFootStep();
		}
	}
	
	public void DeleteSequenceFootSteps()
	{
		if (steps != null)
			foreach ( GameObject step in steps )
				if (step != null)
					GameObject.DestroyObject(step);	
	
		if (texts != null)
			foreach ( TextMesh text in texts )
				if (text != null)
					TextMesh.DestroyObject(text);	
	
		if (steps2 != null)
			foreach ( GameObject step in steps2 )
				if (step != null)
					GameObject.DestroyObject(step);	
	
		if (texts2 != null)
			foreach ( TextMesh text in texts2 )
				if (text != null)
					TextMesh.DestroyObject(text);
		
		footstepsPlaced = false;
	}
	
	
	
	public void PlaceSequenceFootSteps(bool tunnel = false)
	{	
		FootstepPlanningAction[] plan = planning.GetOutputPlan();
		
		if (plan == null)
			return;
		
		numberOfActions = plan.Length;
		
		//Debug.Log("Number of actions = " + numberOfActions);
		
//		Debug.Log("Footsteps " +  this.gameObject.name + " " + numberOfActions);
		
		if (!tunnel)
			steps = new Object[numberOfActions];
		else
		{
			steps2 = new Object[numberOfActions];
			steps2Pos = new Vector3[numberOfActions];
		}
			
		if (drawTimes)
		{
			if (!tunnel)
				texts = new Object[numberOfActions];
			else
				texts2 = new Object[numberOfActions];
		}
			
		for ( int i=0; i<numberOfActions; i++)
		{
			if (plan[i] != null)
			{
				FootstepPlanningState state = plan[i].state as FootstepPlanningState;
								
				if (state != null)
				{					
					AnotatedAnimation animInfo = analyzer.GetAnotatedAnimation(state.actionName);
					
					Quaternion rotation = state.currentRotation;
					
					Vector3 position;
					
					if ( animInfo.swing == Joint.LeftFoot )
					{					
						position = state.leftFoot;					
						position[1] = auxHeight;
						
						if (!tunnel)
							steps[i] = GameObject.Instantiate((Object)leftFootStep,position,rotation);
						else
							steps2[i] = GameObject.Instantiate((Object)leftFootStep,position,rotation);
						
						debugText.alignment = TextAlignment.Left;
					}
					else
					{						
						position = state.rightFoot;					
						position[1] = auxHeight;					
								
						if (!tunnel)
							steps[i] = GameObject.Instantiate((Object)rightFootStep,position,rotation);					
						else
							steps2[i] = GameObject.Instantiate((Object)leftFootStep,position,rotation);
						
						debugText.alignment = TextAlignment.Left;
					}
					
					if (drawTimes)
					{
						debugText.text = "" + state.time;					
						if (!tunnel)
							texts[i] = GameObject.Instantiate((Object)debugText,position,rotation);
						else
							texts2[i] = GameObject.Instantiate((Object)debugText,position,rotation);
					}
				}
				
				else // if state is not a footstep state (it is a grid state or a gridTime state)
				{
					// we instantiate a gridstep
					
					GridTimeState gridTimeState = plan[i].state as GridTimeState;
					if (gridTimeState != null)
					{
						//Debug.Log("we place a gridTimeStep");
						if (!tunnel)
							steps[i] = GameObject.Instantiate((Object)gridTimeSphere,gridTimeState.currentPosition,Quaternion.identity);
						else
						{
							steps2[i] = GameObject.Instantiate((Object)gridTimeSphere,gridTimeState.currentPosition,Quaternion.identity);
							steps2Pos[i] = gridTimeState.currentPosition;
						}
						
						if (drawTimes)
						{
							debugText.text = "" + gridTimeState.time;					
							if (!tunnel)
								texts[i] = GameObject.Instantiate((Object)debugText,gridTimeState.currentPosition,Quaternion.identity);
							else
								texts2[i] = GameObject.Instantiate((Object)debugText,gridTimeState.currentPosition,Quaternion.identity);
						}
					}					
					else{
						//Debug.Log("We Should have a gridPlanningStep");
						
						GridPlanningState gridState = plan[i].state as GridPlanningState;
						if (gridState != null)
						{
							if (!tunnel)
								steps[i] = GameObject.Instantiate((Object)sphere,gridState.currentPosition,Quaternion.identity);
							else
							{
								steps2[i] = GameObject.Instantiate((Object)sphere,gridState.currentPosition,Quaternion.identity);
								steps2Pos[i] = gridState.currentPosition;
							}
						}
					}
				}
			}			
		}
		
		footstepsPlaced = true;
	}
	
	
	public void PlaceFootStep () {
		
		if (engine == null ||  engine.actionNum < 1)
			return;
		
		FootstepPlanningAction currentAction = engine.action;
		
		if (currentAction != null)
		{						
			AnotatedAnimation animInfo = currentAction.animInfo;
			if (animInfo != null)
			{
				Vector3 translation = new Vector3(0,0,0);
								
				//Quaternion quat = transform.rotation;
				Quaternion quat = Quaternion.Euler(new Vector3(0,transform.eulerAngles.y,0));
								
				int endSample = animInfo.LeftFoot.Length-1;
				
				if ( animInfo.swing == Joint.LeftFoot )
					translation = animInfo.LeftFoot[endSample].position;
				else
					translation = animInfo.RightFoot[endSample].position;										 ;										
				
				Vector3 newFootStepPos = /*engine.lastRootPos*/transform.position + quat * translation;								
					
				newFootStepPos[1] = auxHeight;					
				
				counter = (counter)%numberOfFootSteps;
					
				GameObject.Destroy(footSteps[counter]);
				footSteps[counter] = GameObject.Instantiate((Object)redFootStep,newFootStepPos,quat);	
						
				FootstepPlanningState state = currentAction.state as FootstepPlanningState;
				lastPos = state.currentPosition;
				lastObstacle = state.obstaclePos;
				
				counter++; 
			}					
		}
	}
	
	
	private void DebugDraw(Transform t)
	{
		//Debug.DrawLine(t.position,t.position-t.right*2,Color.cyan);
		
		//Debug.DrawLine(t.position,t.position-Vector3.up,Color.magenta);
		
		if (steps2Pos != null)
		{
			for (int i = 0;	i < steps2Pos.Length-2; i++)
				Debug.DrawLine(steps2Pos[i],steps2Pos[i+1]);
		}
	}
	
}
	

