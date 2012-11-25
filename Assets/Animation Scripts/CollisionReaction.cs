using UnityEngine;
using System.Collections;

public class CollisionReaction : MonoBehaviour {
	
	//private FootstepPlanningTest planning;
	private Planner planning;
	private AnimationAnalyzer analyzer;
	private AnimationEngine engine;
	
	public bool meshCollisions;
	
	// Joint Transforms
	public Transform root;
	public Transform leftFoot; public Transform leftHand;
	public Transform rightFoot; public Transform rightHand;		
	
	[HideInInspector] public bool initialized;		
	
	[HideInInspector] public bool reacting;
	
	void Awake()
	{
		initialized = false;
	}	
	
	//public void Init(AnimationAnalyzer animAnalyzer, FootstepPlanningTest planner, AnimationEngine animEngine, NeighbourAgents agents, NeighbourObstacles obstacles)
	public void Init(AnimationAnalyzer animAnalyzer, Planner planner, AnimationEngine animEngine,NeighbourAgents agents, NeighbourObstacles obstacles)
	{
		analyzer = animAnalyzer;
		if (analyzer != null && !analyzer.initialized)
			analyzer.Init();
		
		planning = planner;	
		if (planning != null && !planning.initialized)
			planning.Init(analyzer, agents, obstacles);
		
		engine = animEngine;
		if (engine != null && !engine.initialized)
			engine.Init(analyzer,planning, agents, obstacles);
		
		reacting = false;
		
		initialized = true;
	}	

	
	public bool FrameCollisionCheck()
	{
		if (!initialized)
			return false;
		
		// we create a state determining the current frame
		FootstepPlanningState frameState = CreateFrameState();
		
		return CollisionCheck(frameState);
	}
	
	public FootstepPlanningState CreateFrameState()
	{
		if (!initialized)
			return null;
		
		/*
		Quaternion rot;		
		if (planning.currentState != null)
			rot = planning.currentState.currentRotation;
		else
		{
			float initAngle = this.gameObject.transform.eulerAngles.y;	
			rot = Quaternion.AngleAxis(initAngle,new Vector3(0,1,0));
		}		
		
		FootstepPlanningState frameState;
		if (engine.action != null && engine.action.state != null)
		{
			frameState = new FootstepPlanningState(engine.action.state as FootstepPlanningState);
		
			frameState.currentPosition = root.position;			
			frameState.currentRotation = rot;
		
			frameState.time = Time.time;
		}
		else
		{
			frameState = new FootstepPlanningState(root.position,rot,Time.time);
		}
		*/
		
		//FootstepPlanningState frameState = new FootstepPlanningState(engine.lastRootPos,engine.lastRootRot,Time.time);
		FootstepPlanningState frameState = new FootstepPlanningState(transform.position,transform.rotation,Time.time);	
		
		frameState.leftFoot = leftFoot.position;
		frameState.rightFoot = rightFoot.position;
		frameState.leftHand = leftHand.position;
		frameState.rightHand = rightHand.position;	
		
		return frameState;		
	}
	
	public bool CurrentActionCollisionCheck()
	{
		if (engine == null)
			return false;
		
		if (!initialized)
			return false;
		
		if (engine.action == null)
			return false;
				
		FootstepPlanningState actionEndState = engine.action.state as FootstepPlanningState;
		if (actionEndState != null)
			return CollisionCheck(actionEndState);
		else
			return false;
	}
	
	// We are going to check for collisions from a coarse resolution to a finer grain resolution
	private bool CollisionCheck(FootstepPlanningState state)
	{	
		bool isColliding = true;
		
		// For every domain we have, we are going to make a collision check, from the coarser to the finer
		int d = 0;
		int numberOfDomains = planning.GetNumberOfDomains();
		while ( isColliding && d < numberOfDomains)
		{
			isColliding = planning.CheckDomainStateCollisions(d,state);			
			d++;
		}
		
		// if collisions are not found in some of the checks we return false
		if (!isColliding)
			return false;		
		
		if (meshCollisions)
		{
			// if collisions are found for all our previous checks		
			// We are going a make a final check at the coarser possible resolution
			// using the whole mesh			
			return MeshCollisionCheck();
		}
		else 
			return true;
	}
	
	private bool MeshCollisionCheck()
	{
		
		
		return false;
	}
	
	public void React()
	{
		FootstepPlanningState frameState = CreateFrameState();
		
		animation.Stop();
		
		
		AnotatedAnimation stopAnim = analyzer.GetAnotatedAnimation("Idle");
		
		FootstepPlanningAction stopAction = new FootstepPlanningAction(frameState,stopAnim,1,analyzer.meanStepSize, analyzer.mass);				
			
		engine.InsertAction(stopAction);	
		
		reacting = true;
	}

}
