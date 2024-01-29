using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fixrightarmupper : MonoBehaviour
{
    Vector3 initialPosition;
    Vector3 initialRotation;
    // Start is called before the first frame update
    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation.eulerAngles;
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(initialPosition.x, initialPosition.y, initialPosition.z);
        transform.rotation = Quaternion.Euler(initialRotation.x, initialRotation.y, initialRotation.z);
        
    }
}


