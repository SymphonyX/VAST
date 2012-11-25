using UnityEngine;
using System.Collections;

//[ExecuteInEditMode]
public class TextureGroundScript : MonoBehaviour {
		
	
	
	void Start () {
		
		
		
		renderer.material.mainTextureScale = new Vector2(transform.localScale.x*10,transform.localScale.z*10);
		
	}
}
