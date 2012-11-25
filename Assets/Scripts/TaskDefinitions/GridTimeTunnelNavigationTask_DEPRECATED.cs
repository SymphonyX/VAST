using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridTimeTunnelNavigationTask_DEPRECATED : Task
{
	public State startState;
	public State goalState; 
	
	public List<State> tunnel; 
	public float tunnelDistanceThreshold; 
	public float tunnelTimeThreshold; 
	public GridTimeDomain gridTimeDomain;
	
	public List<State> spaceTimePath; 
	
	public GridTimeTunnelNavigationTask_DEPRECATED (State t_startState, State t_goalState, List<State> t_tunnel, float t_tunnelDistanceThreshold, float t_tunnelTimeThreshold, 
		ref GridTimeDomain t_gridTimeDomain, ref List<State> t_spaceTimePath,
		TaskPriority t_taskPriority, TaskManager taskManager)
		: base (taskManager, t_taskPriority) // defaults to a real-time task unless the priority is explicitly set 
	{
		startState = t_startState;
		goalState = t_goalState;
		
		taskType = TaskType.GridTimeTunnelNavigationTask_DEPRECATED;
		
		// this IS a reference -- we never actually assign it again -- it just refers the path of the grid task
		tunnel = t_tunnel;
		
		tunnelDistanceThreshold = t_tunnelDistanceThreshold;
		tunnelTimeThreshold = t_tunnelTimeThreshold;
		gridTimeDomain = t_gridTimeDomain;
		
		spaceTimePath = t_spaceTimePath;
		
	}
	
	public override TaskStatus execute (float maxTime)
	{
		
		Stack<DefaultAction> convertedTunnel = new Stack<DefaultAction> ();
		// convert tunnel 
		for(int i = tunnel.Count-1; i >=0; i--)
		{
			GridPlanningState gs = new GridPlanningState(tunnel[i].getPosition());
			GridPlanningAction g = new GridPlanningAction (gs, new Vector3());
			convertedTunnel.Push(g);
			
		}
		
		GridTunnelSearch gridTunnelSearch = 
			new GridTunnelSearch(convertedTunnel, startState.getPosition(), 0.0F, 10.0F, gridTimeDomain, 250, 2.0F, 1.0F, 1.0F);
		
		Debug.Log ("grid tunnel plan " + gridTunnelSearch.newPlan.Count);
		
		//// generating plan for now 
		spaceTimePath.Clear();
		
		while (gridTunnelSearch.newPlan.Count != 0)
		{
			DefaultAction action = gridTunnelSearch.newPlan.Pop();
			GridTimeState state = action.state as GridTimeState;
			spaceTimePath.Add(new State(state.currentPosition, state.time));
		}
		
		bool planComputed = gridTunnelSearch.goalReached;		
		
		
		
		// TODO : determine task status 
		TaskStatus taskStatus = TaskStatus.Success;
		
		if (planComputed == true) 
			taskStatus = TaskStatus.Success;
		else
			taskStatus = TaskStatus.Failure; // TODO: I dont know, check 
		
		
		setTaskPriority(TaskPriority.Inactive);
			
		return taskStatus;
		
		
	}
	
	public override void notifyEvent (Event ev, System.Object data)
	{
		switch (ev)
		{
		
		case Event.GRID_PATH_CHANGED : 
				
			Debug.Log("Grid path has changed");
			
			if ( data == null) 
			{
				Debug.Log ("null tunnel recieved, we have reached goal");
				taskCompleted ();
			}
			else
			{
				setTaskPriority(TaskPriority.RealTime);
			}
			
			break;
			
		}
	}
	
}
