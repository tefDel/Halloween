using UnityEngine;
using TMPro; // Importa TextMeshPro

public class PlayerCollector : MonoBehaviour
{
    public TextMeshProUGUI contadorMonedasText; // Campo para el contador de monedas
    public TextMeshProUGUI contadorJoyasText;   // Campo para el contador de joyas
    public TextMeshProUGUI contadorGemasText;   // Campo para el contador de gemas

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Colisi�n detectada con: " + other.name); // Verifica si se detecta la colisi�n
        if (other.CompareTag("Moneda") || other.CompareTag("Joya") || other.CompareTag("Gema"))
        {
            GameManager.Instance.AddItem(other.tag);
            UpdateUI();
            Destroy(other.gameObject);
        }
    }


    void UpdateUI()
    {
        // Actualiza cada contador espec�fico con solo los n�meros
        contadorMonedasText.text = GameManager.Instance.CantidadMonedas.ToString();
        contadorJoyasText.text = GameManager.Instance.CantidadJoyas.ToString();
        contadorGemasText.text = GameManager.Instance.Cantidadgemas.ToString();

    }

}