using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager_Gen : MonoBehaviour
{

    private List<GameObject> listeGen;
    [Tooltip("Set the frequency of the chosen signals")]
    public string frequency;

    // Start is called before the first frame update
    void Start()
    {
        listeGen = new List<GameObject>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            

            // Ajouter chaque enfant à la liste
            listeGen.Add(child.gameObject);

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Utilisé dans MovementManager lorsque le bouton est cliqué 
    public void SetSourceBySpeed(string speed, bool mismatch)
    {

        if (!mismatch)
        {

            for (int i = 0; i < listeGen.Count; i++)
            {

            speed = speed.Replace(".", "-");
            string SignalName = "signal" + frequency + "_" + i + "_" + speed ;

            Debug.Log(SignalName);

            AudioGen AG = listeGen[i].GetComponent<AudioGen>();
            AG.LoadAudioFile(SignalName);
            
            }

        }

        else
        {
            for (int i = listeGen.Count - 1; i >= 0; i--)
            {

                speed = speed.Replace(",", "-");
                string SignalName = "signal" + frequency + "_" + (3-i) + "_" + speed ;

                AudioGen AG = listeGen[i].GetComponent<AudioGen>();
                AG.LoadAudioFile(SignalName);

            }
        }




    }
}
