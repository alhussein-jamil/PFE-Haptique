using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class nullrot : MonoBehaviour
{
    Vector3 initialRotation;
    // Start is called before the first frame update
    void Start()
    {
        initialRotation = transform.rotation.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        //always set x and z rotation to initial value
        transform.rotation = Quaternion.Euler(initialRotation.x, transform.rotation.eulerAngles.y, initialRotation.z);

    }
}
