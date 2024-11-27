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
    public GameObject lootbugPrefab;
    public GameObject dropletsPrefab;
    public GameObject gooPrefab;

    // Listas para almacenar las referencias a los objetos instanciados en la escena
    private List<GameObject> walls = new List<GameObject>(); // Lista de paredes
    private List<GameObject> doors = new List<GameObject>(); // Lista de puertas
    private List<GameObject> pois = new List<GameObject>(); // Lista de POIs
    private List<GameObject> agents = new List<GameObject>(); // Lista de agentes
    private List<GameObject> threatMarkers = new List<GameObject>(); // Lista de marcadores de amenaza

    // Métodos para limpiar objetos de la escena
    private void ClearWalls()
    {
        foreach (var obj in walls) Destroy(obj);
        walls.Clear();
    }
    private void ClearDoors()
    {
        foreach (var obj in doors) Destroy(obj);
        doors.Clear();
    }
    private void ClearPois()
    {
        foreach (var obj in pois) Destroy(obj);
        pois.Clear();
    }
    private void ClearAgents()
    {
        foreach (var obj in agents) Destroy(obj);
        agents.Clear();
    }
    private void ClearThreatMarkers()
    {
        foreach (var obj in threatMarkers) Destroy(obj);
        threatMarkers.Clear();
    }


    private int currentStep = 0; // Paso actual de la simulación
    private int totalSteps = 50; // Define el número máximo de pasos
    
    private float stepInterval = 0.3f; // Intervalo de tiempo entre pasos

    IEnumerator StepCoroutine()
    {
        // Inicia la simulación y procesa cada paso de forma iterativa
        while (currentStep < totalSteps)
        {
            Debug.Log($"Procesando paso: {currentStep}"); // Imprime en el paso actual de la simulación

            // Ejecuta el paso actual con lógica de reintentos en caso de error
            yield return StartCoroutine(SendRequestWithRetry(currentStep)); 

            currentStep++; // Incrementa el contador de pasos
            yield return new WaitForSeconds(stepInterval); // Espera el tiempo definido entre steps
        }

        Debug.Log("Simulación completada."); 
    }


    // Mecanismo con solicitud con reintentos
    IEnumerator SendRequestWithRetry(int step)
    {
        int retries = 3; // Número máximo de reintentos
        bool success = false; // Bandera para verificar si la solicitud fue exitosa

        // Mientras haya reintentos disponibles y no se haya tenido éxito
        while (retries > 0 && !success)
        {
            yield return StartCoroutine(SendRequest(step)); // Envía la solicitud para el paso actual

            success = true; // Asume éxito si no ocurren excepciones durante la solicitud

            if (!success) // Si la solicitud no tiene éxito
            {
                retries--; // Decrementa el número de reintentos disponibles
                Debug.LogWarning($"Reintentando paso {step}. Reintentos restantes: {retries}"); // Mensaje de advertencia en consola
                yield return new WaitForSeconds(1.0f); // Espera antes de realizar un nuevo intento
            }
        }

        // Si no se tuvo éxito después de todos los reintentos
        if (!success)
        {
            Debug.LogError($"No se pudo procesar el paso {step} después de varios reintentos."); // Mensaje de error en consola
        }
    }



    // Clase para representar la solicitud de simulación
    [System.Serializable]
    public class SimulationRequest
    {
        public int step; // Paso solicitado al servidor
    }

    // Clase para representar la respuesta del JSON
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

    // Método para enviar una solicitud HTTP POST al servidor y procesar la respuesta
    IEnumerator SendRequest(int step)
    {
        // Crear una instancia de la solicitud con el paso actual
        SimulationRequest request = new SimulationRequest { step = step };
        
        // Convertir la solicitud a formato JSON para enviarla al servidor
        string jsonData = JsonUtility.ToJson(request);

        // Configurar una solicitud HTTP POST al servidor
        using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw); 
            www.downloadHandler = new DownloadHandlerBuffer(); 
            www.SetRequestHeader("Content-Type", "application/json"); 

            yield return www.SendWebRequest();

            // Manejar posibles errores de conexión o protocolo
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error en la solicitud: {www.error}"); 
            }
            else
            {
                try
                {
                    // Intentar deserializar la respuesta JSON a un objeto de tipo SimulationResponse
                    SimulationResponse response = JsonConvert.DeserializeObject<SimulationResponse>(www.downloadHandler.text);
                    
                    // Actualizar la escena con los datos recibidos del servidor
                    UpdateScene(response);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error al deserializar la respuesta: {ex.Message}\nJSON: {www.downloadHandler.text}");
                }
            }
        }
    }

    // Método para limpiar todos los objetos de la escena
    private void ClearScene()
    {
        ClearWalls();
        ClearDoors();
        ClearPois();
        ClearAgents();
        ClearThreatMarkers();
    }

    // Método para actualizar la escena con una nueva respuesta del servidor
    void UpdateScene(SimulationResponse response)
    {
        ClearScene(); // Limpia todos los objetos actuales de la escena antes de actualizar

        // Validar que los datos necesarios no sean nulos ni estén vacíos
        if (response.grid_poi == null || response.grid_poi.Count == 0 ||
            response.wall_states == null || response.wall_states.Count == 0 ||
            response.door_states == null || response.door_states.Count == 0 ||
            response.grid_agents == null || response.grid_agents.Count == 0)
        {
            Debug.LogError("Error: Uno o más datos de la respuesta son nulos o están vacíos (grid_poi, wall_states, door_states, grid_agents).");
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
        // Iterar sobre cada pared definida en el diccionario wallStates
        foreach (var wall in wallStates)
        {
            string key = wall.Key; // Clave que representa las coordenadas de la pared
            string state = wall.Value; // Estado de la pared (e.g., "okay", "damaged")

            try
            {
                // Parsear las coordenadas de la clave (formato: "((x1, y1), (x2, y2))")
                string[] coordinates = key.Trim('(', ')').Split(new[] { "), (" }, System.StringSplitOptions.None);
                string[] coord1 = coordinates[0].Split(','); // Coordenadas iniciales (x1, y1)
                string[] coord2 = coordinates[1].Split(','); // Coordenadas finales (x2, y2)

                // Convertir las coordenadas de string a enteros
                int x1 = int.Parse(coord1[0].Trim());
                int y1 = int.Parse(coord1[1].Trim());
                int x2 = int.Parse(coord2[0].Trim());
                int y2 = int.Parse(coord2[1].Trim());

                // Transformar las coordenadas según la representación de Unity (intercambiar x e y)
                (x1, y1) = (y1, x1);
                (x2, y2) = (y2, x2);

                // Determinar la posición y orientación de la pared en Unity
                if (x1 == x2) // Si las coordenadas x son iguales, es una pared horizontal
                {
                    // Calcular la posición central de la pared
                    float centerX = x1 * cellWidth;
                    float centerZ = ((y1 + y2) / 2.0f) * -cellHeight;
                    Vector3 position = new Vector3(centerX, 0, centerZ);

                    // Instanciar la pared horizontal
                    Instantiate(horizontalWallPrefab, position, Quaternion.identity);
                    Debug.Log($"Instanciando pared horizontal entre ({x1}, {y1}) y ({x2}, {y2}), Estado: {state}");
                }
                else if (y1 == y2) // Si las coordenadas y son iguales, es una pared vertical
                {
                    // Calcular la posición central de la pared
                    float centerX = ((x1 + x2) / 2.0f) * cellWidth;
                    float centerZ = y1 * -cellHeight;
                    Vector3 position = new Vector3(centerX, 0, centerZ);

                    // Instanciar la pared vertical
                    Instantiate(verticalWallPrefab, position, Quaternion.identity);
                    Debug.Log($"Instanciando pared vertical entre ({x1}, {y1}) y ({x2}, {y2}), Estado: {state}");
                }
            }
            catch (System.Exception ex)
            {
                // Manejar errores durante el procesamiento de la pared
                Debug.LogError($"Error al procesar la pared: {key}. Detalles: {ex.Message}");
            }
        }
    }


    // Procesar las puertas
    void ProcessDoors(Dictionary<string, string> doorStates, float cellWidth, float cellHeight)
    {
        // Iterar sobre cada puerta definida en el diccionario doorStates
        foreach (var door in doorStates)
        {
            string key = door.Key; // Clave que representa las coordenadas de la puerta
            string state = door.Value; // Estado de la puerta (e.g., "open", "closed")

            try
            {
                // Parsear las coordenadas de la clave (formato: "((x1, y1), (x2, y2))")
                string[] coordinates = key.Trim('(', ')').Split(new[] { "), (" }, System.StringSplitOptions.None);
                string[] coord1 = coordinates[0].Split(','); // Coordenadas iniciales (x1, y1)
                string[] coord2 = coordinates[1].Split(','); // Coordenadas finales (x2, y2)

                // Convertir las coordenadas de string a enteros
                int x1 = int.Parse(coord1[0].Trim());
                int y1 = int.Parse(coord1[1].Trim());
                int x2 = int.Parse(coord2[0].Trim());
                int y2 = int.Parse(coord2[1].Trim());

                // Transformar las coordenadas según la representación de Unity (intercambiar x e y)
                (x1, y1) = (y1, x1);
                (x2, y2) = (y2, x2);

                // Determinar la posición y orientación de la puerta en Unity
                if (x1 == x2) // Si las coordenadas x son iguales, es una puerta horizontal
                {
                    // Calcular la posición central de la puerta
                    float centerX = x1 * cellWidth;
                    float centerZ = ((y1 + y2) / 2.0f) * -cellHeight;
                    Vector3 position = new Vector3(centerX, 0, centerZ);

                    // Instanciar la puerta horizontal
                    Instantiate(horizontalDoor, position, Quaternion.identity);
                    Debug.Log($"Instanciando puerta horizontal entre ({x1}, {y1}) y ({x2}, {y2}), Estado: {state}");
                }
                else if (y1 == y2) // Si las coordenadas y son iguales, es una puerta vertical
                {
                    // Calcular la posición central de la puerta
                    float centerX = ((x1 + x2) / 2.0f) * cellWidth;
                    float centerZ = y1 * -cellHeight;
                    Vector3 position = new Vector3(centerX, 0, centerZ);

                    // Instanciar la puerta vertical
                    Instantiate(verticalDoor, position, Quaternion.identity);
                    Debug.Log($"Instanciando puerta vertical entre ({x1}, {y1}) y ({x2}, {y2}), Estado: {state}");
                }
            }
            catch (System.Exception ex)
            {
                // Manejar errores durante el procesamiento de la puerta
                Debug.LogError($"Error al procesar la puerta: {key}. Detalles: {ex.Message}");
            }
        }
    }


    // Procesar los POIs
    void ProcessPois(List<float[]> gridPoi, float cellWidth, float cellHeight)
    {
        // Iterar sobre cada fila de la matriz grid_poi
        for (int row = 0; row < gridPoi.Count; row++)
        {
            // Validar si la fila actual está vacía o es nula
            if (gridPoi[row] == null || gridPoi[row].Length == 0)
            {
                Debug.LogWarning($"Fila {row} de grid_poi está vacía o es nula."); // Mostrar advertencia
                continue; // Pasar a la siguiente fila
            }

            // Iterar sobre cada columna de la fila actual
            for (int col = 0; col < gridPoi[row].Length; col++)
            {
                float poiValue = gridPoi[row][col]; // Obtener el valor del POI en la posición actual

                // Verificar si hay un POI definido (valor mayor a 0)
                if (poiValue > 0)
                {
                    // Calcular la posición en Unity usando las coordenadas de la matriz
                    float xPosition = col * cellWidth; // Posición en el eje X
                    float zPosition = -row * cellHeight; // Posición en el eje Z (negativo porque Unity usa un sistema de coordenadas diferente)

                    Vector3 poiPosition = new Vector3(xPosition, 0, zPosition); // Crear el vector de posición para Unity
                    
                    // Instanciar el prefab del POI en la posición calculada
                    GameObject poiInstance = Instantiate(poiPrefab, poiPosition, Quaternion.identity);

                    // Escalar el POI según su valor
                    poiInstance.transform.localScale *= poiValue / 4.0f;

                    // Cambiar el color del POI según su valor (de amarillo a rojo)
                    // Quitar esto luego
                    poiInstance.GetComponent<Renderer>().material.color = Color.Lerp(Color.yellow, Color.red, poiValue / 4.0f);

                    Debug.Log($"Instanciando POI en posición ({col}, {row}) transformada a Unity ({poiPosition}), valor: {poiValue}");
                }
            }
        }
    }


    // Procesar los agentes
    // Diccionario para almacenar agentes activos y sus referencias en Unity
    private Dictionary<int, GameObject> activeAgents = new Dictionary<int, GameObject>();

    void ProcessAgents(List<float[]> gridAgents, float cellWidth, float cellHeight)
    {
        // Iterar sobre la grid para mover o instanciar agentes
        for (int row = 0; row < gridAgents.Count; row++)
        {
            for (int col = 0; col < gridAgents[row].Length; col++)
            {
                float agentValue = gridAgents[row][col]; // Obtener el valor del agente en la posición actual

                if (agentValue == 6 || agentValue == 7) // Procesar solo si el valor es 6 (agente) o 7 (lootbug)
                {
                    int agentId = Mathf.FloorToInt(agentValue); // Obtener el ID único del agente (parte entera del valor)
                    Vector3 agentPosition = new Vector3(col * cellWidth, 0, -row * cellHeight); // Calcular la posición en Unity

                    if (activeAgents.ContainsKey(agentId))
                    {
                        // Si el agente ya existe, actualizar su posición
                        activeAgents[agentId].transform.position = agentPosition;
                    }
                    else
                    {
                        // Si el agente no existe, seleccionar el prefab correspondiente e instanciarlo
                        GameObject prefabToInstantiate = (agentValue == 6) ? agentPrefab : lootbugPrefab;
                        GameObject agentInstance = Instantiate(prefabToInstantiate, agentPosition, Quaternion.identity);
                        activeAgents[agentId] = agentInstance; // Almacenar la referencia en el diccionario
                    }
                }
            }
        }

        // Remover agentes que ya no están en la grid
        var idsToRemove = new List<int>(); // Lista para almacenar IDs de agentes que deben ser eliminados

        foreach (var kvp in activeAgents) // Recorrer los agentes activos
        {
            bool exists = false; // Bandera para verificar si el agente sigue en la grid

            // Comprobar si el agente aún está presente en la grid
            for (int row = 0; row < gridAgents.Count; row++)
            {
                for (int col = 0; col < gridAgents[row].Length; col++)
                {
                    if (Mathf.FloorToInt(gridAgents[row][col]) == kvp.Key) // Comparar el ID del agente
                    {
                        exists = true; // El agente sigue presente
                        break;
                    }
                }
                if (exists) break; // Salir del bucle si el agente fue encontrado
            }

            if (!exists) idsToRemove.Add(kvp.Key); // Si el agente no está en la grid, agregarlo a la lista de eliminación
        }

        // Eliminar los agentes que ya no están presentes en la grid
        foreach (int id in idsToRemove)
        {
            Destroy(activeAgents[id]); // Destruir el GameObject asociado al agente
            activeAgents.Remove(id); // Remover el agente del diccionario
        }
    }



    // Procesar los Threat Markers
    // Diccionario para almacenar la relación entre posiciones y objetos de Threat Markers
    private Dictionary<Vector3, GameObject> threatMarkersMap = new Dictionary<Vector3, GameObject>();

    void ProcessThreatMarkers(List<float[]> gridThreatMarkers, float cellWidth, float cellHeight)
    {
        // HashSet para rastrear las posiciones actuales de los Threat Markers
        HashSet<Vector3> currentPositions = new HashSet<Vector3>();

        // Iterar sobre la grid de Threat Markers
        for (int row = 0; row < gridThreatMarkers.Count; row++)
        {
            for (int col = 0; col < gridThreatMarkers[row].Length; col++)
            {
                float markerValue = gridThreatMarkers[row][col]; // Valor del Threat Marker en la posición actual

                if (markerValue > 0) // Procesar solo si hay un Threat Marker
                {
                    // Calcular la posición en Unity
                    Vector3 position = new Vector3(col * cellWidth, 0, -row * cellHeight);
                    
                    // Agregar la posición al conjunto de posiciones actuales
                    currentPositions.Add(position);

                    // Verificar si ya existe un Threat Marker en esta posición
                    if (!threatMarkersMap.ContainsKey(position))
                    {
                        // Seleccionar el prefab adecuado según el valor del Threat Marker
                        GameObject prefab = markerValue == 1.0f ? dropletsPrefab : gooPrefab;

                        // Instanciar el prefab y almacenarlo en el diccionario
                        GameObject markerInstance = Instantiate(prefab, position, Quaternion.identity);
                        threatMarkersMap[position] = markerInstance;
                    }
                }
            }
        }

        // Eliminar Threat Markers obsoletos que ya no están en la grid
        foreach (var pos in new List<Vector3>(threatMarkersMap.Keys)) // Crear una copia de las claves para iterar
        {
            if (!currentPositions.Contains(pos)) // Si la posición no está en las posiciones actuales
            {
                Destroy(threatMarkersMap[pos]); // Destruir el objeto asociado
                threatMarkersMap.Remove(pos); // Remover la posición del diccionario
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
        StartCoroutine(StepCoroutine()); // Inicia la actualización automática
    }

}
