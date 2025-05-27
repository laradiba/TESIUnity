using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using MiniJSON;


public class ApiDataFetcher : MonoBehaviour
{
    [Header("Parametri API")]
    [SerializeField] private string apiUrl = "https://api.databoom.com/v1/signals/all";
    [SerializeField] private string chartUrl = "https://api.databoom.com/v1/chart";
    [SerializeField] private string apiKey = "INSERISCI_LA_TUA_API_KEY";

    [Header("Coltura e Simulazione")]
    [SerializeField] private string deviceTokenFilter = "";
    [SerializeField] private string localFilteredFileName = "dati_coltura.json";
    [SerializeField] private SoilSimulator soilSimulator;

    void Start()
    {
        if (PlayerPrefs.HasKey("DeviceToken"))
        {
            deviceTokenFilter = PlayerPrefs.GetString("DeviceToken");
            Debug.Log($"✅ DeviceToken caricato da PlayerPrefs: {deviceTokenFilter}");
        }

        if (string.IsNullOrEmpty(localFilteredFileName))
            localFilteredFileName = deviceTokenFilter + ".json";

        StartCoroutine(FetchFilterSaveAndSimulate());
    }

    IEnumerator FetchFilterSaveAndSimulate()
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        Debug.Log("📡 Richiesta GET a /signals/all inviata...");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Errore API segnali: " + request.error);
            yield break;
        }

        string json = request.downloadHandler.text;
        Debug.Log("✅ Risposta da /signals/all ricevuta.");

        SensorData[] allData = JsonHelper.FromJson<SensorData>(json);
        if (allData == null || allData.Length == 0)
        {
            Debug.LogWarning("⚠️ Nessun dato ricevuto o parsing fallito.");
            yield break;
        }

        List<SensorData> filteredData = allData.Where(d => d.device_token == deviceTokenFilter).ToList();
        Debug.Log($"📦 Sensori filtrati: {filteredData.Count}");

        var chartRequestPayload = new ChartRequest
        {
            startDate = System.DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss+00:00"),
            endDate = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss+00:00"),
            granularity = "a",
            signals = filteredData.Select(s => s._id).ToList()
        };

        string chartPayloadJson = JsonUtility.ToJson(chartRequestPayload);
        Debug.Log("📤 Corpo POST /chart:");
        Debug.Log(chartPayloadJson);

        UnityWebRequest chartRequest = new UnityWebRequest(chartUrl, "POST");
        chartRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(chartPayloadJson));
        chartRequest.downloadHandler = new DownloadHandlerBuffer();
        chartRequest.SetRequestHeader("Content-Type", "application/json");
        chartRequest.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return chartRequest.SendWebRequest();

        if (chartRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Errore nel recupero dati da /chart: " + chartRequest.error);
            yield break;
        }

        string chartJson = chartRequest.downloadHandler.text;
        Debug.Log("✅ Risposta da /chart ricevuta.");
        Debug.Log("📥 JSON ricevuto da /chart:");
        Debug.Log(chartJson);

        var parsed = MiniJSON.Json.Deserialize(chartJson) as Dictionary<string, object>;
        if (parsed == null)
        {
            Debug.LogError("❌ Errore nel parsing JSON chart.");
            yield break;
        }

        foreach (var sensor in filteredData)
        {
            if (parsed.ContainsKey(sensor._id))
            {
                var entries = parsed[sensor._id] as List<object>;
                if (entries != null && entries.Count > 0)
                {
                    var entry = entries[0] as Dictionary<string, object>;
                    if (entry != null && entry.ContainsKey("value"))
                    {
                        sensor.valore = float.Parse(entry["value"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                        Debug.Log($"✅ {sensor.description} ({sensor.signal_token}) → {sensor.valore} {sensor.unit_readable}");
                    }
                }
                else
                {
                    Debug.LogWarning($"⚠️ Nessun dato valido per signal {sensor._id}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Nessun valore chart trovato per signal con id: {sensor._id}");
            }
        }

        string filteredJson = JsonHelper.ToJsonWithKey(filteredData.ToArray(), "sensori", true);
        string path = Path.Combine(Application.persistentDataPath, localFilteredFileName);

        File.WriteAllText(path, filteredJson);
        Debug.Log($"💾 File salvato in: {path}");

        if (soilSimulator != null)
        {
            soilSimulator.nomeFileJSON = localFilteredFileName;
            soilSimulator.AggiornaSimulazione();
        }
        else
        {
            Debug.LogWarning("⚠️ SoilSimulator non assegnato.");
        }
    }

    [System.Serializable]
    public class ChartRequest
    {
        public string startDate;
        public string endDate;
        public string granularity;
        public List<string> signals;
    }
} 
