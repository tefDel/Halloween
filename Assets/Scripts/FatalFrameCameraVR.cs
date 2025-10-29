using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class FatalFrameCameraVR : MonoBehaviour
{
    [Header("Cámaras")]
    public Camera playerCamera;      // Cámara VR normal
    public Camera spiritCamera;      // Cámara especial tipo Fatal Frame

    [Header("Modelo y UI")]
    public GameObject cameraModel;   // Modelo de cámara en la mano
    public GameObject visorUI;       // UI del visor (opcional)

    [Header("Posicionamiento")]
    public float distanceFromFace = 0.2f;  // Distancia desde la cara
    public float smoothSpeed = 10f;         // Velocidad de suavizado
    public Vector3 positionOffset = new Vector3(0, -0.05f, 0); // Offset adicional

    [Header("Configuración")]
    public float photoRange = 10f;   // Alcance del "disparo"
    public LayerMask ghostLayer;     // Capa de los fantasmas

    [Header("Efectos")]
    public AudioClip shutterSound;   // Sonido de captura
    public GameObject flashEffect;   // Panel blanco para flash
    public float flashDuration = 0.1f;

    [Header("Controles VR")]
    public InputActionProperty gripLeftAction;   // Grip izquierdo
    public InputActionProperty gripRightAction;  // Grip derecho
    public InputActionProperty triggerLeftAction;  // Trigger izquierdo
    public InputActionProperty triggerRightAction; // Trigger derecho

    private bool isCameraActive = false;
    private bool isGripping = false;
    private AudioSource audioSource;
    private Transform activeController; // Controlador que tiene la cámara

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Desactivar al inicio
        if (spiritCamera != null)
            spiritCamera.gameObject.SetActive(false);
        if (cameraModel != null)
            cameraModel.SetActive(false);
        if (visorUI != null)
            visorUI.SetActive(false);
        if (flashEffect != null)
            flashEffect.SetActive(false);

        // Configurar culling mask para que fantasmas solo se vean con spiritCamera
        ConfigureGhostVisibility();
    }

    void ConfigureGhostVisibility()
    {
        // La cámara del jugador NO debe ver la capa Ghost
        if (playerCamera != null)
        {
            playerCamera.cullingMask &= ~ghostLayer;
        }

        // Solo spiritCamera ve fantasmas
        if (spiritCamera != null)
        {
            spiritCamera.cullingMask = -1; // Ve todo
        }
    }

    void Update()
    {
        // Detectar grip en cualquier mano
        bool gripLeftPressed = gripLeftAction.action.IsPressed();
        bool gripRightPressed = gripRightAction.action.IsPressed();

        // Para testing con teclado
        if (Keyboard.current != null && Keyboard.current.gKey.isPressed)
        {
            gripLeftPressed = true;
        }

        bool isCurrentlyGripping = gripLeftPressed || gripRightPressed;

        // Si se empieza a agarrar
        if (isCurrentlyGripping && !isGripping)
        {
            ActivateCamera();
            isGripping = true;
        }
        // Si se suelta
        else if (!isCurrentlyGripping && isGripping)
        {
            DeactivateCamera();
            isGripping = false;
        }

        // Si la cámara está activa, posicionarla frente a la cara
        if (isCameraActive && playerCamera != null)
        {
            PositionCameraInFrontOfFace();

            // Detectar trigger para tomar foto
            bool triggerPressed = triggerLeftAction.action.WasPressedThisFrame() ||
                                  triggerRightAction.action.WasPressedThisFrame() ||
                                  (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

            if (triggerPressed)
            {
                TakePhoto();
            }
        }
    }

    void ActivateCamera()
    {
        isCameraActive = true;

        if (spiritCamera != null)
            spiritCamera.gameObject.SetActive(true);

        if (cameraModel != null)
            cameraModel.SetActive(true);

        if (visorUI != null)
            visorUI.SetActive(true);

        Debug.Log("Cámara Fatal Frame activada");
    }

    void DeactivateCamera()
    {
        isCameraActive = false;

        if (spiritCamera != null)
            spiritCamera.gameObject.SetActive(false);

        if (cameraModel != null)
            cameraModel.SetActive(false);

        if (visorUI != null)
            visorUI.SetActive(false);

        Debug.Log("Cámara guardada");
    }

    void PositionCameraInFrontOfFace()
    {
        // Calcular posición objetivo frente a la cara del jugador
        Vector3 targetPosition = playerCamera.transform.position +
                                 playerCamera.transform.forward * distanceFromFace +
                                 playerCamera.transform.TransformDirection(positionOffset);

        // Suavizar movimiento
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        // Rotar para que mire hacia donde mira el jugador
        Quaternion targetRotation = playerCamera.transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }

    void TakePhoto()
    {
        // Raycast desde la cámara spirit
        Ray ray = new Ray(spiritCamera.transform.position, spiritCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, photoRange, ghostLayer))
        {
            Debug.Log("¡Fantasma capturado!: " + hit.collider.name);

            // Intentar obtener el componente del fantasma
            Fantasma ghost = hit.collider.GetComponent<Fantasma>();
            if (ghost != null)
            {
                ghost.OnCaptured();
            }
        }
        else
        {
            Debug.Log("Foto tomada, pero no hay fantasma en rango");
        }

        // Efectos visuales y sonoros
        StartCoroutine(FlashEffect());

        if (shutterSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shutterSound);
        }
    }

    System.Collections.IEnumerator FlashEffect()
    {
        if (flashEffect != null)
        {
            flashEffect.SetActive(true);
            yield return new WaitForSeconds(flashDuration);
            flashEffect.SetActive(false);
        }
        else
        {
            Debug.Log("¡FLASH!");
            yield return new WaitForSeconds(flashDuration);
        }
    }

    // Dibujar el rango en el editor
    void OnDrawGizmosSelected()
    {
        if (spiritCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(spiritCamera.transform.position, spiritCamera.transform.forward * photoRange);

            // Dibujar esfera en la posición objetivo
            if (playerCamera != null && Application.isPlaying && isCameraActive)
            {
                Vector3 targetPos = playerCamera.transform.position +
                                   playerCamera.transform.forward * distanceFromFace;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(targetPos, 0.05f);
            }
        }
    }
}