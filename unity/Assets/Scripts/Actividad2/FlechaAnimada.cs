using UnityEngine;

/// <summary>Movimiento de vaiven para llamar la atencion sobre el punto de medicion.</summary>
public class FlechaAnimada : MonoBehaviour
{
    public float amplitud = 0.008f;   // metros
    public float velocidad = 4f;

    Vector3 posicionInicial;

    void Awake()
    {
        posicionInicial = transform.localPosition;
    }

    void Update()
    {
        float desplazamiento = (Mathf.Sin(Time.time * velocidad) + 1f) * 0.5f * amplitud;
        transform.localPosition = posicionInicial + Vector3.up * desplazamiento;
    }
}
