using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HapticColliderTrigger : MonoBehaviour
{
    public enum e_detectElement
    {
        ByTag, ByHapticDevice
    }

    [Tooltip("Set the source that will be connect to trigged HapticDevice")]
    public HapticSource hapticSource;
    [Tooltip("Test detection only on Object with the tag or look for all HapticDevice element on it")]
    public e_detectElement detectElement = e_detectElement.ByTag;
    [Tooltip("Tag of the HapticDevice GameObject")]
    public string hapticDeviceTag = "HapticDevice";
    [Tooltip("Weight amplitude with relative Velocity")]
    public bool weightAmplitudeWithVelocity = false;
    [Range(0, 100)]
    public float velocityWeight= 10f;


    [ContextMenu("Get Source on GameObject of child")]
    void GetSourceFromCurrentGameObject()
    {
        hapticSource = GetComponent<HapticSource>();
        if (hapticSource == null)
        {
            hapticSource = GetComponentInChildren<HapticSource>();
            if (hapticSource == null)
                Debug.LogWarning("No Haptic Source found on the Gameobject or Child");
        }
    }
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
        if(detectElement == e_detectElement.ByHapticDevice)
        {
            HapticDevice[] hds = collision.collider.GetComponentsInChildren<HapticDevice>();
            if (hds.Length > 0)
            {
                Debug.Log("Magnitude " + collision.relativeVelocity.magnitude);
                foreach (HapticDevice h in hds)
                {
                    Debug.Log(string.Format("Collision ({0}/{1}) by {2}", transform.parent.name, name, collision.collider.name));
                    if (weightAmplitudeWithVelocity)
                    {
                        hapticSource.Volume = Mathf.Clamp01(collision.relativeVelocity.magnitude * velocityWeight);
                    }
                    h.setSource(hapticSource);
                    hapticSource.start();
                }
            }
        } else
        {
            if (collision.collider.tag == hapticDeviceTag)
            {
                Debug.Log("Magnitude " + collision.relativeVelocity.magnitude);
                Debug.Log(string.Format("Collision ({0}/{1}) by {2}", transform.parent.name, name, collision.collider.name));
                HapticDevice hd = collision.collider.GetComponent<HapticDevice>();
                if (hd != null)
                {
                    if (weightAmplitudeWithVelocity)
                    {
                        hapticSource.Volume = Mathf.Clamp01(collision.relativeVelocity.magnitude * velocityWeight);
                    }
                    hd.setSource(hapticSource);
                    hapticSource.start();
                }

            }
        }

       
    }
}
