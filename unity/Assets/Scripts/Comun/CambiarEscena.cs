using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Boton que carga otra escena de la app (para pasar de una actividad a la otra).
/// Se oculta si la escena de destino no esta incluida en la compilacion.
/// </summary>
[RequireComponent(typeof(Button))]
public class CambiarEscena : MonoBehaviour
{
    [Tooltip("Nombre de la escena de destino, sin extension (debe estar en Build Settings)")]
    public string escena;

    void Start()
    {
        if (!Application.CanStreamedLevelBeLoaded(escena))
        {
            gameObject.SetActive(false);
            return;
        }
        GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene(escena));
    }
}
