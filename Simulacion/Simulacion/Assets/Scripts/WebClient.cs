using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;

[System.Serializable]
public class SimulationData
{
    public int step;
    public int[][] grid_doors_entries;
    public int[][] grid_walls;
    public float[][] grid_poi;
    public float[][] grid_threat_markers;
    public float[][] grid_agents;
    public Dictionary<string, string> wall_states;
    public Dictionary<string, string> door_states;
    public bool collapsed_building;
    public int saved_victims;
    public int lost_victims;
}

[System.Serializable]
public class Summary
{
    public int steps;
    public bool collapsed_building;
    public int saved_victims;
    public int lost_victims;
}

[System.Serializable]
public class RootObject
{
    public SimulationData[] simulation_data;
    public Summary summary;
}

public class WebClient : MonoBehaviour
{
    public GameObject openDoorPrefab;
    public GameObject closedDoorPrefab;
    public GameObject horizontalWallPrefab;
    public GameObject verticalWallPrefab;
    public GameObject gooPrefab;
    public GameObject dropletsPrefab;
    public GameObject POIPrefab;
    public GameObject fakePoiPrefab;
    public GameObject damagedWallPrefab;
    public GameObject destroyedWallPrefab;
    public GameObject lootbugAgentPrefab;
    public GameObject employeeAgentPrefab;

    public TextMeshProUGUI savedVictimsText;
    public TextMeshProUGUI lostVictimsText;
    public TextMeshProUGUI stepsText;

    private List<GameObject> instantiatedObjects = new List<GameObject>();
    private RootObject rootObject;
    private int currentStep = 0;

    void Start()
    {
        // Call the server at the start of the game
        StartCoroutine(SendRequest());
        StartCoroutine(UpdateEverySecond());

    }

    IEnumerator SendRequest()
    {
        using (UnityWebRequest www = UnityWebRequest.PostWwwForm("http://localhost:8585", ""))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                // Parse the JSON response
                rootObject = JsonConvert.DeserializeObject<RootObject>(www.downloadHandler.text);

                Debug.Log("Datos JSON procesados correctamente.");
                Debug.Log(www.downloadHandler.text);

                UpdateVictimTexts();


                // Process the first step
                ProcessStep(1);
            }
        }
    }

    void UpdateVictimTexts()
    {
        savedVictimsText.text = $"Chatarra salvada: {rootObject.summary.saved_victims}";
        lostVictimsText.text = $"Chatarra perdida: {rootObject.summary.lost_victims}";
    }

    void UpdateStepsText()
    {
        stepsText.text = $"Pasos: {rootObject.summary.steps}";
    }

    IEnumerator UpdateEverySecond()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            Debug.Log("A seconds passed");
            // Clear the current step objects
            ClearCurrentStep();

            // Move to the next step
            currentStep++;
            ProcessStep(currentStep);
        }
    }

    void ClearCurrentStep()
    {
        foreach (var obj in instantiatedObjects)
        {
            Destroy(obj);
        }
        instantiatedObjects.Clear();
    }

    void ProcessStep(int step)
    {
        if (step > rootObject.simulation_data.Length)
        {
            Debug.Log("No more steps available.");
            return;
        }

        var simulationData = rootObject.simulation_data[step - 1];
        Debug.Log($"Procesando datos de simulación para el paso: {simulationData.step}");

        // Instantiate doors
        for (int i = 0; i < simulationData.grid_doors_entries.Length; i++)
        {
            for (int j = 0; j < simulationData.grid_doors_entries[i].Length; j++)
            {
                if (simulationData.grid_doors_entries[i][j] != 0)
                {
                    // Determine the position for the door
                    Vector3 position = new Vector3((i * -1) - 0.5f, -0.375f, (j * -1) + 0.5f);
                    GameObject doorPrefab = null;

                    // Construct the key for the door state
                    string key = $"(({i}, {j}), ({i}, {j + 1}))";

                    // Check if the key exists in the dictionary
                    if (simulationData.door_states.ContainsKey(key))
                    {
                        if (simulationData.door_states[key] == "open" || simulationData.door_states[key] == "broken")
                        {
                            doorPrefab = openDoorPrefab;
                        }
                        else if (simulationData.door_states[key] == "closed")
                        {
                            doorPrefab = closedDoorPrefab;
                        }

                        if (doorPrefab != null)
                        {
                            GameObject door = Instantiate(doorPrefab, position, Quaternion.identity);
                            instantiatedObjects.Add(door);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Key not found in door_states: {key}");
                    }
                }
            }
        }

        // Instantiate walls
        for (int i = 0; i < simulationData.grid_walls.Length; i++)
        {
            for (int j = 0; j < simulationData.grid_walls[i].Length; j++)
            {
                int wallValue = simulationData.grid_walls[i][j];
                Vector3 position = new Vector3(i * -1, 0, j * -1);
                GameObject obj = null;

                // Construct the key for the wall state
                string key = $"(({i}, {j}), ({i}, {j + 1}))";

                // Check if the key exists in the dictionary
                if (simulationData.wall_states.ContainsKey(key))
                {
                    string wallState = simulationData.wall_states[key];
                    switch (wallState)
                    {
                        case "damaged":
                            obj = damagedWallPrefab;
                            break;
                        case "destroyed":
                            obj = destroyedWallPrefab;
                            break;
                        default:
                            obj = null;
                            break;
                    }
                }

                if (obj == null)
                {
                    switch (wallValue)
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
                else
                {
                    GameObject wall = Instantiate(obj, position, Quaternion.identity);
                    instantiatedObjects.Add(wall);
                }
            }
        }

        // Instantiate POIs
        for (int i = 0; i < simulationData.grid_poi.Length; i++)
        {
            for (int j = 0; j < simulationData.grid_poi[i].Length; j++)
            {
                float poiValue = simulationData.grid_poi[i][j];
                Vector3 position = new Vector3((i * -1) - 0.5f, 0, (j * -1) + 0.5f);
                GameObject obj = null;

                if (poiValue == 4)
                {
                    obj = Instantiate(POIPrefab, position, Quaternion.identity);
                    instantiatedObjects.Add(obj);
                }
                else if (poiValue == 3)
                {
                    obj = Instantiate(fakePoiPrefab, position, Quaternion.identity);
                    instantiatedObjects.Add(obj);
                }
            }
        }

        // Instantiate Goo and Droplets
        for (int i = 0; i < simulationData.grid_threat_markers.Length; i++)
        {
            for (int j = 0; j < simulationData.grid_threat_markers[i].Length; j++)
            {
                float gooValue = simulationData.grid_threat_markers[i][j];
                Vector3 position = new Vector3((i * -1) - 0.5f, -0.308f, (j * -1) + 0.5f);
                GameObject obj = null;

                if (gooValue == 2)
                {
                    obj = Instantiate(gooPrefab, position, Quaternion.identity);
                    instantiatedObjects.Add(obj);
                }
                else if (gooValue == 1)
                {
                    obj = Instantiate(dropletsPrefab, position, Quaternion.identity);
                    instantiatedObjects.Add(obj);
                }
            }
        }
        // Instantiate agents
        for (int i = 0; i < simulationData.grid_agents.Length; i++)
        {
            for (int j = 0; j < simulationData.grid_agents[i].Length; j++)
            {
                float agentValue = simulationData.grid_agents[i][j];
                Vector3 position = new Vector3((i * -1) - 0.5f, 0, (j * -1) + 0.5f);
                GameObject obj = null;

                if (agentValue == 7)
                {
                    obj = Instantiate(lootbugAgentPrefab, position, Quaternion.identity);
                    instantiatedObjects.Add(obj);
                }
                else if (agentValue == 6)
                {
                    obj = Instantiate(employeeAgentPrefab, position, Quaternion.identity);
                    instantiatedObjects.Add(obj);
                }
            }
        }
    }
}