using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class InterfaceTransition : MonoBehaviour
{

    public Button Button1;
    public Slider Slider1;
    public Button Send;
    private bool PutButton1 = false;
    private bool Send_result = false;

    // method that is played by the button on the Unity scene. It starts the screen transition
    public void StartButton1()
    {
        PutButton1 = true;
    }

    public void StartSendResult()
    {
        Send_result = true;
    }


    private void Update()
    {
        // if the button on the Unity screen is clicked then show slider

        if (PutButton1)
        {
            Button1.gameObject.SetActive(false);
            Slider1.gameObject.SetActive(true);
            Send.gameObject.SetActive(true);
        }

        if (Send_result)
        {
            float sliderValue = Slider1.value;

            // Chemin du fichier où enregistrer la valeur
            string filePath = "Assets/Results/SliderValue.txt";

            // Écrire la valeur dans le fichier
            File.WriteAllText(filePath, sliderValue.ToString());
            
            Slider1.gameObject.SetActive(false);
            Send.gameObject.SetActive(false);
        }

    }

}

