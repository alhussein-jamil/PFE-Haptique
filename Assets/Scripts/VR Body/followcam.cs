using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class followcam : MonoBehaviour
{
    public Transform camTransform;
    private Vector3 initialPosOffset;
    private Vector3 initialRotOffset;
    // Start is called before the first frame update
    void Start()
    {
        if (camTransform != null)
        {
            // Calculate the initial offset at the beginning
            initialPosOffset = transform.position - camTransform.position;
            initialRotOffset = transform.rotation.eulerAngles - camTransform.rotation.eulerAngles;
        }
    }

    void Update()
    {
        if (camTransform != null)
        {
            // Keep the same difference
            Vector3 newPosition = new Vector3(camTransform.position.x + initialPosOffset.x, transform.position.y+ initialPosOffset.y, camTransform.position.z + initialPosOffset.z);
            // Keep the same rotation but not along x axis
            // Vector3 newRotation = new Vector3(transform.rotation.eulerAngles.x + initialRotOffset.x , camTransform.rotation.eulerAngles.y +initialRotOffset.y, transform.rotation.eulerAngles.z + initialRotOffset.z);
            
            // Set the camera's rotation to the new calculated rotation
            // transform.rotation = Quaternion.Euler(newRotation);
            // Set the camera's position to the new calculated position
            transform.position = newPosition;
        }
    }
}
