from mesa import Agent
from poi_agent import POIAgent
from cell_agent import CellAgent

class EmployeeAgent(Agent):
    MAX_ACTIONS = 4
    STORED_ENERGY = 4
    MAX_ENERGY = 8

    def __init__(self, id, model, position, entry_position):
        super().__init__(id, model)
        self.position = position #Donde esta?
        self.entry_position = entry_position
        self.actions = EmployeeAgent.STORED_ENERGY #Con cuanta energia cuento
        self.salvaged = 0 #Cuanta scrap rescato el empleado?
        self.carrying_scrap = False
        self.energy = 0

    def move(self, new_position):
        if self.actions > 0:
            self.model.grid.move_agent(self,new_position)
            self.position = new_position
            self.actions -= 1
            print(f"{self} moved to {self.position}. Actions left f{self.actions}")
            self.interact_with_poi()
            self.inteact_with_cell()
        else:
            print("No actions left")

    def stall(self):
        if self.energy < EmployeeAgent.MAX_ENERGY:
            self.energy += 1
            print(f"{self} is staying idle. Energy accumulated: {self.energy}.")
        else:
            print(f"{self} has reached maximum energy: {self.energy}.")


    def interact_with_poi(self):
        cell_agents = self.model.grid.get_cells_contents([self.position])
        for agent in cell_agents:
            if isinstance(agent, POIAgent):
                if agent.type == "hidden":
                    print(f"{self} is revealing the POI at {self.position}.")
                    agent.reveal(self)
                elif agent.type == "scrap" and not agent.picked_up:
                    print(f"{self} is picking up the scrap at {self.position}.")
                    agent.pick_up()
                    self.salvaged += 1
                    self.carrying_scrap = True
                elif agent.type == "mine":
                    print(f"{self} triggered a mine at {self.position}.")
                    agent.blow_up(self)
                elif agent.type == "false_alarm":
                    print(f"{self} encountered a false alarm you fool")

    def interact_with_cell(self):
        cell_agents = self.model.grid.get_cell_list_contents([self.position])

        for agent in cell_agents:
            if isinstance(agent, CellAgent):
                # Verificar si la celda tiene "gooed" o está marcada como peligrosa
                if agent.state == "gooed":
                    print(f"{self} encountered deadly goo at {self.position}!")
                    agent.swallowed_by_the_goo(self)
                
                elif agent.state == "droplets":
                    print(f"{self} encountered droplets at {self.position}.")
                elif agent.state == "clear":
                    print(f"{self} is on a clear cell at {self.position}.")


    def reset_actions(self):
        self.actions = EmployeeAgent.MAX_ACTIONS
        print(f"{self}'s actions have been reset to {self.actions}.")

