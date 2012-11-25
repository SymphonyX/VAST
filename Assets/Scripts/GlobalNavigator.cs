using UnityEngine;
using System.Collections;


// NavMeshWaypoint is pretty much doing this 

public enum NavigationMeshChoice
{
	USE_RECAST,
	USE_UNITY
}

public static class GlobalNavigator 
{
	public static int maxNumberOfNodes = 32; 
	public static NavigationMeshChoice navigationMeshChoice = NavigationMeshChoice.USE_UNITY;
	public static bool usingDynamicNavigationMesh; 
	
	public static NavMeshPathStatus pathStatus;
	public static int numberOfNodes;
	public static Vector3[] path = new Vector3[maxNumberOfNodes];
	public static float[] pathCost = new float[maxNumberOfNodes];
	public static float totalPathCost;
	
	public static SteeringManager recastSteeringManager = GameObject.Find("ADAPTPrefab").GetComponentInChildren<SteeringManager>();
	
	public static void CalculateNavigationMeshPath (Vector3 startPosition, Vector3 goalPosition, int passableMask)
	{
		ClearNavigationMeshPath ();
		
		switch (navigationMeshChoice)
		{
			
		default:
		case NavigationMeshChoice.USE_UNITY :
			
			NavMeshPath unityPath = new NavMeshPath();
			NavMesh.CalculatePath(startPosition,goalPosition, passableMask,unityPath);
			
			pathStatus = unityPath.status;
			
			
			if (pathStatus == NavMeshPathStatus.PathInvalid)
			{
				numberOfNodes = 0;
			}
			else
			{
				if ( unityPath.corners.Length >= maxNumberOfNodes)
				{
					Debug.LogWarning("Navigation mesh path greater than maximum number of nodes");
					numberOfNodes = maxNumberOfNodes;
					pathStatus = NavMeshPathStatus.PathPartial;
				}
				else 
				{
					numberOfNodes = unityPath.corners.Length;
				}
				
				for(int i =0; i < numberOfNodes; i++)
				{
					// MAKING SURE LAST WAYPOINT IS THE GOAL 
					if ( i == numberOfNodes - 1)
						path[i] = goalPosition;
					else
					path[i] = unityPath.corners[i];
					
					// computing path cost 
					if (i==0) 
						pathCost[i] = 0.0f;
					else 
					{
						// assuming that all off mesh links will override cost 
						if (CentralizedManager.IsOffMeshLink(path[i]))
							pathCost[i] = CentralizedManager.GetOffMeshLinkInformation(path[i]).offMeshLink.costOverride;
						else
							pathCost[i] = Vector3.Distance(path[i],path[i-1]);
					}
					
					totalPathCost += pathCost[i];
					
				}
			}
			
			break;
			
		case NavigationMeshChoice.USE_RECAST:
			
			if (recastSteeringManager == null)
				recastSteeringManager = GameObject.Find("ADAPTPrefab").GetComponentInChildren<SteeringManager>();
			
			numberOfNodes = recastSteeringManager.FindPath(startPosition,goalPosition,path,maxNumberOfNodes);
			
			if( numberOfNodes > 0)
				path[numberOfNodes-1] = goalPosition;
			
			// computing path cost 
			for (int i=1; i < numberOfNodes; i++)
			{
				// TODO : incorporate number of obstacles from centralized manager here 
				pathCost[i] = Vector3.Distance(path[i],path[i-1]);
				totalPathCost += pathCost[i];
			}
			
			if (numberOfNodes == 0)
				pathStatus = NavMeshPathStatus.PathInvalid;
			else if ( numberOfNodes == maxNumberOfNodes)
				pathStatus = NavMeshPathStatus.PathPartial;
			else
				pathStatus = NavMeshPathStatus.PathComplete;
					
			break;
			
		}
	}
	
	public static void ClearNavigationMeshPath ()
	{
		numberOfNodes = 0;
		pathStatus = NavMeshPathStatus.PathInvalid;
		totalPathCost = 0.0f;
	}	
	
}
