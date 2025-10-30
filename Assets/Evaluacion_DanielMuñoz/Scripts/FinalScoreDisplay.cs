using UnityEngine;
using TMPro;

public class FinalScoreDisplay : MonoBehaviour
{
    public TextMeshProUGUI contadorMonedasText;
    public TextMeshProUGUI contadorJoyasText;
    public TextMeshProUGUI contadorGemasText;
    public TextMeshProUGUI puntosTotalesText;
    public TextMeshProUGUI tiempoTotalText;

    void Start()
    {
        // Muestra los datos recolectados y el tiempo total en el Canvas
        contadorMonedasText.text = GameManager.Instance.CantidadMonedas.ToString();
        contadorJoyasText.text = GameManager.Instance.CantidadJoyas.ToString();
        contadorGemasText.text = GameManager.Instance.Cantidadgemas.ToString();
        puntosTotalesText.text = GameManager.Instance.TotalPuntos.ToString();
        tiempoTotalText.text = $"{GameManager.Instance.ElapsedTime:F2} segundos";
    }
}
