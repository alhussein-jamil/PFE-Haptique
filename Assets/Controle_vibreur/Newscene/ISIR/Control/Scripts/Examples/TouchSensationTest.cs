using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchSensationTest : MonoBehaviour
{
    public string colliderTag = "TouchObject";
    public HapticDevice device1;
    public HapticSource hapticSource1;
    public HapticDevice device2;
    public HapticSource hapticSource2;

    private float value1 = 0;
    private float value2 = 0;
    private Vector3 positionCollision = Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == colliderTag)
        {
            positionCollision = collision.contacts[0].point;
            device1.setSource(hapticSource1);
            device2.setSource(hapticSource2);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == colliderTag)
        {
            device1.setSource(null);
            device2.setSource(null);
            positionCollision = Vector3.zero;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.tag == colliderTag)
        {
            positionCollision = collision.contacts[0].point;
            computeHapticValue(collision);
        }
    }

    private void computeHapticValue(Collision collision)
    {
        float dist1 = Vector3.Distance(device1.transform.position, collision.contacts[0].point);
        float dist2 = Vector3.Distance(device2.transform.position, collision.contacts[0].point);
        float distDevice = Vector3.Distance(device1.transform.position, device2.transform.position);
        float totalDist = dist1 + dist2;
        value1 = 0;
        value2 = 0;

        if(dist1 > distDevice && dist2 <= distDevice) {
            value2 = 1f - dist2  / distDevice;
        } else if(dist2 > distDevice && dist1 <= distDevice) {
            value1 = 1f - dist1  / distDevice; ;
        } else if (dist2 <= distDevice && dist1 <= distDevice)
        {
            value1 = 1f - dist1  / totalDist;
            value2 = 1f - dist2  / totalDist;
        }
        value1 = Mathf.Clamp01(value1);
        value2 = Mathf.Clamp01(value2);
        hapticSource1.Volume = value1;
        hapticSource2.Volume = value2;
    }


    void OnDrawGizmosSelected()
    {
        if (device1 != null && device2 != null)
        {
            // Draws a blue line from this transform to the target
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(device1.transform.position,value1 * 0.5f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(device2.transform.position, value2 * 0.5f);

            if (positionCollision != Vector3.zero)
            {
                Gizmos.DrawLine(device1.transform.position, positionCollision);
                Gizmos.DrawLine(device2.transform.position, positionCollision);
            }
        }
    }
}
