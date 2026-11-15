using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>Etiqueta flotante que muestra el voltaje de un nodo del circuito.</summary>
public class EtiquetaVoltaje : MonoBehaviour
{
    [Tooltip("Debe coincidir con el id que envia el ESP32: N1, N2, N3")]
    public string idNodo = "N1";
    public TMP_Text texto;

    public void Mostrar(float voltios)
    {
        texto.text = idNodo + "\n" + voltios.ToString("0.00", CultureInfo.InvariantCulture) + " V";
    }

    public void MostrarSinDatos()
    {
        texto.text = idNodo + "\n-.-- V";
    }
}
