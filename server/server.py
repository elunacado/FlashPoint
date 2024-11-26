from http.server import BaseHTTPRequestHandler, HTTPServer
import logging
import json
import os

# Ruta al archivo JSON con los datos de simulación
JSON_FILE = '../simulation_data/simulation_output.json'

class SimulationServer(BaseHTTPRequestHandler):

    # Cargar datos del JSON al iniciar el servidor
    def load_simulation_data(self):
        if os.path.exists(JSON_FILE):
            with open(JSON_FILE, 'r') as file:
                return json.load(file).get('simulation_data', [])
        else:
            logging.error(f"Archivo JSON no encontrado: {JSON_FILE}")
            return []

    # Establecer cabeceras de respuesta
    def _set_response(self, content_type='application/json'):
        self.send_response(200)
        self.send_header('Content-type', content_type)
        self.end_headers()

    # Manejar solicitudes GET (puede usarse para debug)
    def do_GET(self):
        self._set_response('text/html')
        self.wfile.write("Servidor activo y esperando datos.".encode('utf-8'))

    # Manejar solicitudes POST (envío de pasos de la simulación)
    def do_POST(self):
        try:
            # Cargar los datos de la simulación
            simulation_data = self.load_simulation_data()
            
            # Extraer el paso desde los parámetros de la solicitud
            content_length = int(self.headers['Content-Length'])
            post_data = self.rfile.read(content_length)
            request_body = json.loads(post_data.decode('utf-8'))

            # Validar el paso solicitado
            step = request_body.get('step', 1) - 1  # Paso base 1 (convertido a índice)
            if 0 <= step < len(simulation_data):
                response_data = simulation_data[step]
                self._set_response()
                self.wfile.write(json.dumps(response_data).encode('utf-8'))
            else:
                self.send_response(404)
                self.wfile.write(json.dumps({"error": "Step no encontrado"}).encode('utf-8'))
        except Exception as e:
            logging.error(f"Error procesando solicitud POST: {e}")
            self.send_response(500)
            self.wfile.write(json.dumps({"error": "Error interno del servidor"}).encode('utf-8'))


def run(server_class=HTTPServer, handler_class=SimulationServer, port=8585):
    logging.basicConfig(level=logging.INFO)
    server_address = ('', port)
    httpd = server_class(server_address, handler_class)
    logging.info("Servidor iniciado en el puerto %d...\n", port)
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        pass
    httpd.server_close()
    logging.info("Servidor detenido.\n")


if __name__ == '__main__':
    from sys import argv

    if len(argv) == 2:
        run(port=int(argv[1]))
    else:
        run()
