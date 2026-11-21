using UnityEngine;

/// <summary>
/// Modelo 3D del circuito de la Actividad 2: gira la perilla del potenciometro
/// segun el voltaje del nodo N3 que reporta el ESP32.
/// </summary>
public class Circuito3DActividad2 : MonoBehaviour
{
    public GemeloDigital gemelo;
    public Transform perilla;

    void Update()
    {
        float v;
        if (gemelo == null || perilla == null || !gemelo.Conectado) return;
        if (!gemelo.TryObtenerVoltaje("N3", out v)) return;

        float angulo = Mathf.Lerp(-135f, 135f, Mathf.Clamp01(v / 3.3f));
        perilla.localRotation = Quaternion.Euler(0f, angulo, 0f);
    }
}
