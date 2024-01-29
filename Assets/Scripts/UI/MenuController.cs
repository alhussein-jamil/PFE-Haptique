using UnityEngine;
using UnityEngine.SceneManagement;
using Franka;

public class MenuController : MonoBehaviour
{
    public void PlayGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("RobotScene");
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