using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

using Newtonsoft.Json;

public class WebClient : MonoBehaviour
{
    // URL del servidor Python
    private string serverUrl = "http://localhost:8585";

    // Referencias de prefabs (asignadas desde el Inspector)
    public GameObject verticalWallPrefab;
    public GameObject horizontalWallPrefab;
    public GameObject horizontalDoor;
    public GameObject verticalDoor;
    public GameObject poiPrefab;

    [System.Serializable]
    public class SimulationRequest
    {
        public int step; // Paso solicitado
    }

    [System.Serializable]
    public class SimulationResponse
    {
        public List<int[]> grid_doors_entries;
        public List<int[]> grid_walls;
        public List<float[]> grid_poi;
        public List<float[]> grid_threat_markers;
        public List<float[]> grid_agents;
        public Dictionary<string, string> wall_states;
        public Dictionary<string, string> door_states;
        public bool collapsed_building;
        public int saved_victims;
        public int lost_victims;

    }

    // Coroutine para enviar solicitudes POST
    IEnumerator SendRequest(int step)
    {
        SimulationRequest request = new SimulationRequest { step = step };
        string jsonData = JsonUtility.ToJson(request);

        using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error en la solicitud: {www.error}");
            }
            else
            {
                Debug.Log($"Respuesta del servidor: {www.downloadHandler.text}");

                try
                {
                    SimulationResponse response = JsonConvert.DeserializeObject<SimulationResponse>(www.downloadHandler.text);

                    if (response.wall_states == null || response.wall_states.Count == 0)
                    {
                        Debug.LogError("Error: wall_states es nulo o está vacío después de deserialización.");
                    }
                    else
                    {
                        Debug.Log($"wall_states contiene {response.wall_states.Count} entradas.");
                        UpdateScene(response);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error al deserializar la respuesta: {ex.Message}\nJSON: {www.downloadHandler.text}");
                }
            }
        }
    }


    void UpdateScene(SimulationResponse response)
    {
        // Validar que los datos necesarios no sean nulos ni estén vacíos
        if (response.grid_poi == null || response.grid_poi.Count == 0)
        {
            Debug.LogError("Error: grid_poi es nulo o está vacío.");
            return;
        }
        if (response.wall_states == null || response.wall_states.Count == 0)
        {
            Debug.LogError("Error: wall_states es nulo o está vacío.");
            return;
        }
        if (response.door_states == null || response.door_states.Count == 0)
        {
            Debug.LogError("Error: door_states es nulo o está vacío.");
            return;
        }

        // Definir el tamaño de las celdas en función del plano
        float cellWidth = 10.0f;
        float cellHeight = 10.0f;

        // Procesar las paredes
        ProcessWalls(response.wall_states, cellWidth, cellHeight);

        // Procesar las puertas
        ProcessDoors(response.door_states, cellWidth, cellHeight);

        // Procesar los POIs
        ProcessPois(response.grid_poi, cellWidth, cellHeight);
    }

    // Procesar las paredes
    void ProcessWalls(Dictionary<string, string> wallStates, float cellWidth, float cellHeight)
    {
        foreach (var wall in wallStates)
        {
            string key = wall.Key;
            string state = wall.Value;

            try
            {
                // Parsear coordenadas
                string[] coordinates = key.Trim('(', ')').Split(new[] { "), (" }, System.StringSplitOptions.None);
                string[] coord1 = coordinates[0].Split(',');
                string[] coord2 = coordinates[1].Split(',');

                int x1 = int.Parse(coord1[0].Trim());
                int y1 = int.Parse(coord1[1].Trim());
                int x2 = int.Parse(coord2[0].Trim());
                int y2 = int.Parse(coord2[1].Trim());

                // Transformar coordenadas
                (x1, y1) = (y1, x1);
                (x2, y2) = (y2, x2);

                // Calcular la posición de la pared
                if (x1 == x2)
                {
                    float centerX = x1 * cellWidth;
                    float centerZ = ((y1 + y2) / 2.0f) * -cellHeight;
                    Vector3 position = new Vector3(centerX, 0, centerZ);

                    Instantiate(horizontalWallPrefab, position, Quaternion.identity);
                    Debug.Log($"Instanciando pared horizontal entre ({x1}, {y1}) y ({x2}, {y2}), Estado: {state}");
                }
                else if (y1 == y2)
                {
                    float centerX = ((x1 + x2) / 2.0f) * cellWidth;
                    float centerZ = y1 * -cellHeight;
                    Vector3 position = new Vector3(centerX, 0, centerZ);

                    Instantiate(verticalWallPrefab, position, Quaternion.identity);
                    Debug.Log($"Instanciando pared vertical entre ({x1}, {y1}) y ({x2}, {y2}), Estado: {state}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error al procesar la pared: {key}. Detalles: {ex.Message}");
            }
        }
    }

    // Procesar las puertas
    void ProcessDoors(Dictionary<string, string> doorStates, float cellWidth, float cellHeight)
    {
        foreach (var door in doorStates)
        {
            string key = door.Key;
            string state = door.Value;

            try
            {
                // Parsear coordenadas
                string[] coordinates = key.Trim('(', ')').Split(new[] { "), (" }, System.StringSplitOptions.None);
                string[] coord1 = coordinates[0].Split(',');
                string[] coord2 = coordinates[1].Split(',');

                int x1 = int.Parse(coord1[0].Trim());
                int y1 = int.Parse(coord1[1].Trim());
                int x2 = int.Parse(coord2[0].Trim());
                int y2 = int.Parse(coord2[1].Trim());

                // Transformar coordenadas
                (x1, y1) = (y1, x1);
                (x2, y2) = (y2, x2);

                // Calcular la posición de la puerta
                if (x1 == x2)
                {
                    float centerX = x1 * cellWidth;
                    float centerZ = ((y1 + y2) / 2.0f) * -cellHeight;
                    Vector3 position = new Vector3(centerX, 0, centerZ);

                    Instantiate(horizontalDoor, position, Quaternion.identity);
                    Debug.Log($"Instanciando puerta horizontal entre ({x1}, {y1}) y ({x2}, {y2}), Estado: {state}");
                }
                else if (y1 == y2)
                {
                    float centerX = ((x1 + x2) / 2.0f) * cellWidth;
                    float centerZ = y1 * -cellHeight;
                    Vector3 position = new Vector3(centerX, 0, centerZ);

                    Instantiate(verticalDoor, position, Quaternion.identity);
                    Debug.Log($"Instanciando puerta vertical entre ({x1}, {y1}) y ({x2}, {y2}), Estado: {state}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error al procesar la puerta: {key}. Detalles: {ex.Message}");
            }
        }
    }

    // Procesar los POIs
    void ProcessPois(List<float[]> gridPoi, float cellWidth, float cellHeight)
    {
        for (int row = 0; row < gridPoi.Count; row++)
        {
            if (gridPoi[row] == null || gridPoi[row].Length == 0)
            {
                Debug.LogWarning($"Fila {row} de grid_poi está vacía o es nula.");
                continue;
            }

            for (int col = 0; col < gridPoi[row].Length; col++)
            {
                float poiValue = gridPoi[row][col];
                if (poiValue > 0)
                {
                    float xPosition = col * cellWidth;
                    float zPosition = -row * cellHeight;

                    Vector3 poiPosition = new Vector3(xPosition, 0, zPosition);
                    GameObject poiInstance = Instantiate(poiPrefab, poiPosition, Quaternion.identity);

                    poiInstance.transform.localScale *= poiValue / 4.0f;
                    poiInstance.GetComponent<Renderer>().material.color = Color.Lerp(Color.yellow, Color.red, poiValue / 4.0f);

                    Debug.Log($"Instanciando POI en posición ({col}, {row}) transformada a Unity ({poiPosition}), valor: {poiValue}");
                }
            }
        }
    }




    // Método Start
    void Start()
    {
        StartCoroutine(SendRequest(1)); // Solicitar datos para el paso 1
    }
}
