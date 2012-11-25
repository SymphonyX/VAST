using UnityEngine;
using System.Collections;

public interface ISteeringManager
{
	void ChangeActorGoal(int id, Vector3 pos);
    void HoldActorAtCurrentPoint(int id);
	
	Vector3 GetActorPosition(int id);
	Vector3 GetActorCurrentVelocity(int id);
	Vector3 GetActorDesiredVelocity(int id);
	
	int AddActor(Vector3 vPos, float radius, float height, float accel, float maxSpeed);
}
