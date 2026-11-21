using UnityEngine;

/// <summary>
/// Cable flexible entre un borne del multimetro y una sonda. Se dibuja con un
/// LineRenderer que sigue a la sonda cuando el usuario la cambia de nodo.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class CableSonda : MonoBehaviour
{
    public Transform origen;    // borne del multimetro
    public Transform destino;   // extremo del mango de la sonda
    [Tooltip("Cuanto cuelga el cable en su punto medio, en metros (antes de la escala del marcador)")]
    public float holgura = 0.012f;

    const int Puntos = 16;
    LineRenderer linea;

    void Awake()
    {
        linea = GetComponent<LineRenderer>();
        linea.useWorldSpace = true;
        linea.positionCount = 0;
    }

    void LateUpdate()
    {
        bool visible = origen != null && destino != null && destino.gameObject.activeInHierarchy;
        if (!visible)
        {
            if (linea.positionCount != 0) linea.positionCount = 0;
            return;
        }

        if (linea.positionCount != Puntos) linea.positionCount = Puntos;
        Vector3 a = origen.position, b = destino.position;
        Vector3 abajo = -transform.up * holgura * transform.lossyScale.y;
        for (int i = 0; i < Puntos; i++)
        {
            float t = i / (float)(Puntos - 1);
            linea.SetPosition(i, Vector3.Lerp(a, b, t) + abajo * 4f * t * (1f - t));
        }
    }
}
