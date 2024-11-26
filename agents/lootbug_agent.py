# -----------------------------------------------------------------------------------------------------------
# IMPORTS
# -----------------------------------------------------------------------------------------------------------

from mesa import Agent

# Importamos el siguiente paquete para manejar valores numéricos aleatorios.
import random

# -----------------------------------------------------------------------------------------------------------
# AGENTE
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