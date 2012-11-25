using System.Collections;
using UnityEngine;


public enum TaskType
{
	GlobalNavigationTask,
	GridNavigationTask,
	GridTimeTunnelNavigationTask_DEPRECATED
}

public enum TaskPriority 
{
	Complete = 0, // this is when a task has finished execution of its plan and is removed from the task manager 
	Asleep, // this is used when the task is currently not to be used. 
	Inactive, // this should be used when the current plan is optimal
	LowPriority,
	MidPriority, 
	HighPriority,
	RealTime
}

public enum TaskStatus
{
	Failure = 0,
	Incomplete, 
	Success 
	// this should probably become a struct and give new priority etc etc 
}

public abstract class Task : Observable, Observer {
	
	public TaskType taskType; 
	
	// this will be deprecated 
	public string taskName;
	
	public struct TaskPriorityChange
	{
		public Task task;
		public TaskPriority newTaskPriority; 
		
		public TaskPriorityChange ( Task task, TaskPriority newTaskPriority)
		{
			this.task = task;
			this.newTaskPriority = newTaskPriority;
		}
	}
	
	public Task ( TaskManager taskManager, TaskPriority t_taskPriority )
	{
		
		setTaskPriority ( t_taskPriority ); // the first signal will go nowhere 
		
		registerObserver(Event.CHANGE_IN_TASK_PRIORITY, taskManager);
		taskManager.addTask((int) taskPriority, this);
		
	}
	
	public TaskPriority taskPriority;
	// public int taskID;
	
	public abstract TaskStatus execute (float maxTime);
	
	public void setTaskPriority ( TaskPriority newTaskPriority )
	{
		// allowing task to be put in the same priority list from which it was popped out of.
		//if ( this.taskPriority == newTaskPriority )
		//	return;
		
		// the data we would send is old and new task priority 
		notifyObservers ( Event.CHANGE_IN_TASK_PRIORITY, new TaskPriorityChange(this,newTaskPriority) );
		
		taskPriority = newTaskPriority;
		
	}
	
	public void taskCompleted ()
	{
		// the data we would send is old and new task priority 
		setTaskPriority(TaskPriority.Complete);
		notifyObservers ( Event.TASK_COMPLETED, this );
		
	}
	
	public abstract void notifyEvent (Event ev, System.Object data ); 
		
}


/// a planner task would have a planner object in it ? 
// or the planner could just inherit Task and have an execute function 
// what other tasks --- the planner task could have a selection which tells which method to invoke 
/*
 * checking for changes task 
 * updating lists etc (would this not just be part of the planner ) 
 * 
 * do we need some mechanism for task dependency ? 
 *  i.e. tasks in the same pqueue which depend on other tasks 
 * you need to clearly enumerate exactly what are the different thinking tasks the agent does 
 * can 1 task increase the priority of another task? or can it make a task which was asleep awake again ? 
 * 
 * 
 * 
 * different types of tasks : 
 *  update neigbhors list (perception) 
 *  search 
 *  * 
 * */


