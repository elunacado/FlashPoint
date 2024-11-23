# --------------------------------------------------------------------
# ------------------------- Mesa & Imports ---------------------------
# --------------------------------------------------------------------

# Importamos las clases que se requieren para manejar los agentes (Agent) y su entorno (Model).
from mesa import Agent, Model

# Usamos ''MultiGrid'' para representar una cuadrícula donde cada celda puede contener como máximo un agente.
from mesa.space import MultiGrid

# ''DataCollector'' nos permite recolectar y almacenar datos en cada paso de la simulación para su análisis posterior.
from mesa.datacollection import DataCollector

# Random Activation
from mesa.time import RandomActivation

# Importamos los siguientes paquetes para el mejor manejo de valores numéricos.
import numpy as np
import pandas as pd

import json

# --------------------------------------------------------------------
# --------------------------- Employee -------------------------------
# --------------------------------------------------------------------
class EmployeeAgent(Agent):
    """Empleado ..."""

    def __init__(self, id, model):
        super().__init__(id, model)
        self.AP = 4  
        self.carrying_victim = False  
        self.finished_turn = False  
        self.next_cell = None  


# --------------------------------------------------------------------
# --------------------------- Loot Bug  ------------------------------
# --------------------------------------------------------------------
class LootBugAgent(Agent):
    """Agente Lootbug que interactúa con POIs y regresa al nido."""
    def __init__(self, id, model):
        super().__init__(id, model)
        self.cargando_poi = False  
        self.poi_cargado = None  
        self.state = "capturar_poi"

# --------------------------------------------------------------------
# ---------------------------- Grids ---------------------------------
# --------------------------------------------------------------------

def get_grid_doors_entries(model):
    """Grid que representa las puertas y puntos de entrada."""
    # Copiar la matriz base (puertas) para superponer información adicional
    combined_grid = np.copy(model.doors)

    # Agregar los puntos de entrada 
    for x, y in model.entry_points:
        combined_grid[x, y] = max(combined_grid[x, y], 16)  

    return combined_grid

def get_grid_walls(model):
    """Grid que representa las paredes."""
    grid = model.walls.copy()

    return grid

def get_grid_poi(model):
    """Grid que representa los POI."""
    grid = model.poi_placement.copy()

    return grid

def get_grid_threat_markers(model):
    """Grid que representa el Goo y los Droplets."""
    grid = model.threat_markers.copy()

    return grid

def get_grid(model):
    """Grid que representa los agentes."""
    grid = np.zeros((model.grid.width, model.grid.height))

    # Iterar sobre todas las celdas del grid
    for content, (x, y) in model.grid.coord_iter():
        for agent in content:
            # Si el contenido es una instancia de EmployeeAgent, representarlo en el grid
            if isinstance(agent, EmployeeAgent):
                grid[x][y] = 6  # Representar el agente con el valor 6
            if isinstance(agent, LootBugAgent):
                grid[x][y] = 7  # Representar el agente con el valor 7


    return grid

# --------------------------------------------------------------------
# ------------------------- Modelo Edificio --------------------------
# --------------------------------------------------------------------

class ModeloEdificio(Model):
    """Modelo del edificio Lethal Company."""
    def __init__(self, wall_data, poi_data, goo_data, doors_data, entry_points_data):
        super().__init__()

        # Agentes
        self.employee_agents = 6
        self.lootbug_agents = 1
        self.schedule = RandomActivation(self)

        # Dimensiones del modelo
        self.height = 6
        self.width = 8

        # Grid del modelo
        self.grid = MultiGrid(self.height, self.width, torus=False)
        self.running = True
        self.steps = 0

        # Variables del modelo
        self.droplets = 33
        self.goo = 33
        self.poi_total_count = 15
        self.poi_false_alarm = 5
        self.poi_real_victim  = 10
        self.damage_counter = 24
        self.door_markers = 8
        self.start_point = 4
        self.saved_victims = 0
        self.lost_victims = 0
        self.collapsed_building = False
        self.in_explosion = False
        self.poi_in_building = 0

        # Diccionario para rastrear estados de POIs
        self.poi_states = {}

        # Diccionario para rastrear estados de puertas
        self.door_states = {}

        # Matrices para representar el estado del edificio
        self.entry_points_location = np.zeros((self.height, self.width))
        self.walls = np.zeros((self.height, self.width), dtype=int)
        self.doors = np.zeros((self.height, self.width), dtype=int)

        # Matrices para objetos
        self.threat_markers = np.zeros((self.height, self.width)) 
        self.poi_placement = np.zeros((self.height, self.width)) 


        # Colocar elementos iniciales
        self.place_walls(wall_data)   
        self.place_poi(poi_data)
        self.place_goo(goo_data)
        self.place_doors(doors_data)
        self.place_start_point(entry_points_data)

        # Crear y colocar agentes
        self.place_employees()
        self.place_lootbug()

        # Inicializar recolector de datos
        self.datacollector = DataCollector(
            model_reporters={
                "Grid 1 Puertas y salidas": get_grid_doors_entries,
                "Grid 2 Paredes": get_grid_walls,
                "Grid 3 POI": get_grid_poi,
                "Grid 4 Threatmarkers": get_grid_threat_markers,
                "Grid 5 Agents": get_grid,
                "Steps": lambda model: model.steps,
            }
        )

    def place_lootbug(self):
        """Coloca agente Loot Bug en su punto de partida."""
        for i in range(self.lootbug_agents):

            # Crear el agente
            agent = LootBugAgent(i, self)

            # Colocar el agente en el punto de entrada
            self.grid.place_agent(agent, (0, 0))

            # Agregar el agente al schedule
            self.schedule.add(agent)

    def place_employees(self):
        """Coloca agentes empleados en puntos de entrada seleccionados aleatoriamente."""
        for i in range(self.employee_agents):
            # Seleccionar un punto de entrada aleatorio
            random_entry_point = self.random.choice(self.entry_points)

            # Crear el agente
            agent = EmployeeAgent(i, self)

            # Colocar el agente en el punto de entrada
            self.grid.place_agent(agent, random_entry_point)

            # Agregar el agente al schedule
            self.schedule.add(agent)

    def place_walls(self, wall_data):
        """Coloca paredes en posiciones específicas."""
        # Convierte las cadenas binarias de entrada en números enteros para representar las paredes en la matriz walls.
        for i in range(self.height):
            for j in range(self.width):
                self.walls[i, j] = int(wall_data[i][j], 2)

    def place_poi(self, poi_data):
        """Coloca POIs en posiciones específicas respetando el número por tipo y actualiza las variables."""
        # Crear lista de tipos de POIs respetando los contadores
        poi_types = (
            [3] * self.poi_false_alarm +  
            [4] * self.poi_real_victim    
        )

        # Barajar aleatoriamente los tipos de POIs
        self.random.shuffle(poi_types)

        # Seleccionar solo los necesarios para las posiciones disponibles
        selected_poi_types = poi_types[:len(poi_data)]

        # Asignar tipos de POIs a las posiciones disponibles
        for (x, y, poi_type), poi_val in zip(poi_data, selected_poi_types):
            # Ajustar índices para comenzar desde 0
            adjusted_x, adjusted_y = x - 1, y - 1

            # Validar que las posiciones ajustadas estén dentro de los límites
            if 0 <= adjusted_x < self.height and 0 <= adjusted_y < self.width:
                # Determinar el valor de POI basado en tipo ('v' -> verdadero, 'f' -> falso)
                value = 4 if poi_type == 'v' else 3
                self.poi_placement[adjusted_x][adjusted_y] = value
                # Inicializar estado de POI como "cerrado"
                self.poi_states[(adjusted_x, adjusted_y)] = "closed"  

        # Actualizar las variables relacionadas
        self.poi_false_alarm -= selected_poi_types.count(3)
        self.poi_real_victim -= selected_poi_types.count(4)
        self.poi_total_count = self.poi_false_alarm + self.poi_real_victim
        self.poi_in_building = selected_poi_types.count(3) + selected_poi_types.count(4)

    def place_goo(self, goo_data):
        """Coloca goo en posiciones específicas basadas en los datos proporcionados con ajuste de índice."""
        for x, y in goo_data:
            # Ajustar índices para comenzar desde 0
            adjusted_x, adjusted_y = x - 1, y - 1
            # Validar que las posiciones ajustadas estén dentro de los límites
            if 0 <= adjusted_x < self.height and 0 <= adjusted_y < self.width:
                # Representar el goo con un valor de 2
                self.threat_markers[adjusted_x][adjusted_y] = 2

    def place_doors(self, doors_data):
        """Coloca puertas en posiciones específicas basadas en los datos proporcionados con ajuste de índice."""
        for row1, col1, row2, col2 in doors_data:
            # Ajustar índices para comenzar desde 0
            adjusted_row1, adjusted_col1 = row1 - 1, col1 - 1
            adjusted_row2, adjusted_col2 = row2 - 1, col2 - 1

            # Validar que las posiciones estén dentro de los límites
            if (
                0 <= adjusted_row1 < self.height and
                0 <= adjusted_col1 < self.width and
                0 <= adjusted_row2 < self.height and
                0 <= adjusted_col2 < self.width
            ):
                # Marcar las puertas en la matriz doors
                if adjusted_row1 == adjusted_row2:  # Puerta entre columnas en la misma fila
                    self.doors[adjusted_row1, adjusted_col1] |= 4
                    self.doors[adjusted_row2, adjusted_col2] |= 1
                elif adjusted_col1 == adjusted_col2:  # Puerta entre filas en la misma columna
                    self.doors[adjusted_row1, adjusted_col1] |= 2
                    self.doors[adjusted_row2, adjusted_col2] |= 8

                # Inicializar el estado de la puerta como "closed"
                self.door_states[((adjusted_row1, adjusted_col1), (adjusted_row2, adjusted_col2))] = "closed"

    def place_start_point(self, entry_points_data):
        """Coloca puntos de entrada en posiciones específicas y las guarda en una lista."""
        # Lista para almacenar puntos de entrada
        self.entry_points = []
        for x, y in entry_points_data:
            # Ajustar índices para comenzar desde 0
            adjusted_x, adjusted_y = x - 1, y - 1
            # Validar que las posiciones estén dentro de los límites
            if 0 <= adjusted_x < self.height and 0 <= adjusted_y < self.width:
                self.entry_points.append((adjusted_x, adjusted_y))  

    def step(self):
        """Realiza un paso de la simulación."""
        if self.running:
            self.steps += 1
            self.datacollector.collect(self)

# Leer el archivo testCase.txt
with open("../testCase/testCase.txt", "r") as file:
    lines = file.readlines()

# Procesar las líneas según la cantidad fija de líneas por sección
# Sección 1: matrix_walls (6 líneas)
matrix_walls = [line.split() for line in lines[:6]]

# Sección 2: matrix_poi (3 líneas)
def process_poi_row(row):
    return [int(item) if item.isdigit() else item for item in row.split()]

matrix_poi = [process_poi_row(line) for line in lines[6:9]]

# Sección 3: matrix_goo (10 líneas)
matrix_goo = [list(map(int, line.split())) for line in lines[9:19]]

# Sección 4: matrix_doors (8 líneas)
matrix_doors = [list(map(int, line.split())) for line in lines[19:27]]

# Sección 5: matrix_entry_points (4 líneas)
matrix_entry_points = [list(map(int, line.split())) for line in lines[27:31]]

"""
# Imprimir los resultados para verificar
print("Walls Matrix:")
for row in matrix_walls:
    print(row)

print("\nPOI Matrix:")
for row in matrix_poi:
    print(row)

print("\nGoo Matrix:")
for row in matrix_goo:
    print(row)

print("\nDoors Matrix:")
for row in matrix_doors:
    print(row)

print("\nEntry Points Matrix:")
for row in matrix_entry_points:
    print(row)
"""


# Crear una instancia del modelo
model = ModeloEdificio(matrix_walls, matrix_poi, matrix_goo, matrix_doors, matrix_entry_points)

data = {
    "Walls": matrix_walls,
    "Poi": matrix_poi,
    "Goo": matrix_goo,
    "Doors": matrix_doors,
    "Entry_points": matrix_entry_points
}

json_data = json.dumps(data)

def get_matrixes():
    print("HEARD FROM LEHTAL COMPANY")
    print(json_data)
    return json_data

get_matrixes()