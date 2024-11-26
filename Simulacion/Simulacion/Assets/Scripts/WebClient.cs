using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

public class WebClient : MonoBehaviour
{
    // URL del servidor Python
    private string serverUrl = "http://localhost:8585";

    // Estructura para enviar y recibir datos (clases que coinciden con el JSON)
    [System.Serializable]
    public class SimulationRequest
    {
        public int step; // Paso solicitado
    }

    [System.Serializable]
    public class SimulationResponse
    {
        public List<int[]> grid_walls;
        public List<int[]> grid_doors;
        public List<int[]> grid_agents;
    }

    // Coroutine para enviar solicitudes POST
    IEnumerator SendRequest(int step)
    {
        // Crear la solicitud JSON
        SimulationRequest request = new SimulationRequest { step = step };
        string jsonData = JsonUtility.ToJson(request);

        // Configurar la solicitud POST
        using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            // Enviar la solicitud al servidor
            yield return www.SendWebRequest();

            // Manejo de errores
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error en la solicitud: {www.error}");
            }
            else
            {
                // Procesar la respuesta del servidor
                Debug.Log($"Respuesta del servidor: {www.downloadHandler.text}");
                SimulationResponse response = JsonUtility.FromJson<SimulationResponse>(www.downloadHandler.text);

                // Actualizar la escena con los datos del servidor
                UpdateScene(response);
            }
        }
    }

    // Método para actualizar la escena en Unity
    void UpdateScene(SimulationResponse response)
    {
        // Ejemplo: Pintar paredes desde grid_walls
        foreach (int[] wall in response.grid_walls)
        {
            Vector3 wallPosition = new Vector3(wall[0], 0, wall[1]);
            Instantiate(Resources.Load<GameObject>("Prefabs/WallPrefab"), wallPosition, Quaternion.identity);
        }

        // Ejemplo: Manejo similar para grid_doors y grid_agents
    }

    // Método Start para iniciar el proceso de solicitud
    void Start()
    {
        StartCoroutine(SendRequest(1)); // Solicitar el paso 1 al iniciar
    }

    // Método Update para avanzar entre pasos (opcional)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Avanzar con la barra espaciadora
        {
            int nextStep = Random.Range(2, 10); // Solicitar un paso aleatorio (por ejemplo)
            StartCoroutine(SendRequest(nextStep));
        }
    }
}
