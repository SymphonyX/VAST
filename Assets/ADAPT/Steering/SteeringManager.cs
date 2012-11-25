using UnityEngine;

using System;
using System.Collections;
using System.Runtime.InteropServices;

public class SteeringManager : MonoBehaviour, ISteeringManager
{
	public Navmesh navmesh = null;
	public int maxAgents = 10000;
	public float maxAgentRadius = 0.5f;
	
    public enum Pushiness { PUSHINESS_LOW, PUSHINESS_MEDIUM, PUSHINESS_HIGH };
    public enum NavigationQuality { Low, Med, High };
    public enum PathRequestStatus { PRS_NONE, PRS_PENDING, PRS_SUCCEEDED, PRS_FAILED };
	
	bool initialized = false;
	protected IntPtr dtNavMesh;
	protected IntPtr dtNavMeshQuery;
	protected IntPtr dtCrowd;
		
	public int MAX_COUNT = 5;
	public float MAX_AREA_COST = 4;
	private int prevMAX_COUNT = 5;
	private float prevMAX_AREA_COST = 4;
			
	public bool densityBasedPath = true;
	private bool prevDensityBasedPath = true;
	
	public bool isInitialized()
	{
		return initialized;	
	}
	
	void Awake()
	{
     	if (!initialized)
			Initialize();
	}
	
	public void Initialize()
	{
	   if (this.navmesh != null)
		{
            if (this.navmesh.Data != null)
			{
				this.dtNavMesh = NativeInitNavmesh(
                    this.navmesh.Data,
                    this.navmesh.Data.Length);
				this.dtNavMeshQuery = NativeInitQuery(this.dtNavMesh);
				this.dtCrowd = NativeInitCrowd(
					this.dtNavMesh, 
					this.maxAgents, 
					this.maxAgentRadius);
				this.initialized = true;
			}
			else
			{
				Debug.LogError("Null Navmesh");
			}
		}
		else
		{
			Debug.LogError("No Navmesh");
		}		
	}
	
	void Update()
    {
		CrowdStep(Time.deltaTime);
		
		if (MAX_COUNT != prevMAX_COUNT)
		{
			SetMaxCount(MAX_COUNT);	
			
			//Debug.Log("At time " + Time.time + " MAX_COUNT = " + GetMaxCount());
		}
		prevMAX_COUNT = MAX_COUNT;
		
		if (MAX_AREA_COST != prevMAX_AREA_COST)
		{
			SetMaxAreaCost(MAX_AREA_COST);
			
			//Debug.Log("At time " + Time.time + " MAX_AREA_COST = " + GetMaxAreaCost());
		}
		prevMAX_AREA_COST = MAX_AREA_COST;
		
		if (prevDensityBasedPath != densityBasedPath)
		{
			if (densityBasedPath)
			{				
				SetMaxAreaCost(MAX_AREA_COST);
			}
			else
			{				
				SetMaxAreaCost(0);
			}
		}
		
		prevDensityBasedPath = densityBasedPath;
    }
	
	void OnDisable()
	{
		if (initialized)
		{
			NativeDestroyNavmesh(this.dtNavMesh);
			NativeDestroyQuery(this.dtNavMeshQuery);
			NativeDestroyCrowd(this.dtCrowd);
		}
	}
	
	public void CrowdStep(float dT)
	{
		if (initialized)
			NativeCrowdStep(this.dtCrowd, dT);
	}
	
	public int AddActor(Vector3 vPos, float radius, float height, float accel, float maxSpeed)
	{
		if (initialized)
			return NativeAddActor(this.dtCrowd, vPos, radius, height, accel, maxSpeed);
		return -1;
	}
	
	public void ChangeActorGoal(int actor, Vector3 vGoal)
	{
		if (initialized)
			NativeChangeActorGoal(this.dtNavMeshQuery, this.dtCrowd, actor, vGoal);
	}
	
	public Vector3 GetActorPosition(int actor)
	{
		if (initialized)
			return NativeGetActorPosition(this.dtCrowd, actor);
		return Vector3.zero;
	}
	
	public Vector3 GetActorCurrentVelocity(int actor)
	{
		if (initialized)
			return NativeGetActorCurrentVelocity(this.dtCrowd, actor);
		return Vector3.zero;
	}
	
	public Vector3 GetActorDesiredVelocity(int actor)
	{
		if (initialized)
			return NativeGetActorDesiredVelocity(this.dtCrowd, actor);
		return Vector3.zero;
	}

    public void HoldActorAtCurrentPoint(int actor)
    {
        if (initialized)
            NativeHoldActorAtCurrentPoint(
                this.dtNavMeshQuery, this.dtCrowd, actor);
    }
	
	public int FindPath(Vector3 startPos, Vector3 endPos, Vector3[] path, int maxPath)
	{
		if (initialized)
			return NativeFindPath(this.dtNavMesh,this.dtNavMeshQuery,startPos,endPos,path,maxPath);
		
		return 0;
	}	
	
	public uint GetClosestPolygon(Vector3 pos)
	{
		if (initialized)
			return NativeGetClosestPolygon(this.dtNavMeshQuery,pos);
		
		return 0;
	}
	
	public void IncrNumObjectsInPolygon(uint polyRef)
	{
		if (initialized)
			NativeIncrNumObjectsInPolygon(this.dtNavMesh,polyRef);
	}
	
	public void DecrNumObjectsInPolygon(uint polyRef)
	{
		if (initialized)
			NativeDecrNumObjectsInPolygon(this.dtNavMesh,polyRef);
	}
	
	public uint GetNumOjbectsInPolygon(uint polyRef)
	{
		if (initialized)
			return NativeGetNumObjectsInPolygon(this.dtNavMesh,polyRef);
		
		return 0;
	}
	
	public uint ReturnOne()
	{
		if (initialized)
			return NativeReturnOne();
				
		return 0;
	}
	
	public void SetMaxCount(int maxCount)
	{
		if (initialized)
			NativeSetMaxCount(maxCount);
	}
	
	public int GetMaxCount()
	{
		if (initialized)
			return NativeGetMaxCount();
		
		else
			return 0;
	}	
	
	public void SetMaxAreaCost(float maxAreaCost)
	{
		if (initialized)
			NativeSetMaxAreaCost(maxAreaCost);
	}
	
	public float GetMaxAreaCost()
	{
		if (initialized)
			return NativeGetMaxAreaCost();
		
		else
			return 0;
	}
	
	[DllImport("Steering_RecastDetour", EntryPoint="CrowdStep")]
    private static extern IntPtr NativeCrowdStep(IntPtr dtCrowd, float dT);
	[DllImport("Steering_RecastDetour", EntryPoint="AddActor")]
    private static extern int NativeAddActor(
		IntPtr dtCrowd, 
		[MarshalAs(UnmanagedType.LPArray)] Vector3 vPos, 
		float radius, 
		float height, 
		float accel, 
		float maxSpeed);
	[DllImport("Steering_RecastDetour", EntryPoint="ChangeActorGoal")]
    private static extern int NativeChangeActorGoal(
		IntPtr dtNavMeshQuery,
		IntPtr dtCrowd, 
		int actor,
		[MarshalAs(UnmanagedType.LPArray)] Vector3 vPos);
    [DllImport("Steering_RecastDetour", EntryPoint = "HoldActorAtCurrentPoint")]
    private static extern int NativeHoldActorAtCurrentPoint(
        IntPtr dtNavMeshQuery,
        IntPtr dtCrowd,
        int actor);
	[DllImport("Steering_RecastDetour", EntryPoint="GetActorPosition")]
    private static extern Vector3 NativeGetActorPosition(IntPtr dtCrowd, int Actor);
	[DllImport("Steering_RecastDetour", EntryPoint="GetActorCurrentVelocity")]
    private static extern Vector3 NativeGetActorCurrentVelocity(IntPtr dtCrowd, int Actor);
	[DllImport("Steering_RecastDetour", EntryPoint="GetActorDesiredVelocity")]
    private static extern Vector3 NativeGetActorDesiredVelocity(IntPtr dtCrowd, int Actor);
	
	[DllImport("Steering_RecastDetour", EntryPoint="InitNavmesh")]
	private static extern IntPtr NativeInitNavmesh(
		[MarshalAs(UnmanagedType.LPArray)] byte[] data, 
		int dataSize);
	[DllImport("Steering_RecastDetour", EntryPoint="DestroyNavmesh")]
	private static extern void NativeDestroyNavmesh(IntPtr ptr);
	
	[DllImport("Steering_RecastDetour", EntryPoint="InitQuery")]
	private static extern IntPtr NativeInitQuery(IntPtr dtNavMesh);
	[DllImport("Steering_RecastDetour", EntryPoint="DestroyQuery")]
	private static extern void NativeDestroyQuery(IntPtr dtNavMeshQuery);
		
	[DllImport("Steering_RecastDetour", EntryPoint="ReturnOne")]
	private static extern uint NativeReturnOne();
	
	[DllImport("Steering_RecastDetour", EntryPoint="IncrNumObjectsInPolygon")]
	private static extern void NativeIncrNumObjectsInPolygon(
		IntPtr dtNavMesh, 
		uint polygonRef);

	[DllImport("Steering_RecastDetour", EntryPoint="DecrNumObjectsInPolygon")]
	private static extern void NativeDecrNumObjectsInPolygon(
		IntPtr dtNavMesh, 
		uint polygonRef);
	
	[DllImport("Steering_RecastDetour", EntryPoint="GetNumObjectsInPolygon")]
	private static extern uint NativeGetNumObjectsInPolygon(
		IntPtr dtNavMesh, 
		uint polygonRef);
	
	[DllImport("Steering_RecastDetour", EntryPoint="SetMaxCount")]
	private static extern void NativeSetMaxCount(
		int maxCount);
	
	[DllImport("Steering_RecastDetour", EntryPoint="GetMaxCount")]
	private static extern int NativeGetMaxCount();
	
	[DllImport("Steering_RecastDetour", EntryPoint="SetMaxAreaCost")]
	private static extern void NativeSetMaxAreaCost(
		float maxAreaCost);
	
	[DllImport("Steering_RecastDetour", EntryPoint="GetMaxAreaCost")]
	private static extern float NativeGetMaxAreaCost();
	
	[DllImport("Steering_RecastDetour", EntryPoint="GetClosestPolygon")]
	private static extern uint NativeGetClosestPolygon(
		IntPtr dtNavMeshQuery,
		[MarshalAs(UnmanagedType.LPArray)] Vector3 pos);
	
	[DllImport("Steering_RecastDetour", EntryPoint="FindPath")]
	private static extern int NativeFindPath(
		IntPtr dtNavMesh, 
		IntPtr dtNavMeshQuery,
		[MarshalAs(UnmanagedType.LPArray)] Vector3 startPos,
		[MarshalAs(UnmanagedType.LPArray)] Vector3 endPos,
	    [MarshalAs(UnmanagedType.LPArray)] Vector3[] path,	    
	    int maxPath);
	
	[DllImport("Steering_RecastDetour", EntryPoint="InitCrowd")]
	private static extern IntPtr NativeInitCrowd(IntPtr dtNavMesh, int maxAgents, float maxAgentRadius);
	[DllImport("Steering_RecastDetour", EntryPoint="DestroyCrowd")]
	private static extern void NativeDestroyCrowd(IntPtr dtCrowd);
}







/*
        if (orientate && status==MoveStatus.EnRoute) {
			// If we're close enough to the end-point, turn to face it
            if (targetName != null && (targetPos - transform.position).magnitude < turnAtDistance + holdingRadius)
            {
                if(holdingRadius > 0)
                    dho.desiredOrientation = Quaternion.LookRotation(targetPos - transform.position);
                else
                    dho.desiredOrientation = endRotation;
			// Drivespeed is how fast you turn? (Less for walking, more for ending)
                dho.driveSpeed = arrivingOrientSpeed;
            }
            else
            {
			// If we're moving, significantly
                if(vel.sqrMagnitude > 0.01f)
                {
                    dho.desiredOrientation = Quaternion.LookRotation(vel);
                    dho.driveSpeed = walkingOrientSpeed;
                }
            }
        }
*/
