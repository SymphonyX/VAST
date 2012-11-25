using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TaskManager : Observer {
	
	// currently the order in which tasks at the same priority are put -- thats the order in which they are executed 
	public Dictionary<int, List<Task>> taskDictionary;
	
	private List<int> orderedPriorityList;
	
	public TaskManager ()
	{
		
		// keep the priority values in descending order from highest to lowest priority 
		orderedPriorityList = new List<int>();
		foreach (int val in System.Enum.GetValues(typeof(TaskPriority)))
			orderedPriorityList.Add(val);
		
		orderedPriorityList.Sort ();
		orderedPriorityList.Reverse ();
		
		taskDictionary = new Dictionary<int, List<Task>> ();
		foreach ( int priorityValue in orderedPriorityList)
			taskDictionary.Add(priorityValue, new List<Task>());
	
		
	}
	
	public void addTask ( int priority, Task task ) 
	{
		// TODO : error checkign on priority 
		taskDictionary[priority].Add(task);
	}
	
	/*
	private void removeTask (Task task)
	{
		foreach (int priority in taskDictionary.Keys)
		{
			if (taskDictionary[priority].Contains(task))
				taskDictionary[priority].Remove(task);
		}
	}
	*/
	
	private void removeTask ( Task task ) 
	{
		taskDictionary[(int)task.taskPriority].Remove(task);
	}
	
	public Task getHighestPriorityTask ()
	{
		Task bestTask = null;
		foreach (int priority in orderedPriorityList)
		{
			if (priority == (int) TaskPriority.Asleep || priority == (int) TaskPriority.Inactive 
				|| priority == (int) TaskPriority.Complete ) 
			{
				// we have reached inactive priority or asleep  or Complete priority -- we dont want to execute them 
				break;
			}
			if (taskDictionary[priority].Count > 0 ) 
			{
				// if there is a task at this priority 
				bestTask = taskDictionary[priority][0];
				taskDictionary[priority].Remove(bestTask); // bad ?
				break;
			}
			
		}
		
		return bestTask; 
	}
	
	public void notifyEvent (Event ev, object data)
	{
		
		switch (ev)
		{
			
		case Event.CHANGE_IN_TASK_PRIORITY : 
			Task.TaskPriorityChange tpc = (Task.TaskPriorityChange) data;
			removeTask( tpc.task);
			addTask((int) tpc.newTaskPriority, tpc.task);
			break;
			
		case Event.TASK_COMPLETED : 
			Task task = data as Task;
			Debug.Log("Task " + task.taskName + " is complete, removing");
			removeTask(task);
			break;
			
		default : 
			Debug.LogWarning("Undefined event sent to TaskManager " + ev.ToString());
			break;
			
		}
	}
	
		
}

