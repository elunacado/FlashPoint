using System.Collections.Generic;

"""
Cada lista almacena las posiciones en una cuadrícula, donde cada 
posición es un arreglo de dos enteros (int[]), representando las 
coordenadas (x, y).
"""

[System.Serializable]
public class SimulationStep
{
    // Lista de posiciones para las paredes
    public List<int[]> grid_walls;

    // Lista de posiciones para las puertas
    public List<int[]> grid_doors;

    // Lista de posiciones para los agentes
    public List<int[]> grid_agents;
}