using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class AnimationEngine : MonoBehaviour {
	
	private AnimationAnalyzer analyzer;	
		
	//private FootstepPlanningTest planning;	
	private Planner planning;
	
	public AnimationState currentAnimation;		
	[HideInInspector] public FootstepPlanningAction action;
	
	private float currentActionTime;
	private float currentBlendingTime;
	private float previousBlendingTime;
	private float currentActionEndTime;
	
	[HideInInspector] public int actionNum;
	[HideInInspector] public int actionsSinceLastPlan;
	
	[HideInInspector] public bool initialized;
	
	[HideInInspector] public bool changed;
	
	private Vector3 errorT;
	private float errorR;
	
	public bool blending;
	//public bool posCorrection = true;
	
	[HideInInspector] public bool insertedAction;
	
	[HideInInspector] public float angleError;	
	
	public Transform root;		
	
	private Vector3 initGameObjectPos;
	private float initGameObjectRotY;
	
	private Vector3 initLocalRootPos;
	
	[HideInInspector] public Vector3 lastRootPos;
	[HideInInspector] public Quaternion lastRootRot;
	
	/* Debug */
	public AnimationCurve xPos;
	public AnimationCurve yPos;
	public AnimationCurve zPos;
	
	public AnimationCurve yRot;
	/*********/
	
	void Awake(){
		initialized = false;		
	}	
		
	//public void Init(AnimationAnalyzer animAnalyzer, FootstepPlanningTest planner, RootMotionComputer computer, NeighbourAgents agents, NeighbourObstacles obstacles){
	public void Init(AnimationAnalyzer animAnalyzer, Planner planner, NeighbourAgents agents, NeighbourObstacles obstacles){
		
		analyzer = animAnalyzer;
		if (analyzer != null && !analyzer.initialized)
			analyzer.Init();
		
		planning = planner;
		if (planning != null && !planning.initialized)
			planning.Init(analyzer,agents,obstacles);
								
		foreach (AnimationState state in animation)
		{
			state.enabled = true;
			state.speed = 1.0f;
			state.wrapMode = WrapMode.Loop;
			state.weight = 0;
		}	
		
		currentAnimation = null;
		action = null;
		
		animation.Stop();
		
		initialized = true;
		
		actionNum = 0;
		
		changed = false;
		
		errorT = new Vector3(0,0,0);
		errorR = 0;
		
		currentBlendingTime = 0;
		currentActionTime = 0;
		previousBlendingTime = 0;
		currentActionEndTime = 0;
		
		actionsSinceLastPlan = 0;	
		
		insertedAction = false;
		
		initGameObjectPos = transform.position;
		initGameObjectRotY = transform.eulerAngles.y;
		
		initLocalRootPos = root.localPosition;	
		
		lastRootPos = root.position;
		lastRootRot = root.rotation;
	}
	
	public void AnimationEngineUpdate(){
	//void Update(){	
		
		if (!initialized)
			return;
		
		//if (currentAnimation != null)
		//	Debug.Log("currentAnimationTime: " + currentAnimation.time);
		
				
		if (
		    currentAnimation == null 
		    || Mathf.Abs(currentAnimation.time) >= currentActionTime  
		    //|| Time.time >= currentActionEndTime
		    || !currentAnimation.enabled
		    )
		{	
			
			
			// Get new animation
			if (!insertedAction)
				action = planning.getFirstAction();
				
			if (action != null)
			{	
				string animationName = action.animInfo.name;				
				
				/*
				if (blending)
				{
					// We blend out the previous animation
					// if it exists and it is not the first one
					if (currentAnimation != null) 
					{						
						if (actionNum > 0) 
							animation.Blend(currentAnimation.name,0.0f,currentBlendingTime);											
						else
						{
							animation.Stop();
							animation.Sample();
						}
					}
				}
				*/
			
				// We set up the speed of the new animation
				AnimationState newAnimationState =  animation[animationName];
				newAnimationState.speed = action.speed;
				// We set the animation at the beginning
				if (currentAnimation == null || planning.goalReached)
					newAnimationState.time = 0;
				else
					//newAnimationState.time = currentAnimation.time - currentActionTime;
					newAnimationState.time = Time.time - currentActionEndTime;
				newAnimationState.enabled = true;
				
				// We save the new animation
				currentAnimation = newAnimationState;				
				
				// We get the info of the new animation
				AnotatedAnimation animInfo = action.animInfo;			
				
				// We compute the time of the action and the time of the blending
				float newActionTime = animInfo.time * animInfo.totalLength;
				
				float newBlendingTime = animInfo.totalLength * animInfo.footPlantLenght * action.speed;
				if (animInfo.type != LocomotionMode.Walk)
					newBlendingTime = 0.5f;
								
				currentActionTime = newActionTime;
				
				
				if (previousBlendingTime < newBlendingTime)
					currentBlendingTime = previousBlendingTime;
				else
					currentBlendingTime = newBlendingTime;
				
				previousBlendingTime = newBlendingTime;
				
				if (blending)
				{
					/*
					//We play/blend in the new animation if it's not the first one				
					if (actionNum > 0)
						animation.Blend(currentAnimation.name,1.0f,currentBlendingTime);						
					else
					{
						animation.Play(currentAnimation.name);
						animation.Sample();
					}
					*/							
					
					//Debug.Log("blending time = " + currentBlendingTime);
					
					if (insertedAction)
						currentBlendingTime = 1.5f;
					
					//animation.CrossFade(currentAnimation.name,currentBlendingTime);
					animation.CrossFade(currentAnimation.name,0.5f);
					
				}
				else
					animation.Play(currentAnimation.name);	
					
				
				changed = true;				
								
				insertedAction = false;				
				
				if (action != null && action.state != null)
					currentActionEndTime = (action.state as FootstepPlanningState).time;
				
				float startTime = Time.time;
				
				/*
				Debug.Log("anim starts at time: " + startTime);
				Debug.Log("offset: " + newAnimationState.time);
				float length = animInfo.time * animInfo.totalLength * action.speed - newAnimationState.time;				
				float endTime = startTime + length;
				Debug.Log("action length: " +  length);
				Debug.Log("predicted end time: " + endTime);				
				Debug.Log("state time: " + currentActionEndTime);
				*/				
			}
			else
			{
				//if (!blending)
					animation.Stop();
				//else if (currentAnimation != null)
				//	animation.Blend(currentAnimation.name,0.0f,currentBlendingTime);
				
				changed = false;
				
				planning.goalReached = true;
				//Debug.Log("goalReached at time " + Time.time);
			}
			
		}			
		else
			changed = false;
		
	
	}
	
	public void AnimationEngineLateUpdate(){
	//void LateUpdate(){	
		
		if (!initialized)
			return;	
		
		/*
		if (changed && action != null)
		{
			// compute error			
			
			Vector3 translation = new Vector3(0,0,0);											
			Quaternion newQuat = Quaternion.FromToRotation(new Vector3(1,0,0),-root.right);
			AnotatedAnimation animInfo = action.animInfo;			
			
			FootstepPlanningState predictedState = action.state as FootstepPlanningState;
			
			Vector3 predictedStep = new Vector3(0,0,0);
			if ( animInfo.swing == Joint.LeftFoot )
			{
				translation = - animInfo.Root[0].position + animInfo.movement_LeftFoot.position + animInfo.LeftFoot[0].position;
				predictedStep = predictedState.leftFoot;
			}
			else
			{
				translation = - animInfo.Root[0].position + animInfo.movement_RightFoot.position + animInfo.RightFoot[0].position;														
				predictedStep = predictedState.rightFoot;
			}						
			
			Quaternion predictedQuat = predictedState.previousState.currentRotation;
			
			Vector3 newFootStepPos = root.position + newQuat * translation;								
			newFootStepPos[1] = 0;					
			
			Vector3 error = newFootStepPos - predictedStep;								
			
			error[1] = 0;												
			
			//if (currentBlendingTime != 0)
				errorT = error / (currentActionTime * action.speed); 			
			//else
			//	errorT = new Vector3(0,0,0);
			
			
			//Debug.Log("Error vector = " + error);
			//Debug.Log("Error vector translation = " + errorT);
			//Debug.Log("CurrentActionTime = " + currentActionTime);
			//Debug.Log("Action Speed = " + action.speed);
						
			angleError = newQuat.eulerAngles.y - predictedQuat.eulerAngles.y;
			//angleError = (root.eulerAngles.y - 90.0f) - predictedQuat.eulerAngles.y;
			if (angleError > 180) angleError -= 360;
			if (angleError < -180) angleError += 360;			
			errorR = angleError / (currentActionTime * action.speed);
			//Debug.Log("Angle error = " + angleError);							
			//Debug.Log("Error R = " + errorR);							
			
		}		
		
		if (posCorrection && currentAnimation != null && action != null)
		{
			if (animation.isPlaying && actionNum > 1)
			{	
				//if (action.animInfo.type == LocomotionMode.WalkTurn
				  //  || action.animInfo.type == LocomotionMode.Turn
				    //)
					root.Rotate(0,-errorR*Time.deltaTime,0);					
				//Debug.Log("Rotating: " + errorR*Time.deltaTime);
				
				//if (action.state != null)
				{				
					FootstepPlanningState previousState = (action.state as FootstepPlanningState).previousState;
					
					if (previousState != null && previousState.actionName != null)
					{
						LocomotionMode previousActionType =	analyzer.GetAnotatedAnimation(previousState.actionName).type;
						
						if (action.animInfo.type == LocomotionMode.Walk
						    && previousActionType == LocomotionMode.Walk
						    )
							transform.Translate(-errorT*Time.deltaTime, Space.World);					
					}
				}
			}			
		}
		*/	
	
		if (changed)
		{
			actionNum++;
			
			actionsSinceLastPlan++;
						
			initGameObjectPos = transform.position;
			initGameObjectRotY = transform.eulerAngles.y;
		}
		
		lastRootPos = root.position;
		lastRootRot = root.rotation;
		
		ComputeRootMotion();
		
	}	
	
	
	public void InsertAction(FootstepPlanningAction newAction)
	{
		currentAnimation.time = currentActionTime;
		
		action = newAction;
		
		insertedAction = true;
		
		float lastHeight = initGameObjectPos.y;
		
		initGameObjectPos = transform.position;
		initGameObjectPos.y = lastHeight;		
		initGameObjectRotY = transform.eulerAngles.y;
		
		//Debug.Log("Before = " + planning.currentState.currentPosition);
		
		planning.currentState = action.state as FootstepPlanningState;
		planning.currentState.currentPosition.y = lastHeight;
		
		//Debug.Log("After = " + planning.currentState.currentPosition);
	}	
	
	public void ComputeRootMotion()
	{
		if (currentAnimation == null || action == null )
		{
			//Debug.Log("something is wrong at time " + Time.time );
						
			return;
		}
		
		AnimationState anim = currentAnimation;
				
		Vector3 displacement = Vector3.zero;
		float rotY = 0;
		
		float currentTime = anim.normalizedTime;
		
		CompleteAnimationCurve animCurve = action.animInfo.RootCurve;		
		
		displacement.x = animCurve.xPos.Evaluate(currentTime);
		displacement.y = animCurve.yPos.Evaluate(currentTime);
		displacement.z = animCurve.zPos.Evaluate(currentTime);		
		
		rotY = animCurve.yRot.Evaluate(currentTime);
		
		transform.position = initGameObjectPos;
		transform.rotation = Quaternion.Euler(0,initGameObjectRotY,0);			
		
		displacement = transform.rotation * displacement;
		
		transform.Translate(displacement,Space.World);						
										
		transform.Rotate(new Vector3(0,rotY,0));				
		
		Vector3 forwardProj = new Vector3(-root.right.x,0,-root.right.z);
		Vector3 zVector = new Vector3(0,0,1);
		float newRotY = AnimationAnalyzer.CalculateAngleOf2Vectors(zVector,forwardProj);		
		
		root.RotateAround(transform.position,Vector3.up,-newRotY + initGameObjectRotY + rotY);			
		
		root.localPosition = initLocalRootPos;
		
		/****/
		xPos = animCurve.xPos;		
		yPos = animCurve.yPos;
		zPos = animCurve.zPos;
		yRot = animCurve.yRot;
		/****/
	}
	
}
