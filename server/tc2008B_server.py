# TC2008B Modelación de Sistemas Multiagentes con gráficas computacionales
# Python server to interact with Unity via POST
# Sergio Ruiz-Loza, Ph.D. March 2021

from http.server import BaseHTTPRequestHandler, HTTPServer
import logging
import json
import sys
import os

# Añadir el directorio de LethalCompany.py al sys.path si no están en el mismo directorio
sys.path.append(os.path.dirname(__file__))

# Importar LethalCompany
import LethalCompany

class Server(BaseHTTPRequestHandler):
    
    def _set_response(self):
        self.send_response(200)
        self.send_header('Content-type', 'application/json')
        self.end_headers()
        
    def do_GET(self):
        self._set_response()
        self.wfile.write("GET request for {}".format(self.path).encode('utf-8'))

    def do_POST(self):
        content_length = int(self.headers['Content-Length'])
        post_data = self.rfile.read(content_length)
        logging.info(f"Received POST data: {post_data.decode('utf-8')}")

        # Inicializar el modelo si no está inicializado
        if LethalCompany.modelo is None:
            LethalCompany.initialize_model()

        # Llamar a la función run_model_and_save_to_json de LethalCompany
        json_data = LethalCompany.run_model_and_save_to_json(steps=1000, model_instance=LethalCompany.modelo, output_file="simulation_output.json")
        
        # Enviar la respuesta
        self._set_response()
        self.wfile.write(json.dumps(json_data).encode('utf-8'))

def run(server_class=HTTPServer, handler_class=Server, port=8585):
    logging.basicConfig(level=logging.INFO)
    server_address = ('', port)
    httpd = server_class(server_address, handler_class)
    logging.info("Starting httpd...\n")
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        pass
    httpd.server_close()
    logging.info("Stopping httpd...\n")

if __name__ == "__main__":
    run()