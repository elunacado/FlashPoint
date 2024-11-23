using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ServerResponse
{
    public List<List<string>> Walls;         // Cambiado a listas de listas
    public List<List<object>> Poi;          // Lista de listas de objetos
    public List<List<int>> Goo;             // Lista de listas de enteros
    public List<List<int>> Doors;           // Lista de listas de enteros
    public List<List<int>> Entry_points;    // Lista de listas de enteros
}



public class WebClient : MonoBehaviour
{
    void Update()
    {
        // Detecta si se presionó la barra espaciadora
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Simula el envío de datos al servidor
            Vector3 newPosition = new Vector3(1, 2, 3); // Puedes cambiar esto por la posición real de tu objeto
            string json = JsonUtility.ToJson(newPosition);
            StartCoroutine(SendData(json));
        }
    }

    IEnumerator SendData(string json)
    {
        using (UnityWebRequest www = new UnityWebRequest("http://localhost:8585", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("Data sent successfully!");
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
            for (int i = 0; i < response.Walls.Count; i++)
            {
                for (int j = 0; j < response.Walls[i].Count; j++)
                {
                    Debug.Log($"Walls[{i}][{j}]: {response.Walls[i][j]}");
                }
            }

            // Ejemplo: Acceder a Entry_points
            for (int i = 0; i < response.Entry_points.Count; i++)
            {
                Debug.Log($"Entry_points[{i}]: ({response.Entry_points[i][0]}, {response.Entry_points[i][1]})");
            }

            // Repite esto para Poi, Goo, y Doors si necesitas procesarlos
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error parsing JSON: {ex.Message}");
        }
    }
}