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
                    
                    if (response.grid_walls == null || response.grid_walls.Count == 0)
                    {
                        Debug.LogError("Error: grid_walls es nulo o está vacío después de deserialización.");
                    }
                    else
                    {
                        Debug.Log($"grid_walls contiene {response.grid_walls.Count} filas.");
                    }
                    
                    UpdateScene(response);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error al deserializar la respuesta: {ex.Message}");
                }
            }

        }
    }

    // Método para actualizar la escena
    void UpdateScene(SimulationResponse response)
    {
        // Validar que grid_walls no sea nulo ni esté vacío
        if (response.grid_walls == null || response.grid_walls.Count == 0)
        {
            Debug.LogError("Error: grid_walls es nulo o está vacío. Revisa el servidor o los datos enviados.");
            return;
        }

        // Definir el tamaño de las celdas en función del plano
        float cellWidth = 10.0f; // Ajusta según el tamaño real de las celdas en el eje X
        float cellHeight = 10.0f; // Ajusta según el tamaño real de las celdas en el eje Z

        for (int row = 0; row < response.grid_walls.Count; row++)
        //for (int row = 0; row < 2; row++)
        {
            // Validar que la fila no sea nula ni esté vacía
            if (response.grid_walls[row] == null || response.grid_walls[row].Length == 0)
            {
                Debug.LogError($"Error: La fila {row} de grid_walls es nula o está vacía.");
                continue;
            }

            for (int col = 0; col < response.grid_walls[row].Length; col++)
            {
                // Obtener el valor de la celda
                int wallValue = response.grid_walls[row][col];
                Vector3 cellPosition = new Vector3(col * cellWidth, 0, -row * cellHeight);

                // Instanciar paredes según el valor de grid_walls
                if ((wallValue & 1) != 0) // Pared derecha
                {
                    Vector3 position = cellPosition + new Vector3(cellWidth / 2, 0, 0);
                    Instantiate(verticalWallPrefab, position, Quaternion.Euler(0, 90, 0));
                    Debug.Log($"Instanciando pared derecha en posición: {position}");
                }
                if ((wallValue & 2) != 0) // Pared abajo
                {
                    Vector3 position = cellPosition + new Vector3(0, 0, -cellHeight / 2);
                    Instantiate(horizontalWallPrefab, position, Quaternion.identity );
                    Debug.Log($"Instanciando pared abajo en posición: {position}");
                }
                if ((wallValue & 4) != 0) // Pared izquierda
                {
                    Vector3 position = cellPosition + new Vector3(-cellWidth / 2, 0, 0);
                    Instantiate(verticalWallPrefab, position, Quaternion.Euler(0, 90, 0));
                    Debug.Log($"Instanciando pared izquierda en posición: {position}");
                }
                if ((wallValue & 8) != 0) // Pared arriba
                {
                    Vector3 position = cellPosition + new Vector3(0, 0, cellHeight / 2);
                    Instantiate(horizontalWallPrefab, position, Quaternion.identity);
                    Debug.Log($"Instanciando pared arriba en posición: {position}");
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
