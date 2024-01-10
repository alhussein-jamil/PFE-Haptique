using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class spacer : MonoBehaviour
{
    public Transform local_transform;
    public float default_space = 66;
    // Start is called before the first frame update
    
    void Start()
    {
        local_transform = this.GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        // equally space sliders 
        int idx = 0;
        while (idx < local_transform.childCount)
        {
            local_transform.GetChild(idx).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, default_space * idx);
            idx++;
        }

        
    }
}
