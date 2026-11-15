using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Cliente HTTP del ESP32. Consulta periodicamente una ruta de estado y permite
/// enviar comandos. Mide la latencia y detecta la perdida de conexion.
/// </summary>
public class Esp32Http : MonoBehaviour
{
    const string ClaveIp = "esp32_ip";

    [Tooltip("IP del ESP32 si el usuario no ha guardado otra (192.168.4.1 = modo red propia)")]
    public string ipPorDefecto = "192.168.4.1";
    [Tooltip("Ruta que se consulta periodicamente: /state (Actividad 1) o /voltages (Actividad 2)")]
    public string rutaEstado = "/state";
    [Tooltip("Pausa entre consultas, en segundos")]
    public float intervalo = 0.1f;
    [Tooltip("Tiempo maximo de espera por respuesta, en segundos")]
    public int timeoutSegundos = 2;

    public string Ip { get; private set; }
    public bool Conectado { get; private set; }
    public float LatenciaMs { get; private set; }

    /// <summary>JSON recibido del ESP32 (estado periodico o respuesta a un comando).</summary>
    public event Action<string> EstadoRecibido;
    public event Action<bool> ConexionCambiada;

    Coroutine sondeo;

    void Awake()
    {
        Ip = PlayerPrefs.GetString(ClaveIp, ipPorDefecto);
    }

    void OnEnable()
    {
        sondeo = StartCoroutine(Sondear());
    }

    void OnDisable()
    {
        if (sondeo != null) StopCoroutine(sondeo);
        sondeo = null;
    }

    public void CambiarIp(string nuevaIp)
    {
        nuevaIp = (nuevaIp ?? "").Trim();
        if (nuevaIp.Length == 0) return;
        Ip = nuevaIp;
        PlayerPrefs.SetString(ClaveIp, Ip);
        PlayerPrefs.Save();
    }

    /// <summary>Envia un comando, por ejemplo "/led?id=1&state=toggle".</summary>
    public void EnviarComando(string rutaConParametros)
    {
        StartCoroutine(Pedir(rutaConParametros, false));
    }

    IEnumerator Sondear()
    {
        while (true)
        {
            yield return Pedir(rutaEstado, true);
            // Si no hay conexion se espera mas para no saturar la red
            yield return new WaitForSecondsRealtime(Conectado ? intervalo : 1f);
        }
    }

    IEnumerator Pedir(string ruta, bool actualizaConexion)
    {
        string url = "http://" + Ip + ruta;
        float inicio = Time.realtimeSinceStartup;

        using (UnityWebRequest peticion = UnityWebRequest.Get(url))
        {
            peticion.timeout = timeoutSegundos;
            yield return peticion.SendWebRequest();

            bool ok = peticion.result == UnityWebRequest.Result.Success;
            if (ok)
            {
                LatenciaMs = (Time.realtimeSinceStartup - inicio) * 1000f;
                MarcarConexion(true);
                EstadoRecibido?.Invoke(peticion.downloadHandler.text);
            }
            else if (actualizaConexion)
            {
                MarcarConexion(false);
            }
            else
            {
                Debug.LogWarning("Comando fallido (" + url + "): " + peticion.error);
            }
        }
    }

    void MarcarConexion(bool estado)
    {
        if (Conectado == estado) return;
        Conectado = estado;
        ConexionCambiada?.Invoke(estado);
    }
}
