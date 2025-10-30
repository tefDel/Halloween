using UnityEngine;
using TMPro;

public class UpdateUIInNewScene : MonoBehaviour
{
    public TextMeshProUGUI contadorMonedasText;
    public TextMeshProUGUI contadorJoyasText;
    public TextMeshProUGUI contadorGemasText;
    public TextMeshProUGUI puntosTotalesText;

    void Start()
    {
        contadorMonedasText.text = GameManager.Instance.CantidadMonedas.ToString();
        contadorJoyasText.text = GameManager.Instance.CantidadJoyas.ToString();
        contadorGemasText.text = GameManager.Instance.Cantidadgemas.ToString();
        puntosTotalesText.text = GameManager.Instance.TotalPuntos.ToString();
    }
}
