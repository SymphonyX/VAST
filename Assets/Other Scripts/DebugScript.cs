using UnityEngine;
using System.Collections;

// Component for debugging purposes. 
// It uses a TextMesh to print whatever messages we want over the agent

public class DebugScript : MonoBehaviour {
	
	public TextMesh debugText;
	
	public bool debug;
	
	public Transform root;
	
	private float startTime;
	private float timeLeft;
	
	private AnimationEngine engine;
	private AnimationAnalyzer analyzer;
	//private FootstepPlanningTest planning;
	private Planner planning;
	private CollisionReaction collision;
	
	[HideInInspector] public bool initialized;
	
	void Awake()
	{
		initialized = false;
	}
	
	//public void Init (AnimationAnalyzer animAnalyzer, AnimationEngine animEngine, FootstepPlanningTest planner,
	public void Init (AnimationAnalyzer animAnalyzer, AnimationEngine animEngine, Planner planner,
	                  NeighbourAgents agents, NeighbourObstacles obstacles, CollisionReaction colReact) {
		
		analyzer = animAnalyzer;
		if (analyzer != null && !analyzer.initialized)
			analyzer.Init();
		
		planning = planner;	
		if (planning != null && !planning.initialized)
			planning.Init(analyzer, agents, obstacles);
		
		engine = animEngine;
		if (engine != null && !engine.initialized)
			engine.Init(analyzer,planning,agents,obstacles);
		
		collision = colReact;
		if (collision != null && !collision.initialized)
			collision.Init(analyzer,planning,engine,agents,obstacles);
		
		if (debugText != null)
			debugText.gameObject.active = false;			
		
		initialized = true;		
	}
	
	// Use this for initialization
	void Start () {
		
		if (debug && debugText != null)
			debugText.gameObject.active = true;
		
		startTime = Time.time;
		
		if (planning != null)
			timeLeft = planning.goalTime - Time.time;
		else
			timeLeft = 0;
		
	}
	
	// Update is called once per frame
	void Update () {
	
		if (!initialized)
			return;
		
		if (debug)
		{
			debugText.text = "";
			
			//if (sequence.planComputed)
			//	debugText
			
			if (engine != null && engine.currentAnimation != null)
				debugText.text = engine.currentAnimation.name;
			else
			{
				
				//debugText.text = "NULL";
			}
			
			if ( !debugText.gameObject.active )
				debugText.gameObject.active = true;
						
			if (timeLeft >= 0)
				timeLeft = planning.goalTime - Time.time;
						
			debugText.text += "\n" + (int)timeLeft;
			//debugText.text += "\n" + (int)gameObject.gameObject.transform.rotation.eulerAngles.y;
			//debugText.text += "\n" + (int)sequence.currentEndState.currentRotation.eulerAngles.y;
			//debugText.text += "\n" + debugText.transform.position.y;				
			
			
			if (engine != null && engine.action != null)
			{
				/*
				float orient = root.rotation.eulerAngles.y - 90.0f;
				//if (orient > 180) orient -= 360;
				//if (orient < -180) orient += 360;
				
				//debugText.text += "\nR=" + orient;
				
			 	float predictedOrient = (engine.action.state as FootstepPlanningState).previousState.currentRotation.eulerAngles.y;
				//if (predictedOrient > 180) predictedOrient -= 360;
				//if (predictedOrient < -180) predictedOrient += 360;
				
				//debugText.text += "\nW=" + predictedOrient;
											
				float orientError = orient - predictedOrient;
				if (orientError > 180) orientError -= 360;
				if (orientError < -180) orientError += 360;
				
				debugText.text += "\nerrorD=" + orientError;
				debugText.text += "\nerrorR=" + engine.angleError;
				*/
				
				float predictedTime = (engine.action.state as FootstepPlanningState).time;
				debugText.text += "\nPT=" + predictedTime;
				
			}
			
			//debugText.text += "\newAction? " + engine.insertedAction;
			//debugText.text += "\nreacting? " + collision.reacting;
		}
		
	}
}
