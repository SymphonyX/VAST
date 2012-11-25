using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// Main Controller class to initialize our world and agents
public class Analysis : MonoBehaviour {
	
	public string fileName;
	
	public GameObject agent;
	
	private AnimationAnalyzer analyzer;
	
	[HideInInspector] public AnimationState currentAnimation;
		
	private float currentActionTime;
	private float currentBlendingTime;
	private float previousBlendingTime;
		
	public bool blending;
	
	private string[] animationNames;
	private string currentAnimationName;
	private int currentAnimationIndex;
	
	private int currentSample;
	
	private bool changed;
	
	private bool stored;
	
	void Awake () {
			
		analyzer = null;
		
		animationNames = null;
		
		currentAnimationName = null;
		
		currentAnimationIndex = 0;
		
		currentAnimation = null;
		
		currentActionTime = 0;
		currentBlendingTime = 0;
		previousBlendingTime = 0;
		
		changed = false;
		
		stored = false;
		
	}
	
	// Use this for initialization
	void Start () {
									
		//AnimationAnalyzer analyzer = agent.GetComponent("AnimationAnalyzer") as AnimationAnalyzer;
		
		if (analyzer == null)
		{
			analyzer = agent.GetComponent("AnimationAnalyzer") as AnimationAnalyzer;
			if (analyzer != null)
				analyzer.Init();				
			
			animationNames = analyzer.animationNames;			
		}	
		
		currentAnimationIndex = 0;
		
		
		
	}
	
		
	void LateUpdate () {		
		
		if (stored)
			return;
		
		// If analysis done remove all animations		
		if (currentAnimationIndex == animationNames.Length)
		{
			if (analyzer != null)
			{
				// Stop and remove animations from the character			
				analyzer.RemoveAnimations(animation);		
			
				// Update global information
				analyzer.UpdateGlobalInfo();			
			
				// Store analysis
				// writing an object to file 
				ObjectSerializer os = new ObjectSerializer();
				os.serializedObject = analyzer.analyzedAnimations;
				os.writeObjectToFile(fileName);
				
				stored = true;
				
				if (analyzer.log)
					analyzer.ReadAnalysisFromFile(fileName);
			}
				
			// End execution	
			return;			
		}		
		
		//if (currentAnimation != null 
		//    && Mathf.Abs(currentAnimation.time) < currentActionTime  		    
		//)
			// We compute the root displacement
			//rootMotion.ComputeRootMotion();
		
		if (changed && currentAnimationName != null)
		{
			//currentAnimation.time = 0;
			//agent.animation.Sample();
			//rootMotion.ComputeRootMotion();
			
			AnotatedAnimation animInfo = analyzer.GetAnotatedAnimation(currentAnimationName);						
			
			// analyze initial state
			analyzer.analyzeInitState(animInfo);
			
			currentSample = 1;
		}
		
		// If its the first animation or the current animation ended,
		if (currentAnimation == null 
		    || Mathf.Abs(currentAnimation.time) >= currentActionTime  		    
		    )
		{				
			if (currentAnimationName != null)			
			{
				Debug.Log("Time difference = " + (currentAnimation.time - currentActionTime));
				
				// We correct the pose to the one we really want to analyze
				//currentAnimation.time = currentActionTime;				
				//agent.animation.Sample();				
				//rootMotion.ComputeRootMotion();
				
				
				AnotatedAnimation endedAnimInfo = analyzer.GetAnotatedAnimation(currentAnimationName);			
				
				// Analyze end state
				analyzer.analyzeState(endedAnimInfo,analyzer.samples-1,currentAnimation.normalizedTime);
				
				// Analyze the movement
				analyzer.analyzeMovement(endedAnimInfo);
				
				// Update global values
				analyzer.updateGlobalInfo(endedAnimInfo);
			}
				
			// Get new animation
			currentAnimationIndex++;
			
			// If there is no more animations we stop
			if (currentAnimationIndex == animationNames.Length)
				return;
			
			string animationName = animationNames[currentAnimationIndex];
				
			if (blending)
			{
				// We blend out the previous animation
				// if it exists and it is not the first one
				if (currentAnimation != null) 
				{
					if (currentAnimationIndex > 0) 
						agent.animation.Blend(currentAnimation.name,0.0f,currentBlendingTime);											
					else
					{
						agent.animation.Stop();
						agent.animation.Sample();
					}
				}
			}
			
			// We set up the speed of the new animation
			AnimationState newAnimationState =  agent.animation[animationName];
			newAnimationState.speed = 1.0f;
			// We set the animation at the beginning
			newAnimationState.time = 0; 	
			newAnimationState.enabled = true;
				
			// We save the new animation
			currentAnimation = newAnimationState;
			currentAnimationName = animationName;
				
			// We get the info of the new animation
			AnotatedAnimation animInfo = analyzer.GetAnotatedAnimation(animationName);
				
			// We compute the time of the action and the time of the blending
			float newActionTime = animInfo.time * animInfo.totalLength;
				
			float newBlendingTime = animInfo.totalLength * animInfo.footPlantLenght;
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
				//We play/blend in the new animation if it's not the first one				
				if (currentAnimationIndex > 0)
					agent.animation.Blend(currentAnimation.name,1.0f,currentBlendingTime);
				else
				{
					agent.animation.Play(currentAnimation.name);
					agent.animation.Sample();
				}							
			}
			else
				agent.animation.Play(currentAnimation.name);
			
			changed = true;
			
			currentSample = 0;
					
		}
		else
		{
			changed = false;
			
			if (currentAnimationName != null)
			{					
				AnotatedAnimation animInfo = analyzer.GetAnotatedAnimation(currentAnimationName);
								
				float previousSampleTime = (currentSample-1)*animInfo.time/(analyzer.samples-1);
				previousSampleTime *= animInfo.totalLength;
				
				float timeBetweenSamples = animInfo.time/(analyzer.samples-1);
				
				float timeDifference = Mathf.Abs(currentAnimation.time) - previousSampleTime;
				
				if (timeDifference >= timeBetweenSamples)					
				{
					// We correct the pose to the one we really want to analyze
					//currentAnimation.time = currentSample*animInfo.time/(analyzer.samples-1);
					//agent.animation.Sample();
					
					analyzer.analyzeState(animInfo,currentSample,currentAnimation.normalizedTime);					
				
					currentSample++;
				}			
			}
		}

	}
	
}