using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class SensorData
{
    public string _id;
    public string description;
    public string chart_type;
    public string signal_token;
    public string unit_readable;
    public string device_token;
    public string device_description;
    public float valore;
}

[System.Serializable]
public class ListaSensori
{
    public List<SensorData> sensori;
}

public class CaricaDatiJSON : MonoBehaviour
{
    public string nomeFile = "franchino_asparago.json";
    public ListaSensori datiSensori;

    void Start()
    {
        string percorso = Path.Combine(Application.persistentDataPath, nomeFile);
        Debug.Log("📂 Lettura JSON da: " + percorso);

        if (File.Exists(percorso))
        {
            string json = File.ReadAllText(percorso);
            
            // ✅ Usa JsonHelper per leggere l'array "sensori"
            SensorData[] arraySensori = JsonHelper.FromJsonWithKey<SensorData>(json, "sensori");

            if (arraySensori != null && arraySensori.Length > 0)
            {
                datiSensori = new ListaSensori { sensori = new List<SensorData>(arraySensori) };
                Debug.Log($"📈 Numero di sensori caricati: {datiSensori.sensori.Count}");

                foreach (var s in datiSensori.sensori)
                {
                    Debug.Log($"🔹 {s.description} ({s.signal_token}) → {s.valore} {s.unit_readable}");
                }
            }
            else
            {
                Debug.LogError("❌ Errore: il JSON è stato caricato ma non contiene elementi validi.");
            }
        }
        else
        {
            Debug.LogError("❌ File JSON non trovato: " + percorso);
        }
    }
}
