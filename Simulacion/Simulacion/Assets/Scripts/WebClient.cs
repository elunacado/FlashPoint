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
    public GameObject agentVictimPrefab;
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
    private int totalSteps = 200; // Define el número máximo de pasos
    
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

    // Clase para representar cada agente en 'agents_info'
    [System.Serializable]
    public class AgentInfo
    {
        public int[] position; // Posición del agente en el formato [x, y]
        public int value; // Valor que representa el tipo de agente (6 para EmployeeAgent, 7 para LootBugAgent)
        public bool carrying_victim; // Indica si el agente está transportando una víctima
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
        public List<AgentInfo> agents_info;
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
        ProcessAgents(response.agents_info, cellWidth, cellHeight);
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
                    GameObject wallInstance = Instantiate(horizontalWallPrefab, position, Quaternion.identity);
                    
                    // Actualizar el estado visual de la pared
                    UpdateWallState(wallInstance, state);
                    walls.Add(wallInstance); // Agregar la instancia a la lista de paredes
                    
                    Debug.Log($"Instanciando pared horizontal entre ({x1}, {y1}) y ({x2}, {y2}), Estado: {state}");
                }
                else if (y1 == y2) // Si las coordenadas y son iguales, es una pared vertical
                {
                    // Calcular la posición central de la pared
                    float centerX = ((x1 + x2) / 2.0f) * cellWidth;
                    float centerZ = y1 * -cellHeight;
                    Vector3 position = new Vector3(centerX, 0, centerZ);

                    // Instanciar la pared vertical
                    GameObject wallInstance = Instantiate(verticalWallPrefab, position, Quaternion.identity);
                    
                    // Actualizar el estado visual de la pared
                    UpdateWallState(wallInstance, state);
                    walls.Add(wallInstance); // Agregar la instancia a la lista de paredes


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

    void UpdateWallState(GameObject wallObject, string state)
    {
        Renderer renderer = wallObject.GetComponent<Renderer>();
        if (state == "damaged")
        {
            renderer.material.color = Color.green; // Representa una pared dañada
        }
        else if (state == "destroyed")
        {
            // Destruir el objeto de la pared
            Destroy(wallObject);
            Debug.Log("La pared ha sido destruida.");
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
                    Vector3 position = new Vector3(centerX, 3, centerZ);

                    // Instanciar la puerta horizontal y agregarla a la lista
                    GameObject doorInstance = Instantiate(horizontalDoor, position, Quaternion.identity);
                     // Actualizar el estado visual de la puerta
                    UpdateDoorState(doorInstance, state);
                    doors.Add(doorInstance); // Agregar la instancia a la lista de puertas

                    Debug.Log($"Instanciando puerta horizontal entre ({x1}, {y1}) y ({x2}, {y2}), Estado: {state}");
                }
                else if (y1 == y2) // Si las coordenadas y son iguales, es una puerta vertical
                {
                    // Calcular la posición central de la puerta
                    float centerX = ((x1 + x2) / 2.0f) * cellWidth;
                    float centerZ = y1 * -cellHeight;
                    Vector3 position = new Vector3(centerX, 3, centerZ);

                    // Instanciar la puerta vertical y agregarla a la lista
                    GameObject doorInstance = Instantiate(verticalDoor, position, Quaternion.identity);
                    
                    // Actualizar el estado visual de la puerta
                    UpdateDoorState(doorInstance, state);
                    doors.Add(doorInstance); // Agregar la instancia a la lista de puertas


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

    void UpdateDoorState(GameObject doorObject, string state)
    {
        Renderer renderer = doorObject.GetComponent<Renderer>();

        if (state == "open")
        {
            // Simular apertura rotando la puerta
            doorObject.transform.rotation = Quaternion.Euler(0, 90, 0); // Rotar 90° en el eje Y
            // Cambiar el color de la puerta a verde
            if (renderer != null)
            {
                renderer.material.color = Color.green; // Cambiar el color a verde
            }
            Debug.Log("La puerta se ha abierto (color verde).");
        }
        else if (state == "closed")
        {
            // Restablecer la rotación a su estado original
            doorObject.transform.rotation = Quaternion.identity; // Sin rotación
            Debug.Log("La puerta está cerrada.");
        }
        else if (state == "removed")
        {
            // Cambiar el color de la puerta a rojo en lugar de destruirla
            if (renderer != null)
            {
                renderer.material.color = Color.red; // Cambiar el color a rojo
            }
            Debug.Log("La puerta está marcada como destruida (color rojo).");
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

                    // Agregar el POI instanciado a la lista de POIs
                    pois.Add(poiInstance);

                    Debug.Log($"Instanciando POI en posición ({col}, {row}) transformada a Unity ({poiPosition}), valor: {poiValue}");
                }
            }
        }
    }


    // Procesar los agentes
    void ProcessAgents(List<AgentInfo> agentsInfo, float cellWidth, float cellHeight)
    {
        // Limpiar la lista de agentes actuales
        foreach (var agent in agents)
        {
            Destroy(agent); // Destruir los agentes instanciados previamente
        }
        agents.Clear(); // Vaciar la lista de agentes

        // Iterar sobre la lista de agentes en 'agents_info'
        foreach (var agentInfo in agentsInfo)
        {
            // Invertir las coordenadas: usar `y` como `x` y `x` como `y`
            float xPosition = agentInfo.position[1] * cellWidth; // Posición en el eje X
            float zPosition = -agentInfo.position[0] * cellHeight; // Posición en el eje Z (invertir para Unity)

            Vector3 agentPosition = new Vector3(xPosition, 0, zPosition); // Crear la posición en Unity

            // Determinar el prefab a instanciar según el valor y el estado de transporte
            GameObject prefabToInstantiate;
            if (agentInfo.value == 6)
            {
                // Seleccionar prefab según `carrying_victim`
                prefabToInstantiate = agentInfo.carrying_victim ? agentVictimPrefab : agentPrefab;
            }
            else if (agentInfo.value == 7)
            {
                prefabToInstantiate = lootbugPrefab;
            }
            else
            {
                Debug.LogWarning($"Valor inesperado en agents_info: {agentInfo.value}");
                continue; // Saltar al siguiente agente si el valor no es válido
            }

            // Instanciar el prefab correspondiente
            GameObject agentInstance = Instantiate(prefabToInstantiate, agentPosition, Quaternion.identity);

            // Agregar el agente instanciado a la lista
            agents.Add(agentInstance);

            Debug.Log($"Instanciando {prefabToInstantiate.name} en posición ({agentInfo.position[1]}, {agentInfo.position[0]}) transformada a Unity ({agentPosition}), carrying_victim: {agentInfo.carrying_victim}");
        }
    }



    // Procesar los Threat Markers
    void ProcessThreatMarkers(List<float[]> gridThreatMarkers, float cellWidth, float cellHeight)
    {
        // Iterar sobre cada fila de la matriz gridThreatMarkers
        for (int row = 0; row < gridThreatMarkers.Count; row++)
        {
            // Verificar si la fila actual está vacía o es nula
            if (gridThreatMarkers[row] == null || gridThreatMarkers[row].Length == 0)
            {
                Debug.LogWarning($"Fila {row} de grid_threat_markers está vacía o es nula."); // Advertencia en caso de error
                continue; // Continuar con la siguiente fila
            }

            // Iterar sobre cada columna de la fila actual
            for (int col = 0; col < gridThreatMarkers[row].Length; col++)
            {
                float markerValue = gridThreatMarkers[row][col]; // Obtener el valor del Threat Marker en la posición actual

                // Verificar si el valor indica un Threat Marker válido (1.0 para Droplets o 2.0 para Goo)
                if (markerValue == 1.0f || markerValue == 2.0f)
                {
                    // Calcular la posición en Unity usando las coordenadas de la matriz
                    float xPosition = col * cellWidth; // Posición en el eje X
                    float zPosition = -row * cellHeight; // Posición en el eje Z (negativo para adaptar el sistema de coordenadas)

                    Vector3 markerPosition = new Vector3(xPosition, 0, zPosition); // Crear el vector de posición para Unity

                    // Instanciar el prefab correspondiente según el valor del marcador
                    GameObject markerInstance;
                    if (markerValue == 1.0f)
                    {
                        markerInstance = Instantiate(dropletsPrefab, markerPosition, Quaternion.identity);
                        Debug.Log($"Instanciando Droplets en posición ({col}, {row}) transformada a Unity ({markerPosition})");
                    }
                    else // markerValue == 2.0f
                    {
                        markerInstance = Instantiate(gooPrefab, markerPosition, Quaternion.identity);
                        Debug.Log($"Instanciando Goo en posición ({col}, {row}) transformada a Unity ({markerPosition})");
                    }

                    // Agregar el Threat Marker instanciado a la lista
                    threatMarkers.Add(markerInstance);
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
        StartCoroutine(StepCoroutine()); // Inicia la actualización automática
    }

}
