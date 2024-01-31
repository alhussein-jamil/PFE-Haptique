using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class follow_transform : MonoBehaviour
{
    public Transform target;
    // Start is called before the first frame update
    void Start()
    {
        // apply transform

    }

    // Update is called once per frame
    void Update()
    {
                transform.position = target.position;
    }
}
