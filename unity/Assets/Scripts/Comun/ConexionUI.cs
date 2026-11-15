using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra en pantalla para escribir la IP del ESP32 y ver el estado de la conexion.
/// </summary>
public class ConexionUI : MonoBehaviour
{
    public Esp32Http cliente;
    public TMP_InputField campoIp;
    public Button botonConectar;
    public TMP_Text textoEstado;

    void Start()
    {
        campoIp.text = cliente.Ip;
        botonConectar.onClick.AddListener(() => cliente.CambiarIp(campoIp.text));
    }

    void Update()
    {
        if (cliente.Conectado)
        {
            textoEstado.text = "Conectado  " + cliente.LatenciaMs.ToString("0") + " ms";
            textoEstado.color = new Color(0.3f, 1f, 0.4f);
        }
        else
        {
            textoEstado.text = "Sin conexión con " + cliente.Ip;
            textoEstado.color = new Color(1f, 0.4f, 0.3f);
        }
    }
}
