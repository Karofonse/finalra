# Realidad Aumentada aplicada a sistemas electrónicos

Proyecto de la asignatura Realidad Aumentada (Ingeniería Electrónica).
ESP32 + Unity + Vuforia, comunicación bidireccional por HTTP sobre Wi-Fi.

- **Actividad 1:** panel de instrumentación IoT flotante (medidor del potenciómetro, estado del pulsador, control de 2 LEDs).
- **Actividad 2:** gemelo digital con etiquetas de voltaje sobre el circuito y "Guía de Medición" interactiva del multímetro.

## Contenido

```
firmware/
  actividad1_panel_iot/actividad1_panel_iot.ino
  actividad2_gemelo_digital/actividad2_gemelo_digital.ino
unity/Assets/
  Scripts/Comun/        Esp32Http.cs  ConexionUI.cs  Billboard.cs
  Scripts/Actividad1/   PanelIoT.cs
  Scripts/Actividad2/   GemeloDigital.cs  EtiquetaVoltaje.cs  GuiaMedicion.cs  FlechaAnimada.cs
  Editor/               ConstructorEscenasRA.cs   (menú "RA" que arma la interfaz)
```

`unity/Assets` no es un proyecto de Unity completo: se copia dentro de la carpeta `Assets` de tu proyecto.

## Arquitectura

```
 Sensores/actuadores        Wi-Fi (HTTP, JSON)            Teléfono Android
┌──────────────────┐   GET /state  o  /voltages     ┌──────────────────────────┐
│ ESP32            │ <───────────────────────────── │ Unity                    │
│  WebServer :80   │ ─────────────────────────────> │  Esp32Http (sondeo 10 Hz)│
│  ADC1, GPIO      │   GET /led?id=1&state=toggle   │  PanelIoT / GemeloDigital│
│                  │ <───────────────────────────── │  Vuforia Image Target    │
└──────────────────┘                                └──────────────────────────┘
```

- El ESP32 es el servidor. Unity consulta el estado cada ~100 ms y envía comandos cuando se toca un botón.
- `Esp32Http` mide la latencia de cada petición, la muestra en pantalla, y si se pierde la conexión reintenta cada segundo.
- El ESP32 reintenta el Wi-Fi cada 5 s si se cae. Si al arrancar no encuentra la red en 15 s, crea la red propia `RA-ESP32` (clave `12345678`, IP `192.168.4.1`), útil para la demostración en campo sin router.

## Hardware

### Actividad 1

| Elemento | Conexión |
|---|---|
| Potenciómetro 10 kΩ | extremos a 3V3 y GND, cursor a **GPIO34** |
| Pulsador | entre **GPIO27** y GND (pull-up interno) |
| LED 1 | **GPIO25** → 220 Ω → LED → GND |
| LED 2 | **GPIO26** → 220 Ω → LED → GND |

```
 3V3 ──┬── POT ──┬── GND          GPIO27 ──[ pulsador ]── GND
       │  cursor │
       └───┴─────┘                GPIO25 ──[220Ω]──►|── GND   (LED 1)
           │                      GPIO26 ──[220Ω]──►|── GND   (LED 2)
        GPIO34
```

### Actividad 2

```
 3V3 ──[R1 10k]──(N1)──[R2 10k]──(N2)──[R3 10k]── GND
                  │               │
               GPIO34          GPIO35

 3V3 ──[ POT 10k ]── GND     cursor (N3) ── GPIO32
```

Valores teóricos: N1 = 2,20 V, N2 = 1,10 V, N3 variable.

Notas importantes:

- Con Wi-Fi activo **solo sirven los pines del ADC1 (GPIO32 a GPIO39)**. Los del ADC2 no leen.
- Nunca aplicar más de 3,3 V a un pin del ESP32.
- El ADC del ESP32 pierde precisión por debajo de ~0,15 V y por encima de ~3,1 V; por eso la guía valida con tolerancia de 5 % o 0,1 V (lo que sea mayor).

### Cargar el firmware

1. Arduino IDE con el paquete de placas **esp32** de Espressif (versión 2.x o superior).
2. Abrir el `.ino`, cambiar `WIFI_SSID` y `WIFI_PASS`.
3. Subir y abrir el Monitor Serie a 115200: ahí aparece la **IP del ESP32**.
4. Probar desde el navegador del computador: `http://IP/state` (Actividad 1) o `http://IP/voltages` (Actividad 2).

## Unity

### Preparación del proyecto (una sola vez)

1. Crear un proyecto 3D e instalar **Vuforia Engine** (paquete desde el portal de desarrolladores de Vuforia) y poner la licencia en `VuforiaConfiguration`.
2. Importar TextMeshPro: `Window > TextMeshPro > Import TMP Essential Resources`.
3. Copiar el contenido de `unity/Assets` dentro de `Assets` del proyecto.
4. `File > Build Settings > Android > Switch Platform`.
5. `Edit > Project Settings > Player > Other Settings`:
   - **Allow downloads over HTTP: Always allowed** (sin esto Android bloquea las peticiones al ESP32).
   - Internet Access: Require.
   - Minimum API Level según lo que pida Vuforia.

### Montar cada escena

1. Escena nueva. Borrar la `Main Camera` y agregar `GameObject > Vuforia Engine > AR Camera`.
2. Agregar `GameObject > Vuforia Engine > Image Target` y asignarle el marcador (base de datos de Vuforia o imagen propia). Poner el ancho real del marcador impreso en metros.
3. Menú **RA > Crear contenido Actividad 1** (o **Actividad 2**). Se crea el objeto `ContenidoActividadX` con toda la interfaz ya conectada.
4. Arrastrar `ContenidoActividadX` **dentro del Image Target** (como hijo) y dejar su posición en (0, 0, 0).
5. Solo Actividad 2: en `Anclas (mover sobre el circuito)` mover `AnclaN1`, `AnclaN2`, `AnclaN3` y `AnclaGND` hasta que queden sobre los puntos reales de la protoboard respecto al marcador. Las etiquetas y las sondas virtuales siguen a las anclas.
6. Guardar la escena, agregarla a Build Settings y compilar al teléfono.

Si los botones del panel no responden al toque, revisar que exista un `EventSystem` en la escena y que la AR Camera tenga la etiqueta `MainCamera`.

## Manual rápido de uso

### Actividad 1

1. Encender el ESP32 y conectar el teléfono a la misma red Wi-Fi (o a la red `RA-ESP32`).
2. Abrir la app, escribir la IP del ESP32 en la barra superior y tocar **Conectar**. Debe aparecer "Conectado" con la latencia en ms.
3. Apuntar la cámara al marcador: aparece el panel flotante.
4. Girar el potenciómetro: el medidor circular y el voltaje cambian en tiempo real.
5. Presionar el pulsador físico: la luz "PULSADOR" se enciende.
6. Tocar **LED 1** o **LED 2**: se enciende o apaga el LED físico y su luz virtual.

### Actividad 2

1. Conectar igual que en la Actividad 1.
2. Apuntar al marcador junto a la protoboard: sobre cada nodo aparece una etiqueta con el voltaje medido por el ESP32.
3. Tocar **Guía de Medición**.
4. Seguir cada paso: las sondas virtuales roja y negra muestran dónde poner las puntas del multímetro.
5. Escribir la lectura del multímetro y tocar **Verificar**. La app la compara con el valor del ESP32:
   - coincide → se habilita **Siguiente**;
   - mismo valor con signo contrario → avisa que las sondas están invertidas;
   - cerca de 0 V → pide revisar contacto y selector;
   - otro valor → muestra la diferencia y pide revisar la posición.
6. Al final se muestra el resumen con los aciertos al primer intento.

## API del ESP32

| Ruta | Respuesta |
|---|---|
| `GET /state` | `{"analog":2048,"percent":50.0,"volts":1.65,"button":false,"led1":true,"led2":false,"uptime":12345}` |
| `GET /led?id=1&state=toggle` | cambia el LED (`state` = `0`, `1` o `toggle`) y devuelve el estado completo |
| `GET /voltages` | `{"nodes":[{"id":"N1","mv":2200},{"id":"N2","mv":1100},{"id":"N3","mv":1650}],"uptime":12345}` |

## Simulador (probar sin hardware)

`simulador/simulador_esp32.py` responde igual que el ESP32 (`/state`, `/led`, `/voltages`) con valores simulados. Solo necesita Python 3.

```bash
python simulador/simulador_esp32.py
```

1. Abrir `http://localhost:8080/` en el navegador: es la "protoboard" virtual (potenciómetros, pulsador y LEDs).
2. En Unity pulsar Play, escribir `127.0.0.1:8080` en el campo de IP y tocar **Conectar**.
3. Para probar desde el teléfono, usar la IP del computador que imprime el simulador (misma red Wi-Fi; puede que el firewall pida permiso).

El simulador es solo para desarrollo: la entrega y el video requieren el circuito físico.
