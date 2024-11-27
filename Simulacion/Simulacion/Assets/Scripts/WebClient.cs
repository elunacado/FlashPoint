using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

using Newtonsoft.Json;
using TMPro;

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
    public GameObject agentPrefab;
    public GameObject dropletsPrefab;
    public GameObject gooPrefab;

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
        if (response.grid_agents == null || response.grid_agents.Count == 0)
        {
            Debug.LogError("Error: grid_agents es nulo o está vacío.");
            return;
        }
        // Definir el tamaño de las celdas en función del plano
        float cellWidth = 10.0f;
        float cellHeight = 10.0f;

        // Procesar las paredes, puertas, POIs, agentes y threat markers
        ProcessWalls(response.wall_states, cellWidth, cellHeight);
        ProcessDoors(response.door_states, cellWidth, cellHeight);
        ProcessPois(response.grid_poi, cellWidth, cellHeight);
        ProcessAgents(response.grid_agents, cellWidth, cellHeight);
        ProcessThreatMarkers(response.grid_threat_markers, cellWidth, cellHeight);
        ProcessTexts(response.saved_victims, response.lost_victims, response.collapsed_building);
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

    // Procesar los agentes
    void ProcessAgents(List<float[]> gridAgents, float cellWidth, float cellHeight)
    {
        for (int row = 0; row < gridAgents.Count; row++)
        {
            if (gridAgents[row] == null || gridAgents[row].Length == 0)
            {
                Debug.LogWarning($"Fila {row} de grid_agents está vacía o es nula.");
                continue;
            }

            for (int col = 0; col < gridAgents[row].Length; col++)
            {
                float agentValue = gridAgents[row][col];
                if (agentValue > 0)
                {
                    // Transformar las coordenadas
                    float xPosition = col * cellWidth;
                    float zPosition = -row * cellHeight;

                    // Crear posición en Unity
                    Vector3 agentPosition = new Vector3(xPosition, 0, zPosition);

                    // Instanciar el agente
                    GameObject agentInstance = Instantiate(agentPrefab, agentPosition, Quaternion.identity);

                    // Opción: Escalar o personalizar el agente en función de su valor
                    agentInstance.transform.localScale *= agentValue / 7.0f; // Escalar según el valor
                    agentInstance.GetComponent<Renderer>().material.color = Color.Lerp(Color.blue, Color.green, agentValue / 7.0f);

                    Debug.Log($"Instanciando agente en posición ({col}, {row}) transformada a Unity ({agentPosition}), valor: {agentValue}");
                }
            }
        }
    }

    // Procesar los Threat Markers
    void ProcessThreatMarkers(List<float[]> gridThreatMarkers, float cellWidth, float cellHeight)
    {
        for (int row = 0; row < gridThreatMarkers.Count; row++)
        {
            if (gridThreatMarkers[row] == null || gridThreatMarkers[row].Length == 0)
            {
                Debug.LogWarning($"Fila {row} de grid_threat_markers está vacía o es nula.");
                continue;
            }

            for (int col = 0; col < gridThreatMarkers[row].Length; col++)
            {
                float markerValue = gridThreatMarkers[row][col];

                if (markerValue == 1.0f || markerValue == 2.0f) // Verificar si hay un Threat Marker definido
                {
                    // Transformar las coordenadas
                    float xPosition = col * cellWidth;
                    float zPosition = -row * cellHeight;

                    // Crear posición en Unity
                    Vector3 markerPosition = new Vector3(xPosition, 0, zPosition);

                    // Instanciar el prefab correspondiente
                    if (markerValue == 1.0f)
                    {
                        Instantiate(dropletsPrefab, markerPosition, Quaternion.identity);
                        Debug.Log($"Instanciando Droplets en posición ({col}, {row}) transformada a Unity ({markerPosition})");
                    }
                    else if (markerValue == 2.0f)
                    {
                        Instantiate(gooPrefab, markerPosition, Quaternion.identity);
                        Debug.Log($"Instanciando Goo en posición ({col}, {row}) transformada a Unity ({markerPosition})");
                    }
                }
            }
        }
    }

    // Procesar textos de victimas salvadas, perdidas y edificio colapsado
    public TextMeshProUGUI savedVictimsText;
    public TextMeshProUGUI lostVictimsText;
    public TextMeshProUGUI collapsedBuildingText;

    public void ProcessTexts(int savedVictims, int lostVictims, bool collapsedBuilding)
    {
        savedVictimsText.text = $"Saved victims: {savedVictims}";
        lostVictimsText.text = $"Lost victims: {lostVictims}";
        collapsedBuildingText.text = $"Collapsed building: {collapsedBuilding}";
    }


    // Método Start
    void Start()
    {
        StartCoroutine(SendRequest(1)); // Solicitar datos para el paso 1
    }
}
