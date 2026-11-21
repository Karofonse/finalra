# Contexto para continuar el trabajo (sesión local de Claude)

Proyecto de la asignatura Realidad Aumentada: ESP32 + Unity 6 + Vuforia 11.4.4, comunicación HTTP.
El enunciado y la arquitectura están en `README.md`. **No hay hardware físico**: el circuito está simulado
(`simulador/simulador_esp32.py`) y modelado en 3D dentro de la app. Esto debe decirse así en la entrega.

## Estado (20 de noviembre de 2026)

- Proyecto de Unity en el Mac del usuario: carpeta `~/ProyectoRA` (Unity 6000.0.85f1, render Built-in,
  Input System nuevo, TextMeshPro y Vuforia importados, licencia puesta, HTTP permitido). Este repo está
  clonado dentro, en `~/ProyectoRA/finalra`; `unity/` es una copia del proyecto **sin** el `.tgz` de Vuforia (138 MB).
  La licencia de Vuforia sí está en el repo público, por decisión del usuario.
- Todo el código C# compila sin errores en Unity. La plataforma activa ya es **Android**
  (IL2CPP, ARM64, API mínima 29, orientación vertical).
- Escenas `Actividad1_AR` y `Actividad2_AR` montadas con el menú `RA > AUTOMATICO` y agregadas a Build Settings.
  Un botón bajo la barra de conexión (`CambiarEscena`) pasa de una actividad a la otra.
- Probado en modo Play contra el simulador (`127.0.0.1:8080`), de forma automática:
  - Actividad 2: conexión, etiquetas N1/N2/N3, multímetro (panel y 3D), sondas invertidas, mismo nodo,
    medición correcta, resumen de aciertos y toque sobre las etiquetas flotantes.
  - Actividad 1: voltaje y porcentaje del potenciómetro, pulsador, LEDs desde la app, perilla y luces del modelo 3D.
- Panel web del simulador rehecho (`simulador/panel.html`): circuito 3D interactivo con three.js, indicadores,
  historial y registro de comunicación. Probado en el navegador con las dos actividades.
- El panel de la Actividad 1 se redujo (16 × 10,5 cm) y se bajó el brillo de los materiales tras la primera prueba
  en el teléfono (la app ya rastrea y conecta en el teléfono del usuario).
- APK generado: `~/ProyectoRA/Builds/ProyectoRA.apk` (menú `RA > Compilar APK para Android`).
  El primer APK se probó en el teléfono; el segundo (panel reducido) falta por confirmar.

## Pendiente

1. Instalar el APK en el teléfono y probar con el marcador real: IP `IP_DEL_MAC:8080` (Mac y teléfono en la misma Wi-Fi).
2. Revisar con la cámara real la alineación del modelo 3D con el dibujo de la página del simulador.
   Solo se verificó en renders (`~/ProyectoRA/Capturas`), no con rastreo.
3. El simulador que estaba abierto en el Mac era una copia vieja (`~/Downloads/finalra-main 2`): cerrarlo y
   abrir el de este repo, cuya página trae el dibujo alineado con el modelo 3D.
4. Video de máximo 3 minutos y mensaje al profesor aclarando que el circuito es simulado.
5. Más adelante: reemplazar piezas por modelos 3D descargados (ESP32 y protoboard primero).

## Preferencias del usuario

Habla español, no conoce Unity: dar pasos uno por uno, con el nombre exacto de menús y paneles, y pedir capturas.
Quiere que el modelo se vea lo más real posible.
