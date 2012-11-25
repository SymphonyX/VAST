using UnityEngine;
using System.Collections;

public class GlobalNavigationtask : Task
{
	public State startState;
	public State goalState;
	int passableMask;
	
	public GlobalNavigationtask (State t_startState, State t_goalState, int t_passableMask, 
		TaskPriority t_taskPriority, TaskManager taskManager)
		: base (taskManager, t_taskPriority) // defaults to a real-time task unless the priority is explicitly set 
	{
		taskType = TaskType.GlobalNavigationTask;
		
		startState = t_startState;
		goalState = t_goalState;
		
		// EVENT: globalNavigationTask is triggered by the goalState when STATE_CHANGED
		goalState.registerObserver(Event.STATE_POSITION_CHANGED, this);
		
		// NOTE: this does not replan when the start state changes 
		
		passableMask = t_passableMask;
		
	}
	
	public override TaskStatus execute (float maxTime)
	{
		GlobalNavigator.CalculateNavigationMeshPath (startState.getPosition(), goalState.getPosition(), passableMask);
		
		TaskStatus taskStatus = TaskStatus.Success;
		
		switch (GlobalNavigator.pathStatus)
		{
		case NavMeshPathStatus.PathComplete:
			taskStatus = TaskStatus.Success; break;
		case NavMeshPathStatus.PathInvalid:
			taskStatus = TaskStatus.Failure; break;
		case NavMeshPathStatus.PathPartial:
			taskStatus = TaskStatus.Incomplete; break;
		}
		
		// TODO: change task priority after execution 
		setTaskPriority(TaskPriority.Inactive);
		
		// notify agent that the global path has changed 
		notifyObservers(Event.GLOBAL_PATH_CHANGED, null);
		
		return taskStatus;
		
		
	}
	
	public override void notifyEvent (Event ev, System.Object data)
	{
		switch (ev)
		{
		case Event.STATE_POSITION_CHANGED:
			
			Debug.Log ("the global goal has changed, we must do something");
			setTaskPriority(TaskPriority.RealTime);
			break;
			
		case Event.GLOBAL_WORLD_CHANGED :
			 Debug.LogWarning("the global world has changed -- TODO: we need to replan the global path");
			setTaskPriority(TaskPriority.RealTime);
			break;
			
		}
	}
	
}
