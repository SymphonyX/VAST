using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DrawTest : MonoBehaviour {
	
	TrajectoryVisualizer trajectoryVisualizer; 
	
	
	public Mesh sphereMesh;
	public Material material;
	TextMesh textMesh;
	public Font font; 
	
	// for text meshes 
	// http://unity3d.com/support/documentation/Components/class-TextMesh.html
	
		
	// Use this for initialization
	void Start () {
		
		//textMesh = new TextMesh();
		//textMesh.text = sfk,fs=
		//textMesh.font = font;
		//textMesh.characterSize = 2;
				
	
		trajectoryVisualizer = new TrajectoryVisualizer(Color.black,10.0F);
		
		for (int i = 0; i < 10; i ++)
			trajectoryVisualizer.AddPoint((float) i, new Vector3((float) i, 0.0F, 0.0F));
	}
	
	
	void Update ()
	{
		 //Graphics.DrawMesh(textMesh, Vector3.zero, Quaternion.identity, material, 0);
		//Graphics.DrawMesh(sphereMesh, Vector3.zero, Quaternion.identity, material, 0);
	}
	// Update is called once per frame
	void OnPostRender() {
		
		Debug.Log ("hell");
		trajectoryVisualizer.Render ();
	
	}
}
