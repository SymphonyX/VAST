using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Enum
{
	public enum Enumeration {Sphere, Rectangle, Cylinder}
	
	public Enumeration Shape;
}

[RequireComponent(typeof(Animation))]

[ExecuteInEditMode]
public class ObstaclesScript : MonoBehaviour {
	
	public bool deterministic = false;
	public Enum ShapeChoice;
	public Vector3 size;
	
	public bool useDisplacementMode = false;
	public bool useGameObjectTransformAsInitPoint = true;
	public List<Vector4> spaceTimePoints = new List<Vector4>();
		
	[HideInInspector] public AnimationCurve translation_x, translation_y, translation_z;
					//rotation_x, rotation_y, rotation_z;
	[HideInInspector] public AnimationClip clip;
	
	bool wasPlaying;
	[HideInInspector] public GameObject gameObj = null;
	
	public Material obstacleMaterial;
	
	// Use this for initialization
	void Start () {
	
		init();
		
		if ( gameObj == null)
			CreatePrimitive(ref gameObj);
		
	}
	
	public void init(){
				
		Vector3 pos = transform.position;
		float time = 0;
		
		translation_x = new AnimationCurve();
		translation_y = new AnimationCurve();
		translation_z = new AnimationCurve();
		
		if (useGameObjectTransformAsInitPoint)
		{
			// We add the start keyframe at the position of the gameObject
			translation_x.AddKey(time, pos.x);
			translation_y.AddKey(time, pos.y);
			translation_z.AddKey(time, pos.z);
		}
			
		foreach( Vector4 spaceTimePoint in spaceTimePoints)
		{
			if (!useDisplacementMode)
			{
				time = spaceTimePoint.w;
				
				translation_x.AddKey(time,spaceTimePoint.x);
				translation_y.AddKey(time,spaceTimePoint.y);
				translation_z.AddKey(time,spaceTimePoint.z);
			}
			else
			{
				time += spaceTimePoint.w;
				
				pos.x += spaceTimePoint.x;
				pos.y += spaceTimePoint.y;
				pos.z += spaceTimePoint.z;
				
				translation_x.AddKey(time,pos.x);
				translation_y.AddKey(time, pos.y);
				translation_z.AddKey(time, pos.z);				
			}
		}
		
		// We return the obstacle to the start position with a last keyframe
		//translation_x.AddKey(time, transform.position.x);
		//translation_y.AddKey(time, transform.position.y);
		//translation_Z.AddKey(time, transform.position.z);
		
		
		translation_x.preWrapMode = WrapMode.PingPong;
		translation_x.postWrapMode = WrapMode.PingPong;
		translation_y.preWrapMode = WrapMode.PingPong;
		translation_y.postWrapMode = WrapMode.PingPong;
		translation_z.preWrapMode = WrapMode.PingPong;
		translation_z.postWrapMode = WrapMode.PingPong;
		
		clip = new AnimationClip();
						
		clip.SetCurve("", typeof(Transform), "localPosition.x", translation_x);
		clip.SetCurve("", typeof(Transform), "localPosition.y", translation_y);
		clip.SetCurve("", typeof(Transform), "localPosition.z", translation_z);
		//clip.SetCurve("", typeof(Transform), "localRotation.x", rotation_x);
		//clip.SetCurve("", typeof(Transform), "localRotation.y", rotation_y);
		//clip.SetCurve("", typeof(Transform), "localRotation.z", rotation_z);
		
		//Debug.Log("Clip lenght = " + clip.length);
		
		animation.AddClip(clip, "test");		
		
		//animation["test"].enabled = true;
		animation.clip = clip;
		
		//animation.Play("test");
		
		//for (float i = 0; i<translation_x.length*5; i+=0.5f)
		//	Debug.Log("Time " + i + ": " + translation_x.Evaluate(i));
	}
	
	// Update is called once per frame
	void Update () {
		
		initUpdate();
				
	}
	
	public void initUpdate()
	{
		if(EditorApplication.isPlaying)
		{
			if(!animation.isPlaying)
				animation.Play("test");				
		}		
		else		
		{
			for(int i=0; i < transform.childCount; ++i)
			{
				GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
			}
						
		}
		
	}
	
	
	public void CreatePrimitive(ref GameObject obj)
	{
		
		if(ShapeChoice.Shape.Equals(Enum.Enumeration.Sphere)){
			obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			
		}
		else if(ShapeChoice.Shape.Equals(Enum.Enumeration.Rectangle))
			obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
		else 
			obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		
		MeshRenderer renderer = obj.GetComponent("MeshRenderer") as MeshRenderer;
		renderer.material = obstacleMaterial;
		
		obj.transform.parent = transform;
		obj.transform.position = transform.position;
		obj.transform.localScale = size;		
		
		//obj.AddComponent<SphereCollider>();
		//obj.AddComponent<MeshCollider>();		
		//obj.AddComponent<Rigidbody>();
		obj.collider.transform.localScale = size;
		
		if (!deterministic)
			obj.layer = LayerMask.NameToLayer("Obstacles");
		else
		{
			//MeshCollider meshCollider = obj.GetComponent("MeshCollider") as MeshCollider;
			//meshCollider.isTrigger = true;			
			//Debug.Log("name " + gameObject.rigidbody.name);
			
			//Rigidbody rigidBody = obj.GetComponent("Rigidbody") as Rigidbody;
			//rigidbody.useGravity = false;			
			
			obj.layer = LayerMask.NameToLayer("DeterministicObstacles");
			
			obj.AddComponent<DynamicObstacle>();
		}
		
				
		
	}
	
	
}

