using UnityEngine;
using System.Collections;
using UnityEditor;



public class MenuItems : EditorWindow
{
	
	[MenuItem("Reproduction/Normal Execution")]
	static void SelectNormal(){
		//FootstepPlanningTest.MODE = REPRODUCTIONMODE.NORMAL;
		//Debug.Log(FootstepPlanningTest.MODE);
	}
	
	[MenuItem("Reproduction/Record Execution")]
	static void SelectRecord(){
		//FootstepPlanningTest.MODE = REPRODUCTIONMODE.RECORD;	
		//Debug.Log(FootstepPlanningTest.MODE);
	}
	
	
	[MenuItem("Reproduction/PlayBack")]
	static void SelectPlayback(){
		//FootstepPlanningTest.MODE = REPRODUCTIONMODE.PLAYBACK;
		//Debug.Log(FootstepPlanningTest.MODE);
	}
	
	          

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
