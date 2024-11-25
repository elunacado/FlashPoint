using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Wall
{
    public int x;
    public int y;
    public int WallValue;
}

[System.Serializable]
public class POI {
    public int x;
    public int y;
    public int PoiValue;
} 



[System.Serializable]
public class ServerResponse
{
    public List<Wall> Walls;
    public List<POI> POIs;
}

public class WebClient : MonoBehaviour
{
    public GameObject horizontalWallPrefab;
    public GameObject verticalWallPrefab;

    public GameObject POIPrefab;

    public GameObject fakePoiPrefab;

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
                Debug.Log("Error en la solicitud: " + www.error);
            }
            else
            {
                // Maneja la respuesta
                string responseText = www.downloadHandler.text;
                Debug.Log($"Response: {responseText}");

                // Procesar el JSON recibido
                ProcessReceivedJson(responseText);
            }
        }
    }

    void ProcessReceivedJson(string json){
        try
        {
            // Deserializa el JSON recibido
            ServerResponse response = JsonUtility.FromJson<ServerResponse>(json);
            Debug.Log("JSON deserialized successfully!");

            // Accede a la lista Walls y muestra su contenido
            foreach (Wall wall in response.Walls)
            {
                // Instanciar prefabs dependiendo de la información de la pared
                Vector3 position = new Vector3(wall.x * -1, 0, wall.y * -1);
                if (wall.WallValue == 7)
                {
                    //WORKS
                    Instantiate(horizontalWallPrefab, position + new Vector3(-.894f, 0, 0), Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0.5f), Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, -0.5f), Quaternion.identity);
                }
                else if (wall.WallValue == 13)
                {
                    Instantiate(horizontalWallPrefab, position, Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0.5f), Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, -0.5f), Quaternion.identity);
                }
                else if (wall.WallValue == 5)
                {
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0.5f), Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, -0.5f), Quaternion.identity);
                }
                else if (wall.WallValue == 10)
                {
                    Instantiate(horizontalWallPrefab, position, Quaternion.identity);
                    Instantiate(horizontalWallPrefab, position + new Vector3(-0.9f, 0, 0), Quaternion.identity);
                }
                else if (wall.WallValue == 6)
                {
                    Instantiate(horizontalWallPrefab, position + new Vector3(-0.9f, 0, 0), Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0.5f), Quaternion.identity);
                }
                else if (wall.WallValue == 3)
                {
                    Instantiate(horizontalWallPrefab, position + new Vector3(-.9f, 0, 0), Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, -0.5f), Quaternion.identity);
                }
                else if (wall.WallValue == 12)
                {
                    Instantiate(horizontalWallPrefab, position, Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0.5f), Quaternion.identity);
                }
                else if (wall.WallValue == 9)
                {
                    Instantiate(horizontalWallPrefab, position, Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, -0.5f), Quaternion.identity);
                }
                else if (wall.WallValue == 8)
                {
                    Instantiate(horizontalWallPrefab, position, Quaternion.identity);
                }
                else if (wall.WallValue == 1)
                {
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, -0.5f), Quaternion.identity);
                }
                else if (wall.WallValue == 2)
                {
                    Instantiate(horizontalWallPrefab, position + new Vector3(-.9f, 0, 0), Quaternion.identity);
                }
                else if (wall.WallValue == 4)
                {
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0.5f), Quaternion.identity);
                }

                else
                {
                    Debug.Log("No walls");
                }
            }
            // Accede a la lista POIs y muestra su contenido
            foreach (POI poi in response.POIs)
            {
                Vector3 position = new Vector3(poi.x * -1, 0, poi.y * -1);
                if (poi.PoiValue == 4)
                {
                    Debug.Log("POI");
                    Instantiate(POIPrefab, position, Quaternion.identity);
                }
                else if (poi.PoiValue == 3)
                {
                    Debug.Log("FAKE POI");
                    Instantiate(fakePoiPrefab, position, Quaternion.identity);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error parsing JSON: {ex.Message}");
        }
    }
}