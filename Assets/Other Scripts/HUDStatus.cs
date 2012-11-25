using UnityEngine;
using System.Collections;

public class HUDStatus : MonoBehaviour 
{	 
	 
	void Start()
	{
	    if( !guiText )
	    {
	        Debug.Log("HUDStatus needs a GUIText component!");
	        enabled = false;
	        return;		
		}
	    
		guiText.material.color = Color.green;
		
	}
	 
	void Update()
	{
	    guiText.text = "Time: " + Time.time;		
	}
}