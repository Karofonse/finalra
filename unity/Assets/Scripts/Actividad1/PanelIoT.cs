using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Actividad 1: panel de instrumentacion en espacio mundial.
/// Muestra el sensor analogico en un medidor circular, el pulsador y los LEDs
/// como luces virtuales, y envia comandos al ESP32 con los botones virtuales.
/// </summary>
public class PanelIoT : MonoBehaviour
{
    [Serializable]
    class Estado
    {
        public int analog;
        public float percent;
        public float volts;
        public bool button;
        public bool led1;
        public bool led2;
    }

    public Esp32Http cliente;

    [Header("Sensor analogico")]
    public Image medidor;              // Image tipo Filled / Radial 360
    public TMP_Text textoPorcentaje;
    public TMP_Text textoVoltaje;

    [Header("Entrada digital")]
    public Image luzPulsador;
    public TMP_Text textoPulsador;

    [Header("Actuadores")]
    public Image luzLed1;
    public Image luzLed2;
    public Button botonLed1;
    public Button botonLed2;

    [Header("Colores")]
    public Color colorApagado = new Color(0.25f, 0.25f, 0.28f);
    public Color colorPulsador = new Color(1f, 0.8f, 0.1f);
    public Color colorLed1 = new Color(1f, 0.2f, 0.2f);
    public Color colorLed2 = new Color(0.2f, 1f, 0.3f);
    public Color colorBajo = new Color(0.2f, 0.8f, 1f);
    public Color colorAlto = new Color(1f, 0.3f, 0.2f);

    float porcentajeObjetivo;
    float porcentajeMostrado;

    void Start()
    {
        botonLed1.onClick.AddListener(() => cliente.EnviarComando("/led?id=1&state=toggle"));
        botonLed2.onClick.AddListener(() => cliente.EnviarComando("/led?id=2&state=toggle"));
        cliente.EstadoRecibido += AlRecibirEstado;
        cliente.ConexionCambiada += AlCambiarConexion;
        AlCambiarConexion(cliente.Conectado);
    }

    void OnDestroy()
    {
        if (cliente == null) return;
        cliente.EstadoRecibido -= AlRecibirEstado;
        cliente.ConexionCambiada -= AlCambiarConexion;
    }

    void AlRecibirEstado(string json)
    {
        Estado e;
        try { e = JsonUtility.FromJson<Estado>(json); }
        catch (Exception) { return; }
        if (e == null) return;

        porcentajeObjetivo = Mathf.Clamp(e.percent, 0f, 100f);
        textoVoltaje.text = e.volts.ToString("0.00", CultureInfo.InvariantCulture) + " V";

        luzPulsador.color = e.button ? colorPulsador : colorApagado;
        textoPulsador.text = e.button ? "PRESIONADO" : "LIBRE";

        luzLed1.color = e.led1 ? colorLed1 : colorApagado;
        luzLed2.color = e.led2 ? colorLed2 : colorApagado;
    }

    void AlCambiarConexion(bool conectado)
    {
        botonLed1.interactable = conectado;
        botonLed2.interactable = conectado;
        if (!conectado)
        {
            textoPulsador.text = "---";
            textoVoltaje.text = "-.-- V";
            luzPulsador.color = colorApagado;
            luzLed1.color = colorApagado;
            luzLed2.color = colorApagado;
        }
    }

    void Update()
    {
        // Suaviza el movimiento del medidor entre una lectura y la siguiente
        porcentajeMostrado = Mathf.Lerp(porcentajeMostrado, porcentajeObjetivo, 1f - Mathf.Exp(-12f * Time.deltaTime));
        float t = porcentajeMostrado / 100f;
        medidor.fillAmount = t;
        medidor.color = Color.Lerp(colorBajo, colorAlto, t);
        textoPorcentaje.text = porcentajeMostrado.ToString("0") + " %";
    }
}
