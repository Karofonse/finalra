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

        Finalizar(raiz);
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

        // ----- Anclas de los nodos: MOVERLAS en la escena hasta que coincidan con el circuito real -----
        Transform anclas = new GameObject("Anclas (mover sobre el circuito)").transform;
        anclas.SetParent(raiz.transform, false);

        Material matNodo = Mat("NodoCian", ColorAcento);
        Material matGnd = Mat("NodoGris", new Color(0.75f, 0.75f, 0.75f));

        Transform anclaN1 = CrearAncla("AnclaN1", anclas, new Vector3(-0.08f, 0f, 0.02f), matNodo);
        Transform anclaN2 = CrearAncla("AnclaN2", anclas, new Vector3(0f, 0f, 0.02f), matNodo);
        Transform anclaN3 = CrearAncla("AnclaN3", anclas, new Vector3(0.08f, 0f, 0.02f), matNodo);
        Transform anclaGnd = CrearAncla("AnclaGND", anclas, new Vector3(0f, 0f, -0.06f), matGnd);

        List<EtiquetaVoltaje> etiquetas = new List<EtiquetaVoltaje>();
        etiquetas.Add(CrearEtiqueta(anclaN1, "N1"));
        etiquetas.Add(CrearEtiqueta(anclaN2, "N2"));
        etiquetas.Add(CrearEtiqueta(anclaN3, "N3"));
        CrearEtiqueta(anclaGnd, "GND").texto.text = "GND\n0.00 V";

        GemeloDigital gemelo = raiz.AddComponent<GemeloDigital>();
        gemelo.cliente = cliente;
        gemelo.etiquetas = etiquetas.ToArray();

        // ----- Sondas virtuales -----
        Transform sondaRoja = CrearSonda("SondaRoja", raiz.transform, Mat("SondaRoja", new Color(0.95f, 0.1f, 0.1f)), -20f);
        Transform sondaNegra = CrearSonda("SondaNegra", raiz.transform, Mat("SondaNegra", new Color(0.05f, 0.05f, 0.05f)), 20f);

        // ----- Interfaz en pantalla -----
        GameObject overlay = CrearCanvasPantalla("CanvasPantalla", raiz.transform);
        Transform o = overlay.transform;
        CrearBarraConexion(o, cliente);

        Button botonAbrir = Boton(o, "BotonGuiaDeMedicion", "Guía de Medición", 40, Abajo, new Vector2(0, 60), new Vector2(600, 120));

        Image panelImg = Imagen(o, "PanelGuia", SpriteUI, ColorPanel, Abajo, Vector2.zero, new Vector2(1080, 650));
        panelImg.type = Image.Type.Sliced;
        panelImg.raycastTarget = true;
        Transform p = panelImg.transform;

        TextMeshProUGUI textoPaso = Texto(p, "TextoPaso", "Paso 1 de 5", 36, Arriba, new Vector2(-50, -20), new Vector2(900, 70),
              TextAlignmentOptions.Left, ColorAcento, true);
        Button botonCerrar = Boton(p, "BotonCerrar", "X", 34, Arriba, new Vector2(480, -20), new Vector2(90, 70));
        TextMeshProUGUI textoInstruccion = Texto(p, "TextoInstruccion", "", 30, Arriba, new Vector2(0, -100), new Vector2(1000, 230),
              TextAlignmentOptions.TopLeft, Color.white, false);

        TMP_InputField campoLectura = Campo(p, "CampoLectura", "Lectura del multímetro (V)", 34, Arriba, new Vector2(-170, -345), new Vector2(520, 90));
        campoLectura.contentType = TMP_InputField.ContentType.Standard;
        Button botonVerificar = Boton(p, "BotonVerificar", "Verificar", 34, Arriba, new Vector2(290, -345), new Vector2(300, 90));

        TextMeshProUGUI textoResultado = Texto(p, "TextoResultado", "", 26, Arriba, new Vector2(0, -445), new Vector2(1000, 90),
              TextAlignmentOptions.TopLeft, Color.white, false);

        Button botonAnterior = Boton(p, "BotonAnterior", "Anterior", 34, Arriba, new Vector2(-270, -545), new Vector2(460, 85));
        Button botonSiguiente = Boton(p, "BotonSiguiente", "Siguiente", 34, Arriba, new Vector2(270, -545), new Vector2(460, 85));

        GuiaMedicion guia = overlay.AddComponent<GuiaMedicion>();
        guia.gemelo = gemelo;
        guia.botonAbrir = botonAbrir;
        guia.panel = panelImg.gameObject;
        guia.textoPaso = textoPaso;
        guia.textoInstruccion = textoInstruccion;
        guia.textoResultado = textoResultado;
        guia.campoLectura = campoLectura;
        guia.botonVerificar = botonVerificar;
        guia.botonAnterior = botonAnterior;
        guia.botonSiguiente = botonSiguiente;
        guia.botonCerrar = botonCerrar;
        guia.sondaRoja = sondaRoja;
        guia.sondaNegra = sondaNegra;

        guia.pasos = new List<GuiaMedicion.Paso>
        {
            new GuiaMedicion.Paso
            {
                titulo = "Prepara el multímetro",
                instruccion = "Gira el selector a VOLTAJE DC (V con línea recta) en la escala de 20 V.\n" +
                              "Conecta el cable NEGRO en el borne COM y el cable ROJO en el borne V/Ω.",
                requiereMedicion = false
            },
            new GuiaMedicion.Paso
            {
                titulo = "Voltaje en el nodo N1",
                instruccion = "Ubica la sonda ROJA en el nodo N1 (unión de R1 y R2) y la sonda NEGRA en GND, " +
                              "donde lo indican las sondas virtuales. Escribe la lectura del multímetro.",
                requiereMedicion = true, nodoRojo = "N1", nodoNegro = "GND", anclaRoja = anclaN1, anclaNegra = anclaGnd
            },
            new GuiaMedicion.Paso
            {
                titulo = "Voltaje en el nodo N2",
                instruccion = "Mantén la sonda NEGRA en GND y mueve la sonda ROJA al nodo N2 (unión de R2 y R3). " +
                              "Escribe la lectura.",
                requiereMedicion = true, nodoRojo = "N2", nodoNegro = "GND", anclaRoja = anclaN2, anclaNegra = anclaGnd
            },
            new GuiaMedicion.Paso
            {
                titulo = "Voltaje en el potenciómetro (N3)",
                instruccion = "Mueve la sonda ROJA al cursor del potenciómetro (pin central, nodo N3). " +
                              "No gires el potenciómetro mientras mides. Escribe la lectura.",
                requiereMedicion = true, nodoRojo = "N3", nodoNegro = "GND", anclaRoja = anclaN3, anclaNegra = anclaGnd
            },
            new GuiaMedicion.Paso
            {
                titulo = "Caída de tensión en R2",
                instruccion = "Ahora mide entre dos nodos: sonda ROJA en N1 y sonda NEGRA en N2. " +
                              "La lectura es la caída de tensión sobre R2 (N1 menos N2).",
                requiereMedicion = true, nodoRojo = "N1", nodoNegro = "N2", anclaRoja = anclaN1, anclaNegra = anclaN2
            }
        };

        Finalizar(raiz);
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

    static Transform CrearAncla(string nombre, Transform padre, Vector3 posicion, Material material)
    {
        GameObject ancla = new GameObject(nombre);
        ancla.transform.SetParent(padre, false);
        ancla.transform.localPosition = posicion;

        GameObject punto = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        punto.name = "Punto";
        Object.DestroyImmediate(punto.GetComponent<Collider>());
        punto.transform.SetParent(ancla.transform, false);
        punto.transform.localScale = Vector3.one * 0.005f;
        punto.GetComponent<Renderer>().sharedMaterial = material;
        return ancla.transform;
    }

    static EtiquetaVoltaje CrearEtiqueta(Transform ancla, string idNodo)
    {
        // 6.6 cm x 3.3 cm, flotando 4 cm sobre el nodo
        GameObject canvasGO = CrearCanvasMundo("Etiqueta" + idNodo, ancla, new Vector2(220, 110), 0.0003f);
        canvasGO.transform.localPosition = new Vector3(0f, 0.04f, 0f);

        Image fondo = Imagen(canvasGO.transform, "Fondo", SpriteUI, ColorPanel, Centro, Vector2.zero, new Vector2(220, 110));
        fondo.type = Image.Type.Sliced;
        TextMeshProUGUI texto = Texto(canvasGO.transform, "Texto", idNodo + "\n-.-- V", 36, Centro, Vector2.zero, new Vector2(210, 100),
              TextAlignmentOptions.Center, Color.white, true);

        EtiquetaVoltaje etiqueta = canvasGO.AddComponent<EtiquetaVoltaje>();
        etiqueta.idNodo = idNodo;
        etiqueta.texto = texto;
        return etiqueta;
    }

    /// <summary>Sonda virtual: la punta queda en el origen del objeto y el cuerpo oscila hacia ella.</summary>
    static Transform CrearSonda(string nombre, Transform padre, Material material, float inclinacion)
    {
        GameObject raiz = new GameObject(nombre);
        raiz.transform.SetParent(padre, false);
        raiz.transform.localRotation = Quaternion.Euler(0f, 0f, inclinacion);

        GameObject punta = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        punta.name = "Punta";
        Object.DestroyImmediate(punta.GetComponent<Collider>());
        punta.transform.SetParent(raiz.transform, false);
        punta.transform.localScale = Vector3.one * 0.007f;
        punta.GetComponent<Renderer>().sharedMaterial = material;

        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cuerpo.name = "Cuerpo";
        Object.DestroyImmediate(cuerpo.GetComponent<Collider>());
        cuerpo.transform.SetParent(raiz.transform, false);
        cuerpo.transform.localScale = new Vector3(0.006f, 0.03f, 0.006f);   // 6 cm de largo
        cuerpo.transform.localPosition = new Vector3(0f, 0.035f, 0f);
        cuerpo.GetComponent<Renderer>().sharedMaterial = material;
        cuerpo.AddComponent<FlechaAnimada>();

        raiz.SetActive(false);
        return raiz.transform;
    }

    static Material Mat(string nombre, Color color)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materiales")) AssetDatabase.CreateFolder("Assets", "Materiales");
        string ruta = "Assets/Materiales/" + nombre + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
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
