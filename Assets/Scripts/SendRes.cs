using UnityEngine;
using System.IO;

public class DataExporter : MonoBehaviour
{
    private string cheminFichierCSV;

    void Start()
    {
        // Définir le chemin du fichier CSV dans le répertoire racine du projet Unity
        cheminFichierCSV = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "donneesSlider.csv");

        // Créer un nouveau fichier CSV ou effacer l'ancien au démarrage
        StreamWriter writer = new StreamWriter(cheminFichierCSV, false);
        writer.WriteLine("ValeurSlider"); // En-tête du fichier CSV
        writer.Close();
    }

    public void EnregistrerDansCSV()
    {
        float valeurActuelle1 = SliderManager.valeurSlider1;
        float valeurActuelle2 = SliderManager.valeurSlider2;
        StreamWriter writer = new StreamWriter(cheminFichierCSV, true);
        writer.WriteLine(valeurActuelle1);
        writer.WriteLine(valeurActuelle2);
        writer.Close();
    }
}
