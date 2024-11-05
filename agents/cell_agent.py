from mesa import Agent

class CellAgent(Agent):
    STATES = {
            0: "clear",
            1: "droplets",
            2: "gooed",
    }
        

    def __init__(self, pos, model, raw_walls):
        super().__init__(pos, model)
        self.pos = pos
        self.walls = self.parse_walls(raw_walls)  # Extraer las paredes de la celda
        self.state = CellAgent.STATES[0]

    #We process the walls info
    def parse_walls(self, raw_walls):
        WALLS = {
            "top": raw_walls[0] == "1",
            "left": raw_walls[1] == "1",
            "bottom": raw_walls[2] == "1",
            "right": raw_walls[3] == "1",
        }
        return WALLS

    def set_state(self, state_key):
        if state_key in CellAgent.STATES:
            self.state = CellAgent.STATES[state_key]
        else:
            raise ValueError("Estado no válido")
