using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * ARAstar Node
 * This class represents a given state reached from a particular state.
 * Many nodes could be associated with the same state as long as their parents differ from each other.
 */
public class ARAstarNode
{
	/// Empty constructor
	public ARAstarNode ()
	{
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="ARAstarNode"/> class.
	/// </summary>
	/// <param name='_g'>
	/// Node g-value
	/// </param>
	/// <param name='_h'>
	/// Node h-value
	/// </param>
	/// <param name='_previousStateRef'>
	/// Node's parent's state
	/// </param>
	/// <param name='_actionRef'>
	/// Action taken to generate node
	/// </param>
	public ARAstarNode (float _g, float _h, DefaultState _previousStateRef, DefaultAction _actionRef)
	{
		g = _g;
		h = _h;
		previousState = _previousStateRef;
		action = _actionRef;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="ARAstarNode"/> class.
	/// </summary>
	/// <param name='_g'>
	/// Node's g-value
	/// </param>
	/// <param name='_h'>
	/// Node's h-value
	/// </param>
	/// <param name='_previousStateRef'>
	/// Node's parent's state
	/// </param>
	/// <param name='_stateRef'>
	/// Node's state
	/// </param>
	public ARAstarNode (float _g, float _h, DefaultState _previousStateRef, DefaultState _stateRef)
	{
		g = _g;
		h = _h;
		previousState = _previousStateRef;
		action = new DefaultAction ();
		action.cost = 0.0f;
		action.state = _stateRef;
	}
	
	/// <summary>
	/// Computes the given node's key.
	/// </summary>
	/// <param name='inflationFactor'>
	/// Planner's current inflation factor
	/// </param>
	public float[] Key(float inflationFactor)
	{
		float[] key;
		if(g > rhs){
			key = new float[] {rhs + (inflationFactor*h), rhs};
		}else{
			key = new float[] {g + h, g};	
		}
		return key;
	}

	public float g; 						//!< Node's g-value
	public float h;							//!< Node's h-value
	public float rhs;						//!< Node's one-step lookahead value
	public float weightExpanded=0;			//!< Weight with which this node was created
	public bool isDirty = false;			//!< Dirty flag
	public bool touched = false;			//!< Touch flag
	public bool updated = false;			//!< Updated flag
	public int highPriority = 0;			//!< High priority counter
	public DefaultState previousState;		//!< Node's parent state
	public DefaultAction action;			//!< Action taken to reach this node
}

public class NodeComparer : IComparer <ARAstarNode>
{  
	ARAstarPlanner parentPlanner;
	
	public NodeComparer(ARAstarPlanner planner)
	{
		parentPlanner = planner;	
	}
	
     public int Compare(ARAstarNode n1, ARAstarNode n2)  
     {  
        float[] n1Key = n1.Key(parentPlanner.inflationFactor);
		float[] n2Key = n2.Key(parentPlanner.inflationFactor);
		if(n1.highPriority > 0 && n2.highPriority <= 0)
			return -1;
		else if(n1.highPriority <= 0 && n2.highPriority > 0)
			return 1;
		else{ 
			if (n1Key[0] < n2Key[0])
				return -1;
			else if(n1Key[0] > n2Key[0])
				return 1;
			else {
				if(n1Key[1] < n2Key[1])
					return -1;
				else
					return 1;
			}
		}  
     }  
}  