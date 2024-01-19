using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HapticZoneTrigger : MonoBehaviour
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

    [ContextMenu("Get Source on GameObject of child")]
    void GetSourceFromCurrentGameObject()
    {
        hapticSource = GetComponent<HapticSource>();
        if(hapticSource == null)
        {
            hapticSource = GetComponentInChildren<HapticSource>();
            if (hapticSource == null)
                Debug.LogWarning("No Haptic Source found on the Gameobject or Child");
        }
    }

    void Start()
    {
            hapticSource.start();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (detectElement == e_detectElement.ByHapticDevice)
        {
            HapticDevice[] hds = other.GetComponentsInChildren<HapticDevice>();
            if (hds.Length > 0)
            {
                foreach (HapticDevice h in hds)
                {
                    Debug.Log(string.Format("Trigger ON ({0}/{1}) by {2} - Device: {3}", transform.parent.name, name, other.name, h.name));
                    h.setSource(hapticSource);
                }
            }
        }
        else
        {
            if (other.tag == hapticDeviceTag)
            {
                Debug.Log(string.Format("Trigger ON ({0}/{1}) by {2}", transform.parent.name, name, other.name));
                HapticDevice hd = other.GetComponent<HapticDevice>();
                if (hd != null) hd.setSource(hapticSource);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (detectElement == e_detectElement.ByHapticDevice)
        {
            HapticDevice[] hds = other.GetComponentsInChildren<HapticDevice>();
            if (hds.Length > 0)
            {
                foreach (HapticDevice h in hds)
                {
                    Debug.Log(string.Format("Trigger OFF ({0}/{1}) by {2} - Device: {3}", transform.parent.name, name, other.name, h.name));
                    h.setSource(null);
                }
            }
        }
        else
        {
            if (other.tag == hapticDeviceTag)
            {
                Debug.Log(string.Format("Trigger OFF ({0}/{1}) by {2}", transform.parent.name, name, other.name));
                other.GetComponent<HapticDevice>().setSource(null);
            }
        }
    }
}
