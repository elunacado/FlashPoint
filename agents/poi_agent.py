from mesa import Agent

class POIAgent(Agent):
    TYPES = {
        0: "hidden",
        1: "scrap",
        2: "false_alarm",
        3: "mine",
    }

    def __init__(self, id, model, position, poi_type):
        super().__init__(id, model)
        self.position = position
        self.type = POIAgent.TYPES.get(poi_type, "hidden")
        self.picked_up = False
        self.revealed = False

    def reveal(self, employee):
        if not self.revealed and self.type == "hidden":
            available_types = [
                t for t, count in self.model.poi_pool.items() if count > 0
            ]
            if available_types:
                new_type = self.random.choice(available_types)
                self.type = POIAgent.TYPES[new_type]
                self.model.poi_pool[new_type] -= 1
                self.revealed = True
                print(f"The POI at {self.position} has been revealed as {self.type}.")
            else:
                print("No POI types available to reveal.")
        elif self.revealed:
            print(f"The POI at {self.position} has already been revealed as {self.type}.")

    def blow_up(self, employee):
        entrance_position = self.model.entrance_position
        employee.position = entrance_position
        print("BOOM!")
        self.remove_from_model()

    def pick_up(self):
        if self.type == "scrap" and self.picked_up:
            self.picked_up = True
            print(f"The POI in {self.position} was picked up")
            self.remove_from_model()

    def remove_from_model(self):
        self.model.grid.remove_agent(self)
        self.model.schedule.remove(self)
