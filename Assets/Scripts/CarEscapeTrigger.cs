using UnityEngine;
using UnityEngine.UI; // O usa TMPro si prefieres
using UnityEngine.SceneManagement;
using System.Collections;

public class CarEscapeTrigger : MonoBehaviour
{
    [Header("Jugador")]
    public Transform playerTransform;
    public int requiredItems = 4;
    public int collectedItems = 0;

    [Header("Carro")]
    public Transform carSeatTransform;
    public GameObject carObject;
    public AudioSource carStartSound;

    [Header("UI Mensaje")]
    public Canvas messageCanvas;
    public Text messageText; // O usa TMP_Text si usas TextMeshPro
    public float messageDuration = 3f;

    [Header("Final del juego")]
    public string sceneToLoad = "GameOverScene";

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            if (collectedItems < requiredItems)
            {
                ShowMessage("Aún no tienes lo necesario para escapar");
            }
            else
            {
                hasTriggered = true;
                StartCoroutine(EscapeSequence());
            }
        }
    }

    void ShowMessage(string text)
    {
        if (messageCanvas != null && messageText != null)
        {
            messageText.text = text;
            messageCanvas.enabled = true;
            Invoke(nameof(HideMessage), messageDuration);
        }
    }

    void HideMessage()
    {
        if (messageCanvas != null)
            messageCanvas.enabled = false;
    }

    IEnumerator EscapeSequence()
    {
        // Mover jugador al asiento del carro
        playerTransform.position = carSeatTransform.position;
        playerTransform.rotation = carSeatTransform.rotation;

        // Reproducir sonido de arranque
        if (carStartSound != null)
            carStartSound.Play();

        // Esperar un momento antes de terminar
        yield return new WaitForSeconds(3f);

        // Cargar escena final
        SceneManager.LoadScene(sceneToLoad);
    }

    // Puedes llamar esto desde otro script cuando recojas un ítem
    public void AddItem()
    {
        collectedItems++;
    }
}