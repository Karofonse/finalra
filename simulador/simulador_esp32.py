#!/usr/bin/env python3
"""
Simulador del ESP32 para probar la app de Unity sin hardware.

Responde igual que los dos firmware del proyecto:
    GET /state                     (Actividad 1)
    GET /led?id=1&state=toggle     (Actividad 1)
    GET /voltages                  (Actividad 2)

Ademas sirve en  http://localhost:8080/  el "circuito virtual": el marcador de
Vuforia y, debajo, la protoboard dibujada a escala (potenciometro, pulsador,
LEDs, divisor de voltaje). Al apuntar la app de AR a esa pantalla, el modelo 3D
y las etiquetas quedan encima del dibujo.

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


PAGINA = """<!DOCTYPE html>
<html lang="es"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Circuito virtual ESP32</title>
<style>
  *{box-sizing:border-box}
  body{font-family:system-ui,sans-serif;background:#12161f;color:#e8ecf2;margin:0;padding:10px 14px}
  header{display:flex;flex-wrap:wrap;align-items:center;gap:10px;margin-bottom:8px}
  h1{font-size:1.05rem;margin:0 8px 0 0}
  .tab{font-size:.95rem;padding:8px 14px;border:0;border-radius:8px;background:#2a3142;color:#cfd6e4;cursor:pointer}
  .tab.activa{background:#2973d9;color:#fff}
  small{color:#99a3b3}
  #lienzo{display:block;width:100%;max-height:72vh;background:#f4f4ef;border-radius:8px}
  .controles{display:flex;flex-wrap:wrap;align-items:center;gap:14px;margin-top:10px}
  .controles label{display:flex;align-items:center;gap:8px;flex:1 1 280px}
  input[type=range]{flex:1}
  .valor{min-width:120px;font-variant-numeric:tabular-nums}
  #btn{font-size:1rem;padding:10px 16px;border:0;border-radius:8px;background:#2973d9;color:#fff;touch-action:none;cursor:pointer}
  #btn:active{background:#ffcc1a;color:#000}
  svg text{font-family:system-ui,sans-serif}
</style></head><body>
<header>
  <h1>Circuito virtual ESP32</h1>
  <button class="tab" id="tab1">Actividad 1 &middot; Panel IoT</button>
  <button class="tab" id="tab2">Actividad 2 &middot; Gemelo digital</button>
  <small>Apunta la app de AR al marcador. Este dibujo hace de protoboard: lo que cambies aqu&iacute; lo lee Unity.</small>
</header>

<svg id="lienzo" viewBox="-125 -58 250 206" preserveAspectRatio="xMidYMid meet"></svg>

<div class="controles" id="ctl1">
  <label>Potenci&oacute;metro <input id="pot" type="range" min="0" max="100" step="0.5"><span class="valor" id="potV"></span></label>
  <button id="btn">Pulsador (mantener)</button><span id="btnV"></span>
</div>
<div class="controles" id="ctl2">
  <label>Potenci&oacute;metro (N3) <input id="n3" type="range" min="0" max="100" step="0.5"><span class="valor" id="n3V"></span></label>
</div>

<script>
const $ = id => document.getElementById(id);
const poner = q => fetch('/set?' + q);
// Coordenadas en milimetros. El marcador mide 140 x 100 y esta centrado en (0, 0).
// La protoboard (220 x 70) esta centrada en (0, 105). Coincide con el modelo 3D de Unity.
const CY = 105;
const P = (x, z) => [x, CY - z];          // (x, z) de la protoboard -> coordenadas del dibujo
let act = location.hash === '#2' ? 2 : 1;
let est = {pot: 50, n3: 50, button: false, led1: false, led2: false};
let volt = {N1: 2.2, N2: 1.1, N3: 1.65};
let arrastrando = false;

function linea(a, b, color, w) {
  return `<line x1="${a[0]}" y1="${a[1]}" x2="${b[0]}" y2="${b[1]}" stroke="${color}" stroke-width="${w || 1.2}" stroke-linecap="round"/>`;
}
function resistencia(x, z, vertical, nombre) {
  const [cx, cy] = P(x, z);
  const w = vertical ? 6 : 16, h = vertical ? 16 : 6;
  let t = `<rect x="${cx - w / 2}" y="${cy - h / 2}" width="${w}" height="${h}" rx="2" fill="#d9b380" stroke="#8a6a3a" stroke-width=".4"/>`;
  if (nombre) t += `<text x="${cx}" y="${cy - 5}" font-size="3.6" text-anchor="middle" fill="#444">${nombre}</text>`;
  return t;
}
function pote(x, z, pct) {
  const [cx, cy] = P(x, z);
  const ang = -135 + 270 * pct / 100;
  return `<rect x="${cx - 7}" y="${cy - 7}" width="14" height="14" rx="1.5" fill="#2659cc"/>
    <g transform="rotate(${ang} ${cx} ${cy})"><circle cx="${cx}" cy="${cy}" r="5" fill="#eee" stroke="#555" stroke-width=".4"/>
    <line x1="${cx}" y1="${cy}" x2="${cx}" y2="${cy - 4.5}" stroke="#111" stroke-width="1.2"/></g>`;
}
function nodo(x, z, nombre, valor, color, dy) {
  const [cx, cy] = P(x, z);
  const ty = cy + (dy || -5);
  return `<circle cx="${cx}" cy="${cy}" r="1.8" fill="${color}" stroke="#111" stroke-width=".3"/>
    <text x="${cx}" y="${ty}" font-size="3.8" font-weight="700" text-anchor="middle" fill="#111">${nombre}${valor ? ' ' + valor : ''}</text>`;
}
function base() {
  return `<image href="/marcador.jpg" x="-70" y="-50" width="140" height="100"/>
    <rect x="-110" y="70" width="220" height="70" rx="3" fill="#ecece6" stroke="#b9b9ad" stroke-width=".6"/>
    ${linea(P(-105, 30), P(105, 30), '#e61a1a', 1)}${linea(P(-105, -30), P(105, -30), '#1a4de6', 1)}
    <text x="-108" y="${CY - 31.5}" font-size="3" fill="#e61a1a">3V3</text><text x="-108" y="${CY + 33.5}" font-size="3" fill="#1a4de6">GND</text>
    <rect x="-100" y="${CY - 14}" width="50" height="28" rx="1.5" fill="#15181c"/>
    <rect x="-92" y="${CY - 8}" width="18" height="16" fill="#c7c7cc"/>
    <rect x="-102" y="${CY - 4}" width="6" height="8" fill="#c7c7cc"/>
    <text x="-62" y="${CY + 1.5}" font-size="4" text-anchor="middle" fill="#fff">ESP32</text>
    ${linea(P(-60, 14), P(-60, 30), '#e61a1a')}${linea(P(-68, -14), P(-68, -30), '#1a4de6')}`;
}
function dibujo1() {
  const led = (x, on, color) => { const [cx, cy] = P(x, 12);
    return `<circle cx="${cx}" cy="${cy}" r="${on ? 8 : 0}" fill="${color}" opacity=".35"/>
            <circle cx="${cx}" cy="${cy}" r="4" fill="${on ? color : '#55555c'}" stroke="#222" stroke-width=".4"/>`; };
  const [bx, by] = P(40, 0);
  return base() +
    linea(P(-7, -12), P(-7, 30), '#e61a1a') + linea(P(7, -12), P(7, -30), '#1a4de6') + linea(P(0, -20), P(-50, -10), '#e6b800') +
    pote(0, -12, est.pot) +
    linea(P(40, -6), P(40, -30), '#1a4de6') + linea(P(34, 6), P(-50, 6), '#1ab34d') +
    `<g id="pulsador" style="cursor:pointer"><rect x="${bx - 6}" y="${by - 6}" width="12" height="12" rx="1.5" fill="#15181c"/>
       <circle cx="${bx}" cy="${by}" r="3.5" fill="${est.button ? '#ffcc1a' : '#9a9aa0'}"/></g>` +
    linea(P(75, 8), P(75, 0), '#888') + linea(P(95, 8), P(95, 0), '#888') +
    linea(P(75, -16), P(75, -30), '#1a4de6') + linea(P(95, -16), P(95, -30), '#1a4de6') +
    linea(P(71, 16), P(-50, 12), '#ff8019') + linea(P(91, 16), P(-50, -4), '#e6b800') +
    resistencia(75, -8, true) + resistencia(95, -8, true) +
    led(75, est.led1, '#ff2626') + led(95, est.led2, '#33ff4d') +
    `<text x="0" y="${CY + 27}" font-size="3.4" text-anchor="middle" fill="#333">POT ${(est.pot / 100 * 3.3).toFixed(2)} V</text>
     <text x="40" y="${CY - 9}" font-size="3.4" text-anchor="middle" fill="#333">PULSADOR</text>
     <text x="75" y="${CY - 19}" font-size="3.4" text-anchor="middle" fill="#333">LED 1</text>
     <text x="95" y="${CY - 19}" font-size="3.4" text-anchor="middle" fill="#333">LED 2</text>`;
}
function dibujo2() {
  const v = k => (volt[k] !== undefined ? volt[k].toFixed(2) + ' V' : '');
  return base() +
    linea(P(-45, 12), P(-28, 12), '#888') + linea(P(-12, 12), P(22, 12), '#888') +
    linea(P(38, 12), P(72, 12), '#888') + linea(P(88, 12), P(105, 12), '#888') +
    linea(P(-45, 12), P(-45, 30), '#e61a1a') + linea(P(105, 12), P(105, -30), '#1a4de6') +
    linea(P(23, -14), P(16, 30), '#e61a1a') + linea(P(37, -14), P(37, -30), '#1a4de6') + linea(P(30, -21), P(30, -26), '#888') +
    linea(P(5, 12), P(-50, 8), '#e6b800') + linea(P(55, 12), P(-50, 2), '#ff8019') + linea(P(30, -26), P(-50, -8), '#1ab34d') +
    resistencia(-20, 12, false, 'R1 10k') + resistencia(30, 12, false, 'R2 10k') + resistencia(80, 12, false, 'R3 10k') +
    pote(30, -14, est.n3) +
    nodo(-45, 12, '3V3', '', '#ff4d4d', 7) + nodo(5, 12, 'N1', v('N1'), '#33ccff', 7) + nodo(55, 12, 'N2', v('N2'), '#33ccff', 7) +
    nodo(105, 12, 'GND', '', '#bbb', 7) + nodo(30, -26, 'N3', v('N3'), '#33ccff', 6.5);
}
function pintar() {
  $('lienzo').innerHTML = act === 1 ? dibujo1() : dibujo2();
  $('ctl1').style.display = act === 1 ? '' : 'none';
  $('ctl2').style.display = act === 2 ? '' : 'none';
  $('tab1').className = 'tab' + (act === 1 ? ' activa' : '');
  $('tab2').className = 'tab' + (act === 2 ? ' activa' : '');
  $('potV').textContent = est.pot.toFixed(1) + ' % = ' + (est.pot / 100 * 3.3).toFixed(2) + ' V';
  $('n3V').textContent = (est.n3 / 100 * 3.3).toFixed(2) + ' V';
  $('btnV').textContent = est.button ? 'PRESIONADO' : 'libre';
}
function elegir(n) { act = n; location.hash = '#' + n; pintar(); }
$('tab1').onclick = () => elegir(1);
$('tab2').onclick = () => elegir(2);

for (const id of ['pot', 'n3']) {
  $(id).addEventListener('input', e => { arrastrando = true; est[id] = parseFloat(e.target.value); poner(id + '=' + e.target.value); pintar(); });
  $(id).addEventListener('change', () => { arrastrando = false; });
}
const presionar = on => { est.button = on; poner('button=' + (on ? 1 : 0)); pintar(); };
$('btn').addEventListener('pointerdown', () => presionar(true));
for (const ev of ['pointerup', 'pointerleave', 'pointercancel']) $('btn').addEventListener(ev, () => { if (est.button) presionar(false); });
$('lienzo').addEventListener('pointerdown', e => { if (e.target.closest('#pulsador')) presionar(true); });
window.addEventListener('pointerup', () => { if (est.button) presionar(false); });

async function refrescar() {
  try {
    const s = await (await fetch('/sim')).json();
    if (arrastrando) { s.pot = est.pot; s.n3 = est.n3; }
    else { $('pot').value = s.pot; $('n3').value = s.n3; }
    est = s;
    const r = await (await fetch('/voltages')).json();
    for (const n of r.nodes) volt[n.id] = n.mv / 1000;
    pintar();
  } catch (e) {}
}
pintar(); setInterval(refrescar, 300); refrescar();
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
        elif url.path == "/marcador.jpg":
            try:
                with open(RUTA_MARCADOR, "rb") as f:
                    self.responder(200, f.read(), "image/jpeg")
            except OSError:
                self.responder(404, {"error": "no se encontro marcador/marcador_ra.jpg"})
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
