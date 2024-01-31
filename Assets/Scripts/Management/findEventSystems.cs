using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class findEventSystems : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        foreach (var eventSystem in FindObjectsOfType<UnityEngine.EventSystems.EventSystem>())
        {
            Debug.Log(eventSystem.gameObject.name);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
