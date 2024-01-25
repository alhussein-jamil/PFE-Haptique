using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartSceneOnButtonPress : MonoBehaviour
{
    public string sceneName; // Nom de la scène à redémarrer (doit correspondre exactement au nom de la scène dans la configuration Build Settings).

    // Fonction pour redémarrer la scène
    public void RestartCurrentScene()
    {
        SceneManager.LoadScene(sceneName); // Charge la scène avec le nom spécifié.
    }
}
