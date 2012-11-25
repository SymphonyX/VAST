using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class AnimationCurveHelper  {
		
	
	/*
	public static AnimationCurve[] GetPlanAnimationCurve(List<DefaultAction> outputPlan)
	{
		int numKeys = outputPlan.Count;
		AnimationCurve curveX = new AnimationCurve();
		AnimationCurve curveY = new AnimationCurve();
		AnimationCurve curveZ = new AnimationCurve();
		float time = Time.time;
		Vector3 pos;
		
		foreach ( DefaultAction action in outputPlan)
		{
			//DefaultAction action = outputPlan.ElementAt(i);
			if (action != null && action.state != null)
			{
				GridTimeState gridTimeState = action.state as GridTimeState;
				time = gridTimeState.time;
				pos = gridTimeState.currentPosition;
					
				curveX.AddKey(time,pos.x);
				curveY.AddKey(time,pos.y);
				curveZ.AddKey(time,pos.z);
				
				totalTime = time;
			}
		}
		
		curves[0] = curveX;
		curves[1] = curveY;
		curves[2] = curveZ;
		return curves;
	
	}
	*/
	
	
	public static float GetPlanAnimationCurve(List<State> outputPlan,AnimationCurve[] curves)
	{
				
		float endTime = 0;
		
		float time;
		Vector3 pos;
		float speed;
		
		if (outputPlan.Count == 0) return endTime;
		
		for (int i= 0; i < outputPlan.Count; i++)
		{
			time = outputPlan[i]._time ;
			pos = outputPlan[i].getPosition();
			speed = outputPlan[i]._speed;
			
			//Debug.LogWarning("adding key " + i + " of " +  outputPlan.Count + " " + time + " "  +pos);
			curves[0].AddKey(time,pos.x);
			curves[1].AddKey(time,pos.y);
			curves[2].AddKey(time,pos.z);
			
			curves[3].AddKey(time,speed);
			
		}
		
		endTime = outputPlan[outputPlan.Count-1]._time;
		return endTime;

	}
	
	
	public static Vector3 getPositionAt(AnimationCurve[] curves, float time)
	{
		float x = curves[0].Evaluate(time);
		float y = curves[1].Evaluate(time);
		float z = curves[2].Evaluate(time);
		Vector3 pos = new Vector3(x,y,z);
		
		return pos; 
	}
	public static void DrawAnimationCurveGizmos (AnimationCurve[] curves, float endTime, float timeDelta, Color curveColor )
	{
		if ( curves.Length != 4 )
		{
			Debug.LogError("4 curves need to be given -- even though this function uses only 3");
			return;
		}
		
		Gizmos.color = curveColor;
		for (float t =timeDelta; t < endTime; t = t + timeDelta)
		{
			Vector3 start = getPositionAt(curves, t-timeDelta);
			Vector3 end = getPositionAt(curves, t);
			//Debug.DrawLine(start,end,curveColor);
			Gizmos.DrawLine(start,end);
			
		}
		
		
	}
}