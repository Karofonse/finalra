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
    [Tooltip("Piezas de cada LED que cambian de color (cupula y cuerpo)")]
    public Renderer[] led1;
    public Renderer[] led2;
    [Tooltip("Luz puntual que ilumina la protoboard cuando el LED esta encendido")]
    public Light luz1;
    public Light luz2;
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
        Pintar(led1, luz1, led1Encendido, false);
        Pintar(led2, luz2, led2Encendido, false);
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

        Pintar(led1, luz1, led1Encendido, e.led1);
        Pintar(led2, luz2, led2Encendido, e.led2);

        if (perilla != null)
        {
            float angulo = Mathf.Lerp(-135f, 135f, Mathf.Clamp01(e.percent / 100f));
            perilla.localRotation = Quaternion.Euler(0f, angulo, 0f);
        }
        if (tapaPulsador != null)
            tapaPulsador.localPosition = posicionTapa - (e.button ? Vector3.up * recorridoPulsador : Vector3.zero);
    }

    void Pintar(Renderer[] piezas, Light luz, Color encendido, bool activo)
    {
        if (luz != null)
        {
            luz.color = encendido;
            luz.enabled = activo;
        }
        if (piezas == null) return;
        foreach (Renderer r in piezas)
        {
            if (r == null) continue;
            Material m = r.material;
            m.color = activo ? encendido : ledApagado;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", activo ? encendido * 2.5f : Color.black);
        }
    }
}
