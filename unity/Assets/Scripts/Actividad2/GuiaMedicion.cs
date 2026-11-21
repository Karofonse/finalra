using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Actividad 2: "Guia de Medicion" con multimetro virtual.
/// En cada paso unas guias 3D senalan donde va cada sonda. El usuario elige una sonda
/// (roja o negra) y toca un nodo del circuito; el multimetro virtual muestra la lectura
/// y el sistema valida si las sondas quedaron donde corresponde.
/// </summary>
public class GuiaMedicion : MonoBehaviour
{
    [Serializable]
    public class Paso
    {
        public string titulo;
        [TextArea(2, 5)] public string instruccion;
        [Tooltip("Si esta activo, el usuario debe ubicar las sondas")]
        public bool requiereMedicion;
        [Tooltip("Nodo donde va la sonda roja: 3V3, N1, N2, N3, GND")]
        public string nodoRojo;
        [Tooltip("Nodo donde va la sonda negra")]
        public string nodoNegro = "GND";
    }

    [Serializable]
    public class PuntoNodo
    {
        public string id;
        public Transform ancla;
    }

    [Serializable]
    public class BotonNodo
    {
        public string id;
        public Button boton;
    }

    public GemeloDigital gemelo;
    public List<Paso> pasos = new List<Paso>();
    [Tooltip("Posicion de cada nodo sobre el circuito")]
    public PuntoNodo[] nodos;
    [Tooltip("Botones que seleccionan un nodo (los del panel y las etiquetas flotantes)")]
    public BotonNodo[] botonesNodo;

    [Header("Interfaz")]
    public Button botonAbrir;          // boton "Guia de Medicion"
    public GameObject panel;
    public TMP_Text textoPaso;
    public TMP_Text textoInstruccion;
    public TMP_Text textoResultado;
    public TMP_Text textoMultimetro;   // pantalla del multimetro virtual (panel)
    public TMP_Text textoMultimetro3D; // pantalla del multimetro 3D (opcional)
    public GameObject zonaMedicion;    // contiene sondas, nodos y multimetro
    public Button botonSondaRoja;
    public Button botonSondaNegra;
    public Button botonAnterior;
    public Button botonSiguiente;
    public Button botonCerrar;

    [Header("Objetos 3D")]
    [Tooltip("Sondas que coloca el usuario")]
    public Transform sondaRoja;
    public Transform sondaNegra;
    [Tooltip("Guias que senalan donde debe ir cada sonda")]
    public Transform guiaRoja;
    public Transform guiaNegra;

    static readonly Color Verde = new Color(0.3f, 1f, 0.4f);
    static readonly Color Rojo = new Color(1f, 0.45f, 0.35f);
    static readonly Color Amarillo = new Color(1f, 0.85f, 0.2f);
    static readonly Color RojaActiva = new Color(0.9f, 0.15f, 0.15f);
    static readonly Color RojaInactiva = new Color(0.4f, 0.12f, 0.12f);
    static readonly Color NegraActiva = new Color(0.38f, 0.38f, 0.42f);
    static readonly Color NegraInactiva = new Color(0.14f, 0.14f, 0.16f);

    int indice;
    bool validado;
    bool terminado;
    bool sondaActivaRoja = true;
    string puestoRojo;
    string puestoNegro;
    string ultimaConfiguracion;
    int fallosPaso;
    bool[] aciertoPrimerIntento;

    void Start()
    {
        botonAbrir.onClick.AddListener(Abrir);
        botonCerrar.onClick.AddListener(Cerrar);
        botonAnterior.onClick.AddListener(Anterior);
        botonSiguiente.onClick.AddListener(Siguiente);
        botonSondaRoja.onClick.AddListener(() => ElegirSonda(true));
        botonSondaNegra.onClick.AddListener(() => ElegirSonda(false));

        foreach (BotonNodo b in botonesNodo)
        {
            if (b == null || b.boton == null) continue;
            string id = b.id;
            b.boton.onClick.AddListener(() => SeleccionarNodo(id));
        }
        Cerrar();
    }

    public void Abrir()
    {
        if (pasos.Count == 0) return;
        indice = 0;
        terminado = false;
        aciertoPrimerIntento = new bool[pasos.Count];
        panel.SetActive(true);
        botonAbrir.gameObject.SetActive(false);
        MostrarPaso();
    }

    public void Cerrar()
    {
        panel.SetActive(false);
        botonAbrir.gameObject.SetActive(true);
        OcultarObjetos();
    }

    void OcultarObjetos()
    {
        Colocar(sondaRoja, null);
        Colocar(sondaNegra, null);
        Colocar(guiaRoja, null);
        Colocar(guiaNegra, null);
    }

    void MostrarPaso()
    {
        Paso p = pasos[indice];
        validado = !p.requiereMedicion;
        fallosPaso = 0;
        puestoRojo = null;
        puestoNegro = null;
        ultimaConfiguracion = null;
        sondaActivaRoja = true;

        textoPaso.text = "Paso " + (indice + 1) + " de " + pasos.Count + ": " + p.titulo;
        textoInstruccion.text = p.instruccion;
        textoResultado.text = p.requiereMedicion ? "Elige una sonda y toca el nodo donde la quieres poner." : "";
        textoResultado.color = Color.white;

        zonaMedicion.SetActive(p.requiereMedicion);
        botonAnterior.gameObject.SetActive(true);
        botonAnterior.interactable = indice > 0;
        botonSiguiente.interactable = validado;
        PonerTexto(botonSiguiente, indice == pasos.Count - 1 ? "Finalizar" : "Siguiente");

        Colocar(sondaRoja, null);
        Colocar(sondaNegra, null);
        Colocar(guiaRoja, p.requiereMedicion ? Ancla(p.nodoRojo) : null);
        Colocar(guiaNegra, p.requiereMedicion ? Ancla(p.nodoNegro) : null);
        ActualizarBotonesSonda();
    }

    void ElegirSonda(bool roja)
    {
        sondaActivaRoja = roja;
        ActualizarBotonesSonda();
    }

    /// <summary>Pone la sonda activa en el nodo indicado.</summary>
    public void SeleccionarNodo(string id)
    {
        if (!panel.activeSelf || terminado) return;
        if (!pasos[indice].requiereMedicion) return;

        if (sondaActivaRoja) puestoRojo = id;
        else puestoNegro = id;

        // Si la otra sonda aun no esta puesta, pasa a ser la activa
        if (sondaActivaRoja && puestoNegro == null) sondaActivaRoja = false;
        else if (!sondaActivaRoja && puestoRojo == null) sondaActivaRoja = true;

        Colocar(sondaRoja, Ancla(puestoRojo));
        Colocar(sondaNegra, Ancla(puestoNegro));
        ActualizarBotonesSonda();
        Evaluar();
    }

    void Evaluar()
    {
        Paso p = pasos[indice];

        if (puestoRojo == null || puestoNegro == null)
        {
            Mensaje("Ahora ubica la otra sonda.", Amarillo);
            return;
        }

        string configuracion = puestoRojo + ">" + puestoNegro;
        if (configuracion == ultimaConfiguracion) return;
        ultimaConfiguracion = configuracion;

        if (puestoRojo == p.nodoRojo && puestoNegro == p.nodoNegro)
        {
            if (!validado && fallosPaso == 0) aciertoPrimerIntento[indice] = true;
            validado = true;
            botonSiguiente.interactable = true;
            Mensaje("Correcto: las sondas están bien ubicadas. Observa la lectura del multímetro.", Verde);
            return;
        }

        if (!validado) fallosPaso++;

        if (puestoRojo == puestoNegro)
            Mensaje("Las dos sondas están en el mismo nodo: la lectura es 0 V.", Rojo);
        else if (puestoRojo == p.nodoNegro && puestoNegro == p.nodoRojo)
            Mensaje("Sondas invertidas: por eso la lectura es negativa. Intercambia la roja y la negra.", Rojo);
        else
            Mensaje("Esa no es la medición pedida. Ubica cada sonda donde lo indica la guía de su color.", Rojo);
    }

    void Update()
    {
        string lectura = "-.--";
        float vRojo, vNegro;
        bool midiendo = panel.activeSelf && !terminado && zonaMedicion.activeSelf;
        if (midiendo && puestoRojo != null && puestoNegro != null && gemelo.Conectado &&
            gemelo.TryObtenerVoltaje(puestoRojo, out vRojo) && gemelo.TryObtenerVoltaje(puestoNegro, out vNegro))
        {
            lectura = (vRojo - vNegro).ToString("0.00", CultureInfo.InvariantCulture);
        }

        if (midiendo) textoMultimetro.text = lectura + " V";
        if (textoMultimetro3D != null) textoMultimetro3D.text = lectura;
    }

    void Anterior()
    {
        if (indice == 0) return;
        indice--;
        MostrarPaso();
    }

    void Siguiente()
    {
        if (terminado)
        {
            Cerrar();
            return;
        }
        if (!validado) return;

        if (indice < pasos.Count - 1)
        {
            indice++;
            MostrarPaso();
        }
        else
        {
            MostrarResumen();
        }
    }

    void MostrarResumen()
    {
        terminado = true;
        int total = 0, aciertos = 0;
        for (int i = 0; i < pasos.Count; i++)
        {
            if (!pasos[i].requiereMedicion) continue;
            total++;
            if (aciertoPrimerIntento[i]) aciertos++;
        }

        textoPaso.text = "Guía completada";
        textoInstruccion.text = "Realizaste las " + total + " mediciones correctamente.\n" +
                                "Aciertos al primer intento: " + aciertos + " de " + total + ".";
        textoResultado.text = "";
        zonaMedicion.SetActive(false);
        botonAnterior.gameObject.SetActive(false);
        botonSiguiente.interactable = true;
        PonerTexto(botonSiguiente, "Cerrar");
        OcultarObjetos();
    }

    void ActualizarBotonesSonda()
    {
        PonerTexto(botonSondaRoja, (sondaActivaRoja ? "> " : "") + "ROJA: " + (puestoRojo ?? "sin ubicar"));
        PonerTexto(botonSondaNegra, (!sondaActivaRoja ? "> " : "") + "NEGRA: " + (puestoNegro ?? "sin ubicar"));
        Pintar(botonSondaRoja, sondaActivaRoja ? RojaActiva : RojaInactiva);
        Pintar(botonSondaNegra, !sondaActivaRoja ? NegraActiva : NegraInactiva);
    }

    Transform Ancla(string id)
    {
        if (string.IsNullOrEmpty(id) || nodos == null) return null;
        foreach (PuntoNodo n in nodos)
            if (n != null && n.id == id) return n.ancla;
        return null;
    }

    void Mensaje(string texto, Color color)
    {
        textoResultado.text = texto;
        textoResultado.color = color;
    }

    static void Pintar(Button boton, Color color)
    {
        Image img = boton.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    static void PonerTexto(Button boton, string texto)
    {
        TMP_Text t = boton.GetComponentInChildren<TMP_Text>(true);
        if (t != null) t.text = texto;
    }

    static void Colocar(Transform objeto, Transform ancla)
    {
        if (objeto == null) return;
        objeto.gameObject.SetActive(ancla != null);
        if (ancla != null) objeto.position = ancla.position;
    }
}
