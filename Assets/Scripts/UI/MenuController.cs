using UnityEngine;
using UnityEngine.SceneManagement;
using Franka;

public class MenuController : MonoBehaviour
{
    public string sceneName = "RobotScene";
    public void PlayGame()
    {
        
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
void Update()
    {
        if(Input.GetKeyDown(KeyCode.S))
        {
            PlayGame();        } 
 
    }
}