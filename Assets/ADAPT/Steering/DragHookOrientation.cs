/*using UnityEngine;
using System.Collections;

public enum DragHookQuality
{
    Low,
    High
}

public class DragHookOrientation : MonoBehaviour {
    public float dragRadius;
    public bool planar = true;
    public bool driveOrientation;
    public float driveSpeed = 0;
    public Quaternion desiredOrientation = Quaternion.identity;
    public DragHookQuality quality;

    private Vector3 lastPosition;

	IEnumerator Start () {
        lastPosition = transform.position;
        //dragPoint = transform.TransformPoint(0, 0, -dragRadius);

        for (int i = 0, n = Random.Range(0, 3); i < n; i++)
        {
            yield return null;
        }
        while (true)
        {
            yield return null;
            switch (quality)
            {
                case DragHookQuality.Low:
                    if (driveOrientation)
                    {
                        transform.rotation = desiredOrientation;
                        yield return null;
                        yield return null;
                    }
                    break;

                case DragHookQuality.High:
                    Vector3 pointArm = CalcPointArm();
                    Quaternion rotation = Quaternion.LookRotation(-pointArm);
                    if (driveOrientation)
                    {
                        rotation = Quaternion.RotateTowards(rotation, desiredOrientation, driveSpeed * Time.deltaTime);
                    }

                    transform.rotation = rotation;
                    lastPosition = transform.position;
                    break;
            }
        }
	}


    Vector3 CalcPointArm() {
        Vector3 dragPoint = lastPosition - transform.forward * dragRadius;
        Vector3 pointArm = dragPoint - transform.position;
        if (planar) pointArm.y = 0;
        pointArm = pointArm.normalized * dragRadius;
        return pointArm;
    }

    void OnDrawGizmos () {
        Vector3 pointArm = CalcPointArm();
        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.05f);
        Gizmos.DrawLine(transform.position, transform.position + pointArm);
        Gizmos.DrawSphere(transform.position + pointArm, 0.07f);

        if (driveOrientation)
        {
            Vector3 desiredPointArm = desiredOrientation * new Vector3(0, 0, -1);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + desiredPointArm);
            Gizmos.DrawSphere(transform.position + desiredPointArm, 0.06f);
        }
    }
}
*/
