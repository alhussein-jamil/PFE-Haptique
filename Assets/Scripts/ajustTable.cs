using UnityEngine;

public class TableAlignment : MonoBehaviour
{
    public Transform handTransform; 
    public GameObject table; 
    public Transform environment; 

    private Vector3 initialHandPosition;
    private Vector3 initialTablePosition;
    private Vector3 initialEnvironmentPosition;

    void Start()
    {
        initialHandPosition = handTransform.position;
        initialTablePosition = table.transform.position;
        initialEnvironmentPosition = environment.position;
    }

    void Update()
    {
        Vector3 handOffset = handTransform.position - initialHandPosition;
        table.transform.position = initialTablePosition + handOffset;

        Vector3 environmentOffset = table.transform.position - initialTablePosition;
        environment.position = initialEnvironmentPosition - environmentOffset;
    }
}
