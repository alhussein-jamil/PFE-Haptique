using UnityEngine;

public class MoveObjectOnButtonPress : MonoBehaviour
{
    public GameObject objectToMove; // L'objet à déplacer
    public Vector3 newPosition; // La nouvelle position de l'objet

    // Méthode appelée lorsque le bouton est pressé
    public void MoveObject()
    {
        objectToMove.transform.position = newPosition;
    }
}
