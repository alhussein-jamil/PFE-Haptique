using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneRestarter : MonoBehaviour
{
    // Appeler cette méthode pour redémarrer la scène actuelle.
    public void RestartScene()
    {
        // Obtient le nom de la scène actuellement chargée et la recharge.
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}
