using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestZoneTrigger : MonoBehaviour
{

    [Tooltip("Set the source that will be connect to trigged HapticDevice")]
    public HapticSource hapticSource;

    [Tooltip("Tag of the HapticDevice GameObject")]
    public string hapticDeviceTag = "HapticDevice";

    [ContextMenu("Automatic set the HapticSource from current GameObject or child")]
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
        if (other.tag == hapticDeviceTag)
        {
            Debug.Log(string.Format("Trigger ON ({0}/{1}) by {2}", transform.parent.name, name, other.name));
            HapticDevice hd = other.GetComponent<HapticDevice>();
            if (hd != null) hd.setSource(hapticSource);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == hapticDeviceTag)
        {
            Debug.Log(string.Format("Trigger OFF ({0}/{1}) by {2}", transform.parent.name, name, other.name));
            other.GetComponent<HapticDevice>().setSource(null);
        }
    }
}
