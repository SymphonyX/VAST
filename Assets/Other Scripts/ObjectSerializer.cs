using UnityEngine;
using System.Collections;

using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System;

/*
 * ObjectSerializer: can read or write any serializable object to/from xml file. 
 * 
 * Use 1: write object to xml file 
 * 	ObjectSerializer os = new ObjectSerializer();
 * 	os.serializedObject = objectThatNeedsToBeWrittenToFile;
 * 	os.writeObjectToFile("fileName.xml");
 * 
 * Use 2: read object from file 
 * 	ObjectSerializer os1 = new ObjectSerializer();
 *  os1.serializedObject = new XYZClass();
 *  os1.readObjectFromFile("fileName.xml");
 * 	objectThatNeedsToBeReadFromFile = (XYZClass) os1.serializedObject;
 */


public class ObjectSerializer 
{
	public object serializedObject;
	
	public void writeObjectToFile (string fileName)
	{
		XmlSerializer s = new XmlSerializer( serializedObject.GetType());
		TextWriter w = new StreamWriter( fileName );
		s.Serialize(w, serializedObject);
		w.Close();
		
		Debug.Log("Object of type " + serializedObject.GetType() + " written to file " + fileName);	
	}
	
	public void readObjectFromFile (string fileName) 
	{
		XmlSerializer s = new XmlSerializer( serializedObject.GetType());
		TextReader r = new StreamReader( fileName );
		serializedObject = s.Deserialize( r );
		
		Debug.Log("Object of type " + serializedObject.GetType() + " read from file " + fileName);	
	}
	
}
