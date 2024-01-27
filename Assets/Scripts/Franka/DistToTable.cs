using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistToTable : MonoBehaviour
{
    public GameObject table;
    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("updateDist", 0f, 1f );
    }
    void updateDist()
    {
        float dist = transform.position.y - table.transform.position.y;
        Debug.Log("Distance to table: " + dist);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
