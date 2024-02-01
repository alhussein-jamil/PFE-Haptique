using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class brush_velocity : MonoBehaviour
{
    public float frequency = 10f; // 1 Hz
    private Vector3 latestPosition;    
    // Start is called before the first frame update
    void Start()
    {
        latestPosition = gameObject.transform.position;
        InvokeRepeating("DisplayBrushVelocity", 0f, 1f / frequency);
    }

    private void DisplayBrushVelocity()
    {
        Vector3 velocity = (gameObject.transform.position - latestPosition) * frequency;
        latestPosition = gameObject.transform.position;
        Debug.Log("Brush velocity: " + velocity);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
