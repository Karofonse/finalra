/*
  ACTIVIDAD 1 - Panel de instrumentacion IoT en AR
  ESP32 + servidor HTTP (comunicacion bidireccional con Unity)

  Conexiones:
    Potenciometro : extremos a 3V3 y GND, cursor a GPIO34 (ADC1, solo entrada)
    Pulsador      : entre GPIO27 y GND (se usa la resistencia pull-up interna)
    LED 1         : GPIO25 -> resistencia 220 ohm -> LED -> GND
    LED 2         : GPIO26 -> resistencia 220 ohm -> LED -> GND

  IMPORTANTE: con Wi-Fi activo solo funcionan los pines del ADC1 (GPIO32 a GPIO39).

  API HTTP:
    GET /state                      -> estado completo en JSON
    GET /led?id=1&state=1           -> enciende LED 1   (state = 0 | 1 | toggle)
    GET /led?id=2&state=toggle      -> conmuta LED 2
  Todas las respuestas devuelven el estado completo.
*/

#include <WiFi.h>
#include <WebServer.h>

// ---------- Configuracion de red ----------
const char* WIFI_SSID = "TU_WIFI";
const char* WIFI_PASS = "TU_CLAVE";

// Si no logra conectarse al Wi-Fi en 15 s, el ESP32 crea su propia red
// (util para la demostracion en campo). IP en ese modo: 192.168.4.1
const char* AP_SSID = "RA-ESP32";
const char* AP_PASS = "12345678";

// ---------- Pines ----------
const int PIN_POT    = 34;
const int PIN_BOTON  = 27;
const int PIN_LED1   = 25;
const int PIN_LED2   = 26;

// ---------- Estado ----------
WebServer server(80);

float potFiltrado = 0;          // lectura cruda 0..4095 filtrada
bool  botonEstable = false;     // true = presionado
bool  botonUltimaLectura = false;
unsigned long botonUltimoCambio = 0;
bool  led1 = false;
bool  led2 = false;

unsigned long ultimaLecturaSensores = 0;
unsigned long ultimoIntentoWifi = 0;
bool modoAP = false;

const unsigned long PERIODO_SENSORES_MS = 10;
const unsigned long REBOTE_MS = 30;

// ---------- Utilidades ----------
void aplicarLeds() {
  digitalWrite(PIN_LED1, led1 ? HIGH : LOW);
  digitalWrite(PIN_LED2, led2 ? HIGH : LOW);
}

int leerPotPromedio() {
  long suma = 0;
  for (int i = 0; i < 8; i++) suma += analogRead(PIN_POT);
  return suma / 8;
}

void leerSensores() {
  // Filtro exponencial para quitar el ruido del ADC
  potFiltrado = 0.8f * potFiltrado + 0.2f * leerPotPromedio();

  // Antirrebote del pulsador (activo en bajo)
  bool lectura = digitalRead(PIN_BOTON) == LOW;
  if (lectura != botonUltimaLectura) {
    botonUltimaLectura = lectura;
    botonUltimoCambio = millis();
  }
  if (millis() - botonUltimoCambio > REBOTE_MS) botonEstable = lectura;
}

void enviarEstado() {
  int crudo = (int)(potFiltrado + 0.5f);
  float porcentaje = crudo * 100.0f / 4095.0f;
  float voltios = crudo * 3.3f / 4095.0f;

  char json[200];
  snprintf(json, sizeof(json),
           "{\"analog\":%d,\"percent\":%.1f,\"volts\":%.2f,"
           "\"button\":%s,\"led1\":%s,\"led2\":%s,\"uptime\":%lu}",
           crudo, porcentaje, voltios,
           botonEstable ? "true" : "false",
           led1 ? "true" : "false",
           led2 ? "true" : "false",
           millis());

  server.sendHeader("Access-Control-Allow-Origin", "*");
  server.sendHeader("Cache-Control", "no-store");
  server.send(200, "application/json", json);
}

void manejarLed() {
  if (!server.hasArg("id") || !server.hasArg("state")) {
    server.send(400, "application/json", "{\"error\":\"faltan id y state\"}");
    return;
  }
  int id = server.arg("id").toInt();
  String s = server.arg("state");
  if (id != 1 && id != 2) {
    server.send(400, "application/json", "{\"error\":\"id debe ser 1 o 2\"}");
    return;
  }
  bool& led = (id == 1) ? led1 : led2;
  if (s == "toggle") led = !led;
  else if (s == "1" || s == "on") led = true;
  else if (s == "0" || s == "off") led = false;
  else {
    server.send(400, "application/json", "{\"error\":\"state invalido\"}");
    return;
  }
  aplicarLeds();
  enviarEstado();
}

void manejarRaiz() {
  server.send(200, "text/plain", "ESP32 Panel IoT AR - use /state y /led?id=1&state=toggle");
}

void conectarWifi() {
  WiFi.mode(WIFI_STA);
  WiFi.setAutoReconnect(true);
  WiFi.setSleep(false);               // menor latencia
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

  pinMode(PIN_BOTON, INPUT_PULLUP);
  pinMode(PIN_LED1, OUTPUT);
  pinMode(PIN_LED2, OUTPUT);
  aplicarLeds();

  analogReadResolution(12);
  analogSetPinAttenuation(PIN_POT, ADC_11db);   // rango completo 0..3.3 V
  potFiltrado = leerPotPromedio();

  conectarWifi();

  server.on("/", manejarRaiz);
  server.on("/state", enviarEstado);
  server.on("/led", manejarLed);
  server.onNotFound([]() {
    server.send(404, "application/json", "{\"error\":\"ruta no encontrada\"}");
  });
  server.begin();
  Serial.println("Servidor HTTP iniciado en el puerto 80");
}

void loop() {
  server.handleClient();

  if (millis() - ultimaLecturaSensores >= PERIODO_SENSORES_MS) {
    ultimaLecturaSensores = millis();
    leerSensores();
  }

  // Persistencia de la conexion: si se cae el Wi-Fi, reintenta cada 5 s
  if (!modoAP && WiFi.status() != WL_CONNECTED && millis() - ultimoIntentoWifi > 5000) {
    ultimoIntentoWifi = millis();
    Serial.println("Wi-Fi caido, reconectando...");
    WiFi.reconnect();
  }

  delay(1);
}
