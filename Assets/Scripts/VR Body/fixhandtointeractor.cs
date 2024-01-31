using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

public class fixhandtointeractor : MonoBehaviour
{
    public GameObject interactorHand;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // give this object and the interactor the same position and rotation
        transform.position = interactorHand.transform.position;
    }
}
