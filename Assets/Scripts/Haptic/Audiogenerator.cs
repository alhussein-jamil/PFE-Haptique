using UnityEngine;
using System.Collections.Generic;

public class AudioPlayer : MonoBehaviour
{
    public AudioClip audioFile; // Assignez votre fichier audio dans l'éditeur Unity
    public float playbackSpeed = 1.0f; // Vitesse de lecture des échantillons

    private AudioSource audioSource;
    private int idx_sig = 0;
    private float[] audioSamples;

    void Start()
    {
        // Assurez-vous que vous avez assigné un AudioClip dans l'éditeur Unity
        if (audioFile == null)
        {
            Debug.LogError("Veuillez assigner un AudioClip dans l'éditeur Unity.");
            return;
        }

        // Initialiser les échantillons
        initSamples();

        // Initialiser l'AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = audioFile;

        // Jouer le fichier audio
        audioSource.Play();
    }

    void Update()
    {
        // Lire les échantillons audio
        bool sampleEnded;
        float[] audioSamples = getNextSamples(1024, out sampleEnded);

        // Faire quelque chose avec les échantillons audio (par exemple, les traiter, etc.)
        // ...

        // Exemple : Contrôle du volume en fonction du déplacement de la souris
        audioSource.volume = Mathf.Clamp01(Input.mousePosition.x / Screen.width);
    }

    // Méthode pour initialiser les échantillons
    private void initSamples()
    {
        int audioSize = audioFile.samples;
        int audioChannels = audioFile.channels;
        audioSamples = new float[audioSize * audioChannels];
        audioFile.GetData(audioSamples, 0);
    }

    // Méthode pour obtenir les échantillons suivants
    private float[] getNextSamples(int size, out bool sampleEnded)
    {
        sampleEnded = false;
        int audioSize = audioSamples.Length / audioFile.channels;
        List<float> samples = new List<float>();

        for (int i = 0; i < size; i++)
        {
            float currentValue = audioSamples[idx_sig];
            samples.Add(currentValue);

            idx_sig = (idx_sig + 1) % audioSize;
            sampleEnded = idx_sig == 0;
        }

        return samples.ToArray();
    }
}
