# -----------------------------------------------------------------------------------------------------------
# IMPORTS
# -----------------------------------------------------------------------------------------------------------

from mesa import Agent

# Importamos cityblock para calcular la distancia Manhattan entre dos puntos.
from scipy.spatial.distance import cityblock

# -----------------------------------------------------------------------------------------------------------
# IMPORTS
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