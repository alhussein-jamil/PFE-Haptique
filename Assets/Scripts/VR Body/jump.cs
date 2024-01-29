using System;
using UnityEngine;

public class Jump : MonoBehaviour
{
    private Rigidbody rb;
    private bool jumping = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 jumpForce = new Vector3(0.0f, 200.0f, 0.0f); // Adjust the force as needed

        if (Math.Abs(rb.velocity.x) < 0.1)
        {
            jumping = false;
        }

        // Jump when pressing the space key and not already jumping
        if (Input.GetKeyDown(KeyCode.UpArrow) && !jumping)
        {
            rb.AddForce(jumpForce);
            jumping = true;
        }
    }
}
