using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct CompleteAnimationCurve{
	
	public Vector3 initRootPos;
	
	public AnimationCurve xPos;
	public AnimationCurve yPos;
	public AnimationCurve zPos;
	
	public float initRotY;
	
	public AnimationCurve yRot;
	
	public float initHeight;
}

// enum to define the type of animation
public enum LocomotionMode
{
	Walk,
	SideStep,
	Run,
	Jump,
	Action,
	Turn,
	WalkTurn,
	Idle
};

// enum of some known joints used to define the animations
public enum Joint
{
	RightFoot,
	LeftFoot
}	

public class JointState
{
	// the position vector with respect to the considered origin (e.g. the root or the supporting foot)
	public Vector3 position; 
	
	// the orientation vector with respect to the considered reference vector (e.g. the root or the supporting foot orientation)
	public Vector3 orientation; 
	
	// the angle between the orientation vector and the reference vector
	// in degrees
	public float angle; 
};


// Class used to store and anotate all the results of the analysis of one animation
public class AnotatedAnimation
{
	/*********************************************************************/
	// General Attributes
	/*********************************************************************/
	
	// The id of the animation
	public int id;
	
	// The name of the animation
	public string name;
	
	// The total length of the animation clip
	public float totalLength; 
	
	public LocomotionMode type; // the type of the animations
	public Joint supporting; // indicates the foot planted on the floor during the animation
	public Joint swing; // indicates the foot that moves during the animation
	
	/*********************************************************************/
	// Root Movement Attributes
	/*********************************************************************/
	
	// a set of animation curves describing the movement and rotation of the root
	public CompleteAnimationCurve RootCurve; 
	
	public JointState[] Root; // the configuration of the root
	
	public Vector3 rootDisplacement; // the difference between the start and end positions of the root	
	
	
	public float time; // the normalized time required to play the action until the next foot plant at a normal speed (1x) in seconds	
	public float footPlantLenght; // the normalized time of the footplant length (used for blending)
	// If the animation is a cycle, we just want to have the analysis until the first footplan
	// This will determine the end of our action
		
	public float distance; // the magnitude of rootDisplacement in m	
	public float speed;  // (distance/time) in m/s
	
	// the angle between the orientation vector and the rootDisplacementVector
	// in degrees
	public float angle; 
	
	// the angle that the orientation changes between the start and end positions
	public float rotationAngle;
	public float rotationSpeed;
	
	// the angleCost of performing that action, used by the planner
	public float angleCost; 
	
	/*********************************************************************/
	// Foot-Steps Attributes
	/*********************************************************************/
	
	public JointState[] LeftFoot;
	public JointState movement_LeftFoot; // the movement of the left foot
	
	public JointState[] RightFoot;
	public JointState movement_RightFoot; // the movement of the right foot	
	
	public JointState[] SupportingFoot;
	public JointState movement_SupportingFoot;
	public JointState[] SwingFoot;
	public JointState movement_SwingFoot;
		
	public Vector3[] LeftHand;
	public Vector3 movement_LeftHand;

	public Vector3[] RightHand;
	public Vector3 movement_RightHand;
	
	public void Init(int samples)
	{		
		RootCurve.initRootPos = Vector3.zero;		
		
		RootCurve.xPos = new AnimationCurve();
		RootCurve.yPos = new AnimationCurve();
		RootCurve.zPos = new AnimationCurve();		
				
		RootCurve.yRot = new AnimationCurve();
		
		Root = new JointState[samples];
		LeftFoot = new JointState[samples];
		RightFoot = new JointState[samples];
				
		RightHand = new Vector3[samples];
		LeftHand = new Vector3[samples];
		
		SupportingFoot = new JointState[samples];
		SwingFoot = new JointState[samples];
		
		for (int i=0; i<samples; i++)
		{
			Root[i] = new JointState();
			LeftFoot[i] = new JointState();
			RightFoot[i] = new JointState();
			
			SupportingFoot[i] = new JointState();
			SwingFoot[i] = new JointState();
		}
		
		movement_LeftFoot = new JointState();
		movement_RightFoot = new JointState();
	}
	
	public void SetSupportingFoot(Joint sup)
	{
		supporting = sup;
		
		int samples = Root.Length;
		
		if (supporting == Joint.LeftFoot)
		{
			swing = Joint.RightFoot;	
			
			for (int i=0; i<samples; i++)
			{
				SupportingFoot[i] = LeftFoot[i];
				SwingFoot[i] = RightFoot[i];
			}
			
			movement_SupportingFoot = movement_LeftFoot;
			
			movement_SwingFoot = movement_RightFoot;
		}
		else
		{
			swing = Joint.LeftFoot;
			
			for (int i=0; i<samples; i++)
			{
				SupportingFoot[i] = RightFoot[i];
				SwingFoot[i] = LeftFoot[i];
			}
			
			movement_SupportingFoot = movement_RightFoot;
			
			movement_SwingFoot = movement_LeftFoot;
		}
	}
	
	/*********************************************************************/
	// Help Functions
	/*********************************************************************/
	
	public void LogAnotations()
	{
		int samples = Root.Length;
		
		if(swing==Joint.RightFoot)
		 	Debug.Log(name+":\nRoot Speed="+speed+"\tRoot angle="+angle+"\tswing=RF\tStep Time="+time+"\n" +
			    "rotAngle="+rotationAngle+"\trotSpeed="+rotationSpeed+"\n" +  
			    "angleCost="+angleCost+"\n" +
		 		"startRF: pos="+RightFoot[0].position+"\tangle="+RightFoot[0].angle+"\n" +
		 		"endRF:   pos="+RightFoot[samples-1].position+"\tangle="+RightFoot[samples-1].angle);			    
		else
			Debug.Log(name+":\nRoot Speed="+speed+"\tRoot angle="+angle+"\tswing=LF\tStep Time="+time+"\n" +
			    "rotAngle="+rotationAngle+"\trotSpeed="+rotationSpeed+"\n" + 			          
			    "angleCost="+angleCost+"\n" +
				"startLF: pos="+LeftFoot[0].position+"\tangle="+LeftFoot[0].angle+"\n" +
		 		"endLF:   pos="+LeftFoot[samples-1].position+"\tangle="+LeftFoot[samples-1].angle);		
	}
}

