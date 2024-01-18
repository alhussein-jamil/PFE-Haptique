using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class InterfaceTransition : MonoBehaviour
{

    public Button Button1;
    public Slider Slider1_brush;
    public Slider Slider2_brush;
    public Button Send1;
    public Button Return;
    
    private bool PutButton1 = false;
    private bool Send1_result = false;

    public Button Button2;
    public Slider Slider1_arrow;
    public Slider Slider2_arrow;
    public Button Send2;

    private bool PutButton2 = false;
    private bool Send2_result = false;


    string filePath_result = "Assets/Results/SliderValue.txt";

    // method that is played by the button on the Unity scene. It starts the screen transition
    
    public void Start()
    {
        Button1.gameObject.SetActive(true);
        Button2.gameObject.SetActive(false);
        Send1.gameObject.SetActive(false);
        Send2.gameObject.SetActive(false);
        Slider1_arrow.gameObject.SetActive(false);
        Slider2_arrow.gameObject.SetActive(false);
        Slider1_brush.gameObject.SetActive(false);
        Slider2_brush.gameObject.SetActive(false);








    }
    
    
    public void StartButton1()
    {
        PutButton1 = true;
    }

    public void StartSend1Result()
    {
        Send1_result = true;
    }

    public void StartButton2()
    {
        PutButton2 = true;
    }

    public void StartSend2Result()
    {
        Send2_result = true;
    }


    private void Update()
    {
        // if the button on the Unity screen is clicked then show slider

        if (PutButton1)
        {
            Button1.gameObject.SetActive(false);
            Slider1_brush.gameObject.SetActive(true);
            Slider2_brush.gameObject.SetActive(true);

            Send1.gameObject.SetActive(true);

        }

        if (Send1_result)
        {

            // Écrire la valeur dans le fichier
            File.AppendAllText(filePath_result, Slider1_brush.value.ToString() + "\n");
            File.AppendAllText(filePath_result, Slider2_brush.value.ToString() + "\n");

            
            Slider1_brush.gameObject.SetActive(false);
            Slider2_brush.gameObject.SetActive(false);
            
            Send1.gameObject.SetActive(false);
            Button2.gameObject.SetActive(true);

        }

            if (PutButton2)
        {
            Button2.gameObject.SetActive(false);
            Slider1_arrow.gameObject.SetActive(true);
            Slider2_arrow.gameObject.SetActive(true);

            Send2.gameObject.SetActive(true);
           
        }

        if (Send2_result)
        {

            // Écrire la valeur dans le fichier
            File.AppendAllText(filePath_result, Slider1_arrow.value.ToString() + "\n");
            File.AppendAllText(filePath_result, Slider2_arrow.value.ToString() + "\n");

            Slider1_arrow.gameObject.SetActive(false);
            Slider2_arrow.gameObject.SetActive(false);

            Send2.gameObject.SetActive(false);
        }
        }

    }



