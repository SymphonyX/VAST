using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SteeringManager))]
public class Manager : MonoBehaviour
{
	public const string STEERING_NAME = "Steering Manager";
	
	private static Manager instance = null;
    public static Manager Instance
	{
		get
		{
            if (instance == null)
            {
                GameObject go = GameObject.Find("/Manager");
                if (go == null)
                {
                    go = new GameObject("Managers");
                    go.AddComponent<Manager>();
                }
                instance = go.GetComponent<Manager>();
            }
			return instance;
		}
	}

    private static ISteeringManager steering;
	public static ISteeringManager Steering
	{	
		get
		{
            if (steering == null)
                steering = 
                    (ISteeringManager)Instance.gameObject.GetComponent(
                        typeof(ISteeringManager));
            return steering;
		}
	}
}
