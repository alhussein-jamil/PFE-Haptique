using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger_Detect : MonoBehaviour
{

    private List<GameObject> listetrigs;

    public enum e_detectElement
    {
        ByTag, ByHapticDevice
    }

    [Tooltip("Test detection only on Object with the tag or look for all HapticDevice element on it")]
    public e_detectElement detectElement = e_detectElement.ByTag;
    [Tooltip("Tag of the HapticDevice GameObject")]
    public string hapticDeviceTag = "HapticDevice";

    public HapticDevice[] HapticDevices; //predefine the haptic device 

    void Start()
    {


        listetrigs = new List<GameObject>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            

            // Ajouter chaque enfant à la liste
            listetrigs.Add(child.gameObject);

        }
    }

    private void OnTriggerEnter(Collider other)
    {

      
        if (detectElement == e_detectElement.ByHapticDevice)
        {
            for (int i = 0; i < listetrigs.Count; i++)
            {

                HapticSource hs = listetrigs[i].GetComponent<HapticSource>();
                HapticDevices[i].setSource( hs );
                hs.start();

            }
        }
        else
        {
            if (other.tag == hapticDeviceTag)
            {
                for (int i = 0; i < listetrigs.Count; i++)
            {

                HapticSource hs = listetrigs[i].GetComponent<HapticSource>();
                HapticDevices[i].setSource( hs );
                hs.start();
                Debug.Log("test");

            }
            }
        }
    }

    

}
