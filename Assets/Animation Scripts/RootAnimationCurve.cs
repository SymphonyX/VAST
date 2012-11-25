using UnityEngine;
using System.Collections;

public struct RootCurve{
	
	public AnimationCurve xPos;
	public AnimationCurve yPos;
	public AnimationCurve zPos;
	
	public AnimationCurve yRot;	
}

public class RootAnimationCurve : MonoBehaviour {
	
	public GameObject rootRepresentation;
	
	public bool blending = false;
	
	public float time = 1.0f;	
	
	public string[] animationNames;
	
	public int samples = 10; // the number of samples we want to analyze of each animation clip
	
	public Transform root;		
	
	public SerializableDictionary<string,RootCurve> animationCurves;
	
	private AnimationState currentAnimState;
	private int currentAnimIndex;
	
	private Vector3 initRootLocalPos;
	private Vector3 prevEndPos;
	private float prevEndRot;
	
	public AnimationCurve xPos;
	public AnimationCurve yPos;
	public AnimationCurve zPos;
	
	public AnimationCurve yRot;
	
	private bool changed = false;
	
	// Use this for initialization
	void Start () {				
				
		animationCurves = new SerializableDictionary<string,RootCurve>();
		
		Vector3 initPos = transform.position;
		Quaternion initRot = transform.rotation;
		
		initRootLocalPos = root.localPosition;
		
		Vector3 originalRootPos = root.position;
		Quaternion originalRootRot = root.rotation;
		
		Vector3 prevEndPos = transform.position;
		
		for (int i=0; i<animationNames.Length; i++)		
		{			
			RootCurve animCurve;
			
			animCurve.xPos = new AnimationCurve();
			animCurve.yPos = new AnimationCurve();
			animCurve.zPos = new AnimationCurve();		
			
			animCurve.yRot = new AnimationCurve();
			
			float initYRot = 0;
			
			transform.position = Vector3.zero;
			transform.rotation = Quaternion.identity;
						
			AnimationState anim = animation[animationNames[i]];
					
			anim.enabled = true;
			anim.wrapMode = WrapMode.Loop;						
			
			Vector3 initRootPos = root.position;
			
			// For every sample we want
			for (int s=0; s<samples; s++)
			{						
				// Sample the Animation until the end of our action
				anim.normalizedTime = s*time/(samples-1);
				animation.Play(anim.name);		
				animation.Sample();
					
				if (s==0)
				{
					initRootPos = root.position;
					
					Vector3 initForwardProj = new Vector3(-root.right.x,0,-root.right.z);
					Vector3 initZVector = new Vector3(0,0,1);
					initYRot = AnimationAnalyzer.CalculateAngleOf2Vectors(initZVector,initForwardProj);					
				}
								
				Vector3 rootDisplacement = root.position - initRootPos;
				
				animCurve.xPos.AddKey(s*time/(samples-1),rootDisplacement.x);
				if (s==samples-1)
					animCurve.yPos.AddKey(s*time/(samples-1),0);
				else
					animCurve.yPos.AddKey(s*time/(samples-1),rootDisplacement.y);
				animCurve.zPos.AddKey(s*time/(samples-1),rootDisplacement.z);
			
				
				Vector3 forwardProj = new Vector3(-root.right.x,0,-root.right.z);
				Vector3 zVector = new Vector3(0,0,1);
				float yRot = AnimationAnalyzer.CalculateAngleOf2Vectors(zVector,forwardProj) - initYRot;
				
				animCurve.yRot.AddKey(s*time/(samples-1),yRot);
			}			
			
			animCurve.xPos.postWrapMode = WrapMode.Clamp;
			animCurve.xPos.preWrapMode = WrapMode.Clamp;
			animCurve.yPos.postWrapMode = WrapMode.Clamp;
			animCurve.yPos.preWrapMode = WrapMode.Clamp;
			animCurve.zPos.postWrapMode = WrapMode.Clamp;
			animCurve.zPos.preWrapMode = WrapMode.Clamp;
			
			animCurve.yRot.postWrapMode = WrapMode.Clamp;
			animCurve.yRot.preWrapMode = WrapMode.Clamp;
			
			animationCurves.Add(animationNames[i],animCurve);			
			
			// We remove the animation
			anim.normalizedTime = 0.0f;
			anim.enabled = false;
			animation.Stop();		
			animation.Sample();			
			
		}		
		
		root.rotation = originalRootRot;
		root.position = originalRootPos;
		
		transform.position = initPos;
		transform.rotation = initRot;
		
		
		currentAnimState = null;
		currentAnimIndex = -1;
		
		
		Vector3 position = transform.position;
		Quaternion rot = transform.rotation;		
		
		for (int i=0; i<animationNames.Length; i++)		
		{			
			Vector3 displacement = Vector3.zero;
			float rotY = 0;
			
			float currentTime = time;
			
			RootCurve animCurve = animationCurves[animationNames[i]];
			
			displacement.x = animCurve.xPos.Evaluate(currentTime);
			displacement.y = animCurve.yPos.Evaluate(currentTime);
			displacement.z = animCurve.zPos.Evaluate(currentTime);		
									
			rotY = animCurve.yRot.Evaluate(currentTime);
										
			position += rot * displacement;
			
			Quaternion rotation = Quaternion.Euler(0,rotY,0);
			rot = rotation * rot;			
			
			Instantiate(rootRepresentation,position,Quaternion.identity);
		}
				
		Destroy(rootRepresentation);
		
		
		changed = false;
	}
	
	void Update()
	{
	
		if ( currentAnimState == null || currentAnimState.normalizedTime >= time )	
		{							
			//if (currentAnimIndex < animationNames.Length-1)
			currentAnimIndex++;
			currentAnimIndex = currentAnimIndex%animationNames.Length;
			
			currentAnimState = animation[animationNames[currentAnimIndex]];
			currentAnimState.normalizedTime = 0;
			
			if (!blending)
				animation.Play(currentAnimState.name);
			else
				animation.CrossFade(currentAnimState.name);
								
			changed = true;
		}		
		else
			changed = false;
		
	}
	
	
	void LateUpdate () {
	
		AnimationState anim = currentAnimState;
						
		Vector3 displacement = Vector3.zero;
		float rotY = 0;
		
		float currentTime = anim.normalizedTime;
		
		RootCurve animCurve = animationCurves[anim.name];
			
		displacement.x = animCurve.xPos.Evaluate(currentTime);
		displacement.y = animCurve.yPos.Evaluate(currentTime);
		displacement.z = animCurve.zPos.Evaluate(currentTime);		
		
		rotY = animCurve.yRot.Evaluate(currentTime);
	
		if (changed)
		{
			prevEndPos = transform.position;			
			
			prevEndRot = transform.eulerAngles.y;	
		}
		
				
		transform.position = prevEndPos;
		transform.rotation = Quaternion.Euler(0,prevEndRot,0);			
		
		displacement = transform.rotation * displacement;
		
		transform.Translate(displacement,Space.World);						
										
		transform.Rotate(new Vector3(0,rotY,0));
				
		
		Vector3 forwardProj = new Vector3(-root.right.x,0,-root.right.z);
		Vector3 zVector = new Vector3(0,0,1);
		float newRotY = AnimationAnalyzer.CalculateAngleOf2Vectors(zVector,forwardProj);		
		
		root.RotateAround(transform.position,Vector3.up,-newRotY + prevEndRot + rotY);
				
		root.localPosition = initRootLocalPos;			
		
		xPos = animCurve.xPos;		
		yPos = animCurve.yPos;
		zPos = animCurve.zPos;
		yRot = animCurve.yRot;
		
	}
}
