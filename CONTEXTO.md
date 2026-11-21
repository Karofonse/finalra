# Contexto para continuar el trabajo (sesión local de Claude)

Proyecto de la asignatura Realidad Aumentada: ESP32 + Unity 6 + Vuforia 11.4.4, comunicación HTTP.
El enunciado y la arquitectura están en `README.md`. **No hay hardware físico**: el circuito está simulado
(`simulador/simulador_esp32.py`) y modelado en 3D dentro de la app. Esto debe decirse así en la entrega.

## Estado

- Proyecto de Unity en el Mac del usuario: carpeta `ProyectoRA` (Unity 6000.0.85f1, render Built-in,
  Input System nuevo, TextMeshPro y Vuforia importados, licencia puesta, "Allow downloads over HTTP = Always allowed").
- Escenas en `Assets/Scenes`: `Actividad1`, `Actividad2` (pruebas sin AR) y `Actividad2_AR`
  (ARCamera + ImageTarget "From Image" con `marcador_ra`, ancho físico 0.14 m). El rastreo ya funcionó.
- El código fuente de referencia está en este repo, en `unity/Assets/{Scripts,Editor}`. La copia dentro de
  `ProyectoRA/Assets` puede estar desactualizada: copiar encima (sin borrar los `.meta`).
- Nada del código C# se ha compilado fuera del Unity del usuario; las últimas versiones (modelo 3D realista,
  multímetro virtual, menú AUTOMATICO) **aún no se han probado en Unity**.

## Pendiente

1. Copiar `unity/Assets/Scripts` y `unity/Assets/Editor` a `ProyectoRA/Assets` y corregir errores de compilación
   (ver `~/Library/Logs/Unity/Editor.log`).
2. En Unity, con `Actividad2_AR` abierta: menú `RA > AUTOMATICO - Montar Actividad 2 en esta escena AR`.
   Luego `RA > AUTOMATICO - Crear escena Actividad1_AR copiando esta`.
3. Probar con el simulador (`python3 simulador/simulador_esp32.py`, IP en la app: `127.0.0.1:8080`) y la página
   `http://localhost:8080/`, que muestra el marcador y el circuito dibujado alineado con el modelo 3D.
4. Revisar alineación del modelo 3D con el dibujo, que las etiquetas flotantes respondan al toque y el aspecto
   de los materiales.
5. Compilar para Android (módulo Android Build Support) y probar en el teléfono con `IP_DEL_MAC:8080`.
6. Subir el proyecto de Unity al repo (`Assets`, `Packages`, `ProjectSettings`; el `.gitignore` ya excluye `Library`).
   La licencia de Vuforia queda en `Assets/Resources/VuforiaConfiguration.asset` y el repo es público: avisar al usuario.
7. Video de máximo 3 minutos y mensaje al profesor aclarando que el circuito es simulado.

## Preferencias del usuario

Habla español, no conoce Unity: dar pasos uno por uno, con el nombre exacto de menús y paneles, y pedir capturas.
Quiere que el modelo se vea lo más real posible; más adelante le gustaría reemplazar piezas por modelos 3D
descargados (ESP32 y protoboard primero).
