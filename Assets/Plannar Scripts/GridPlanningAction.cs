using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridPlanningAction : DefaultAction
{
	public GridPlanningAction(Vector3 mov)
	{
		movAction = mov;	
		cost = 1;								
	}
	
	public GridPlanningAction(GridPlanningState previousState, Vector3 mov)
	{
		movAction = mov;	
		cost = 1;
		state = new GridPlanningState(previousState, mov);						
	}
	
	//Constructor for actions using dummytype for planningstate
	public GridPlanningAction(GridPlanningState previousState, Vector3 mov, float dummyType)
	{
		movAction = mov;
		cost = 1;
		state = new GridPlanningState(previousState, mov, dummyType);			
	}
	
	public Vector3 movAction;
	
}



