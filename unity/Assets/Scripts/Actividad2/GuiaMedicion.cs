using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Actividad 2: "Guia de Medicion". Indica paso a paso donde ubicar la sonda roja
/// y la sonda negra del multimetro, y valida el aprendizaje comparando la lectura
/// que escribe el usuario con el voltaje que reporta el ESP32.
/// </summary>
public class GuiaMedicion : MonoBehaviour
{
    [Serializable]
    public class Paso
    {
        public string titulo;
        [TextArea(2, 5)] public string instruccion;
        [Tooltip("Si esta activo, el usuario debe escribir la lectura del multimetro")]
        public bool requiereMedicion;
        [Tooltip("Nodo donde va la sonda roja: N1, N2, N3")]
        public string nodoRojo;
        [Tooltip("Nodo donde va la sonda negra: GND, N1, N2, N3")]
        public string nodoNegro = "GND";
        public Transform anclaRoja;
        public Transform anclaNegra;
    }

    public GemeloDigital gemelo;
    public List<Paso> pasos = new List<Paso>();

    [Header("Tolerancia de validacion")]
    [Tooltip("Error relativo permitido (0.05 = 5 %)")]
    public float toleranciaRelativa = 0.05f;
    [Tooltip("Error minimo permitido en voltios (precision del ADC del ESP32)")]
    public float toleranciaMinimaV = 0.1f;

    [Header("Interfaz")]
    public Button botonAbrir;          // boton "Guia de Medicion"
    public GameObject panel;
    public TMP_Text textoPaso;
    public TMP_Text textoInstruccion;
    public TMP_Text textoResultado;
    public TMP_InputField campoLectura;
    public Button botonVerificar;
    public Button botonAnterior;
    public Button botonSiguiente;
    public Button botonCerrar;

    [Header("Sondas virtuales")]
    public Transform sondaRoja;
    public Transform sondaNegra;

    static readonly Color Verde = new Color(0.3f, 1f, 0.4f);
    static readonly Color Rojo = new Color(1f, 0.45f, 0.35f);
    static readonly Color Amarillo = new Color(1f, 0.85f, 0.2f);

    int indice;
    bool validado;
    bool terminado;
    int intentosPaso;
    int aciertosPrimerIntento;
    int totalMediciones;

    void Start()
    {
        botonAbrir.onClick.AddListener(Abrir);
        botonCerrar.onClick.AddListener(Cerrar);
        botonVerificar.onClick.AddListener(Verificar);
        botonAnterior.onClick.AddListener(Anterior);
        botonSiguiente.onClick.AddListener(Siguiente);
        Cerrar();
    }

    public void Abrir()
    {
        if (pasos.Count == 0) return;
        indice = 0;
        terminado = false;
        aciertosPrimerIntento = 0;
        totalMediciones = 0;
        foreach (Paso p in pasos) if (p.requiereMedicion) totalMediciones++;

        panel.SetActive(true);
        botonAbrir.gameObject.SetActive(false);
        MostrarPaso();
    }

    public void Cerrar()
    {
        panel.SetActive(false);
        botonAbrir.gameObject.SetActive(true);
        Colocar(sondaRoja, null);
        Colocar(sondaNegra, null);
    }

    void MostrarPaso()
    {
        Paso p = pasos[indice];
        intentosPaso = 0;
        validado = !p.requiereMedicion;

        textoPaso.text = "Paso " + (indice + 1) + " de " + pasos.Count + ": " + p.titulo;
        textoInstruccion.text = p.instruccion;
        textoResultado.text = "";
        campoLectura.text = "";

        campoLectura.gameObject.SetActive(p.requiereMedicion);
        botonVerificar.gameObject.SetActive(p.requiereMedicion);
        botonAnterior.gameObject.SetActive(true);
        botonAnterior.interactable = indice > 0;
        botonSiguiente.interactable = validado;
        PonerTexto(botonSiguiente, indice == pasos.Count - 1 ? "Finalizar" : "Siguiente");

        Colocar(sondaRoja, p.anclaRoja);
        Colocar(sondaNegra, p.anclaNegra);
    }

    void Verificar()
    {
        Paso p = pasos[indice];

        float lectura;
        string entrada = campoLectura.text.Trim().Replace(',', '.');
        if (!float.TryParse(entrada, NumberStyles.Float, CultureInfo.InvariantCulture, out lectura))
        {
            Mensaje("Escribe la lectura del multímetro en voltios, por ejemplo 2.20", Amarillo);
            return;
        }

        float vRojo, vNegro;
        if (!gemelo.Conectado || !gemelo.TryObtenerVoltaje(p.nodoRojo, out vRojo) || !gemelo.TryObtenerVoltaje(p.nodoNegro, out vNegro))
        {
            Mensaje("Sin datos del ESP32. Revisa la conexión antes de verificar.", Amarillo);
            return;
        }

        float esperado = vRojo - vNegro;
        float tolerancia = Mathf.Max(toleranciaMinimaV, Mathf.Abs(esperado) * toleranciaRelativa);
        intentosPaso++;

        if (Mathf.Abs(lectura - esperado) <= tolerancia)
        {
            if (!validado && intentosPaso == 1) aciertosPrimerIntento++;
            validado = true;
            botonSiguiente.interactable = true;
            Mensaje("Correcto. Tu lectura (" + Formato(lectura) + " V) coincide con el sistema (" + Formato(esperado) + " V).", Verde);
        }
        else if (Mathf.Abs(esperado) > tolerancia && Mathf.Abs(lectura + esperado) <= tolerancia)
        {
            Mensaje("El valor es correcto pero con signo contrario: tienes las sondas invertidas. Intercambia la roja y la negra.", Rojo);
        }
        else if (Mathf.Abs(lectura) < 0.02f)
        {
            Mensaje("Lectura cercana a 0 V. Verifica que las puntas hagan buen contacto y que el selector esté en voltaje DC.", Rojo);
        }
        else
        {
            Mensaje("No coincide: mediste " + Formato(lectura) + " V y el sistema reporta " + Formato(esperado) +
                    " V. Revisa la posición de las sondas según las guías 3D.", Rojo);
        }
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
        textoPaso.text = "Guía completada";
        textoInstruccion.text = "Realizaste las " + totalMediciones + " mediciones correctamente.\n" +
                                "Aciertos al primer intento: " + aciertosPrimerIntento + " de " + totalMediciones + ".";
        textoResultado.text = "";
        campoLectura.gameObject.SetActive(false);
        botonVerificar.gameObject.SetActive(false);
        botonAnterior.gameObject.SetActive(false);
        botonSiguiente.interactable = true;
        PonerTexto(botonSiguiente, "Cerrar");
        Colocar(sondaRoja, null);
        Colocar(sondaNegra, null);
    }

    void Mensaje(string texto, Color color)
    {
        textoResultado.text = texto;
        textoResultado.color = color;
    }

    static string Formato(float v)
    {
        return v.ToString("0.00", CultureInfo.InvariantCulture);
    }

    static void PonerTexto(Button boton, string texto)
    {
        TMP_Text t = boton.GetComponentInChildren<TMP_Text>(true);
        if (t != null) t.text = texto;
    }

    static void Colocar(Transform sonda, Transform ancla)
    {
        if (sonda == null) return;
        sonda.gameObject.SetActive(ancla != null);
        if (ancla != null) sonda.position = ancla.position;
    }
}
