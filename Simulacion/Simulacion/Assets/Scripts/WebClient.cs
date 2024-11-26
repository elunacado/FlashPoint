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
                // Determinar la posición base para instanciar las paredes
                Vector3 position = new Vector3(wall.x * -1, 0, wall.y * -1);

                // Evaluar el valor de la pared y crear las instancias necesarias
                switch (wall.WallValue)
                {
                    case 7: // Izquierda, Abajo, Derecha
                        Instantiate(horizontalWallPrefab, position + new Vector3(-1f, 0, 0), Quaternion.identity); // Abajo
                        Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 1f), Quaternion.identity); // Izquierda
                        Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0), Quaternion.identity); // Derecha
                        break;

                    case 13: // Izquierda, Arriba, Derecha
                        Instantiate(horizontalWallPrefab, position + new Vector3(0, 0, 0.414f), Quaternion.identity); // Arriba
                        Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 1f), Quaternion.identity); // Izquierda
                        Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0), Quaternion.identity); // Derecha
                        break;

                    case 5: // Izquierda, Derecha
                        Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 1f), Quaternion.identity); // Izquierda
                        Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0), Quaternion.identity); // Derecha
                        break;

                    case 10: // Arriba, Abajo
                        Instantiate(horizontalWallPrefab, position + new Vector3(0, 0, 0.414f), Quaternion.identity); // Arriba
                        Instantiate(horizontalWallPrefab, position + new Vector3(-1f, 0, 0), Quaternion.identity); // Abajo
                        break;

                    case 6: // Izquierda, Abajo
                        Instantiate(horizontalWallPrefab, position + new Vector3(-1f, 0, .3f), Quaternion.identity); // Abajo
                        Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 1f), Quaternion.identity); // Izquierda
                        break;

                    case 3: // Abajo, Derecha
                        Instantiate(horizontalWallPrefab, position + new Vector3(-1f, 0, 0), Quaternion.identity); // Abajo
                        Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0), Quaternion.identity); // Derecha
                        break;

                    case 12: // Arriba, Izquierda
                        Instantiate(horizontalWallPrefab, position + new Vector3(0, 0, 0.414f), Quaternion.identity); // Arriba
                        Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 1f), Quaternion.identity); // Izquierda
                        break;

                    case 9: // Arriba, Derecha
                        Instantiate(horizontalWallPrefab, position + new Vector3(0, 0, 0.414f), Quaternion.identity); // Arriba
                        Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0), Quaternion.identity); // Derecha
                        break;

                    case 8: // Arriba
                        Instantiate(horizontalWallPrefab, position + new Vector3(0, 0, 0.414f), Quaternion.identity); // Arriba
                        break;

                    case 1: // Derecha
                        Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0), Quaternion.identity); // Derecha
                        break;

                    case 2: // Abajo
                        Instantiate(horizontalWallPrefab, position + new Vector3(-1f, 0, 0), Quaternion.identity); // Abajo
                        break;

                    case 4: // Izquierda
                        Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 1f), Quaternion.identity); // Izquierda
                        break;

                    default: // No hay pared
                        break;
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