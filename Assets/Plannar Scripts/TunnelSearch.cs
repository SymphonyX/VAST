using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TunnelSearch{
	
	// Creation function
	public TunnelSearch(Stack<DefaultAction> plan, float width)
	{
		plan.CopyTo(currentPlan,0);	
		
		W = width;
		SqrW = W*W;
				
		ExtractTunnelZone();
	}
	
	// The current plan with actions of different resolutions
	private DefaultAction[] currentPlan;
	
	// The new plan, hopefully it will just have HighLevelActions
	public Stack<DefaultAction> newPlan;
	
	// The zone of the current plan around which we are going to define the tunnel
	private GridPlanningAction[] tunnelZone;
	
	private float W; // The width of the tunnel
	private float SqrW; 
	
	// The last HighLevelAction that has been computed
	private FootstepPlanningAction lastFootstepAction;
	
	// This function searches and extracts the tunnel zone, letting it in tunnelZone
	private void ExtractTunnelZone()
	{
		if (currentPlan == null)
			return;
		
		// We look for the first GridPlanningAction
		GridPlanningAction action = currentPlan[0] as GridPlanningAction;; 
		int i = 0;
		while (action == null && i < currentPlan.Length)
		{
			i++;
			action = currentPlan[i] as GridPlanningAction;			
		}
		
		// if we have found a GridPlanningAction
		if (action != null && i < currentPlan.Length) 
		{
			// we copy the first part of the plan (the footsteps)
			newPlan = new Stack<DefaultAction>();
			for (int fs = 0; fs < i; fs++)
				newPlan.Push(currentPlan[fs] as FootstepPlanningAction);
			
			// we save the lastFootstepAction;
			lastFootstepAction = currentPlan[i-1] as FootstepPlanningAction;
			
			// we compute how many grid actions are in the plan
			int numberOfGridActions = currentPlan.Length - (i-1);
					
			// we create and fill the tunnelZone
			tunnelZone = new GridPlanningAction[numberOfGridActions];			
			int j = 0;
			while( i < currentPlan.Length )
			{
				tunnelZone[j] = currentPlan[i] as GridPlanningAction;
				
				j++;
				i++;	
			}			
		}
	}
	
	// This function search for a new path inside the tunnel 
	// Hopefully it will return just a path of FootstepPlanningAction 
	public Stack<DefaultAction> SearchInTunnel()
	{
		int[] backTrackingIndexes = new int[tunnelZone.Length];
		for(int bt = 0; bt < backTrackingIndexes.Length; bt++)
			backTrackingIndexes[bt] = 0;
		
		// For every GridPlanningAction in the tunnel zone
		int i = 0;
		while ( i < tunnelZone.Length && i > 0 ) // if i < 0 it means we have backtracked and found no solution
		{
			GridPlanningAction gridAction = tunnelZone[i];
			
			// We have all the mapping actions coming from the GridPlanningAction
			FootstepPlanningAction[] mappingActions = MappingFromGridAction(gridAction,lastFootstepAction);
			
			// We look for a mapping action that is valid for our tunnel
			int j = backTrackingIndexes[i];
			bool found = false;
			FootstepPlanningAction footstepAction = null;
			while (!found && j < mappingActions.Length)
			{
				footstepAction = mappingActions[j];
				
				// If it falls inside the tunnel
				if ( SqrDistance(gridAction,footstepAction) <= SqrW )
					// TODO: CHECK FOR COLLISIONS
					found = true;
				else
					j++;				
				
			}
			
			// If we have found a valid FootstepAction 
			if (found && footstepAction != null)
			{
				// we add id to our plan
				newPlan.Push(footstepAction);
								
				// we save its mapping index in case we do backtracking later
				backTrackingIndexes[i] = j;
				
				// we also save the FootstepAction we have added
				lastFootstepAction = footstepAction;
				
				i++;				
			}
			else // if we haven't found a valid action
			{
				// we do backtrack
				newPlan.Pop();
				
				// we retrieve the last footstepAction
				lastFootstepAction = newPlan.Pop() as FootstepPlanningAction;
				newPlan.Push(lastFootstepAction);
				
				i--;
			}
		}
				
		return newPlan;
	}
	
	
	// Returns all the FootstepPlanningActions that can be mapped to the GridPlanningAction girdAction
	public FootstepPlanningAction[] MappingFromGridAction(GridPlanningAction gridAction,
	                                            FootstepPlanningAction prevFootstepAction)
		
	{
		FootstepPlanningAction[] mappingActions;
		
		// TODO: all the mapping function using a predefined table that returns the mapping actions
		mappingActions = null;
		
		return mappingActions;
	}
	
	// Returns the square distance between the two states of two actions of different domains
	public float SqrDistance(GridPlanningAction gridAction, FootstepPlanningAction footstepAction)
	{
		Vector3 gridPos = (gridAction.state as GridPlanningState).currentPosition;
		Vector3 footstepPos = (footstepAction.state as FootstepPlanningState).currentPosition;
		
		return (footstepPos - gridPos).sqrMagnitude;
	}
}
