import numpy as np
from mesa import Agent
import random

class EmployeeAgent(Agent):
    MAX_ACTIONS = 8
    STRUCTURE_LP = 24

    CELL_STATES = {
        0: "clear",
        1: "droplets",
        2: "gooed",
        3: "rescue_point",
    }

    DOOR_STATES = {
        0: "closed",
        1: "open",
        2: "broken",
    }

    POI_TYPES = {
        0: "hidden",
        1: "scrap",
        2: "false_alarm",
        3: "mine",
    }

    def __init__(self, id, model, position, entry_position, raw_walls, poi_type=None):
        super().__init__(id, model)
        self.position = position
        self.entry_position = entry_position
        self.actions = 4
        self.salvaged = 0
        self.carrying_scrap = False
        self.energy_storage = 0  # Energy storage for additional energy next turn
        self.cells_visited = 0
        
        # Cell attributes
        self.walls = self.parse_walls(raw_walls)
        self.state = EmployeeAgent.CELL_STATES[0]
        
        # POI attributes
        self.poi_type = EmployeeAgent.POI_TYPES.get(poi_type, "hidden")
        self.picked_up = False
        self.revealed = False

    #--START OF THE TURN METHODS--
    def reset_actions(self):
        # Set initial energy for the next turn including stored energy, capped by MAX_ACTIONS
        self.energy = min(4 + self.energy_storage, EmployeeAgent.MAX_ACTIONS)
        self.energy_storage = 0
        print(f"{self}'s energy reset to {self.energy} with stored energy cleared.")

    def parse_walls(self, raw_walls):
        return {
            "top": raw_walls[0] == "1",
            "left": raw_walls[1] == "1",
            "bottom": raw_walls[2] == "1",
            "right": raw_walls[3] == "1",
        }

    def get_direction(self, old_pos, new_pos):
        delta = (new_pos[0] - old_pos[0], new_pos[1] - old_pos[1])
        if delta == (1, 0): return "top"      # Aumento en Y
        if delta == (-1, 0): return "bottom"  # Disminución en Y
        if delta == (0, -1): return "left"    # Disminución en X
        if delta == (0, 1): return "right"    # Aumento en X
        return None

    #MOVEMENT OF THE EMPLOYEE
    def move(self):
        possible_steps = self.model.grid.get_neighborhood(self.position, moore=False, include_center=False)
        np.random.shuffle(possible_steps)  # Mezclar las posibles posiciones para agregar aleatoriedad

        for step in possible_steps:
            direction = self.get_direction(self.position, step)

            # Revisar si hay una pared o puerta en la dirección del paso
            if direction and (self.walls.get(direction, False) or self.model.door_states.get(direction) not in ["open", "broken"]):
                continue

            # Revisar el estado de la celda de destino
            cell_contents = self.model.grid.get_cell_list_contents(step)
            cell_state = cell_contents[0].state if cell_contents else "clear"

            # Determinar el costo de energía basado en el estado de la celda
            energy_cost = 2 if cell_state == "gooed" else 1

            # Moverse solo si tiene suficiente energía para no terminar en 0 al entrar en una celda "gooed"
            if self.energy >= energy_cost and not (cell_state == "gooed" and self.energy == energy_cost):
                self.model.grid.move_agent(self, step)
                self.energy -= energy_cost
                self.cells_visited += 1
                print(f"{self} moved to {step}, remaining energy: {self.energy}")
                return  # Terminar el turno después de moverse exitosamente
            elif cell_state == "gooed" and self.energy == energy_cost:
                print(f"{self} cannot move to {step} because moving there would leave energy at 0.")
            else:
                print(f"{self} lacks energy to move to {step} with cost {energy_cost}.")

        # Si no se encuentra un movimiento adecuado, el empleado se queda en su lugar
        print(f"{self} could not find a suitable cell to move to and stays at {self.position}.")

    # -- Employee stalls to get AP for next turns
    def stall(self):
        # Accumulate energy to the storage for next turn
        if self.energy < EmployeeAgent.MAX_ACTIONS:
            self.energy += 1
            print(f"{self} is staying idle. Energy accumulated: {self.energy}.")
        else:
            self.energy_storage = min(self.energy_storage + 1, EmployeeAgent.MAX_ACTIONS - self.energy)
            print(f"{self} stored extra energy for next turn. Stored energy: {self.energy_storage}.")

    # POI Interaction methods
    def interact_with_poi(self):
        if self.poi_type == "hidden" and not self.revealed:
            available_types = [
                t for t, count in self.model.poi_pool.items() if count > 0
            ]
            if available_types:
                new_type = self.random.choice(available_types)
                self.poi_type = EmployeeAgent.POI_TYPES[new_type]
                self.model.poi_pool[new_type] -= 1
                self.revealed = True
                print(f"The POI at {self.position} has been revealed as {self.poi_type}.")
            else:
                print("No POI types available to reveal.")
        elif self.poi_type == "scrap" and not self.picked_up:
            self.pick_up_scrap()
        elif self.poi_type == "mine":
            self.trigger_mine()
        elif self.poi_type == "false_alarm":
            print(f"{self} encountered a false alarm at {self.position}.")

    def pick_up_scrap(self):
        self.picked_up = True
        self.salvaged += 1
        self.carrying_scrap = True
        print(f"{self} picked up scrap at {self.position}.")
        self.remove_from_model()

    # Cell interaction methods
    def interact_with_cell(self):
        if self.state == "gooed":
            print(f"{self} encountered deadly goo at {self.position}!")
        elif self.state == "droplets":
            print(f"{self} encountered droplets at {self.position}.")
        elif self.state == "clear":
            print(f"{self} is on a clear cell at {self.position}.")

    def return_to_random_entry(self):
        # Selecciona una posición de entrada aleatoria
        target_entry = random.choice(self.model.entry_positions)
        print(f"{self} is returning to a random entry at {target_entry}.")
        
        while self.position != target_entry:
            # Calcula la dirección hacia la entrada objetivo
            dx = target_entry[0] - self.position[0]
            dy = target_entry[1] - self.position[1]
            
            # Determina el siguiente paso en la dirección hacia la entrada
            if abs(dx) > abs(dy):  # Prioriza movimiento en eje x
                step = (self.position[0] + (1 if dx > 0 else -1), self.position[1])
            else:  # Movimiento en eje y
                step = (self.position[0], self.position[1] + (1 if dy > 0 else -1))
            
            # Calcula el costo de energía y verifica si puede moverse
            cell_contents = self.model.grid.get_cell_list_contents(step)
            cell_state = cell_contents[0].state if cell_contents else "clear"
            energy_cost = 2 if cell_state == "gooed" else 1

            if self.energy >= energy_cost:
                self.model.grid.move_agent(self, step)
                self.energy -= energy_cost
                print(f"{self} moved to {step}, remaining energy: {self.energy}")
            else:
                print(f"{self} does not have enough energy to move towards the entry.")
                break

    #Employee opening doors
    def interact_with_door(self, direction):
        # Verificar que el agente tenga suficiente energía
        if self.energy < 1:
            print(f"{self} does not have enough energy to open the door in the {direction} direction.")
            return

        # Verificar el estado de la puerta en la dirección especificada
        if self.model.door_states.get(direction) == "closed":
            # Abrir la puerta y reducir la energía del agente
            self.model.door_states[direction] = "open"
            self.energy -= 1
            print(f"{self} opened the door in the {direction} direction. Remaining energy: {self.energy}")
        elif self.model.door_states.get(direction) == "open":
            self.model.door_states[direction] = "closed"
            self.energy -= 1
            print(f"{self} closed the door in the {direction} direction. Remaining energy: {self.energy}")
        else:
            print(f"The door in the {direction} direction is not closed, so {self} cannot open it.")

    
    #Employee interaction with the goo
    def reduce_goo(self):
        # Lista de posiciones ortogonales (arriba, abajo, izquierda, derecha)
        orthogonal_positions = [
            (self.position[0] - 1, self.position[1]),  # Arriba
            (self.position[0] + 1, self.position[1]),  # Abajo
            (self.position[0], self.position[1] - 1),  # Izquierda
            (self.position[0], self.position[1] + 1)   # Derecha
        ]
    
        # Incluir la posición actual del agente
        orthogonal_positions.append(self.position)
    
        for pos in orthogonal_positions:
            # Verificar si la posición está dentro de los límites de la cuadrícula
            if self.model.grid.out_of_bounds(pos):
                continue
    
            # Obtener el contenido de la celda en la posición actual
            cell_contents = self.model.grid.get_cell_list_contents(pos)
    
            for agent in cell_contents:
                if agent.state == "gooed":
                    agent.set_state(1)  # Reducir a "droplets"
                    print(f"Goo reduced to droplets at {pos}")
                elif agent.state == "droplets":
                    agent.set_state(0)  # Limpiar la celda
                    print(f"Goo cleared at {pos}")

    def set_state(self, state_key):
        if state_key in EmployeeAgent.CELL_STATES:
            self.state = EmployeeAgent.CELL_STATES[state_key]
        else:
            raise ValueError("Invalid state key")



    #---WAYS FOR THE EMPLOYEE TO "DIE"-----
    def remove_from_model(self):
        self.model.grid.remove_agent(self)
        self.model.schedule.remove(self)

    def trigger_mine(self):
        self.position = self.entry_position
        print("BOOM! Mine triggered. Reset to entry position.")
        self.remove_from_model()

    def swallowed_by_the_goo(self):
        self.position = self.entry_position
        print("GULP! Swallowed by the goo and reset to entry position.")


