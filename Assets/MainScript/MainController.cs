using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// Main Controller class to initialize our world and agents
public class MainController : MonoBehaviour {
	
	public string animationsFileName;
	
	private GameObject[] agents;
	private Vector3[] agentsPos;
	private bool[] agentsInit;
	
	public SteeringManager steering = null;
	
	private bool initialized = false;
	private int initCount = 0;
	private int initCount2 = 0;
	
	void Awake () {
	
		// Check license
		
		// 1) Initialize world --> obstacles
		
		
		// 2) Instantiate agents and final goals		
		/*
		agents = new GameObject[numberOfAgents];
		for (int id = 0; id < numberOfAgents; id++)
		{
			agents[id] = new GameObject("Agent"+id);
			agents[id].AddComponent(NewManPrefab);
		}
		*/
		
		
		agents = GameObject.FindGameObjectsWithTag("Agent");
		
		// 3) Generate NavMesh (ang goals/waypoints?)
		
		agentsPos = new Vector3[agents.Length];
		
		agentsInit = new bool[agents.Length];
		
		//foreach (GameObject agent in agents)
		for(int i = 0; i < agents.Length; i++)
		{			
			GameObject agent = agents[i];
			
			//agent.active = false;
			
			agentsPos[i] = agent.transform.position;
			agent.transform.position = new Vector3(int.MaxValue,int.MaxValue,int.MaxValue);
			
			agentsInit[i] = false;
			
			NeighbourAgents neighbourAgents = agent.GetComponent("NeighbourAgents") as NeighbourAgents;
			if (neighbourAgents != null)
				neighbourAgents.Init();
			
			NeighbourObstacles neighbourObstacles = agent.GetComponent("NeighbourObstacles") as NeighbourObstacles;
			if (neighbourObstacles != null)
				neighbourObstacles.Init();
		}
		
		initCount = 0;
	
	}
	
	void Start(){
		
		initialized = false;	
		
	}
	
	// Use this for initialization
	void InitAgent (int i) {
		
		AnimationAnalyzer analyzer = null;
		
		// Initialize different components of the agents
		// ( Animation Engine, Planner, Neighbors, ...)
		//foreach (GameObject agent in agents)
		//for(int i = 0; i < agents.Length; i++)
		{			
			GameObject agent = agents[i];
					
			//agent.active = true;
			
			agent.transform.position = agentsPos[i];
			
			//if (analyzer == null)
			{
				analyzer = agent.GetComponent("AnimationAnalyzer") as AnimationAnalyzer;
				if (analyzer != null)
				{
					analyzer.Init();				
					
					//analyzer.ReadAnalysisFromFile(animationsFileName);
				}
			}
			/*
			else
			{
				Vector3 startPos = agent.transform.position;
				Quaternion startRot = agent.transform.rotation;				
				analyzer.RemoveAnimations(agent.animation);	
				agent.transform.position = startPos;
				agent.transform.rotation = startRot;
			}
			*/
			
			NeighbourAgents neighbourAgents = agent.GetComponent("NeighbourAgents") as NeighbourAgents;
			//if (neighbourAgents != null)
			//	neighbourAgents.Init();
			
			NeighbourObstacles neighbourObstacles = agent.GetComponent("NeighbourObstacles") as NeighbourObstacles;
			//if (neighbourObstacles != null)
			//	neighbourObstacles.Init();
			
			Planner planning;
			if(ADAFootstepTest.ADA_PLANNER_IN_USE){
				ADAFootstepTest ADAplanning = agent.GetComponent("ADAFootstepTest") as ADAFootstepTest;
				planning = ADAplanning as Planner;
			}
			else
			{
				FootstepPlanningTest FPTplanning = agent.GetComponent("FootstepPlanningTest") as FootstepPlanningTest;
				planning = FPTplanning as Planner;
			}
			if (planning != null)
				planning.Init(analyzer,neighbourAgents,neighbourObstacles);
			
			AnimationEngine engine = agent.GetComponent("AnimationEngine") as AnimationEngine;
			if (engine != null)
				engine.Init(analyzer,planning, neighbourAgents, neighbourObstacles);			
			
			CollisionReaction collider = agent.GetComponent("CollisionReaction") as CollisionReaction;
			if (collider != null)
				collider.Init(analyzer,planning,engine,neighbourAgents,neighbourObstacles);
			
			NavMeshWayPoints waypoints = agent.GetComponent("NavMeshWayPoints") as NavMeshWayPoints;
			if (waypoints != null)
				waypoints.Init(planning,steering,analyzer, neighbourAgents, neighbourObstacles);
			
			PlaceFootSteps footsteps = agent.GetComponent("PlaceFootSteps") as PlaceFootSteps;
			if (footsteps != null)
				footsteps.Init(analyzer,planning,engine,neighbourAgents,neighbourObstacles);
			
			EventsMonitor events = agent.GetComponent("EventsMonitor") as EventsMonitor;
			if (events != null)
			{
				events.Init(analyzer,planning,engine,collider,waypoints,steering,neighbourAgents,neighbourObstacles,footsteps);
			
				events.enabled = false;
			}
				
			DebugScript debug = agent.GetComponent("DebugScript") as DebugScript;
			if (debug != null)
				debug.Init(analyzer,engine,planning,neighbourAgents,neighbourObstacles,collider);			
			
			if (analyzer != null)
				analyzer.RemoveAnimations(agent.animation);
			
		}		
		
		
	
	}
	
	
	// Update is called once per frame
	void Update () {		
		
		if (!initialized)
		{
			//while(initCount < agents.Length)
			if (initCount < agents.Length)
			{
				InitAgent(initCount);
						
				initCount++;
			}
			else if (initCount2 < agents.Length)
			{				
				EventsMonitor e =  agents[initCount2].GetComponent("EventsMonitor") as EventsMonitor;
				e.enabled = true;
				
				initCount2++;
			}
			
			
			if (initCount == agents.Length && initCount2 == agents.Length )
				initialized = true;
		}
		
	}
	
	
}
