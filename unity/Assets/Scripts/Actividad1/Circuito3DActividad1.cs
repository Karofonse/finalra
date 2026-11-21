using System;
using UnityEngine;

/// <summary>
/// Modelo 3D del circuito de la Actividad 1: refleja el estado del hardware
/// (LEDs encendidos, pulsador presionado, giro del potenciometro).
/// </summary>
public class Circuito3DActividad1 : MonoBehaviour
{
    [Serializable]
    class Estado
    {
        public float percent;
        public bool button;
        public bool led1;
        public bool led2;
    }

    public Esp32Http cliente;
    public Renderer led1;
    public Renderer led2;
    public Transform perilla;
    public Transform tapaPulsador;

    public Color led1Encendido = new Color(1f, 0.15f, 0.15f);
    public Color led2Encendido = new Color(0.2f, 1f, 0.3f);
    public Color ledApagado = new Color(0.3f, 0.3f, 0.32f);
    [Tooltip("Cuanto baja la tapa del pulsador al presionarlo, en metros")]
    public float recorridoPulsador = 0.0015f;

    Vector3 posicionTapa;

    void Start()
    {
        if (tapaPulsador != null) posicionTapa = tapaPulsador.localPosition;
        cliente.EstadoRecibido += AlRecibir;
        Pintar(led1, ledApagado);
        Pintar(led2, ledApagado);
    }

    void OnDestroy()
    {
        if (cliente != null) cliente.EstadoRecibido -= AlRecibir;
    }

    void AlRecibir(string json)
    {
        Estado e;
        try { e = JsonUtility.FromJson<Estado>(json); }
        catch (Exception) { return; }
        if (e == null) return;

        Pintar(led1, e.led1 ? led1Encendido : ledApagado);
        Pintar(led2, e.led2 ? led2Encendido : ledApagado);

        if (perilla != null)
        {
            float angulo = Mathf.Lerp(-135f, 135f, Mathf.Clamp01(e.percent / 100f));
            perilla.localRotation = Quaternion.Euler(0f, angulo, 0f);
        }
        if (tapaPulsador != null)
            tapaPulsador.localPosition = posicionTapa - (e.button ? Vector3.up * recorridoPulsador : Vector3.zero);
    }

    static void Pintar(Renderer r, Color color)
    {
        if (r != null) r.material.color = color;
    }
}
