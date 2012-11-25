using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectSerializerTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
		List<AnotatedAnimation> aList = new List<AnotatedAnimation>();
		AnotatedAnimation anotatedAnimation = new AnotatedAnimation();
		anotatedAnimation.angle = 0.1F;
		anotatedAnimation.distance = 1.0F;
		anotatedAnimation.LeftFoot = new JointState[2];
		JointState js = new JointState();
		js.angle = 0.1F;
		js.orientation = new Vector3(1.0F,1.0F,1.0F);
		anotatedAnimation.LeftFoot[0] = js;
		anotatedAnimation.LeftFoot[1] = js;
		aList.Add(anotatedAnimation);
		aList.Add(anotatedAnimation);
		
		// writing an object to file 
		ObjectSerializer os = new ObjectSerializer();
		os.serializedObject = aList;
		os.writeObjectToFile("test.xml");
		
		//reading an object from file 
		ObjectSerializer os1 = new ObjectSerializer();
		os1.serializedObject = new List<AnotatedAnimation>();
		os1.readObjectFromFile("test.xml");
		List<AnotatedAnimation> objectThatNeedsToBeReadFromFile = (List<AnotatedAnimation>) os1.serializedObject;
		Debug.Log("after read " + objectThatNeedsToBeReadFromFile[1].LeftFoot[1].orientation);
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
