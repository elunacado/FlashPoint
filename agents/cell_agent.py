from mesa import Agent

class CellAgent(Agent):
    def __init__(self, pos, model, raw_walls):
        super().__init__(pos, model)
        self.pos = pos
        self.state = "clear"  # Contenido de la celda
        self.walls = self.parse_walls(raw_walls)  # Extraer las paredes de la celda

    #We process the walls info
    def parse_walls(self, walls_code):
        walls = {
            "top": walls_code[0] == "1",
            "left": walls_code[1] == "1",
            "bottom": walls_code[2] == "1",
            "right": walls_code[3] == "1",
        }
        return walls
