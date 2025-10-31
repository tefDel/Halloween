using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class DarkDiaryItem : MonoBehaviour
{
    [Header("Configuración del Dark Diary")]
    [Tooltip("Este item activa a la muñeca cuando se coloca en el socket")]
    public string itemName = "Dark Diary";

    [Header("Referencias")]
    private CarEscapeTrigger carEscapeTrigger;
    public XRSocketInteractor diarySocket; // El socket donde debe colocarse el libro

    [Header("Referencias de cámara")]
    public GameObject dollEnemy;
    public GhostCameraController ghostCameraController;


    [Header("Efectos al Activar")]
    public AudioClip activationSound; // Sonido de terror cuando se activa

    [Header("Estado")]
    private bool hasBeenPlacedInSocket = false;

    void Start()
    {
        // Buscar el CarEscapeTrigger
        carEscapeTrigger = FindObjectOfType<CarEscapeTrigger>();

        if (carEscapeTrigger == null)
        {
            Debug.LogError("No se encontró CarEscapeTrigger en la escena!");
        }

        // Si no se asignó el socket, intentar encontrarlo
        if (diarySocket == null)
        {
            diarySocket = GameObject.Find("DiarySocket")?.GetComponent<XRSocketInteractor>();
        }

        if (ghostCameraController == null)
        {
            ghostCameraController = FindObjectOfType<GhostCameraController>();
            if (ghostCameraController == null)
                Debug.LogError("No se encontró GhostCameraController en la escena.");
        }


        // Suscribirse al evento del socket
        if (diarySocket != null)
        {
            diarySocket.selectEntered.AddListener(OnPlacedInSocket);
        }
        else
        {
            Debug.LogError("¡No se encontró el DiarySocket!");
        }
    }

    void OnDestroy()
    {
        // Limpiar eventos
        if (diarySocket != null)
        {
            diarySocket.selectEntered.RemoveListener(OnPlacedInSocket);
        }
    }

    // Cuando el libro se coloca en el socket
    void OnPlacedInSocket(SelectEnterEventArgs args)
    {
        // Verificar que el objeto colocado es este Dark Diary
        if (args.interactableObject.transform.gameObject == gameObject && !hasBeenPlacedInSocket)
        {
            hasBeenPlacedInSocket = true;

            // Registrar el item como recogido/colocado
            if (carEscapeTrigger != null)
            {
                carEscapeTrigger.CollectItem(itemName);
            }

            // ACTIVAR LA MUÑECA
            if (ghostCameraController != null && ghostCameraController.IsCanvasActive())
            {
                ActivateDoll();
            }
            else
            {
                Debug.Log("📷 Canvas no activo, esperando para mostrar fantasma...");
                StartCoroutine(WaitForCanvasThenActivate());
            }

        }
    }
    System.Collections.IEnumerator WaitForCanvasThenActivate()
    {
        while (ghostCameraController != null && !ghostCameraController.IsCanvasActive())
        {
            yield return null;
        }

        ActivateDoll();
    }

    void ActivateDoll()
    {
        Debug.Log("¡DARK DIARY COLOCADO! Activando muñeca...");

        // Reproducir sonido de terror
        if (activationSound != null)
        {
            AudioSource.PlayClipAtPoint(activationSound, transform.position, 1f);
        }

        // Esperar un momento para crear tensión y luego activar la muñeca
        StartCoroutine(ActivateDollAfterDelay(1.5f));
    }

    System.Collections.IEnumerator ActivateDollAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Activar la muñeca (esto automáticamente inicia su persecución por el script Ghost)
        if (dollEnemy != null)
        {
            dollEnemy.SetActive(true);

            Ghost ghost = dollEnemy.GetComponent<Ghost>();
            if (ghost != null)
            {
                ghost.ActivateMovement();
            }

            Debug.Log("¡Muñeca activada! Ahora perseguirá al jugador automáticamente.");
        }
    }
}