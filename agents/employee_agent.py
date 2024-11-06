from mesa import Agent

class EmployeeAgent(Agent):
    MAX_ACTIONS = 4
    STORED_ENERGY = 4
    MAX_ENERGY = 8
    
    STATES = {
        0: "clear",
        1: "droplets",
        2: "gooed",
    }
    
    POI_TYPES = {
        0: "hidden",
        1: "scrap",
        2: "false_alarm",
        3: "mine",
    }

    def __init__(self, id, model, position, entry_position, raw_walls, poi_type=None):
        super().__init__(id, model)
        # Employee attributes
        self.position = position
        self.entry_position = entry_position
        self.actions = EmployeeAgent.STORED_ENERGY
        self.salvaged = 0
        self.carrying_scrap = False
        self.energy = 0
        
        # Cell attributes
        self.walls = self.parse_walls(raw_walls)
        self.state = EmployeeAgent.STATES[0]
        
        # POI attributes
        self.poi_type = EmployeeAgent.POI_TYPES.get(poi_type, "hidden")
        self.picked_up = False
        self.revealed = False

    def parse_walls(self, raw_walls):
        return {
            "top": raw_walls[0] == "1",
            "left": raw_walls[1] == "1",
            "bottom": raw_walls[2] == "1",
            "right": raw_walls[3] == "1",
        }

    def move(self, new_position):
        if self.actions > 0:
            self.model.grid.move_agent(self, new_position)
            self.position = new_position
            self.actions -= 1
            print(f"{self} moved to {self.position}. Actions left: {self.actions}")
            self.interact_with_poi()
            self.interact_with_cell()
        else:
            print("No actions left")

    def stall(self):
        if self.energy < EmployeeAgent.MAX_ENERGY:
            self.energy += 1
            print(f"{self} is staying idle. Energy accumulated: {self.energy}.")
        else:
            print(f"{self} has reached maximum energy: {self.energy}.")

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

    def trigger_mine(self):
        self.position = self.entry_position
        print("BOOM! Mine triggered. Reset to entry position.")
        self.remove_from_model()

    # Cell interaction methods
    def interact_with_cell(self):
        if self.state == "gooed":
            print(f"{self} encountered deadly goo at {self.position}!")
            self.swallowed_by_the_goo()
        elif self.state == "droplets":
            print(f"{self} encountered droplets at {self.position}.")
        elif self.state == "clear":
            print(f"{self} is on a clear cell at {self.position}.")

    def swallowed_by_the_goo(self):
        self.position = self.entry_position
        print("GULP! Swallowed by the goo and reset to entry position.")

    def reduce_goo(self):
        if self.state == "gooed":
            self.set_state(1)  # Reducir a "droplets"
            print(f"Goo reduced to droplets at {self.position}")
        elif self.state == "droplets":
            self.set_state(0)  # Limpiar la celda
            print(f"Goo cleared at {self.position}")

    def set_state(self, state_key):
        if state_key in EmployeeAgent.STATES:
            self.state = EmployeeAgent.STATES[state_key]
        else:
            raise ValueError("Invalid state key")

    # Reset actions for new turn
    def reset_actions(self):
        self.actions = EmployeeAgent.MAX_ACTIONS
        print(f"{self}'s actions have been reset to {self.actions}.")

    def remove_from_model(self):
        self.model.grid.remove_agent(self)
        self.model.schedule.remove(self)