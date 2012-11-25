using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CentralizedManagerComponent : MonoBehaviour {
	
	public NavigationMeshChoice navigationMeshChoice;
	public bool useDynamicNavigationMesh; 
	
	public GUIText worldGUI;
	
	
	public Object textDisplayPrefab;
	
	
	// Use this for initialization
	void Start () {
		
		GlobalNavigator.navigationMeshChoice = navigationMeshChoice;
		GlobalNavigator.usingDynamicNavigationMesh = useDynamicNavigationMesh;
		
		CentralizedManager.Initialize();
		CentralizedManager.textDisplayPrefab = textDisplayPrefab; // so that all scripts can access it
		
		
	
	}
	
	// Update is called once per frame
	void Update () {
		
		string guiText = "Time " + Time.time + "\n";
		guiText += "Number of Agents" + CentralizedManager.agents.Length.ToString() + "\n";
		
		if ( CentralizedManager.simpleAgents !=null)
			guiText += "Number of Simple Agents" + CentralizedManager.simpleAgents .Length.ToString() + "\n";
		
		guiText += "Number of Non Deterministic Obstacles " + CentralizedManager.nonDeterministicObstacles.Length.ToString() + "\n";
		guiText += "Number of Off Mesh Links " + CentralizedManager.offMeshLinkDictionary.Count.ToString() + "\n";
		
		if ( GlobalNavigator.usingDynamicNavigationMesh == true )
		{
			Dictionary<uint,PolygonData> polygonDictionary = CentralizedManager.polygonDictionary;
			
			foreach(uint key in polygonDictionary.Keys)
			{
				guiText += key.ToString() + ":" + polygonDictionary[key].numberOfChanges.ToString () + ":" + 
					GlobalNavigator.recastSteeringManager.GetNumOjbectsInPolygon(key).ToString() + " " ;
				
			}
			
			guiText += "\n";
		}
		
		worldGUI.text = guiText;
		
	
	}
}
