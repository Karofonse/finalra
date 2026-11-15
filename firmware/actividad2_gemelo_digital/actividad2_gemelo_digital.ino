/*
  ACTIVIDAD 2 - Asistente de medicion y gemelo digital en AR
  ESP32 + servidor HTTP: publica el voltaje de 3 nodos del circuito.

  Circuito en protoboard:

      3V3 ---[R1 10k]---(N1)---[R2 10k]---(N2)---[R3 10k]--- GND
      3V3 ---[ POT 10k ]--- GND      cursor del potenciometro = (N3)

    N1 -> GPIO34   (teorico 2.20 V)
    N2 -> GPIO35   (teorico 1.10 V)
    N3 -> GPIO32   (variable 0 a 3.3 V)

  IMPORTANTE:
    - Con Wi-Fi activo solo funcionan los pines del ADC1 (GPIO32 a GPIO39).
    - Nunca aplicar mas de 3.3 V a un pin del ESP32.
    - El ADC del ESP32 pierde precision por debajo de ~0.15 V y por encima de ~3.1 V.

  API HTTP:
    GET /voltages -> {"nodes":[{"id":"N1","mv":2200},{"id":"N2","mv":1100},{"id":"N3","mv":1650}],"uptime":12345}
*/

#include <WiFi.h>
#include <WebServer.h>

// ---------- Configuracion de red ----------
const char* WIFI_SSID = "TU_WIFI";
const char* WIFI_PASS = "TU_CLAVE";

// Si no logra conectarse en 15 s crea su propia red. IP en ese modo: 192.168.4.1
const char* AP_SSID = "RA-ESP32";
const char* AP_PASS = "12345678";

// ---------- Nodos medidos ----------
const int NUM_NODOS = 3;
const char* NODO_ID[NUM_NODOS]  = { "N1", "N2", "N3" };
const int   NODO_PIN[NUM_NODOS] = { 34, 35, 32 };
float nodoMv[NUM_NODOS] = { 0, 0, 0 };

WebServer server(80);
unsigned long ultimaLectura = 0;
unsigned long ultimoIntentoWifi = 0;
bool modoAP = false;

const unsigned long PERIODO_LECTURA_MS = 20;

// Promedio de 16 muestras usando la calibracion de fabrica del ADC
float leerMilivoltios(int pin) {
  uint32_t suma = 0;
  for (int i = 0; i < 16; i++) suma += analogReadMilliVolts(pin);
  return suma / 16.0f;
}

void leerNodos() {
  for (int i = 0; i < NUM_NODOS; i++) {
    nodoMv[i] = 0.85f * nodoMv[i] + 0.15f * leerMilivoltios(NODO_PIN[i]);
  }
}

void enviarVoltajes() {
  String json = "{\"nodes\":[";
  for (int i = 0; i < NUM_NODOS; i++) {
    if (i > 0) json += ",";
    json += "{\"id\":\"";
    json += NODO_ID[i];
    json += "\",\"mv\":";
    json += String(nodoMv[i], 0);
    json += "}";
  }
  json += "],\"uptime\":";
  json += String(millis());
  json += "}";

  server.sendHeader("Access-Control-Allow-Origin", "*");
  server.sendHeader("Cache-Control", "no-store");
  server.send(200, "application/json", json);
}

void conectarWifi() {
  WiFi.mode(WIFI_STA);
  WiFi.setAutoReconnect(true);
  WiFi.setSleep(false);
  WiFi.begin(WIFI_SSID, WIFI_PASS);

  Serial.print("Conectando a Wi-Fi");
  unsigned long inicio = millis();
  while (WiFi.status() != WL_CONNECTED && millis() - inicio < 15000) {
    delay(300);
    Serial.print(".");
  }
  Serial.println();

  if (WiFi.status() == WL_CONNECTED) {
    Serial.print("Conectado. IP: ");
    Serial.println(WiFi.localIP());
  } else {
    modoAP = true;
    WiFi.mode(WIFI_AP);
    WiFi.softAP(AP_SSID, AP_PASS);
    Serial.print("Sin Wi-Fi. Red propia '");
    Serial.print(AP_SSID);
    Serial.print("'. IP: ");
    Serial.println(WiFi.softAPIP());
  }
}

void setup() {
  Serial.begin(115200);

  analogReadResolution(12);
  for (int i = 0; i < NUM_NODOS; i++) {
    analogSetPinAttenuation(NODO_PIN[i], ADC_11db);
    nodoMv[i] = leerMilivoltios(NODO_PIN[i]);
  }

  conectarWifi();

  server.on("/", []() {
    server.send(200, "text/plain", "ESP32 Gemelo Digital AR - use /voltages");
  });
  server.on("/voltages", enviarVoltajes);
  server.onNotFound([]() {
    server.send(404, "application/json", "{\"error\":\"ruta no encontrada\"}");
  });
  server.begin();
  Serial.println("Servidor HTTP iniciado en el puerto 80");
}

void loop() {
  server.handleClient();

  if (millis() - ultimaLectura >= PERIODO_LECTURA_MS) {
    ultimaLectura = millis();
    leerNodos();
  }

  if (!modoAP && WiFi.status() != WL_CONNECTED && millis() - ultimoIntentoWifi > 5000) {
    ultimoIntentoWifi = millis();
    Serial.println("Wi-Fi caido, reconectando...");
    WiFi.reconnect();
  }

  delay(1);
}
