using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Actividad 2: recibe los voltajes de los nodos desde el ESP32 y actualiza
/// las etiquetas flotantes ubicadas sobre el circuito.
/// </summary>
public class GemeloDigital : MonoBehaviour
{
    [Serializable]
    class Nodo
    {
        public string id;
        public float mv;
    }

    [Serializable]
    class Respuesta
    {
        public Nodo[] nodes;
    }

    public Esp32Http cliente;
    public EtiquetaVoltaje[] etiquetas;

    readonly Dictionary<string, float> voltajes = new Dictionary<string, float>();

    public bool Conectado { get { return cliente != null && cliente.Conectado; } }

    void Start()
    {
        cliente.EstadoRecibido += AlRecibir;
        cliente.ConexionCambiada += AlCambiarConexion;
        AlCambiarConexion(cliente.Conectado);
    }

    void OnDestroy()
    {
        if (cliente == null) return;
        cliente.EstadoRecibido -= AlRecibir;
        cliente.ConexionCambiada -= AlCambiarConexion;
    }

    /// <summary>Voltaje de un nodo en voltios. "GND" siempre vale 0.</summary>
    public bool TryObtenerVoltaje(string idNodo, out float voltios)
    {
        if (string.IsNullOrEmpty(idNodo) || idNodo == "GND")
        {
            voltios = 0f;
            return true;
        }
        return voltajes.TryGetValue(idNodo, out voltios);
    }

    void AlRecibir(string json)
    {
        Respuesta r;
        try { r = JsonUtility.FromJson<Respuesta>(json); }
        catch (Exception) { return; }
        if (r == null || r.nodes == null) return;

        foreach (Nodo n in r.nodes) voltajes[n.id] = n.mv / 1000f;

        foreach (EtiquetaVoltaje etiqueta in etiquetas)
        {
            float v;
            if (etiqueta != null && voltajes.TryGetValue(etiqueta.idNodo, out v)) etiqueta.Mostrar(v);
        }
    }

    void AlCambiarConexion(bool conectado)
    {
        if (conectado) return;
        voltajes.Clear();
        foreach (EtiquetaVoltaje etiqueta in etiquetas)
            if (etiqueta != null) etiqueta.MostrarSinDatos();
    }
}
