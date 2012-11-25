using UnityEngine;
using System.Collections;

[System.Serializable]
public class State : Observable
{
	// MAKING PUBLIC JUST SO WE CAN SEE IT IN THE EDITOR -- WE SHOULD NOT ACCESS THIS DIRECTLY !!! 
	public  Vector3 _position;
	public float _time;
	public float _speed;
	
	public TextMesh textDisplay;
	
	
	public State ()
	{
		
	}
	public State (Vector3 t_position)
	{
		setPosition(t_position);
	}
	
	public State (Vector3 t_position, float t_time)
	{
		setPosition(t_position);
		_time = t_time; // TODO -- we might need getTime 		
	}
	
	public State (Vector3 t_position, float t_time, float t_speed)
	{
		setPosition(t_position);
		_time = t_time; // TODO -- we might need getTime 	
		_speed = t_speed;
	}
	
	public Vector3 getPosition () 
	{ 
		return _position;
	}
	
	public void setPosition ( Vector3 t_position )
	{
		Vector3[] posChange = new Vector3[2]; // {oldPos, newPos}
		posChange[0] = _position; posChange[1] = t_position;
		
		_position = t_position;
		
		// notify observers after you have actually updated the position (because observers will check with the reference to this state )
		notifyObservers(Event.STATE_POSITION_CHANGED,posChange);
		
	}
	
	
	public bool ApproximatelyEquals ( State otherState, float distanceThreshold = 0.25F, float timeThreshold = 1.0F)
	{
		return (Vector3.Distance(otherState.getPosition(),this.getPosition()) < distanceThreshold);
			//&& Mathf.Abs(this._time - otherState._time) < timeThreshold);
	}
	
	// this might go 
	public bool positionEquals ( Vector3 t_pos ) 
	{
		return (Vector3.Distance(t_pos,this.getPosition()) < 0.25F);
	}
	
	public override bool Equals (object obj)
	{
		State objState = obj as State;
		return obj!=null && (Vector3.Distance(objState.getPosition(),this.getPosition()) < 0.25F);
	}
	
	public void drawStateGizmoWithTextDisplay (Color color)
	{
		if (textDisplay == null)
		{
			Transform td = GameObject.Instantiate(CentralizedManager.textDisplayPrefab,_position, Quaternion.identity) as Transform;
			textDisplay = td.GetComponent<TextMesh>();
		}
		
		textDisplay.transform.position = _position;
		textDisplay.text = _time.ToString();
		
		drawStateGizmo(color);
		
	}
	
	public void drawStateGizmo (Color color) 
	{
		
		//Graphics.DrawMesh(textMesh, position, Quaternion.identity, material, 0);
		Gizmos.color = color;
		Gizmos.DrawSphere(_position ,0.1F);
	}
		
}


