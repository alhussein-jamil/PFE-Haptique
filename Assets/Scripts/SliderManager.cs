using UnityEngine;
using UnityEngine.UI;

public class SliderManager : MonoBehaviour
{
    public Slider Slider1;
    public Slider Slider2;
    public static float valeurSlider1;
    public static float valeurSlider2;
    void Update()
    {
        valeurSlider1 = Slider1.value;
        valeurSlider2 = Slider2.value;
    }
}
