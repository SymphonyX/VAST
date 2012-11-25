using UnityEngine;
using System.Collections;

public enum OrientationQuality
{
    Low,
    High
}

public class SteeringComponent : MonoBehaviour 
{
	private ISteeringManager steering = null;
    private int id = -1;
	
	private Vector3 currentPosition = Vector3.zero;
	private Vector3 currentVelocity = Vector3.zero;
	private Vector3 desiredVelocity = Vector3.zero;
    private Vector3 targetPosition = Vector3.zero;
	
	public Vector3 CurrentPosition { get { return this.currentPosition; } }
	public Vector3 CurrentVelocity { get { return this.currentVelocity; } }
	public Vector3 DesiredVelocity { get { return this.desiredVelocity; } }
    public Vector3 TargetPosition { get { return this.targetPosition; } }
	
	private Quaternion desiredOrientation = Quaternion.identity;
	public Quaternion DesiredOrientation 
		{ get { return this.desiredOrientation; } }

    public float radius = 0.6f;
	public float height = 2.0f;
	public float acceleration = 4.0f;
	public float maxSpeed = 1.0f;
    public float holdingRadius = 0.5f;
		
    public bool orientate = true;
    public float walkingOrientationSpeed = 15f;
    public float arrivingOrientationSpeed = 120f;
    public float arrivingRadius = 1.0f;
    public OrientationQuality orientationQuality = OrientationQuality.High;

    public float orientationSpeed;

    public bool mobile = false;

	void Awake()
	{
		this.steering = Manager.Steering;
		if (this.steering == null)
			Debug.Log("No Steering manager");
	}
	
    public void Initialize() 
	{
        this.targetPosition = transform.position;
        this.desiredOrientation = transform.rotation;
        this.orientationSpeed = this.walkingOrientationSpeed;
		if (this.steering != null)
		{
			this.id = steering.AddActor(
				transform.position, 
				this.radius, 
				this.height, 
				this.acceleration, 
				this.maxSpeed);
			// Make sure we get assigned a point on the NavMesh
			transform.position = this.steering.GetActorPosition(this.id);
		}
	}

    public void Stop()
    {
        if (this.steering != null)
        {
            this.steering.HoldActorAtCurrentPoint(this.id);
            this.mobile = false;
        }
    }

    void Update()
    {
		if (this.steering != null)
		{
			this.currentPosition = 
				this.steering.GetActorPosition(this.id);
			this.currentVelocity = 
				this.steering.GetActorCurrentVelocity(this.id);
			this.desiredVelocity = 
				this.steering.GetActorDesiredVelocity(this.id);
            transform.position = this.currentPosition;

            float dist = (this.currentPosition - this.targetPosition).magnitude;
            if (dist < this.holdingRadius)
                this.Stop();

            if (orientate == true)
            {
                this.DetermineOrientation(dist);
                this.ApplyOrientation();
            }
		}
    }

    protected void DetermineOrientation(float distance)
    {
        if (distance < this.arrivingRadius)
        {
            this.desiredOrientation
                = Quaternion.LookRotation(this.targetPosition -
                    transform.position);
            this.orientationSpeed = this.arrivingOrientationSpeed;
        }
        else
        {
            if (this.currentVelocity.sqrMagnitude > 0.01f)
            {
                this.desiredOrientation = Quaternion.LookRotation(
                    this.currentVelocity);
                this.orientationSpeed = this.walkingOrientationSpeed;
            }
        }
    }

    protected void ApplyOrientation()
    {
        switch (this.orientationQuality)
        {
            case OrientationQuality.Low:
                transform.rotation = desiredOrientation;
                break;

            case OrientationQuality.High:
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    this.desiredOrientation, 
                    this.orientationSpeed * Time.deltaTime);
                break;
        }
    }

    public void NewTarget(Vector3 pos)
    {
        if (this.steering != null)
        {
            this.steering.ChangeActorGoal(this.id, pos);
            this.targetPosition = pos;
            this.mobile = true;
        }
    }
}
