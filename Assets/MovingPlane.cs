using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MovingPlane : MonoBehaviour {
	
	
	public AnimationCurve translation_x, translation_y, translation_Z;	
	private AnimationCurve[] curves;
	private Vector3 startPosition; 
	
	
	// Use this for initialization
	void Start () {
	
		curves = new AnimationCurve[3];
		curves[0] = translation_x;
		curves[1] = translation_y;
		curves[2] = translation_Z;
		startPosition = transform.position;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
	
		transform.position = GetPosition(Time.time);
		
	}
	
	public Vector3 GetPosition (float time)
	{
		Vector3 translation = AnimationCurveHelper.getPositionAt(curves,time);
		return (startPosition + translation);
	}
	
}
