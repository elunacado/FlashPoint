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
public class Goo {
    public int x;
    public int y;
    public int GooValue;
}

[System.Serializable]
public class Doors{
    public int x;
    public int y;
    public int DoorValue;
}


[System.Serializable]
public class ServerResponse
{
    public List<Wall> Walls;
    public List<POI> POIs;
    public List<Goo> Goo;
    public List<Doors> Doors;
}

public class WebClient : MonoBehaviour
{
    public GameObject horizontalWallPrefab;
    public GameObject verticalWallPrefab;
    public GameObject POIPrefab;
    public GameObject fakePoiPrefab;
    public GameObject gooPrefab;
    public GameObject exitDoorPrefab;
    public GameObject closedDoorPrefab;
    public GameObject openDoorPrefab;

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

            // Accede a la lista Walls y muestra su contenido
            foreach (Wall wall in response.Walls)
            {
                // Determinar la posición base para instanciar las paredes
                Vector3 position = new Vector3(wall.x * -1, 0, wall.y * -1);

                // Evaluar el valor de la pared y crear las instancias necesarias
                switch (wall.WallValue)
                {
                    case 15: //Arriba, Abajo, Izquierda,Derecha
                        Instantiate(horizontalWallPrefab, position + new Vector3(0, 0, 0.414f), Quaternion.identity); // Arriba
                        Instantiate(horizontalWallPrefab, position + new Vector3(-1f, 0, 0), Quaternion.identity); // Abajo
                        Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 1f), Quaternion.identity); // Izquierda
                        Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0), Quaternion.identity); // Derecha
                        break;
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
                        Instantiate(horizontalWallPrefab, position + new Vector3(-1f, 0, .2f), Quaternion.identity); // Abajo
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
                        Instantiate(horizontalWallPrefab, position + new Vector3(-1f, 0, .2f), Quaternion.identity); // Abajo
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
                // Determinar la posición base para instanciar los POIs
                Vector3 position = new Vector3((poi.x * -1) - .5f , 0, (poi.y * -1) + .5f);
                if (poi.PoiValue == 4)
                {
                    Instantiate(POIPrefab, position, Quaternion.identity);
                }
                else if (poi.PoiValue == 3)
                {
                    Instantiate(fakePoiPrefab, position, Quaternion.identity);
                }
            }
            foreach (Goo goo in response.Goo)
            {
                // Determinar la posición base para instanciar los POIs
                Vector3 position = new Vector3((goo.x * -1) - .5f , -0.408f , (goo.y * -1) + .5f);
                if (goo.GooValue == 2)
                {
                    Instantiate(gooPrefab, position, Quaternion.identity);
                }
            }
            foreach (Doors door in response.Doors)
            {
                // Determinar la posición base para instanciar los POIs
                Vector3 position = new Vector3((door.x * -1) - .5f , -0.375f , (door.y * -1) + .5f);
                if (door.DoorValue == 16) 
                {
                    Instantiate(exitDoorPrefab, position, Quaternion.identity);
                }
                else if (door.DoorValue != 0){
                    Instantiate(closedDoorPrefab, position, Quaternion.identity);
                } 
                else {
                }
            }
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error parsing JSON: {ex.Message}");
        }
    }
}