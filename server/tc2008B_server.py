import json
import logging
from http.server import BaseHTTPRequestHandler, HTTPServer
import LethalCompany

class Server(BaseHTTPRequestHandler):
    def _set_response(self):
        self.send_response(200)
        self.send_header('Content-type', 'application/json')
        self.end_headers()

    def do_POST(self):
        try:
            content_length = int(self.headers['Content-Length'])
            post_data = self.rfile.read(content_length)

            # Parse the JSON data (if needed)
            json_data = LethalCompany.get_matrixes()  # Modify this based on what you need

            # Send the response with the JSON data
            self._set_response()
            self.wfile.write(json.dumps(json_data).encode('utf-8'))

        except Exception as e:
            logging.error(f"Error processing POST request: {str(e)}")
            self.send_response(400)  # Bad Request if something goes wrong
            self.end_headers()
            self.wfile.write(json.dumps({"error": "Bad Request"}).encode('utf-8'))

def run(server_class=HTTPServer, handler_class=Server, port=8585):
    logging.basicConfig(level=logging.INFO)
    server_address = ('', port)
    httpd = server_class(server_address, handler_class)
    logging.info(f"Starting httpd on port {port}...\n")
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        pass
    httpd.server_close()
    logging.info("Stopping httpd...\n")

if __name__ == "__main__":
    run()
