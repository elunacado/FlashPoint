using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class WebClient : MonoBehaviour
{
    IEnumerator SendData()
    {
        string url = "http://localhost:8585";

        using (UnityWebRequest www = UnityWebRequest.Post(url, ""))
        {
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest(); // Talk to Python server

            if (www.result == UnityWebRequest.Result.ConnectionError || 
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + www.error);
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("Response from server: " + jsonResponse);

                // Deserialize JSON response into SimulationData
                SimulationData data = JsonUtility.FromJson<SimulationData>(jsonResponse);

                // Process the data (e.g., render grids)
                ProcessGridData(data);
            }
        }
    }

    void ProcessGridData(SimulationData data)
    {
        Debug.Log("Doors and Entries Grid:");
        foreach (var row in data.doors_entries)
        {
            Debug.Log(string.Join(", ", row));
        }

        Debug.Log("Walls Grid:");
        foreach (var row in data.walls)
        {
            Debug.Log(string.Join(", ", row));
        }

        Debug.Log("POI Grid:");
        foreach (var row in data.poi)
        {
            Debug.Log(string.Join(", ", row));
        }

        Debug.Log("Threat Markers Grid:");
        foreach (var row in data.threat_markers)
        {
            Debug.Log(string.Join(", ", row));
        }

        Debug.Log("Agents Grid:");
        foreach (var row in data.agents)
        {
            Debug.Log(string.Join(", ", row));
        }

        // Add your visualization logic here
    }

    void Start()
    {
        StartCoroutine(SendData());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Trigger updates with spacebar
        {
            StartCoroutine(SendData());
        }
    }
}

[System.Serializable]
public class SimulationData
{
    public int[][] doors_entries;
    public int[][] walls;
    public int[][] poi;
    public int[][] threat_markers;
    public int[][] agents;
    public int steps;
    public int saved_victims;
    public int lost_victims;
    public bool collapsed_building;
}