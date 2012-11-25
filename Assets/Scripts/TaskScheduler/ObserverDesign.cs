using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public enum Event 
{
	NoEvent,
	CHANGE_IN_TASK_PRIORITY,
	TASK_COMPLETED,
	GLOBAL_PATH_CHANGED,
	GLOBAL_WORLD_CHANGED, // the centralized manager sends this to the global navigation task when the world changes too much (time to replan) 
	GRID_PATH_CHANGED, // prob replace this with state based event (maybe not acutally )
	STATE_POSITION_CHANGED,
	NON_DETERMINISTIC_OBSTACLE_CHANGED,
	GOAL_VALID, // the goal state is a valid one and the task should plan for it (task should not be asleep)
	GOAL_INVALID, // the goal state is currently invalid and the corresponding tasks should be made ASLEEP 
	NEIGBHOR_AGENTS_CHANGED, // when NeighborAgents adds a neigbhor agent 
	NEIGBHOR_SIMPLE_AGENTS_CHANGED, // NeighborSimpleAgents have added an agent 
	NEIGBHOR_DYNAMIC_OBSTACLES_CHANGED, // when NeighborObstacles adds a neigbhor obstacle to be considered 
	CURRENT_EXECUTING_TASK // you get this when you become current executing task 
}


public interface Observer 
{
	
	void notifyEvent (Event ev, System.Object data); // this should probably take in some information of the event that took place 
	
}

public class Observable {

	
	private Dictionary<Event,List<Observer>> _observers;
	
	public Observable ()
	{
		_observers = new Dictionary<Event,List<Observer>> ();
	}
	
	public void registerObserver (Event ev, Observer observer) 
	{
		if ( _observers.ContainsKey(ev) )
			_observers[ev].Add(observer);
		else
		{
			_observers.Add(ev,new List<Observer>());
			_observers[ev].Add(observer);
		}
	}
	
	// this unregisters observer from all events that it may have registered for 
	public bool unregisterObserver (Observer observer) 
	{
		foreach (List<Observer> obs in _observers.Values)
			obs.Remove(observer);
		
		return true; 
	}
	
	public bool unregisterObserver (Event ev, Observer observer)
	{
		if ( _observers.ContainsKey(ev) )
		{
			return _observers[ev].Remove(observer);
		}
		
		return false;
	}
	
	public void clearObservers ()
	{
		_observers.Clear ();
	}
	
	public void notifyObservers  (Event ev = Event.NoEvent, System.Object data = null)
	{
		if ( _observers.ContainsKey(ev) == false )
		{
			//Debug.LogWarning("No observer has registered for event " + ev.ToString());
			return;
		}
		
		for (int i=0; i < _observers[ev].Count; i++)
			_observers[ev][i].notifyEvent (ev,data);
	}
	
}


/*
 * task is an observer -- it listens for events 
 * the events are observables -- when event happens, it notifies the task 
 * the task is an observable -- when its priority changes, it notifies the task manager 
 * the task manager is an observer 
 * 
 * changes in environment are most primitive observables 
 * change in goal is an observable 
 * change in tunnel is an observable 
 * dont want to overdo this 
 */

