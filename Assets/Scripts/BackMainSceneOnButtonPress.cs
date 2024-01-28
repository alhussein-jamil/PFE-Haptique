using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackMainSceneOnButtonPress : MonoBehaviour
{
    public void BackMainScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("main");
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            BackMainScene();
        }
    }

}
