# -----------------------------------------------------------------------------------------------------------
# IMPORTS
# -----------------------------------------------------------------------------------------------------------

# pip install mesa==2.3.1
# pip install scipy

# Importamos las clases que se requieren para manejar los agentes (Agent) y su entorno (Model).
from mesa import Agent, Model

# Usamos ''MultiGrid'' para representar una cuadrícula donde cada celda puede contener como máximo un agente.
from mesa.space import MultiGrid

# ''DataCollector'' nos permite recolectar y almacenar datos en cada paso de la simulación para su análisis posterior.
from mesa.datacollection import DataCollector

# Con ''RandomActivation'', activamos a todos los agentes en cada paso.
from mesa.time import RandomActivation

# Importamos el siguiente paquete para el mejor manejo de valores numéricos.
import numpy as np

# Importamos el siguiente paquete para manejar valores numéricos aleatorios.
import random

# Importamos cityblock para calcular la distancia Manhattan entre dos puntos.
from scipy.spatial.distance import cityblock

import json

# -----------------------------------------------------------------------------------------------------------
# AGENTE EMPLEADO
# -----------------------------------------------------------------------------------------------------------

class EmployeeAgent(Agent):
    """Clase que representa a un empleado que salva víctimas en el edificio de Lethal Company."""

    def __init__(self, id, model):
        """
        Inicializa las propiedades del agente.
        - id: Identificador único del agente.
        - model: Referencia al modelo al que pertenece el agente.
        """
        super().__init__(id, model)
        self.ap = 4  # Puntos de acción disponibles
        self.carrying_victim = False  # Estado de transporte de víctima
        self.finished_turn = False  # Estado del turno del agente
        self.next_cell = None  # Próxima celda a la que se moverá el agente
        self.remaining_AP = 0 # Puntos de acción restantes

# -----------------------------------------------------------------------------------------------------------
# AGENTE LOOTBUG
# -----------------------------------------------------------------------------------------------------------

class LootBugAgent(Agent):
    """Clase que representa a un Lootbug. Este se encarga de mover a los POIs para elevar el juego."""
    def __init__(self, id, model):
        super().__init__(id, model)
        """
        Inicializa las propiedades del agente LootBug.
        - id: Identificador único del agente.
        - model: Referencia al modelo.
        """
        self.cargando_poi = False  # Indica si el agente está cargando un POI.
        self.poi_cargado = None  # Almacena el tipo de POI que lleva cargado.
        self.state = "capturar_poi"  # Estado inicial del agente.

# -----------------------------------------------------------------------------------------------------------
# GRIDS
# -----------------------------------------------------------------------------------------------------------

def get_grid_doors_entries(model):
    """Crea una representación de las puertas y puntos de entrada del edificio."""
    # Copiar la matriz base (puertas) para superponer información adicional
    combined_grid = np.copy(model.doors)

    # Superponer los puntos de entrada en la matriz combinada
    for x, y in model.entry_points:
        combined_grid[x, y] = max(combined_grid[x, y], 16)

    # Superponer el lootbug nest en la matriz combinada
    if hasattr(model, "lootbug_nest"):  # Verifica que el atributo lootbug_nest exista
        nest_x, nest_y = model.lootbug_nest
        combined_grid[nest_x, nest_y] = max(combined_grid[nest_x, nest_y], 32)  # Usamos 32 para lootbug nest

    return combined_grid

def get_grid_walls(model):
    """Crea una representación de las paredes del edificio."""
    grid = model.walls.copy()

    return grid

def get_grid_poi(model):
    """Crea una representación de los POI en el edificio."""
    grid = model.poi_placement.copy()

    return grid

def get_grid_threat_markers(model):
    """Crea una representación de los threath markers en el edificio."""
    grid = model.threat_markers.copy()

    return grid

def get_grid(model):
    """Crea una representación de los agentes en el edificio."""
    grid = np.zeros((model.grid.width, model.grid.height))

    # Iterar sobre todas las celdas del grid
    for content, (x, y) in model.grid.coord_iter():
        for agent in content:
            if isinstance(agent, EmployeeAgent):
                grid[x][y] = 6  # Representar el agente con el valor 6
            if isinstance(agent, LootBugAgent):
                grid[x][y] = 7  # Representar el agente con el valor 7


    return grid


# -----------------------------------------------------------------------------------------------------------
# MODELO
# -----------------------------------------------------------------------------------------------------------

class ModeloEdificio(Model):
    """Modelo del edificio Lethal company."""
    def __init__(self, wall_data, poi_data, goo_data, doors_data, entry_points_data):
        super().__init__()

        # Agentes
        self.turn = 0
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
        self.max_threat_markers = 32
        self.current_threat_markers = 0
        self.poi_total_count = 15
        self.poi_false_alarm = 5
        self.poi_real_victim  = 10
        self.poi_in_building = 0

        self.damage_counter = 24
        self.door_markers = 8
        self.start_point = 4
        self.saved_victims = 0
        self.lost_victims = 0
        self.collapsed_building = False
        self.lootbug_nest = (0,0)

        # Diccionario para rastrear estados de POIs ('closes' o 'open').
        self.poi_states = {}

        # Diccionario para rastrear estados de puertas.
        # Tuplas de posiciones adyacentes, y los valores son 'closed' o 'open'.
        self.door_states = {}

        # Diccionario para rastrear estados de paredes.
        # Clave: ((x1, y1), (x2, y2)), Valor: "okay", "damaged", "destroyed".
        self.wall_states = {}

        # Matrices para representar el estado del edificio.
        self.entry_points_location = np.zeros((self.height, self.width))

        # Paredes: cada celda contiene un entero donde cada bit representa una dirección:
        # Bit 0: Arriba, Bit 1: Derecha, Bit 2: Abajo, Bit 3: Izquierda
        self.walls = np.zeros((self.height, self.width), dtype=int)
        self.doors = np.zeros((self.height, self.width), dtype=int)

        # Matrices para objetos
        self.threat_markers = np.zeros((self.height, self.width)) # goo y droplets
        self.poi_placement = np.zeros((self.height, self.width)) # POIs

        # Colocar elementos iniciales
        self.place_walls(wall_data)    # Colocar paredes
        self.place_poi(poi_data)      # Colocar los POIs respetando el número por tipo
        self.place_goo(goo_data)      # Colocar el goo
        self.place_doors(doors_data)    # Colocar puertas
        self.place_start_point(entry_points_data)  # Colocar puntos de entradas

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
                "Edificio colapsado": lambda model: model.collapsed_building,
                "Victimas salvadas": lambda model: model.saved_victims,
                "Victimas perdidas": lambda model: model.lost_victims,
            }
        )

    def place_lootbug(self):
        """Coloca agentes en puntos de entrada seleccionados aleatoriamente."""
        for i in range(self.lootbug_agents):

            # Crear el agente
            agent = LootBugAgent(self.employee_agents, self)

            # Colocar el agente en el punto de entrada
            self.grid.place_agent(agent, self.lootbug_nest)

            # Agregar el agente al schedule
            self.schedule.add(agent)

    def place_employees(self):
        """Coloca agentes en puntos de entrada seleccionados aleatoriamente."""
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
        """Coloca paredes en posiciones específicas y asigna sus estados correctamente según la matriz dada."""
        for i in range(self.height):
            for j in range(self.width):
                wall_value = int(wall_data[i][j], 2)  # Convertir binario a entero
                self.walls[i, j] = wall_value  # Guardar el valor decimal de las paredes

                # Definir las paredes basadas en los bits (arriba, derecha, abajo, izquierda).
                if wall_value & 8:  # Pared arriba (bit 3)
                    if i > 0:
                        wall_pos = ((i, j), (i - 1, j))
                        self.wall_states[tuple(sorted(wall_pos))] = "okay"

                if wall_value & 4:  # Pared derecha (bit 2)
                    if j < self.width - 1:
                        wall_pos = ((i, j), (i, j + 1))
                        self.wall_states[tuple(sorted(wall_pos))] = "okay"

                if wall_value & 2:  # Pared abajo (bit 1)
                    if i < self.height - 1:
                        wall_pos = ((i, j), (i + 1, j))
                        self.wall_states[tuple(sorted(wall_pos))] = "okay"

                if wall_value & 1:  # Pared izquierda (bit 0)
                    if j > 0:
                        wall_pos = ((i, j), (i, j - 1))
                        self.wall_states[tuple(sorted(wall_pos))] = "okay"

    def place_poi(self, poi_data):
        """Coloca POIs en posiciones específicas respetando el número por tipo y actualiza las variables."""

        # Crear lista de tipos de POIs respetando los contadores.
        poi_types = (
            [3] * self.poi_false_alarm +  # POIs falsos
            [4] * self.poi_real_victim    # POIs reales
        )

        # Barajar aleatoriamente los tipos de POIs.
        self.random.shuffle(poi_types)

        # Seleccionar solo los necesarios para las posiciones disponibles.
        selected_poi_types = poi_types[:len(poi_data)]

        # Asignar tipos de POIs a las posiciones disponibles.
        for (x, y, poi_type), poi_val in zip(poi_data, selected_poi_types):
            # Ajustar índices para comenzar desde 0
            adjusted_x, adjusted_y = x - 1, y - 1

            # Validar que las posiciones ajustadas estén dentro de los límites.
            if 0 <= adjusted_x < self.height and 0 <= adjusted_y < self.width:
                # Determinar el valor de POI basado en tipo ('v' -> verdadero, 'f' -> falso).
                value = 4 if poi_type == 'v' else 3
                self.poi_placement[adjusted_x][adjusted_y] = value

                # Inicializar estado como "cerrado".
                self.poi_states[(adjusted_x, adjusted_y)] = "closed"
                self.poi_in_building += 1

    def print_poi_info(self):
        """Imprime información sobre los POIs restantes."""
        print(f"Falsas alarmas restantes: {self.poi_false_alarm}")
        print(f"Víctimas reales restantes: {self.poi_real_victim}")
        print(f"Total POIs restantes: {self.poi_total_count}")
        print(f"Total POIs en edificio: {self.poi_in_building}")

    def place_goo(self, goo_data):
        """Coloca goo en posiciones específicas basadas en los datos proporcionados con ajuste de índice."""
        for x, y in goo_data:
            # Ajustar índices para comenzar desde 0
            adjusted_x, adjusted_y = x - 1, y - 1

            # Asegúrate de que las posiciones ajustadas estén dentro de los límites.
            if 0 <= adjusted_x < self.height and 0 <= adjusted_y < self.width:
                if self.threat_markers[adjusted_x][adjusted_y] == 0:  # Solo colocar si la celda está vacía
                    self.threat_markers[adjusted_x][adjusted_y] = 2  # Representar el goo con un valor de 2
                    self.current_threat_markers += 1  # Incrementa el contador de fichas en el tablero

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

                # Crear una tupla de posiciones ordenadas
                pos1 = (adjusted_row1, adjusted_col1)
                pos2 = (adjusted_row2, adjusted_col2)
                door = tuple(sorted([pos1, pos2]))

                # Inicializar el estado de la puerta como "closed"
                self.door_states[door] = "closed"

    def print_door_info(self):
        """Imprime información sobre los estados de las puertas."""
        print("\nInformación de las puertas:")
        for door, state in self.door_states.items():
            print(f"Puerta {door} - Estado: {state}")


    def place_start_point(self, entry_points_data):
        """Coloca puntos de entrada en posiciones específicas y las guarda en una lista."""
        self.entry_points = []  # Lista para almacenar puntos de entrada
        for x, y in entry_points_data:
            # Ajustar índices para comenzar desde 0
            adjusted_x, adjusted_y = x - 1, y - 1
            # Asegúrate de que las posiciones ajustadas estén dentro de los límites
            if 0 <= adjusted_x < self.height and 0 <= adjusted_y < self.width:
                self.entry_points_location[adjusted_x][adjusted_y] = 16  # Representar el punto de entrada con un valor de 16
                self.entry_points.append((adjusted_x, adjusted_y))  # Agregar punto de entrada a la lista

    def replenish_pois(self):
        """Repone los POIs en el edificio."""
        print("\n[Replenish POIs] Iniciando reposición de POIs...")
        print(f"Estado inicial: POIs en edificio = {self.poi_in_building}, Falsas alarmas = {self.poi_false_alarm}, Víctimas reales = {self.poi_real_victim}, Total POIs restantes = {self.poi_total_count}")

        while self.poi_in_building < min(3, self.poi_total_count):
            # Verificar si quedan POIs disponibles
            if self.poi_false_alarm == 0 and self.poi_real_victim == 0:
                print("No quedan más POIs para colocar.")
                break

            # Elegir un espacio aleatorio en el grid
            x = self.random.randint(0, self.height - 1)
            y = self.random.randint(0, self.width - 1)
            print(f"Intentando colocar un POI en la celda: ({x}, {y})")

            # Verificar las condiciones para colocar un POI
            if self.poi_placement[x, y] == 0:
                print(f"La celda ({x}, {y}) está vacía. Continuando...")

                if self.threat_markers[x, y] in [2, 1]:
                    print(f"Marcador de amenaza detectado en ({x}, {y}). Eliminando marcador...")
                    self.threat_markers[x, y] = 0
                    self.current_threat_markers -= 1  # Decrementar el contador de threat markers
                    print(f"Marcadores de amenaza restantes: {self.current_threat_markers}")

                # Elegir el nuevo POI (aleatoriamente entre 3 y 4)
                poi_types = (
                    [3] * self.poi_false_alarm +  # POIs falsos
                    [4] * self.poi_real_victim    # POIs reales
                )

                # Barajar aleatoriamente los tipos de POIs
                new_poi = self.random.choice(poi_types)
                print(f"Nuevo POI seleccionado: {'Falsa Alarma' if new_poi == 3 else 'Víctima'}")

                # Verificar si hay un agente en la celda
                cell_agents = self.grid.get_cell_list_contents((x, y))
                employee_present = any(isinstance(agent, EmployeeAgent) for agent in cell_agents)

                if employee_present:
                    self.poi_placement[x, y] = new_poi
                    self.poi_in_building += 1
                    print(f"Agente presente en ({x}, {y}). Revelando POI inmediatamente...")
                    if new_poi == 3:  # Falsa alarma
                        print(f"POI en ({x}, {y}) era falsa alarma. Eliminando.")
                        self.poi_placement[x, y] = 0
                        self.poi_false_alarm -= 1
                        self.poi_total_count -= 1
                        self.poi_in_building -= 1
                    elif new_poi == 4:  # Víctima
                        print(f"POI en ({x}, {y}) es una víctima.")
                else:
                    # Colocar el nuevo POI en la celda
                    self.poi_placement[x, y] = new_poi
                    print(f"POI colocado en ({x}, {y}): {'Falsa Alarma' if new_poi == 3 else 'Víctima'}")
                    self.poi_in_building += 1

            else:
                print(f"La celda ({x}, {y}) ya tiene un POI. Intentando otra celda...")

        print(f"[Replenish POIs] Finalizado. Estado final: POIs en edificio = {self.poi_in_building}, Falsas alarmas = {self.poi_false_alarm}, Víctimas reales = {self.poi_real_victim}, Total POIs restantes = {self.poi_total_count}\n")

    def can_place_threat_marker(self):
        """
        Verifica si se puede colocar otro threat marker (goo o droplet) en el tablero.
        """
        return self.current_threat_markers <= self.max_threat_markers

    def update_wall_matrix(self, wall_position, state):
        """
        Actualiza la matriz de paredes del modelo para reflejar el estado actual.
        """
        (pos1, pos2) = wall_position
        x1, y1 = pos1
        x2, y2 = pos2

        # Imprimir la matriz de paredes antes de la actualización.
        print("\nMatriz self.walls antes de actualizar:")
        for row in self.walls:
            print(" ".join(format(cell, '04b') for cell in row))

        if state == "destroyed":
            # Determinar la dirección de la pared a eliminar
            if x1 == x2:  # Pared horizontal
                if y1 < y2:  # pos1 está a la izquierda de pos2
                    self.walls[x1, y1] &= ~4  # Elimina la pared derecha (bit 2) de pos1
                    self.walls[x2, y2] &= ~1  # Elimina la pared izquierda (bit 0) de pos2
                else:  # pos1 está a la derecha de pos2
                    self.walls[x1, y1] &= ~1  # Elimina la pared izquierda (bit 0) de pos1
                    self.walls[x2, y2] &= ~4  # Elimina la pared derecha (bit 2) de pos2
            elif y1 == y2:  # Pared vertical
                if x1 < x2:  # pos1 está arriba de pos2
                    self.walls[x1, y1] &= ~2  # Elimina la pared abajo (bit 1) de pos1
                    self.walls[x2, y2] &= ~8  # Elimina la pared arriba (bit 3) de pos2
                else:  # pos1 está abajo de pos2
                    self.walls[x1, y1] &= ~8  # Elimina la pared arriba (bit 3) de pos1
                    self.walls[x2, y2] &= ~2  # Elimina la pared abajo (bit 1) de pos2

        # Imprimir la matriz de paredes después de la actualización
        print("\nMatriz self.walls después de actualizar:")
        for row in self.walls:
            print(" ".join(format(cell, '04b') for cell in row))

    def update_door_matrix(self, door_position, state):
        """
        Actualiza la matriz de puertas del modelo para reflejar el estado actual.
        """
        (pos1, pos2) = door_position
        x1, y1 = pos1
        x2, y2 = pos2

        # Imprimir la matriz de puertas antes de la actualización
        print("\nMatriz self.doors antes de actualizar:")
        for row in self.doors:
            print(" ".join(format(cell, '04b') for cell in row))

        if state == "removed":
            # Determinar la dirección de la puerta a eliminar
            if x1 == x2:  # Puerta horizontal
                if y1 < y2:  # pos1 está a la izquierda de pos2
                    self.doors[x1, y1] &= ~4  # Elimina el bit derecho (bit 2) de pos1
                    self.doors[x2, y2] &= ~1  # Elimina el bit izquierdo (bit 0) de pos2
                else:  # pos1 está a la derecha de pos2
                    self.doors[x1, y1] &= ~1  # Elimina el bit izquierdo (bit 0) de pos1
                    self.doors[x2, y2] &= ~4  # Elimina el bit derecho (bit 2) de pos2
            elif y1 == y2:  # Puerta vertical
                if x1 < x2:  # pos1 está arriba de pos2
                    self.doors[x1, y1] &= ~2  # Elimina el bit abajo (bit 1) de pos1
                    self.doors[x2, y2] &= ~8  # Elimina el bit arriba (bit 3) de pos2
                else:  # pos1 está abajo de pos2
                    self.doors[x1, y1] &= ~8  # Elimina el bit arriba (bit 3) de pos1
                    self.doors[x2, y2] &= ~2  # Elimina el bit abajo (bit 1) de pos2

        # Imprimir la matriz de puertas después de la actualización
        print("\nMatriz self.doors después de actualizar:")
        for row in self.doors:
            print(" ".join(format(cell, '04b') for cell in row))

    def advance_goo(self):
        """Avanza la cantidad de los marcadores de riesgo."""

        # Verificar si quedan threat markers disponibles
        if not self.can_place_threat_marker():
            print("No se pueden avanzar más threat markers. Límite alcanzado.")
            return

        # Print the threat_markers matrix before advancing goo
        print("\nMatriz threat_markers antes de advance_goo:")
        for row in self.threat_markers:
            print(" ".join(map(str, row)))

        # Obtener una lista de todas las posiciones en threat_markers
        threat_markers_positions = [(x, y) for x in range(self.height) for y in range(self.width)]

        # Elegir una posición aleatoria
        target_position = self.random.choice(threat_markers_positions)
        x, y = target_position

        # Verificar si la celda es el lootbug nest (0, 0)
        if (x, y) == (0, 0):
            print(f"No se puede colocar goo ni droplets en el lootbug nest ({x}, {y}).")
            return

        # Verificar si la celda está vacía
        if self.threat_markers[x, y] == 0:

            # Obtener las celdas adyacentes (vecindad de Moore)
            neighbors = self.grid.get_neighborhood((x, y), moore=True, include_center=False)

            # Verificar si no hay goo en las celdas adyacentes
            no_goo_nearby = all(
                self.threat_markers[nx, ny] != 2 for nx, ny in neighbors if 0 <= nx < self.height and 0 <= ny < self.width
            )

            if no_goo_nearby:
                # Colocar droplet en la celda seleccionada
                self.threat_markers[x, y] = 1
                self.current_threat_markers += 1
                print(f"Advance: Se colocó un droplet en la celda ({x}, {y}).")
            elif not no_goo_nearby:
                # Colocar goo en la celda seleccionada
                self.threat_markers[x, y] = 2
                self.current_threat_markers += 1
                print(f"Advance: Se colocó goo en la celda ({x}, {y}).")

        # Si la celda ya contiene un droplet
        elif self.threat_markers[x, y] == 1:
            self.threat_markers[x, y] = 2  # Colocar goo
            print(f"Advance: El droplet en la celda ({x}, {y}) se convirtió en goo.")

        # Si la celda ya contiene goo
        elif self.threat_markers[x, y] == 2:
            print(f"Goo detectado en la celda ({x}, {y}). Llamando a la función de explosión...")
            self.explosion(x, y)

        # Print the threat_markers matrix after advancing goo
        print("\nMatriz threat_markers después de advance_goo / explosion:")
        for row in self.threat_markers:
            print(" ".join(map(str, row)))

    def explosion(self, x, y):
        """Genera explosión de goo, afectando celdas adyacentes."""

        # Verificar si quedan threat markers disponibles
        if not self.can_place_threat_marker():
            print("No se pueden avanzar más threat markers. Límite alcanzado.")
            return

        print(f"Explosión iniciada en la celda ({x}, {y}).")

        # Obtener las celdas adyacentes (vecindad de Von Neumann)
        neighbors = self.grid.get_neighborhood((x, y), moore=False, include_center=False)

        for nx, ny in neighbors:
            print(f"Evaluando celda adyacente ({nx}, {ny}).")
            # Verificar si la celda está dentro de los límites
            if 0 <= nx < self.height and 0 <= ny < self.width:

                # Verificar si es el lootbug nest
                if (nx, ny) == (0, 0):
                    print(f"No se puede propagar goo ni dañar paredes hacia el lootbug nest ({nx}, {ny}).")
                    continue

                # Eliminar puertas entre la celda actual y la vecina
                door = tuple(sorted([(x, y), (nx, ny)]))
                if door in self.door_states:
                    if self.door_states[door] == "closed":
                        print(f"Puerta cerrada entre ({x}, {y}) y ({nx}, {ny}). La explosión se detiene aquí.")
                        self.door_states[door] = "removed"
                        self.update_door_matrix(door, "removed")
                        continue # Detener la propagación en esta dirección
                    elif self.door_states[door] == "open":
                        print(f"Puerta abierta entre ({x}, {y}) y ({nx}, {ny}). Continuando explosión.")
                        self.door_states[door] = "removed"
                        self.update_door_matrix(door, "removed")

                # Identificar y dañar paredes entre la celda actual y la vecina
                wall = tuple(sorted([(x, y), (nx, ny)]))
                if wall in self.wall_states:
                    current_state = self.wall_states[wall]
                    if current_state == "okay":
                        self.wall_states[wall] = "damaged"
                        self.damage_counter -= 1
                        print(f" Explosion: Pared dañada entre ({x}, {y}) y ({nx}, {ny}).")
                        continue
                    elif current_state == "damaged":
                        self.wall_states[wall] = "destroyed"
                        self.damage_counter -= 1
                        print(f" Explosion: Pared destruida entre ({x}, {y}) y ({nx}, {ny}).")

                        # Actualizar la matriz de paredes para reflejar el estado destruido
                        self.update_wall_matrix(wall, "destroyed")
                        continue  # Detener la propagación en esta dirección


                # Si la celda contiene goo, comenzar shockwave.
                if self.threat_markers[nx, ny] == 2:
                    dx, dy = nx - x, ny - y  # Calcular la dirección
                    print(f"Goo detectado en la celda adyacente ({nx}, {ny}). Llamando a la función shockwave() en dirección ({dx}, {dy})...")
                    self.shockwave(nx, ny, direction=(dx, dy))
                    continue

                # Si la celda contiene un droplet, eliminarlo.
                if self.threat_markers[nx, ny] == 1:
                    self.threat_markers[nx, ny] = 0
                    self.current_threat_markers -= 1
                    print(f" Explosion: Droplet eliminado en la celda ({nx}, {ny}).")


                # Si la celda está vacía o contenía un droplet, colocar goo.
                if self.threat_markers[nx, ny] == 0:
                    self.threat_markers[nx, ny] = 2
                    self.current_threat_markers += 1
                    print(f" Explosion: Se colocó goo en la celda ({nx}, {ny}).")

    def shockwave(self, x, y, direction):
        """Propaga una onda expansiva desde la celda especificada."""

        # Verificar si quedan threat markers disponibles
        if not self.can_place_threat_marker():
            print("No se pueden avanzar más threat markers. Límite alcanzado.")
            return

        print(f"Shockwave iniciada en la celda ({x}, {y}) en dirección {direction}.")

        dx, dy = direction
        current_x, current_y = x, y
        while True:
            # Avanzar en la dirección especificada
            current_x += dx
            current_y += dy

            # Imprimir la celda actual por la que pasa la shockwave
            print(f"Shockwave pasando por la celda ({current_x}, {current_y}).")

            # Verificar si está fuera de los límites del grid
            if not (0 <= current_x < self.height and 0 <= current_y < self.width):
                break

            # Verificar si es el lootbug nest
            if (current_x, current_y) == (0, 0):
                print(f"La onda expansiva no afecta el lootbug nest ({current_x}, {current_y}).")
                break

            # Verificar si hay una puerta
            door = tuple(sorted([((current_x - dx), (current_y - dy)), (current_x, current_y)]))
            if door in self.door_states:
                if self.door_states[door] == "closed":
                    print(f" Shockwave: Puerta cerrada. Eliminando puerta entre {door[0]} y {door[1]}.")
                    self.door_states[door] = "removed"
                    self.update_door_matrix(door, "removed")
                    break
                elif self.door_states[door] == "open":
                    print(f" Shockwave: Puerta abierta. Eliminando puerta entre {door[0]} y {door[1]}.")
                    self.door_states[door] = "removed"
                    self.update_door_matrix(door, "removed")

            # Verificar si hay una pared y/o puerta
            wall = tuple(sorted([((current_x - dx), (current_y - dy)), (current_x, current_y)]))
            if wall in self.wall_states:
                current_state = self.wall_states[wall]
                if current_state == "okay":
                    self.wall_states[wall] = "damaged"
                    self.damage_counter -= 1
                    print(f" Shockwave: Pared dañada entre ({current_x - dx}, {current_y - dy}) y ({current_x}, {current_y}).")
                    break  # Detener la propagación en esta dirección
                elif current_state == "damaged":
                    self.wall_states[wall] = "destroyed"
                    self.damage_counter -= 1
                    print(f" Shockwave: Pared destruida entre ({current_x - dx}, {current_y - dy}) y ({current_x}, {current_y}).")
                    self.update_wall_matrix(wall, "destroyed")
                    break  # La onda no puede pasar paredes destruidas

            # Verificar si es una celda con droplets
            if self.threat_markers[current_x, current_y] == 1:
                print(f" Shockwave: Goo colocado en celda con droplets ({current_x}, {current_y}).")
                self.threat_markers[current_x, current_y] = 2
                break

            # Si la celda está vacía, colocar goo
            if self.threat_markers[current_x, current_y] == 0:
                print(f" Shockwave: Goo colocado en celda vacía ({current_x}, {current_y}).")
                self.threat_markers[current_x, current_y] = 2
                self.current_threat_markers += 1
                break

        # Print the threat_markers matrix after advancing goo
        print("\nMatriz threat_markers después de shockwave:")
        for row in self.threat_markers:
            print(" ".join(map(str, row)))

    def check_secondary_effects(self):
        """Revisar los efectos secundarios después del avance de goo."""
        print("Revisando efectos secundarios en el threat grid...")

        # Iterar por todas las celdas de la cuadrícula
        for x in range(self.height):
            for y in range(self.width):
                # Verificar si la celda contiene un droplet
                if self.threat_markers[x, y] == 1:
                    # Obtener las celdas adyacentes (vecindad de Moore)
                    neighbors = self.grid.get_neighborhood((x, y), moore=True, include_center=False)

                    # Verificar si alguna celda adyacente contiene goo
                    goo_nearby = any(
                        self.threat_markers[nx, ny] == 2
                        for nx, ny in neighbors
                        if 0 <= nx < self.height and 0 <= ny < self.width
                    )

                    if goo_nearby:
                        # Cambiar el droplet por goo
                        self.threat_markers[x, y] = 0
                        self.threat_markers[x, y] = 2
                        print(f"Droplet en ({x}, {y}) convertido en goo debido a proximidad.")

    def end_game(self):
        """Verifica las condiciones de victoria o derrota y detiene la simulación si es necesario."""

        # Evitar múltiples llamadas si el juego ya terminó
        if not self.running:
            return

        if self.damage_counter == 0:
            self.collapsed_building = True

        if self.saved_victims == 7:
            print(f"¡Victoria! Se salvaron {self.saved_victims} víctimas en {self.steps} pasos.")
            self.running = False

        elif self.damage_counter == 0 or self.lost_victims == 4:
            if self.damage_counter == 0:
                print(f"¡Derrota! El edificio colapsó después de {self.steps} pasos.")
            elif self.lost_victims == 4:
                print(f"¡Derrota! Se perdieron {self.lost_victims} víctimas en {self.steps} pasos.")
            elif self.poi_total_count == 0:
                print(f"No se logró alcanzar la meta. Se salvaron {self.saved_victims} y se perdieron {self.lost_victims}.")
            self.running = False

    def to_json(self):
            """
            Serializes the current model state, including all grids as layers.
            """
            return json.dumps({
                "doors_entries": get_grid_doors_entries(self).tolist(),
                "walls": get_grid_walls(self).tolist(),
                "poi": get_grid_poi(self).tolist(),
                "threat_markers": get_grid_threat_markers(self).tolist(),
                "agents": get_grid(self).tolist(),
                "steps": self.steps,
                "saved_victims": self.saved_victims,
                "lost_victims": self.lost_victims,
                "collapsed_building": self.collapsed_building
            })

    def step(self):
        """
        Ejecuta un paso en la simulación si esta está en ejecución.
        - Incrementa el contador de pasos.
        - Recolecta datos del estado actual de la simulación.
        - Avanza el estado de los agentes programados en el modelo.
        """
        if self.running:
            self.steps += 1
            self.schedule.step()
            self.datacollector.collect(self)

            # Return the current state as JSON
            return self.to_json()

# -----------------------------------------------------------------------------------------------------------
# INICIALIZAR
# -----------------------------------------------------------------------------------------------------------

# Leer el archivo testCase.txt
with open("testCase/testCase.txt") as file:
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

# Crear una instancia del modelo
model = ModeloEdificio(matrix_walls, matrix_poi, matrix_goo, matrix_doors, matrix_entry_points)