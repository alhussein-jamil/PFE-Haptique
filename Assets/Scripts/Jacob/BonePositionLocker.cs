using UnityEngine;

public class BonePositionLocker : MonoBehaviour
{
    public Transform[] bonesToLock;
    private Vector3[] initialPositions;

    void Start()
    {
        // Initialiser le tableau des positions initiales
        initialPositions = new Vector3[bonesToLock.Length];

        // Enregistrer la position initiale de chaque os
        for (int i = 0; i < bonesToLock.Length; i++)
        {
            if (bonesToLock[i] != null)
            {
                initialPositions[i] = bonesToLock[i].position;
            }
        }
    }

    void LateUpdate()
    {
        // Réinitialiser la position de chaque os à sa position initiale
        for (int i = 0; i < bonesToLock.Length; i++)
        {
            if (bonesToLock[i] != null)
            {
                bonesToLock[i].position = initialPositions[i];
            }
        }
    }
}
