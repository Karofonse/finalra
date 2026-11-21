#!/usr/bin/env python3
"""
Simulador del ESP32 para probar la app de Unity sin hardware.

Responde igual que los dos firmware del proyecto:
    GET /state                     (Actividad 1)
    GET /led?id=1&state=toggle     (Actividad 1)
    GET /voltages                  (Actividad 2)

Ademas sirve en  http://localhost:8080/  el panel del "circuito virtual"
(panel.html): el circuito en 3D interactivo (se gira el potenciometro y se
presiona el pulsador), las lecturas en vivo y el marcador de Vuforia con la
protoboard dibujada a escala. Al apuntar la app de AR al marcador, el modelo 3D
de la app queda encima de ese dibujo.

Uso:
    python simulador_esp32.py            # puerto 8080
    python simulador_esp32.py 9000       # otro puerto

En Unity, escribir en el campo IP:   127.0.0.1:8080
Desde el telefono (misma red Wi-Fi): IP_DEL_COMPUTADOR:8080

Solo usa la biblioteca estandar de Python 3 (no hay que instalar nada).
"""

import json
import os
import random
import socket
import sys
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlparse

INICIO = time.time()
CARPETA = os.path.dirname(os.path.abspath(__file__))
RUTA_MARCADOR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "marcador", "marcador_ra.jpg")
CANDADO = threading.Lock()

# Estado del "circuito"
estado = {
    "pot": 50.0,      # potenciometro de la Actividad 1, en %
    "button": False,  # pulsador
    "led1": False,
    "led2": False,
    "n3": 50.0,       # potenciometro de la Actividad 2 (nodo N3), en %
}


def uptime_ms():
    return int((time.time() - INICIO) * 1000)


def json_estado():
    """Misma forma que /state del firmware de la Actividad 1."""
    with CANDADO:
        # Ruido pequeno, como el de un ADC real
        porcentaje = min(100.0, max(0.0, estado["pot"] + random.uniform(-0.3, 0.3)))
        crudo = int(round(porcentaje / 100.0 * 4095))
        return {
            "analog": crudo,
            "percent": round(porcentaje, 1),
            "volts": round(crudo * 3.3 / 4095.0, 2),
            "button": estado["button"],
            "led1": estado["led1"],
            "led2": estado["led2"],
            "uptime": uptime_ms(),
        }


def json_voltajes():
    """Misma forma que /voltages del firmware de la Actividad 2."""
    with CANDADO:
        n3 = estado["n3"] / 100.0 * 3300.0
    ruido = lambda: random.uniform(-8, 8)   # +/- 8 mV
    return {
        "nodes": [
            {"id": "N1", "mv": round(2200 + ruido())},
            {"id": "N2", "mv": round(1100 + ruido())},
            {"id": "N3", "mv": round(min(3300, max(0, n3 + ruido())))},
        ],
        "uptime": uptime_ms(),
    }


# Actividad de la app de AR (consultas a /state, /voltages y /led), para mostrarla en el panel
consultas = {"tiempos": [], "ruta": "", "cliente": "", "total": 0}
eventos = []   # ultimos comandos recibidos de la app


def registrar_consulta(ruta, cliente):
    ahora = time.time()
    with CANDADO:
        t = consultas["tiempos"]
        t.append(ahora)
        while t and ahora - t[0] > 2.0:
            t.pop(0)
        consultas["ruta"] = ruta
        consultas["cliente"] = cliente
        consultas["total"] += 1


def json_panel(puerto):
    """Todo lo que necesita el panel web en una sola consulta."""
    datos = {"voltajes": json_voltajes(), "lectura": json_estado()}
    ahora = time.time()
    with CANDADO:
        datos.update(estado)
        t = [x for x in consultas["tiempos"] if ahora - x <= 2.0]
        datos["app"] = {
            "conectada": bool(t) and ahora - t[-1] < 1.5,
            "hz": round(len(t) / 2.0, 1),
            "ruta": consultas["ruta"],
            "cliente": consultas["cliente"],
            "total": consultas["total"],
        }
        datos["eventos"] = list(eventos)
    datos["ip"] = ip_local()
    datos["puerto"] = puerto
    datos["uptime"] = uptime_ms()
    return datos


def leer_archivo(*partes):
    with open(os.path.join(CARPETA, *partes), "rb") as f:
        return f.read()


class Manejador(BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"

    def log_message(self, formato, *args):
        pass  # sin registro por peticion: Unity consulta 10 veces por segundo

    def responder(self, codigo, cuerpo, tipo="application/json"):
        if not isinstance(cuerpo, (bytes, str)):
            cuerpo = json.dumps(cuerpo)
        if isinstance(cuerpo, str):
            cuerpo = cuerpo.encode("utf-8")
        self.send_response(codigo)
        self.send_header("Content-Type", tipo)
        self.send_header("Content-Length", str(len(cuerpo)))
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(cuerpo)

    def do_GET(self):
        url = urlparse(self.path)
        q = {k: v[0] for k, v in parse_qs(url.query).items()}

        if url.path == "/":
            try:
                self.responder(200, leer_archivo("panel.html"), "text/html; charset=utf-8")
            except OSError:
                self.responder(500, "Falta simulador/panel.html", "text/plain; charset=utf-8")
        elif url.path == "/three.module.min.js":
            try:
                self.responder(200, leer_archivo("vendor", "three.module.min.js"), "text/javascript; charset=utf-8")
            except OSError:
                self.responder(404, {"error": "falta simulador/vendor/three.module.min.js"})
        elif url.path == "/marcador.jpg":
            try:
                with open(RUTA_MARCADOR, "rb") as f:
                    self.responder(200, f.read(), "image/jpeg")
            except OSError:
                self.responder(404, {"error": "no se encontro marcador/marcador_ra.jpg"})
        elif url.path == "/state":
            registrar_consulta("/state", self.client_address[0])
            self.responder(200, json_estado())
        elif url.path == "/voltages":
            registrar_consulta("/voltages", self.client_address[0])
            self.responder(200, json_voltajes())
        elif url.path == "/led":
            registrar_consulta("/led", self.client_address[0])
            self.manejar_led(q)
        elif url.path == "/sim":          # estado completo, para el panel web
            self.responder(200, json_panel(self.server.server_address[1]))
        elif url.path == "/set":          # usado por la pagina de control
            self.manejar_set(q)
        else:
            self.responder(404, {"error": "ruta no encontrada"})

    def manejar_led(self, q):
        if "id" not in q or "state" not in q:
            return self.responder(400, {"error": "faltan id y state"})
        if q["id"] not in ("1", "2"):
            return self.responder(400, {"error": "id debe ser 1 o 2"})
        clave = "led" + q["id"]
        s = q["state"]
        with CANDADO:
            if s == "toggle":
                estado[clave] = not estado[clave]
            elif s in ("1", "on"):
                estado[clave] = True
            elif s in ("0", "off"):
                estado[clave] = False
            else:
                return self.responder(400, {"error": "state invalido"})
            encendido = estado[clave]
            eventos.append({"t": uptime_ms(), "texto": "La app %s el LED %s" % ("encendió" if encendido else "apagó", q["id"])})
            del eventos[:-30]
        print("  LED %s -> %s" % (q["id"], "ENCENDIDO" if encendido else "apagado"))
        self.responder(200, json_estado())

    def manejar_set(self, q):
        try:
            with CANDADO:
                for clave in ("pot", "n3"):
                    if clave in q:
                        estado[clave] = min(100.0, max(0.0, float(q[clave])))
                if "button" in q:
                    estado["button"] = q["button"] in ("1", "true", "on")
        except ValueError:
            return self.responder(400, {"error": "valor invalido"})
        self.responder(200, {"ok": True})


def ip_local():
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("192.168.255.255", 1))   # no envia nada; solo averigua la interfaz
        ip = s.getsockname()[0]
        s.close()
        return ip
    except OSError:
        return "IP_DEL_COMPUTADOR"


def main():
    puerto = int(sys.argv[1]) if len(sys.argv) > 1 else 8080
    servidor = ThreadingHTTPServer(("0.0.0.0", puerto), Manejador)
    servidor.daemon_threads = True
    print("Simulador ESP32 en marcha")
    print("  Pagina de control : http://localhost:%d/" % puerto)
    print("  IP para Unity (PC): 127.0.0.1:%d" % puerto)
    print("  IP para telefono  : %s:%d" % (ip_local(), puerto))
    print("Ctrl+C para detener")
    try:
        servidor.serve_forever()
    except KeyboardInterrupt:
        print("\nDetenido")


if __name__ == "__main__":
    main()
