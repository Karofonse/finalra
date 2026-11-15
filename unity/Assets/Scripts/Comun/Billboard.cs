using UnityEngine;

/// <summary>
/// Orienta el objeto hacia la camara AR para que el texto siempre sea legible.
/// Tambien asigna la camara a los Canvas en espacio mundial para que sus botones respondan.
/// </summary>
public class Billboard : MonoBehaviour
{
    Camera camara;

    void Start()
    {
        camara = Camera.main;
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            canvas.worldCamera = camara;
    }

    void LateUpdate()
    {
        if (camara == null)
        {
            camara = Camera.main;
            if (camara == null) return;
        }
        Vector3 direccion = transform.position - camara.transform.position;
        if (direccion.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(direccion, camara.transform.up);
    }
}
