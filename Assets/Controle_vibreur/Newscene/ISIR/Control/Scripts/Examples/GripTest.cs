using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GripTest : MonoBehaviour
{
    
    public HapticSource gripSource;
    public HapticDeviceGrip gripDevice;
    private ConstSamplesGenerator hapticGenerator;


    [Header("Visual elements")]
    public Transform centralElement;
    public Transform leftGripElement;
    public Transform rightGripElement;

    private float centralElementScale = 0.5f;
    private float leftGripElementScale = 0.5f;
    private float rightGripElementScale = 0.5f;

    private float timer = 0;
    private int state = 0;

    // Start is called before the first frame update
    void Start()
    {
        hapticGenerator = (ConstSamplesGenerator)gripSource.getHapticClip();
        if (hapticGenerator == null) {
            Debug.LogWarning("GripTest need a source with a constGenerator ");
        }
        setPosition(0.5f);
        state = 1;
        hapticGenerator.value = 0.0f;
        centralElementScale = centralElement.localScale.x / 2;
        leftGripElementScale = leftGripElement.localScale.x / 2;
        rightGripElementScale = rightGripElement.localScale.x / 2;
    }

    // Update is called once per frame
    void Update()
    {
        //TODO change the value of the hapticgenerator depending of the position 
        //faire que la position loop entre 0.25 et -0.25 avec un arret au extremité
        timer += Time.deltaTime;
        float pos = 0;
        float interTimer = 0;
        switch (state)
        {
            case 1: // wait 1s

                if (timer > state) {
                    state++;
                    hapticGenerator.value = 0.0f;
                    gripDevice.setSource(gripSource, HapticDeviceGrip.e_gripCtrl.SPEED_CTRL);
                }
                break;
            case 2: // grip 1s
                interTimer = Mathf.Clamp01(timer - 1);
                pos = Mathf.Lerp(0.5f, -0.25f, interTimer);
                hapticGenerator.value = interTimer > 0.55f ?interTimer : 0;
                setPosition(pos);

                if (timer > state) {
                    state++;
                    hapticGenerator.value = 1f;
                    gripDevice.setSource(gripSource, HapticDeviceGrip.e_gripCtrl.SPEED_CTRL);
                    setPosition(-0.25f);
                }
                break;
            case 3: // wait 1s
                
                if (timer > state) {   state++; }
                break;
            case 4: // ungrip 1s
                interTimer = Mathf.Clamp01(timer - 3);
                pos = Mathf.Lerp(-0.25f, 0.5f, interTimer);
                setPosition(pos);
                if (interTimer <= 0.55f) {
                    hapticGenerator.value = (1-interTimer);
                }
                else
                {
                    hapticGenerator.value = 0;
                }
                if (timer > state)
                {
                    state=1;
                    timer = 0;
                    setPosition(0.5f);
                    gripDevice.setSource(null);
                }
                break;
        }

    }

    void setPosition(float pos)
    {
        Vector3 posC = centralElement.position;
        float scaleC = pos < 0 ? 1 - Mathf.Abs(pos) * 2 : 1;
        centralElement.localScale = new Vector3(scaleC, 1, 1); //TODO changer le scale avant de deplacer les element
        posC.x -= pos + centralElementScale + leftGripElementScale;
        leftGripElement.position = posC;
        posC = centralElement.position;
        posC.x += pos + centralElementScale + rightGripElementScale;
        rightGripElement.position = posC;
    }
}
