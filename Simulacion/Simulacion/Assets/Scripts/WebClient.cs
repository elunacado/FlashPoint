using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Wall
{
    public int x;
    public int y;
    public Walls walls;
}

[System.Serializable]
public class Walls
{
    public bool top;
    public bool left;
    public bool bottom;
    public bool right;
}

[System.Serializable]
public class ServerResponse
{
    public List<Wall> Walls;
   // public List<List<object>> Poi;
    //public List<List<int>> Goo;
    //public List<List<int>> Doors;
    //public List<List<int>> Entry_points;
}

public class WebClient : MonoBehaviour
{
    void Start()
    {
        // Llama al servidor al iniciar el juego
        StartCoroutine(SendRequest());
    }

    IEnumerator SendRequest()
    {
        using (UnityWebRequest www = UnityWebRequest.PostWwwForm("http://localhost:8585", ""))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("Data received successfully!");
                // Maneja la respuesta
                string responseText = www.downloadHandler.text;
                Debug.Log($"Response: {responseText}");

                // Procesar el JSON recibido
                ProcessReceivedJson(responseText);
            }
        }
    }

    void ProcessReceivedJson(string json)
    {
        try
        {
            // Deserializa el JSON recibido
            ServerResponse response = JsonUtility.FromJson<ServerResponse>(json);

            // Accede a la lista Walls y muestra su contenido
            foreach (Wall wall in response.Walls)
            {
                Debug.Log($"Wall at ({wall.x}, {wall.y}) - Top: {wall.walls.top}, Left: {wall.walls.left}, Bottom: {wall.walls.bottom}, Right: {wall.walls.right}");
            }

            // Ejemplo: Acceder a Entry_points
            //for (int i = 0; i < response.Entry_points.Count; i++)
           // {
           //     Debug.Log($"Entry_points[{i}]: ({response.Entry_points[i][0]}, {response.Entry_points[i][1]})");
           // }

            // Repite esto para Poi, Goo, y Doors si necesitas procesarlos
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error parsing JSON: {ex.Message}");
        }
    }
}