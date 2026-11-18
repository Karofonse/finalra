#!/usr/bin/env python3
"""
Simulador del ESP32 para probar la app de Unity sin hardware.

Responde igual que los dos firmware del proyecto:
    GET /state                     (Actividad 1)
    GET /led?id=1&state=toggle     (Actividad 1)
    GET /voltages                  (Actividad 2)

Ademas sirve una pagina de control en  http://localhost:8080/  para mover el
potenciometro, presionar el pulsador y ver los LEDs "fisicos".

Uso:
    python simulador_esp32.py            # puerto 8080
    python simulador_esp32.py 9000       # otro puerto

En Unity, escribir en el campo IP:   127.0.0.1:8080
Desde el telefono (misma red Wi-Fi): IP_DEL_COMPUTADOR:8080

Solo usa la biblioteca estandar de Python 3 (no hay que instalar nada).
"""

import json
import random
import socket
import sys
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlparse

INICIO = time.time()
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


PAGINA = """<!DOCTYPE html>
<html lang="es"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Simulador ESP32</title>
<style>
  body{font-family:system-ui,sans-serif;background:#12161f;color:#e8ecf2;max-width:520px;margin:0 auto;padding:16px}
  h1{font-size:1.3rem} h2{font-size:1rem;color:#33ccff;margin-top:28px}
  .fila{display:flex;align-items:center;gap:12px;margin:12px 0}
  input[type=range]{flex:1}
  .valor{width:110px;text-align:right;font-variant-numeric:tabular-nums}
  button{font-size:1rem;padding:12px 18px;border:0;border-radius:8px;background:#2973d9;color:#fff;touch-action:none}
  button:active{background:#ffcc1a;color:#000}
  .led{width:36px;height:36px;border-radius:50%;background:#40404a;display:inline-block}
  #led1.on{background:#ff3333;box-shadow:0 0 16px #ff3333}
  #led2.on{background:#33ff4d;box-shadow:0 0 16px #33ff4d}
  small{color:#99a3b3}
</style></head><body>
<h1>Simulador ESP32</h1>
<small>Esta p&aacute;gina hace de protoboard: lo que muevas aqu&iacute; lo ve Unity.</small>

<h2>Actividad 1 &mdash; Panel IoT</h2>
<div class="fila"><span>Potenci&oacute;metro</span>
  <input id="pot" type="range" min="0" max="100" step="0.5"><span class="valor" id="potV"></span></div>
<div class="fila"><button id="btn">Pulsador (mantener)</button><span id="btnV"></span></div>
<div class="fila"><span class="led" id="led1"></span> LED 1
  &nbsp;&nbsp;<span class="led" id="led2"></span> LED 2</div>

<h2>Actividad 2 &mdash; Gemelo digital</h2>
<div class="fila"><span>Pot. N3</span>
  <input id="n3" type="range" min="0" max="100" step="0.5"><span class="valor" id="n3V"></span></div>
<div id="nodos"></div>

<script>
const $ = id => document.getElementById(id);
const poner = q => fetch('/set?' + q);
let arrastrando = false;
for (const id of ['pot', 'n3']) {
  $(id).addEventListener('input', e => { arrastrando = true; poner(id + '=' + e.target.value); });
  $(id).addEventListener('change', () => { arrastrando = false; });
}
const b = $('btn');
b.addEventListener('pointerdown', () => poner('button=1'));
for (const ev of ['pointerup', 'pointerleave', 'pointercancel']) b.addEventListener(ev, () => poner('button=0'));

async function refrescar() {
  try {
    const s = await (await fetch('/sim')).json();
    if (!arrastrando) { $('pot').value = s.pot; $('n3').value = s.n3; }
    $('potV').textContent = s.pot.toFixed(1) + ' % = ' + (s.pot / 100 * 3.3).toFixed(2) + ' V';
    $('n3V').textContent = (s.n3 / 100 * 3.3).toFixed(2) + ' V';
    $('btnV').textContent = s.button ? 'PRESIONADO' : 'libre';
    $('led1').className = 'led' + (s.led1 ? ' on' : '');
    $('led2').className = 'led' + (s.led2 ? ' on' : '');
    const v = await (await fetch('/voltages')).json();
    $('nodos').textContent = v.nodes.map(n => n.id + ' = ' + (n.mv / 1000).toFixed(2) + ' V').join('    ');
  } catch (e) {}
}
setInterval(refrescar, 300); refrescar();
</script></body></html>
"""


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
            self.responder(200, PAGINA, "text/html; charset=utf-8")
        elif url.path == "/state":
            self.responder(200, json_estado())
        elif url.path == "/voltages":
            self.responder(200, json_voltajes())
        elif url.path == "/led":
            self.manejar_led(q)
        elif url.path == "/sim":          # estado crudo, para la pagina de control
            with CANDADO:
                self.responder(200, dict(estado))
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
