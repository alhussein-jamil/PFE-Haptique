using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartSceneOnButtonPress : MonoBehaviour
{

    // Fonction pour redémarrer la scène
    public void RestartCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name; // Obtient le nom de la scène actuellement chargée.
        SceneManager.LoadScene(sceneName); // Charge la scène avec le nom spécifié.
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartCurrentScene();
        }
    }
}
