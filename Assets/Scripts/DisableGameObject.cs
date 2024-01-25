using UnityEngine;
using UnityEngine.UI;

public class DisableGameObject : MonoBehaviour
{
    public GameObject objectToDisable; // Référence au GameObject que vous souhaitez désactiver.

    public void DisableObject()
    {
        if (objectToDisable != null)
        {
            objectToDisable.SetActive(false); // Désactive le GameObject.
        }
    }
}
