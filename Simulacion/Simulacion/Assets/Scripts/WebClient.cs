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
    public GameObject doorOpen;
    public GameObject doorClosed;

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
        // Validar que wall_states y door_states no sean nulos ni estén vacíos
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
        float cellWidth = 10.0f; // Ajusta según el tamaño real de las celdas en el eje X
        float cellHeight = 10.0f; // Ajusta según el tamaño real de las celdas en el eje Z

        // Procesar las paredes
        foreach (var wall in response.wall_states)
        {
            string key = wall.Key; // Clave como "((x1, y1), (x2, y2))"
            string state = wall.Value; // Estado de la pared, por ejemplo, "okay"

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

                // Interpretar las coordenadas de acuerdo con la transformación de la matriz
                (x1, y1) = (y1, x1);
                (x2, y2) = (y2, x2);

                // Calcular la posición de la pared
                if (x1 == x2) // Diferencia en Y -> Pared horizontal
                {
                    float centerX = x1 * cellWidth;
                    float centerZ = ((y1 + y2) / 2.0f) * -cellHeight;
                    Vector3 position = new Vector3(centerX, 0, centerZ);

                    Instantiate(horizontalWallPrefab, position, Quaternion.identity);
                    Debug.Log($"Instanciando pared horizontal entre ({x1}, {y1}) y ({x2}, {y2}), Estado: {state}");
                }
                else if (y1 == y2) // Diferencia en X -> Pared vertical
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

        // Procesar las puertas
        foreach (var door in response.door_states)
        {
            string key = door.Key; // Clave como "((x1, y1), (x2, y2))"
            string state = door.Value; // Estado de la puerta, por ejemplo, "closed"

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

                // Interpretar las coordenadas de acuerdo con la transformación de la matriz
                (x1, y1) = (y1, x1);
                (x2, y2) = (y2, x2);

                // Calcular la posición de la puerta
                if (x1 == x2) // Diferencia en Y -> Puerta horizontal
                {
                    float centerX = x1 * cellWidth;
                    float centerZ = ((y1 + y2) / 2.0f) * -cellHeight;
                    Vector3 position = new Vector3(centerX, 0, centerZ);

                    Instantiate(doorClosed, position, Quaternion.identity); // Usa un prefab específico para puertas
                    Debug.Log($"Instanciando puerta horizontal entre ({x1}, {y1}) y ({x2}, {y2}), Estado: {state}");
                }
                else if (y1 == y2) // Diferencia en X -> Puerta vertical
                {
                    float centerX = ((x1 + x2) / 2.0f) * cellWidth;
                    float centerZ = y1 * -cellHeight;
                    Vector3 position = new Vector3(centerX, 0, centerZ);

                    Instantiate(doorClosed, position, Quaternion.identity); // Usa un prefab específico para puertas
                    Debug.Log($"Instanciando puerta vertical entre ({x1}, {y1}) y ({x2}, {y2}), Estado: {state}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error al procesar la puerta: {key}. Detalles: {ex.Message}");
            }
        }
    }



    // Método Start
    void Start()
    {
        StartCoroutine(SendRequest(1)); // Solicitar datos para el paso 1
    }
}
