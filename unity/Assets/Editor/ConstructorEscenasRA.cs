using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#pragma warning disable CS0618 // FindObjectOfType: se mantiene por compatibilidad entre versiones de Unity

/// <summary>
/// Menu "RA" del editor: construye la interfaz completa de cada actividad y deja
/// todas las referencias conectadas. Despues solo hay que arrastrar el objeto
/// "ContenidoActividadX" dentro del ImageTarget de Vuforia.
/// </summary>
public static class ConstructorEscenasRA
{
    static readonly Color ColorPanel = new Color(0.07f, 0.09f, 0.13f, 0.92f);
    static readonly Color ColorApagado = new Color(0.25f, 0.25f, 0.28f);
    static readonly Color ColorAcento = new Color(0.2f, 0.8f, 1f);
    static readonly Color ColorBoton = new Color(0.16f, 0.45f, 0.85f);
    static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);
    static readonly Vector2 Arriba = new Vector2(0.5f, 1f);
    static readonly Vector2 Abajo = new Vector2(0.5f, 0f);

    static Sprite SpriteUI { get { return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); } }
    static Sprite SpriteCirculo { get { return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"); } }
    static Sprite SpriteCampo { get { return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"); } }

    // =====================================================================
    //  ACTIVIDAD 1
    // =====================================================================
    [MenuItem("RA/Crear contenido Actividad 1 (Panel IoT)")]
    static void CrearActividad1()
    {
        GameObject raiz = new GameObject("ContenidoActividad1");
        Undo.RegisterCreatedObjectUndo(raiz, "Crear contenido Actividad 1");

        Esp32Http cliente = CrearCliente(raiz.transform, "/state");

        // ----- Panel flotante en espacio mundial (25.6 cm x 16.8 cm) -----
        GameObject canvasGO = CrearCanvasMundo("PanelIoT", raiz.transform, new Vector2(640, 420), 0.0004f);
        canvasGO.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        Transform c = canvasGO.transform;

        Image fondo = Imagen(c, "Fondo", SpriteUI, ColorPanel, Centro, Vector2.zero, new Vector2(640, 420));
        fondo.type = Image.Type.Sliced;

        Texto(c, "Titulo", "PANEL DE INSTRUMENTACIÓN IoT", 30, Centro, new Vector2(0, 175), new Vector2(600, 50),
              TextAlignmentOptions.Center, Color.white, true);

        // Medidor circular del sensor analogico
        Imagen(c, "MedidorFondo", SpriteCirculo, ColorApagado, Centro, new Vector2(-180, -5), new Vector2(230, 230));
        Image medidor = Imagen(c, "Medidor", SpriteCirculo, ColorAcento, Centro, new Vector2(-180, -5), new Vector2(230, 230));
        medidor.type = Image.Type.Filled;
        medidor.fillMethod = Image.FillMethod.Radial360;
        medidor.fillOrigin = (int)Image.Origin360.Bottom;
        medidor.fillClockwise = true;
        medidor.fillAmount = 0.5f;
        Imagen(c, "MedidorCentro", SpriteCirculo, new Color(0.07f, 0.09f, 0.13f, 1f), Centro, new Vector2(-180, -5), new Vector2(170, 170));
        TextMeshProUGUI textoPorcentaje = Texto(c, "TextoPorcentaje", "0 %", 46, Centro, new Vector2(-180, 10), new Vector2(170, 60),
              TextAlignmentOptions.Center, Color.white, true);
        TextMeshProUGUI textoVoltaje = Texto(c, "TextoVoltaje", "-.-- V", 24, Centro, new Vector2(-180, -35), new Vector2(170, 36),
              TextAlignmentOptions.Center, ColorAcento, false);
        Texto(c, "EtiquetaSensor", "POTENCIÓMETRO", 20, Centro, new Vector2(-180, -150), new Vector2(260, 30),
              TextAlignmentOptions.Center, new Color(0.7f, 0.75f, 0.8f), false);

        // Entrada digital
        Image luzPulsador = Imagen(c, "LuzPulsador", SpriteCirculo, ColorApagado, Centro, new Vector2(30, 95), new Vector2(56, 56));
        Texto(c, "EtiquetaPulsador", "PULSADOR", 20, Centro, new Vector2(180, 110), new Vector2(220, 30),
              TextAlignmentOptions.Left, new Color(0.7f, 0.75f, 0.8f), false);
        TextMeshProUGUI textoPulsador = Texto(c, "TextoPulsador", "---", 26, Centro, new Vector2(180, 80), new Vector2(220, 36),
              TextAlignmentOptions.Left, Color.white, true);

        // Actuadores
        Image luzLed1 = Imagen(c, "LuzLed1", SpriteCirculo, ColorApagado, Centro, new Vector2(30, 0), new Vector2(56, 56));
        Button botonLed1 = Boton(c, "BotonLed1", "LED 1", 26, Centro, new Vector2(180, 0), new Vector2(220, 64));
        Image luzLed2 = Imagen(c, "LuzLed2", SpriteCirculo, ColorApagado, Centro, new Vector2(30, -90), new Vector2(56, 56));
        Button botonLed2 = Boton(c, "BotonLed2", "LED 2", 26, Centro, new Vector2(180, -90), new Vector2(220, 64));

        Texto(c, "Ayuda", "Toca un botón para encender o apagar el LED físico", 16, Centro, new Vector2(0, -185), new Vector2(600, 28),
              TextAlignmentOptions.Center, new Color(0.6f, 0.65f, 0.7f), false);

        PanelIoT panel = canvasGO.AddComponent<PanelIoT>();
        panel.cliente = cliente;
        panel.medidor = medidor;
        panel.textoPorcentaje = textoPorcentaje;
        panel.textoVoltaje = textoVoltaje;
        panel.luzPulsador = luzPulsador;
        panel.textoPulsador = textoPulsador;
        panel.luzLed1 = luzLed1;
        panel.luzLed2 = luzLed2;
        panel.botonLed1 = botonLed1;
        panel.botonLed2 = botonLed2;

        // ----- Barra de conexion en pantalla -----
        GameObject overlay = CrearCanvasPantalla("CanvasPantalla", raiz.transform);
        CrearBarraConexion(overlay.transform, cliente);

        // ----- Modelo 3D del circuito -----
        CrearCircuitoActividad1(raiz.transform, cliente);

        Finalizar(raiz);
    }

    static void CrearCircuitoActividad1(Transform raiz, Esp32Http cliente)
    {
        Transform c = CrearMontaje(raiz, "Circuito3D");
        CrearProtoboardConEsp32(c);

        Material negro = Mat("PiezaNegra", new Color(0.08f, 0.09f, 0.1f), false);
        Material gris = Mat("PiezaGris", new Color(0.6f, 0.6f, 0.62f), false);
        Material beige = Mat("Resistencia", new Color(0.85f, 0.7f, 0.5f), false);
        Material cableRojo = Mat("CableRojo", new Color(0.9f, 0.1f, 0.1f), false);
        Material cableAzul = Mat("CableAzul", new Color(0.1f, 0.3f, 0.9f), false);
        Material cableAmarillo = Mat("CableAmarillo", new Color(1f, 0.8f, 0.1f), false);
        Material cableVerde = Mat("CableVerde", new Color(0.1f, 0.7f, 0.3f), false);
        Material cableNaranja = Mat("CableNaranja", new Color(1f, 0.5f, 0.1f), false);

        // Potenciometro (sensor analogico) -> GPIO34
        Transform perilla = CrearPotenciometro(c, 0f, -0.012f);
        Cable(c, new Vector3(-0.007f, AltoCable, -0.012f), new Vector3(-0.007f, AltoCable, 0.030f), cableRojo);
        Cable(c, new Vector3(0.007f, AltoCable, -0.012f), new Vector3(0.007f, AltoCable, -0.030f), cableAzul);
        Cable(c, new Vector3(0f, AltoCable, -0.020f), new Vector3(-0.050f, AltoCable, -0.010f), cableAmarillo);

        // Pulsador (entrada digital) -> GPIO27
        Pieza(PrimitiveType.Cube, "PulsadorBase", c, new Vector3(0.040f, 0.011f, 0f), new Vector3(0.012f, 0.006f, 0.012f), negro);
        Transform tapa = Pieza(PrimitiveType.Cylinder, "PulsadorTapa", c, new Vector3(0.040f, 0.0155f, 0f),
                               new Vector3(0.007f, 0.0015f, 0.007f), gris).transform;
        Cable(c, new Vector3(0.040f, AltoCable, -0.006f), new Vector3(0.040f, AltoCable, -0.030f), cableAzul);
        Cable(c, new Vector3(0.034f, AltoCable, 0.006f), new Vector3(-0.050f, AltoCable, 0.006f), cableVerde);

        // LEDs (actuadores) con su resistencia de 220 ohm -> GPIO25 y GPIO26
        Renderer led1 = CrearLed(c, "Led1", 0.075f, 0.012f);
        Renderer led2 = CrearLed(c, "Led2", 0.095f, 0.012f);
        Resistencia(c, "R220_1", 0.075f, -0.008f, beige, true);
        Resistencia(c, "R220_2", 0.095f, -0.008f, beige, true);
        Cable(c, new Vector3(0.075f, AltoCable, 0.008f), new Vector3(0.075f, AltoCable, 0.000f), gris);
        Cable(c, new Vector3(0.095f, AltoCable, 0.008f), new Vector3(0.095f, AltoCable, 0.000f), gris);
        Cable(c, new Vector3(0.075f, AltoCable, -0.016f), new Vector3(0.075f, AltoCable, -0.030f), cableAzul);
        Cable(c, new Vector3(0.095f, AltoCable, -0.016f), new Vector3(0.095f, AltoCable, -0.030f), cableAzul);
        Cable(c, new Vector3(0.071f, 0.013f, 0.016f), new Vector3(-0.050f, 0.013f, 0.012f), cableNaranja);
        Cable(c, new Vector3(0.091f, 0.0145f, 0.016f), new Vector3(-0.050f, 0.0145f, -0.004f), cableAmarillo);

        Circuito3DActividad1 comp = c.gameObject.AddComponent<Circuito3DActividad1>();
        comp.cliente = cliente;
        comp.led1 = led1;
        comp.led2 = led2;
        comp.perilla = perilla;
        comp.tapaPulsador = tapa;
    }

    // =====================================================================
    //  ACTIVIDAD 2
    // =====================================================================
    [MenuItem("RA/Crear contenido Actividad 2 (Gemelo digital y guia)")]
    static void CrearActividad2()
    {
        GameObject raiz = new GameObject("ContenidoActividad2");
        Undo.RegisterCreatedObjectUndo(raiz, "Crear contenido Actividad 2");

        Esp32Http cliente = CrearCliente(raiz.transform, "/voltages");
        GemeloDigital gemelo = raiz.AddComponent<GemeloDigital>();
        gemelo.cliente = cliente;

        // ----- Modelo 3D del circuito -----
        Transform perilla = CrearCircuitoActividad2(raiz.transform);
        Circuito3DActividad2 modelo = perilla.root.GetComponentInChildren<Circuito3DActividad2>();
        modelo.gemelo = gemelo;

        // ----- Anclas de los nodos (coinciden con el modelo 3D y con el dibujo de la pagina) -----
        Transform anclas = CrearMontaje(raiz.transform, "Anclas");
        Material matNodo = Mat("NodoCian", ColorAcento, true);
        Material matGnd = Mat("NodoGris", new Color(0.75f, 0.75f, 0.75f), true);
        Material matVcc = Mat("NodoRojo", new Color(1f, 0.3f, 0.3f), true);

        string[] ids = { "3V3", "N1", "N2", "N3", "GND" };
        Vector3[] posiciones =
        {
            new Vector3(-0.045f, AltoCable, 0.012f),
            new Vector3(0.005f, AltoCable, 0.012f),
            new Vector3(0.055f, AltoCable, 0.012f),
            new Vector3(0.030f, AltoCable, -0.026f),
            new Vector3(0.105f, AltoCable, 0.012f)
        };
        float[] alturas = { 0.045f, 0.045f, 0.045f, 0.028f, 0.045f };
        Material[] materiales = { matVcc, matNodo, matNodo, matNodo, matGnd };

        List<EtiquetaVoltaje> etiquetas = new List<EtiquetaVoltaje>();
        List<GuiaMedicion.PuntoNodo> puntos = new List<GuiaMedicion.PuntoNodo>();
        List<GuiaMedicion.BotonNodo> botones = new List<GuiaMedicion.BotonNodo>();

        for (int n = 0; n < ids.Length; n++)
        {
            Transform ancla = CrearAncla("Ancla" + ids[n], anclas, posiciones[n], materiales[n]);
            EtiquetaVoltaje etiqueta = CrearEtiqueta(ancla, ids[n], alturas[n]);
            if (ids[n] == "3V3") etiqueta.texto.text = "3V3\n3.30 V";
            else if (ids[n] == "GND") etiqueta.texto.text = "GND\n0.00 V";
            else etiquetas.Add(etiqueta);

            puntos.Add(new GuiaMedicion.PuntoNodo { id = ids[n], ancla = ancla });
            botones.Add(new GuiaMedicion.BotonNodo { id = ids[n], boton = etiqueta.GetComponent<Button>() });
        }
        gemelo.etiquetas = etiquetas.ToArray();

        // ----- Sondas del usuario y guias que senalan donde van -----
        Material matRojo = Mat("SondaRoja", new Color(0.95f, 0.1f, 0.1f), true);
        Material matNegro = Mat("SondaNegra", new Color(0.05f, 0.05f, 0.05f), true);
        Transform sondaRoja = CrearSonda("SondaRoja", raiz.transform, matRojo, -25f);
        Transform sondaNegra = CrearSonda("SondaNegra", raiz.transform, matNegro, 25f);
        Transform guiaRoja = CrearGuia("GuiaRoja", raiz.transform, matRojo);
        Transform guiaNegra = CrearGuia("GuiaNegra", raiz.transform, matNegro);

        // ----- Interfaz en pantalla -----
        GameObject overlay = CrearCanvasPantalla("CanvasPantalla", raiz.transform);
        Transform o = overlay.transform;
        CrearBarraConexion(o, cliente);

        Button botonAbrir = Boton(o, "BotonGuiaDeMedicion", "Guía de Medición", 40, Abajo, new Vector2(0, 60), new Vector2(600, 120));

        Image panelImg = Imagen(o, "PanelGuia", SpriteUI, ColorPanel, Abajo, Vector2.zero, new Vector2(1080, 680));
        panelImg.type = Image.Type.Sliced;
        panelImg.raycastTarget = true;
        Transform p = panelImg.transform;

        TextMeshProUGUI textoPaso = Texto(p, "TextoPaso", "Paso 1 de 5", 36, Arriba, new Vector2(-50, -20), new Vector2(900, 70),
              TextAlignmentOptions.Left, ColorAcento, true);
        Button botonCerrar = Boton(p, "BotonCerrar", "X", 34, Arriba, new Vector2(480, -20), new Vector2(90, 70));
        TextMeshProUGUI textoInstruccion = Texto(p, "TextoInstruccion", "", 28, Arriba, new Vector2(0, -95), new Vector2(1000, 150),
              TextAlignmentOptions.TopLeft, Color.white, false);

        // Zona de medicion: sondas, nodos y multimetro virtual
        GameObject zona = new GameObject("ZonaMedicion", typeof(RectTransform));
        zona.transform.SetParent(p, false);
        Ubicar(zona, Arriba, new Vector2(0, -250), new Vector2(1080, 300));
        Transform z = zona.transform;

        Button botonSondaRoja = Boton(z, "BotonSondaRoja", "> ROJA: sin ubicar", 30, Arriba, new Vector2(-270, 0), new Vector2(500, 80));
        Button botonSondaNegra = Boton(z, "BotonSondaNegra", "NEGRA: sin ubicar", 30, Arriba, new Vector2(270, 0), new Vector2(500, 80));

        for (int n = 0; n < ids.Length; n++)
        {
            Button b = Boton(z, "BotonNodo" + ids[n], ids[n], 30, Arriba, new Vector2(-420 + 210 * n, -95), new Vector2(190, 75));
            botones.Add(new GuiaMedicion.BotonNodo { id = ids[n], boton = b });
        }

        Image lcd = Imagen(z, "Multimetro", SpriteUI, new Color(0.62f, 0.72f, 0.58f), Arriba, new Vector2(-300, -190), new Vector2(420, 110));
        lcd.type = Image.Type.Sliced;
        Texto(lcd.transform, "Rotulo", "MULTÍMETRO  V DC", 16, Arriba, new Vector2(0, -4), new Vector2(400, 22),
              TextAlignmentOptions.Center, new Color(0.1f, 0.15f, 0.1f), true);
        TextMeshProUGUI textoMultimetro = Texto(lcd.transform, "Lectura", "-.-- V", 60, Arriba, new Vector2(0, -26), new Vector2(400, 80),
              TextAlignmentOptions.Center, new Color(0.05f, 0.1f, 0.05f), true);

        TextMeshProUGUI textoResultado = Texto(p, "TextoResultado", "", 24, Arriba, new Vector2(215, -440), new Vector2(580, 110),
              TextAlignmentOptions.Left, Color.white, false);

        Button botonAnterior = Boton(p, "BotonAnterior", "Anterior", 34, Arriba, new Vector2(-270, -575), new Vector2(460, 85));
        Button botonSiguiente = Boton(p, "BotonSiguiente", "Siguiente", 34, Arriba, new Vector2(270, -575), new Vector2(460, 85));

        GuiaMedicion guia = overlay.AddComponent<GuiaMedicion>();
        guia.gemelo = gemelo;
        guia.nodos = puntos.ToArray();
        guia.botonesNodo = botones.ToArray();
        guia.botonAbrir = botonAbrir;
        guia.panel = panelImg.gameObject;
        guia.textoPaso = textoPaso;
        guia.textoInstruccion = textoInstruccion;
        guia.textoResultado = textoResultado;
        guia.textoMultimetro = textoMultimetro;
        guia.zonaMedicion = zona;
        guia.botonSondaRoja = botonSondaRoja;
        guia.botonSondaNegra = botonSondaNegra;
        guia.botonAnterior = botonAnterior;
        guia.botonSiguiente = botonSiguiente;
        guia.botonCerrar = botonCerrar;
        guia.sondaRoja = sondaRoja;
        guia.sondaNegra = sondaNegra;
        guia.guiaRoja = guiaRoja;
        guia.guiaNegra = guiaNegra;

        guia.pasos = new List<GuiaMedicion.Paso>
        {
            new GuiaMedicion.Paso
            {
                titulo = "Prepara el multímetro",
                instruccion = "En un multímetro real: gira el selector a VOLTAJE DC (V con línea recta), escala de 20 V. " +
                              "Cable NEGRO en el borne COM y cable ROJO en el borne V/Ω. " +
                              "Aquí usarás un multímetro virtual ya configurado así.",
                requiereMedicion = false
            },
            new GuiaMedicion.Paso
            {
                titulo = "Voltaje en el nodo N1",
                instruccion = "Mide N1 respecto a tierra: sonda ROJA en N1 (unión de R1 y R2) y sonda NEGRA en GND. " +
                              "Las guías de color que suben y bajan muestran dónde va cada una.",
                requiereMedicion = true, nodoRojo = "N1", nodoNegro = "GND"
            },
            new GuiaMedicion.Paso
            {
                titulo = "Voltaje en el nodo N2",
                instruccion = "Deja la sonda NEGRA en GND y pon la sonda ROJA en N2 (unión de R2 y R3). " +
                              "Debe marcar cerca de la mitad de N1.",
                requiereMedicion = true, nodoRojo = "N2", nodoNegro = "GND"
            },
            new GuiaMedicion.Paso
            {
                titulo = "Voltaje del potenciómetro (N3)",
                instruccion = "Sonda ROJA en el cursor del potenciómetro (N3) y sonda NEGRA en GND. " +
                              "Gira el potenciómetro y observa cómo cambia la lectura.",
                requiereMedicion = true, nodoRojo = "N3", nodoNegro = "GND"
            },
            new GuiaMedicion.Paso
            {
                titulo = "Caída de tensión en R2",
                instruccion = "Ahora mide entre dos nodos: sonda ROJA en N1 y sonda NEGRA en N2. " +
                              "La lectura es la caída de tensión sobre R2 (N1 menos N2).",
                requiereMedicion = true, nodoRojo = "N1", nodoNegro = "N2"
            },
            new GuiaMedicion.Paso
            {
                titulo = "Caída de tensión en R1",
                instruccion = "Sonda ROJA en 3V3 (alimentación) y sonda NEGRA en N1. " +
                              "Como las tres resistencias son iguales, la caída es la misma que en R2.",
                requiereMedicion = true, nodoRojo = "3V3", nodoNegro = "N1"
            }
        };

        Finalizar(raiz);
    }

    /// <summary>Divisor de tres resistencias de 10k y potenciometro. Devuelve la perilla del potenciometro.</summary>
    static Transform CrearCircuitoActividad2(Transform raiz)
    {
        Transform c = CrearMontaje(raiz, "Circuito3D");
        CrearProtoboardConEsp32(c);

        Material beige = Mat("Resistencia", new Color(0.85f, 0.7f, 0.5f), false);
        Material gris = Mat("PiezaGris", new Color(0.6f, 0.6f, 0.62f), false);
        Material cableRojo = Mat("CableRojo", new Color(0.9f, 0.1f, 0.1f), false);
        Material cableAzul = Mat("CableAzul", new Color(0.1f, 0.3f, 0.9f), false);
        Material cableAmarillo = Mat("CableAmarillo", new Color(1f, 0.8f, 0.1f), false);
        Material cableVerde = Mat("CableVerde", new Color(0.1f, 0.7f, 0.3f), false);
        Material cableNaranja = Mat("CableNaranja", new Color(1f, 0.5f, 0.1f), false);

        // 3V3 -[R1]- N1 -[R2]- N2 -[R3]- GND
        float zr = 0.012f;
        Resistencia(c, "R1", -0.020f, zr, beige, false);
        Resistencia(c, "R2", 0.030f, zr, beige, false);
        Resistencia(c, "R3", 0.080f, zr, beige, false);
        Cable(c, new Vector3(-0.045f, AltoCable, zr), new Vector3(-0.028f, AltoCable, zr), gris);
        Cable(c, new Vector3(-0.012f, AltoCable, zr), new Vector3(0.022f, AltoCable, zr), gris);
        Cable(c, new Vector3(0.038f, AltoCable, zr), new Vector3(0.072f, AltoCable, zr), gris);
        Cable(c, new Vector3(0.088f, AltoCable, zr), new Vector3(0.105f, AltoCable, zr), gris);
        Cable(c, new Vector3(-0.045f, AltoCable, zr), new Vector3(-0.045f, AltoCable, 0.030f), cableRojo);
        Cable(c, new Vector3(0.105f, AltoCable, zr), new Vector3(0.105f, AltoCable, -0.030f), cableAzul);

        // Potenciometro: extremos a 3V3 y GND, cursor = N3
        Transform perilla = CrearPotenciometro(c, 0.030f, -0.014f);
        Cable(c, new Vector3(0.023f, 0.0125f, -0.014f), new Vector3(0.016f, 0.0125f, 0.030f), cableRojo);
        Cable(c, new Vector3(0.037f, AltoCable, -0.014f), new Vector3(0.037f, AltoCable, -0.030f), cableAzul);
        Cable(c, new Vector3(0.030f, AltoCable, -0.021f), new Vector3(0.030f, AltoCable, -0.026f), gris);

        // Cables de medicion hacia los pines analogicos del ESP32
        Cable(c, new Vector3(0.005f, 0.0135f, zr), new Vector3(-0.050f, 0.0135f, 0.008f), cableAmarillo);   // N1 -> GPIO34
        Cable(c, new Vector3(0.055f, 0.0150f, zr), new Vector3(-0.050f, 0.0150f, 0.002f), cableNaranja);    // N2 -> GPIO35
        Cable(c, new Vector3(0.030f, 0.0135f, -0.026f), new Vector3(-0.050f, 0.0135f, -0.008f), cableVerde); // N3 -> GPIO32

        c.gameObject.AddComponent<Circuito3DActividad2>().perilla = perilla;
        return perilla;
    }

    // =====================================================================
    //  Bloques reutilizables
    // =====================================================================
    static Esp32Http CrearCliente(Transform padre, string ruta)
    {
        GameObject go = new GameObject("ClienteESP32");
        go.transform.SetParent(padre, false);
        Esp32Http cliente = go.AddComponent<Esp32Http>();
        cliente.rutaEstado = ruta;
        return cliente;
    }

    static void CrearBarraConexion(Transform canvas, Esp32Http cliente)
    {
        Image barra = Imagen(canvas, "BarraConexion", SpriteUI, ColorPanel, Arriba, Vector2.zero, new Vector2(1080, 130));
        barra.type = Image.Type.Sliced;
        Transform b = barra.transform;

        TMP_InputField campoIp = Campo(b, "CampoIp", "IP del ESP32", 32, Arriba, new Vector2(-290, -20), new Vector2(440, 90));
        Button botonConectar = Boton(b, "BotonConectar", "Conectar", 32, Arriba, new Vector2(70, -20), new Vector2(240, 90));
        TextMeshProUGUI textoEstado = Texto(b, "TextoEstado", "Sin conexión", 26, Arriba, new Vector2(365, -20), new Vector2(320, 90),
              TextAlignmentOptions.Left, Color.white, false);

        ConexionUI ui = barra.gameObject.AddComponent<ConexionUI>();
        ui.cliente = cliente;
        ui.campoIp = campoIp;
        ui.botonConectar = botonConectar;
        ui.textoEstado = textoEstado;
    }

    // Altura a la que van los cables y los puntos de los nodos sobre la protoboard
    const float AltoCable = 0.0105f;
    // La protoboard se dibuja debajo del marcador (marcador de 14 x 10 cm centrado en el origen)
    static readonly Vector3 CentroProtoboard = new Vector3(0f, 0f, -0.105f);

    /// <summary>Contenedor cuyo origen es el centro de la protoboard.</summary>
    static Transform CrearMontaje(Transform raiz, string nombre)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(raiz, false);
        go.transform.localPosition = CentroProtoboard;
        return go.transform;
    }

    static GameObject Pieza(PrimitiveType tipo, string nombre, Transform padre, Vector3 posicion, Vector3 escala, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    static void Cable(Transform padre, Vector3 desde, Vector3 hasta, Material material)
    {
        Vector3 direccion = hasta - desde;
        GameObject go = Pieza(PrimitiveType.Cube, "Cable", padre, (desde + hasta) * 0.5f,
                              new Vector3(0.0015f, 0.0015f, direccion.magnitude), material);
        go.transform.localRotation = Quaternion.LookRotation(direccion.normalized);
    }

    /// <summary>Resistencia de 16 mm centrada en (x, z). A lo largo de X, o de Z si vertical.</summary>
    static void Resistencia(Transform padre, string nombre, float x, float z, Material material, bool vertical)
    {
        GameObject go = Pieza(PrimitiveType.Cylinder, nombre, padre, new Vector3(x, 0.0115f, z),
                              new Vector3(0.006f, 0.008f, 0.006f), material);
        go.transform.localRotation = vertical ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.Euler(0f, 0f, 90f);
    }

    /// <summary>Protoboard de 22 x 7 cm con rieles de alimentacion y el ESP32 a la izquierda.</summary>
    static void CrearProtoboardConEsp32(Transform c)
    {
        Material blanco = Mat("Protoboard", new Color(0.93f, 0.93f, 0.9f), false);
        Material rielRojo = Mat("CableRojo", new Color(0.9f, 0.1f, 0.1f), false);
        Material rielAzul = Mat("CableAzul", new Color(0.1f, 0.3f, 0.9f), false);
        Material pcb = Mat("PiezaNegra", new Color(0.08f, 0.09f, 0.1f), false);
        Material plata = Mat("Plata", new Color(0.78f, 0.78f, 0.8f), false);

        Pieza(PrimitiveType.Cube, "Protoboard", c, new Vector3(0f, 0.004f, 0f), new Vector3(0.22f, 0.008f, 0.07f), blanco);
        Pieza(PrimitiveType.Cube, "Riel3V3", c, new Vector3(0f, 0.0083f, 0.030f), new Vector3(0.21f, 0.0006f, 0.002f), rielRojo);
        Pieza(PrimitiveType.Cube, "RielGND", c, new Vector3(0f, 0.0083f, -0.030f), new Vector3(0.21f, 0.0006f, 0.002f), rielAzul);

        Pieza(PrimitiveType.Cube, "ESP32", c, new Vector3(-0.075f, 0.010f, 0f), new Vector3(0.05f, 0.004f, 0.028f), pcb);
        Pieza(PrimitiveType.Cube, "ESP32_Modulo", c, new Vector3(-0.083f, 0.0135f, 0f), new Vector3(0.018f, 0.003f, 0.016f), plata);
        Pieza(PrimitiveType.Cube, "ESP32_USB", c, new Vector3(-0.099f, 0.013f, 0f), new Vector3(0.006f, 0.003f, 0.008f), plata);
        Cable(c, new Vector3(-0.060f, AltoCable, 0.014f), new Vector3(-0.060f, AltoCable, 0.030f), rielRojo);   // 3V3
        Cable(c, new Vector3(-0.068f, AltoCable, -0.014f), new Vector3(-0.068f, AltoCable, -0.030f), rielAzul); // GND
    }

    /// <summary>Potenciometro centrado en (x, z). Devuelve la perilla, que gira sobre el eje Y.</summary>
    static Transform CrearPotenciometro(Transform c, float x, float z)
    {
        Material azul = Mat("Potenciometro", new Color(0.15f, 0.35f, 0.8f), false);
        Material blanco = Mat("Protoboard", new Color(0.93f, 0.93f, 0.9f), false);
        Material negro = Mat("PiezaNegra", new Color(0.08f, 0.09f, 0.1f), false);

        Pieza(PrimitiveType.Cube, "PotenciometroBase", c, new Vector3(x, 0.012f, z), new Vector3(0.014f, 0.008f, 0.014f), azul);
        GameObject perilla = new GameObject("PotenciometroPerilla");
        perilla.transform.SetParent(c, false);
        perilla.transform.localPosition = new Vector3(x, 0.0185f, z);
        Pieza(PrimitiveType.Cylinder, "Perilla", perilla.transform, Vector3.zero, new Vector3(0.010f, 0.0025f, 0.010f), blanco);
        Pieza(PrimitiveType.Cube, "Marca", perilla.transform, new Vector3(0f, 0.003f, 0.0028f), new Vector3(0.0015f, 0.001f, 0.0045f), negro);
        return perilla.transform;
    }

    static Renderer CrearLed(Transform c, string nombre, float x, float z)
    {
        Material material = Mat(nombre, new Color(0.3f, 0.3f, 0.32f), true);
        Material gris = Mat("PiezaGris", new Color(0.6f, 0.6f, 0.62f), false);
        Pieza(PrimitiveType.Cylinder, nombre + "_Base", c, new Vector3(x, 0.0105f, z), new Vector3(0.007f, 0.0025f, 0.007f), gris);
        return Pieza(PrimitiveType.Sphere, nombre, c, new Vector3(x, 0.016f, z), Vector3.one * 0.008f, material).GetComponent<Renderer>();
    }

    static Transform CrearAncla(string nombre, Transform padre, Vector3 posicion, Material material)
    {
        GameObject ancla = new GameObject(nombre);
        ancla.transform.SetParent(padre, false);
        ancla.transform.localPosition = posicion;
        Pieza(PrimitiveType.Sphere, "Punto", ancla.transform, Vector3.zero, Vector3.one * 0.004f, material);
        return ancla.transform;
    }

    /// <summary>Etiqueta flotante de 3.3 x 1.65 cm sobre el nodo. Tambien es un boton que selecciona el nodo.</summary>
    static EtiquetaVoltaje CrearEtiqueta(Transform ancla, string idNodo, float altura)
    {
        GameObject canvasGO = CrearCanvasMundo("Etiqueta" + idNodo, ancla, new Vector2(220, 110), 0.00015f);
        canvasGO.transform.localPosition = new Vector3(0f, altura, 0f);

        Image fondo = Imagen(canvasGO.transform, "Fondo", SpriteUI, ColorPanel, Centro, Vector2.zero, new Vector2(220, 110));
        fondo.type = Image.Type.Sliced;
        fondo.raycastTarget = true;
        TextMeshProUGUI texto = Texto(canvasGO.transform, "Texto", idNodo + "\n-.-- V", 36, Centro, Vector2.zero, new Vector2(210, 100),
              TextAlignmentOptions.Center, Color.white, true);

        Button boton = canvasGO.AddComponent<Button>();
        boton.targetGraphic = fondo;

        EtiquetaVoltaje etiqueta = canvasGO.AddComponent<EtiquetaVoltaje>();
        etiqueta.idNodo = idNodo;
        etiqueta.texto = texto;
        return etiqueta;
    }

    /// <summary>Sonda del multimetro: la punta queda en el origen del objeto.</summary>
    static Transform CrearSonda(string nombre, Transform padre, Material material, float inclinacion)
    {
        GameObject raiz = new GameObject(nombre);
        raiz.transform.SetParent(padre, false);
        raiz.transform.localRotation = Quaternion.Euler(-20f, 0f, inclinacion);

        Material plata = Mat("Plata", new Color(0.78f, 0.78f, 0.8f), false);
        Pieza(PrimitiveType.Cylinder, "Punta", raiz.transform, new Vector3(0f, 0.008f, 0f), new Vector3(0.0015f, 0.008f, 0.0015f), plata);
        Pieza(PrimitiveType.Cylinder, "Mango", raiz.transform, new Vector3(0f, 0.041f, 0f), new Vector3(0.006f, 0.025f, 0.006f), material);

        raiz.SetActive(false);
        return raiz.transform;
    }

    /// <summary>Guia que sube y baja sobre el nodo donde debe ir una sonda.</summary>
    static Transform CrearGuia(string nombre, Transform padre, Material material)
    {
        GameObject raiz = new GameObject(nombre);
        raiz.transform.SetParent(padre, false);

        GameObject flecha = new GameObject("Flecha");
        flecha.transform.SetParent(raiz.transform, false);
        flecha.transform.localPosition = new Vector3(0f, 0.006f, 0f);
        Pieza(PrimitiveType.Sphere, "Cabeza", flecha.transform, Vector3.zero, Vector3.one * 0.007f, material);
        Pieza(PrimitiveType.Cylinder, "Cuerpo", flecha.transform, new Vector3(0f, 0.010f, 0f), new Vector3(0.0025f, 0.008f, 0.0025f), material);
        flecha.AddComponent<FlechaAnimada>().amplitud = 0.006f;

        raiz.SetActive(false);
        return raiz.transform;
    }

    /// <summary>Material guardado en Assets/Materiales. sinLuz = color plano (para nodos, LEDs y sondas).</summary>
    static Material Mat(string nombre, Color color, bool sinLuz)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materiales")) AssetDatabase.CreateFolder("Assets", "Materiales");
        string ruta = "Assets/Materiales/" + nombre + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            Shader shader = Shader.Find(sinLuz ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find(sinLuz ? "Unlit/Color" : "Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, ruta);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    // =====================================================================
    //  Utilidades de interfaz
    // =====================================================================
    static GameObject CrearCanvasMundo(string nombre, Transform padre, Vector2 tamano, float escala)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(padre, false);
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
        RectTransform rt = (RectTransform)go.transform;
        rt.sizeDelta = tamano;
        rt.localScale = Vector3.one * escala;
        go.AddComponent<Billboard>();
        return go;
    }

    static GameObject CrearCanvasPantalla(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(padre, false);
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0f;
        return go;
    }

    static void Ubicar(GameObject go, Vector2 ancla, Vector2 posicion, Vector2 tamano)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = ancla;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;
    }

    static Image Imagen(Transform padre, string nombre, Sprite sprite, Color color, Vector2 ancla, Vector2 posicion, Vector2 tamano)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        Ubicar(go, ancla, posicion, tamano);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI Texto(Transform padre, string nombre, string contenido, float tamanoFuente, Vector2 ancla,
                                 Vector2 posicion, Vector2 tamano, TextAlignmentOptions alineacion, Color color, bool negrita)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        Ubicar(go, ancla, posicion, tamano);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = contenido;
        t.fontSize = tamanoFuente;
        t.alignment = alineacion;
        t.color = color;
        t.fontStyle = negrita ? FontStyles.Bold : FontStyles.Normal;
        t.raycastTarget = false;
        return t;
    }

    static Button Boton(Transform padre, string nombre, string etiqueta, float tamanoFuente, Vector2 ancla, Vector2 posicion, Vector2 tamano)
    {
        TMP_DefaultControls.Resources recursos = new TMP_DefaultControls.Resources();
        recursos.standard = SpriteUI;
        GameObject go = TMP_DefaultControls.CreateButton(recursos);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        Ubicar(go, ancla, posicion, tamano);
        go.GetComponent<Image>().color = ColorBoton;

        TMP_Text t = go.GetComponentInChildren<TMP_Text>(true);
        t.text = etiqueta;
        t.fontSize = tamanoFuente;
        t.color = Color.white;
        t.fontStyle = FontStyles.Bold;
        return go.GetComponent<Button>();
    }

    static TMP_InputField Campo(Transform padre, string nombre, string textoGuia, float tamanoFuente, Vector2 ancla, Vector2 posicion, Vector2 tamano)
    {
        TMP_DefaultControls.Resources recursos = new TMP_DefaultControls.Resources();
        recursos.inputField = SpriteCampo;
        GameObject go = TMP_DefaultControls.CreateInputField(recursos);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        Ubicar(go, ancla, posicion, tamano);

        TMP_InputField campo = go.GetComponent<TMP_InputField>();
        campo.pointSize = tamanoFuente;
        if (campo.textComponent != null) campo.textComponent.fontSize = tamanoFuente;
        TMP_Text guia = campo.placeholder as TMP_Text;
        if (guia != null)
        {
            guia.text = textoGuia;
            guia.fontSize = tamanoFuente;
        }
        return campo;
    }

    static void Finalizar(GameObject raiz)
    {
        if (Object.FindObjectOfType<EventSystem>() == null)
            EditorApplication.ExecuteMenuItem("GameObject/UI/Event System");

        Selection.activeGameObject = raiz;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[RA] Se creó '" + raiz.name + "'. Arrástralo dentro del ImageTarget de Vuforia.");
    }
}
