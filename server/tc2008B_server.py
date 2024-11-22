import json
import logging
from http.server import BaseHTTPRequestHandler, HTTPServer


class Server(BaseHTTPRequestHandler):
    def _set_response(self):
        self.send_response(200)
        self.send_header('Content-type', 'application/json')
        self.end_headers()

    def do_POST(self):
        content_length = int(self.headers['Content-Length'])
        post_data = self.rfile.read(content_length)
        logging.info(f"Received POST data: {post_data.decode('utf-8')}")

        # Procesar la posición recibida (si quieres puedes utilizarla)
        received_position = json.loads(post_data)
        print(f"Received Position: {received_position}")

        # Aquí podrías hacer un cálculo para una nueva posición
        new_position = {
            "x": received_position["x"] + 2.0,  # Ejemplo de cálculo
            "y": received_position["y"],
            "z": received_position["z"] + 2.0
        }

        # Enviar la nueva posición al cliente
        self._set_response()
        self.wfile.write(json.dumps(new_position).encode('utf-8'))

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
