using System.Collections.Generic;
using UnityEngine;

public class SceneInitializer : MonoBehaviour
{
    // Prefabs de los objetos a instanciar
    public GameObject wallPrefab;
    public GameObject doorPrefab;
    public GameObject agentPrefab;

    // Datos deserializados del primer paso de la simulación
    [System.Serializable]
    public class SimulationStep
    {
        public List<int[]> grid_walls;
        public List<int[]> grid_doors;
        public List<int[]> grid_agents;
    }

    void Start()
    {
        // Simulación de datos del JSON (normalmente vendría del servidor)
        string jsonString = @"
        {
            ""grid_walls"": [[0, 1], [2, 3], [4, 5]],
            ""grid_doors"": [[1, 1], [3, 3]],
            ""grid_agents"": [[0, 0], [4, 4]]
        }";

        // Deserializar los datos
        SimulationStep initialStep = JsonUtility.FromJson<SimulationStep>(jsonString);

        // Pintar la escena inicial
        PaintWalls(initialStep.grid_walls);
        PaintDoors(initialStep.grid_doors);
        PaintAgents(initialStep.grid_agents);
    }

    // Instanciar paredes
    void PaintWalls(List<int[]> walls)
    {
        foreach (int[] position in walls)
        {
            Vector3 wallPosition = new Vector3(position[0], 0, position[1]); // Z = Y en la cuadrícula
            Instantiate(wallPrefab, wallPosition, Quaternion.identity);
        }
    }

    // Instanciar puertas
    void PaintDoors(List<int[]> doors)
    {
        foreach (int[] position in doors)
        {
            Vector3 doorPosition = new Vector3(position[0], 0, position[1]);
            Instantiate(doorPrefab, doorPosition, Quaternion.identity);
        }
    }

    // Instanciar agentes
    void PaintAgents(List<int[]> agents)
    {
        foreach (int[] position in agents)
        {
            Vector3 agentPosition = new Vector3(position[0], 0, position[1]);
            Instantiate(agentPrefab, agentPosition, Quaternion.identity);
        }
    }
}
