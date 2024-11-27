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
public class POI
{
    public int x;
    public int y;
    public int PoiValue;
}

[System.Serializable]
public class Goo
{
    public int x;
    public int y;
    public int GooValue;
}

[System.Serializable]
public class Doors
{
    public int x;
    public int y;
    public int DoorValue;
}

[System.Serializable]
public class Agents
{
    public int x;
    public int y;
    public int AgentValue;
}

[System.Serializable]
public class ServerResponse
{
    public List<Wall> Walls;
    public List<POI> POIs;
    public List<Goo> Goo;
    public List<Doors> Doors;
    public List<Agents> Agents;
    public int StepCount;
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
    public GameObject lootbugAgentPrefab;
    public GameObject employeeAgentPrefab;

    private List<GameObject> instantiatedObjects = new List<GameObject>();

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
                Debug.Log("JSON recibido: " + responseText); // Imprimir el JSON recibido

                // Procesar el JSON recibido
                ProcessReceivedJson(responseText);
            }
        }
    }

    void ProcessReceivedJson(string json)
    {
        try
        {
            Debug.Log("Procesando JSON recibido");
            // Deserializa el JSON recibido
            ServerResponse response = JsonUtility.FromJson<ServerResponse>(json);
            Debug.Log("JSON deserializado correctamente");

            // Destruir los objetos instanciados previamente
            foreach (GameObject obj in instantiatedObjects)
            {
                Destroy(obj);
                //Debug.Log("Objeto destruido");
            }
            instantiatedObjects.Clear();
            
            Debug.Log("Nuevo paso creado");
            // Accede a la lista Walls y muestra su contenido
            foreach (Wall wall in response.Walls)
            {
                // Determinar la posición base para instanciar las paredes
                Vector3 position = new Vector3(wall.x * -1, 0, wall.y * -1);

                // Evaluar el valor de la pared y crear las instancias necesarias
                GameObject obj = null;
                switch (wall.WallValue)
                {
                    case 15: // Arriba, Abajo, Izquierda, Derecha
                        obj = Instantiate(horizontalWallPrefab, position + new Vector3(0, 0, 0.414f), Quaternion.identity); // Arriba
                        instantiatedObjects.Add(obj);
                        obj = Instantiate(horizontalWallPrefab, position + new Vector3(-1f, 0, 0), Quaternion.identity); // Abajo
                        instantiatedObjects.Add(obj);
                        obj = Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 1f), Quaternion.identity); // Izquierda
                        instantiatedObjects.Add(obj);
                        obj = Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0), Quaternion.identity); // Derecha
                        instantiatedObjects.Add(obj);
                        break;
                    case 7: // Izquierda, Abajo, Derecha
                        obj = Instantiate(horizontalWallPrefab, position + new Vector3(-1f, 0, 0), Quaternion.identity); // Abajo
                        instantiatedObjects.Add(obj);
                        obj = Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 1f), Quaternion.identity); // Izquierda
                        instantiatedObjects.Add(obj);
                        obj = Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0), Quaternion.identity); // Derecha
                        instantiatedObjects.Add(obj);
                        break;

                    case 13: // Izquierda, Arriba, Derecha
                        obj = Instantiate(horizontalWallPrefab, position + new Vector3(0, 0, 0.414f), Quaternion.identity); // Arriba
                        instantiatedObjects.Add(obj);
                        obj = Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 1f), Quaternion.identity); // Izquierda
                        instantiatedObjects.Add(obj);
                        obj = Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0), Quaternion.identity); // Derecha
                        instantiatedObjects.Add(obj);
                        break;

                    case 5: // Izquierda, Derecha
                        obj = Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 1f), Quaternion.identity); // Izquierda
                        instantiatedObjects.Add(obj);
                        obj = Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0), Quaternion.identity); // Derecha
                        instantiatedObjects.Add(obj);
                        break;

                    case 10: // Arriba, Abajo
                        obj = Instantiate(horizontalWallPrefab, position + new Vector3(0, 0, 0.414f), Quaternion.identity); // Arriba
                        instantiatedObjects.Add(obj);
                        obj = Instantiate(horizontalWallPrefab, position + new Vector3(-1f, 0, 0), Quaternion.identity); // Abajo
                        instantiatedObjects.Add(obj);
                        break;

                    case 6: // Izquierda, Abajo
                        obj = Instantiate(horizontalWallPrefab, position + new Vector3(-1f, 0, .3f), Quaternion.identity); // Abajo
                        instantiatedObjects.Add(obj);
                        obj = Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 1f), Quaternion.identity); // Izquierda
                        instantiatedObjects.Add(obj);
                        break;

                    case 3: // Abajo, Derecha
                        obj = Instantiate(horizontalWallPrefab, position + new Vector3(-1f, 0, .2f), Quaternion.identity); // Abajo
                        instantiatedObjects.Add(obj);
                        obj = Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0), Quaternion.identity); // Derecha
                        instantiatedObjects.Add(obj);
                        break;

                    case 12: // Arriba, Izquierda
                        obj = Instantiate(horizontalWallPrefab, position + new Vector3(0, 0, 0.414f), Quaternion.identity); // Arriba
                        instantiatedObjects.Add(obj);
                        obj = Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 1f), Quaternion.identity); // Izquierda
                        instantiatedObjects.Add(obj);
                        break;

                    case 9: // Arriba, Derecha
                        obj = Instantiate(horizontalWallPrefab, position + new Vector3(0, 0, 0.414f), Quaternion.identity); // Arriba
                        instantiatedObjects.Add(obj);
                        obj = Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0), Quaternion.identity); // Derecha
                        instantiatedObjects.Add(obj);
                        break;

                    case 8: // Arriba
                        obj = Instantiate(horizontalWallPrefab, position + new Vector3(0, 0, 0.414f), Quaternion.identity); // Arriba
                        instantiatedObjects.Add(obj);
                        break;

                    case 1: // Derecha
                        obj = Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0), Quaternion.identity); // Derecha
                        instantiatedObjects.Add(obj);
                        break;

                    case 2: // Abajo
                        obj = Instantiate(horizontalWallPrefab, position + new Vector3(-1f, 0, .2f), Quaternion.identity); // Abajo
                        instantiatedObjects.Add(obj);
                        break;

                    case 4: // Izquierda
                        obj = Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 1f), Quaternion.identity); // Izquierda
                        instantiatedObjects.Add(obj);
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
                GameObject obj = null;
                if (poi.PoiValue == 4)
                {
                    obj = Instantiate(POIPrefab, position, Quaternion.identity);
                    instantiatedObjects.Add(obj);
                }
                else if (poi.PoiValue == 3)
                {
                    obj = Instantiate(fakePoiPrefab, position, Quaternion.identity);
                    instantiatedObjects.Add(obj);
                }
            }
            foreach (Goo goo in response.Goo)
            {
                // Determinar la posición base para instanciar los Goo
                Vector3 position = new Vector3((goo.x * -1) - .5f , -0.408f , (goo.y * -1) + .5f);
                GameObject obj = null;
                if (goo.GooValue == 2)
                {
                    obj = Instantiate(gooPrefab, position, Quaternion.identity);
                    instantiatedObjects.Add(obj);
                }
            }
            foreach (Doors door in response.Doors)
            {
                // Determinar la posición base para instanciar las puertas
                Vector3 position = new Vector3((door.x * -1) - .5f , -0.375f , (door.y * -1) + .5f);
                GameObject obj = null;
                if (door.DoorValue == 16) 
                {
                    obj = Instantiate(exitDoorPrefab, position, Quaternion.identity);
                    instantiatedObjects.Add(obj);
                }
                else if (door.DoorValue != 0){
                    obj = Instantiate(closedDoorPrefab, position, Quaternion.identity);
                    instantiatedObjects.Add(obj);
                } 
            }
            foreach (Agents agent in response.Agents)
            {
                // Determinar la posición base para instanciar los agentes
                Vector3 position = new Vector3((agent.x * -1) - .5f , 0, (agent.y * -1) + .5f);
                GameObject obj = null;
                if (agent.AgentValue == 7){
                    obj = Instantiate(lootbugAgentPrefab, position, Quaternion.identity);
                    instantiatedObjects.Add(obj);
                }
                else if (agent.AgentValue == 6){
                    obj = Instantiate(employeeAgentPrefab, position, Quaternion.identity);
                    instantiatedObjects.Add(obj);
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