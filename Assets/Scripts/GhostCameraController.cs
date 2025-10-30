using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class GhostCameraController : MonoBehaviour
{
    [Header("Cámaras")]
    public Camera mainCamera;
    public Camera ghostCamera;

    [Header("UI de la cámara fantasma")]
    public Canvas ghostCameraCanvas;

    [Header("Sistema fantasma")]
    public float captureRange = 15f;
    public LayerMask ghostLayer;

    [Header("Controles PC")]
    public KeyCode toggleCameraKey = KeyCode.C;
    public KeyCode capturePhotoKey = KeyCode.Space;
    public bool useMouseLook = true;
    public float mouseSensitivity = 2f;

    [Header("Controles VR")]
    public UnityEngine.XR.InputFeatureUsage<bool> vrToggleButton = UnityEngine.XR.CommonUsages.primaryButton;
    public UnityEngine.XR.InputFeatureUsage<bool> vrCaptureButton = UnityEngine.XR.CommonUsages.triggerButton;

    private bool isCameraActive = false;
    private XRBaseController rightController;
    private bool isVRMode = false;
    private float lastToggleTime = 0f;
    private float lastCaptureTime = 0f;
    private float buttonCooldown = 0.3f;
    private float mouseX = 0f;
    private float mouseY = 0f;
    private Quaternion originalMainCameraRotation;

    void Awake()
    {
        AutoAssignReferences();
    }

    void Start()
    {
        if (mainCamera != null)
            originalMainCameraRotation = mainCamera.transform.localRotation;

        // Intentar obtener el dispositivo VR derecho
        UnityEngine.XR.InputDevice rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightHandDevice.isValid)
        {
            isVRMode = true;
            Debug.Log("🥽 Modo VR detectado");
        }
        else
        {
            isVRMode = false;
            Debug.Log("🖥️ Modo PC detectado");
        }

        SetAllGhostsVisible(false);
        SetCameraActive(false);
    }

    void AutoAssignReferences()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (ghostCamera == null)
        {
            Camera[] cameras = FindObjectsOfType<Camera>(includeInactive: true);
            foreach (var cam in cameras)
            {
                if (cam != mainCamera)
                {
                    ghostCamera = cam;
                    break;
                }
            }
        }

        if (ghostCameraCanvas == null)
            ghostCameraCanvas = GameObject.Find("GhostCameraCanvas")?.GetComponent<Canvas>();
    }

    void Update()
    {
        if (isVRMode)
            DetectVRToggle();
        else
            DetectPCToggle();

        if (isCameraActive)
        {
            if (isVRMode)
                DetectVRCapture();
            else
                DetectPCCapture();
        }

        if (!isVRMode && isCameraActive && useMouseLook)
            UpdateMouseLook();

        if (!isVRMode && Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void DetectPCToggle()
    {
        if (Input.GetKeyDown(toggleCameraKey) && Time.time > lastToggleTime + buttonCooldown)
        {
            ToggleCamera();
            lastToggleTime = Time.time;
        }
    }

    void DetectVRToggle()
    {
        UnityEngine.XR.InputDevice rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightHandDevice.TryGetFeatureValue(vrToggleButton, out bool buttonPressed))
        {
            if (buttonPressed && Time.time > lastToggleTime + buttonCooldown)
            {
                ToggleCamera();
                lastToggleTime = Time.time;
            }
        }
    }

    void DetectPCCapture()
    {
        if ((Input.GetKeyDown(capturePhotoKey) || Input.GetMouseButtonDown(0)) && Time.time > lastCaptureTime + buttonCooldown)
        {
            CapturePhoto();
            lastCaptureTime = Time.time;
        }
    }

    void DetectVRCapture()
    {
        UnityEngine.XR.InputDevice rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightHandDevice.TryGetFeatureValue(vrCaptureButton, out bool triggerPressed))
        {
            if (triggerPressed && Time.time > lastCaptureTime + buttonCooldown)
            {
                CapturePhoto();
                lastCaptureTime = Time.time;
            }
        }
    }

    void UpdateMouseLook()
    {
        if (ghostCamera == null) return;
        mouseX += Input.GetAxis("Mouse X") * mouseSensitivity;
        mouseY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        mouseY = Mathf.Clamp(mouseY, -90f, 90f);
        ghostCamera.transform.localRotation = Quaternion.Euler(mouseY, mouseX, 0f);
    }

    void ToggleCamera()
    {
        SetCameraActive(!isCameraActive);
    }

    void SetCameraActive(bool active)
    {
        isCameraActive = active;

        if (mainCamera != null && ghostCamera != null)
        {
            if (active)
            {
                mainCamera.enabled = false;
                ghostCamera.enabled = true;

                if (!isVRMode)
                {
                    mouseX = 0f;
                    mouseY = 0f;
                    ghostCamera.transform.localRotation = Quaternion.identity;

                    if (useMouseLook)
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }
            else
            {
                ghostCamera.enabled = false;
                mainCamera.enabled = true;

                if (!isVRMode)
                    mainCamera.transform.localRotation = originalMainCameraRotation;
            }
        }

        if (ghostCameraCanvas != null)
            ghostCameraCanvas.gameObject.SetActive(active);

        SetAllGhostsVisible(active);

        if (isVRMode)
        {
            float intensity = active ? 0.3f : 0.2f;
            float duration = active ? 0.2f : 0.1f;
            SendHapticImpulse(rightController, intensity, duration);
        }

        Debug.Log(active ? "📷 Cámara fantasma ACTIVADA" : "📷 Cámara fantasma DESACTIVADA");
    }

    void CapturePhoto()
    {
        if (ghostCamera == null)
        {
            Debug.LogWarning("⚠️ Ghost Camera no asignada!");
            return;
        }

        int ghostsCaptured = 0;
        Ghost[] allGhosts = FindObjectsOfType<Ghost>();

        foreach (Ghost ghost in allGhosts)
        {
            if (IsInGhostCameraView(ghost.transform))
            {
                ghost.Stun(); // 👻 Aturde al fantasma
                ghostsCaptured++;
            }
        }

        if (ghostsCaptured > 0)
        {
            if (isVRMode)
                SendHapticImpulse(rightController, 0.8f, 0.3f);

            Debug.Log($"📸 ¡{ghostsCaptured} fantasma(s) aturdido(s)!");
            StartCoroutine(FlashEffect());
        }
        else
        {
            if (isVRMode)
                SendHapticImpulse(rightController, 0.2f, 0.1f);

            Debug.Log("❌ No hay fantasmas en el encuadre");
        }
    }

    bool IsInGhostCameraView(Transform target)
    {
        if (ghostCamera == null) return false;

        Vector3 viewportPoint = ghostCamera.WorldToViewportPoint(target.position);
        if (viewportPoint.z > 0 && viewportPoint.x > 0.25f && viewportPoint.x < 0.75f && viewportPoint.y > 0.25f && viewportPoint.y < 0.75f)
        {
            float distance = Vector3.Distance(ghostCamera.transform.position, target.position);
            if (distance <= captureRange)
            {
                if (Physics.Raycast(ghostCamera.transform.position, target.position - ghostCamera.transform.position, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
                {
                    return hit.transform == target || hit.transform.IsChildOf(target);
                }
            }
        }
        return false;
    }

    void SetAllGhostsVisible(bool visible)
    {
        Ghost[] ghosts = FindObjectsOfType<Ghost>();
        foreach (Ghost ghost in ghosts)
            ghost.SetVisible(visible);
    }

    public void SendHapticImpulse(XRBaseController controller, float amplitude, float duration)
    {
        if (controller != null)
        {
            controller.SendHapticImpulse(amplitude, duration);
        }
    }

    IEnumerator FlashEffect()
    {
        if (ghostCamera == null) yield break;

        GameObject flashQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        flashQuad.transform.SetParent(ghostCamera.transform);
        flashQuad.transform.localPosition = new Vector3(0, 0, ghostCamera.nearClipPlane + 0.01f);
        flashQuad.transform.localRotation = Quaternion.identity;
        flashQuad.transform.localScale = new Vector3(2f, 2f, 1);

        Material flashMat = new Material(Shader.Find("Unlit/Color"));
        flashMat.color = Color.white;
        flashQuad.GetComponent<Renderer>().material = flashMat;

        Destroy(flashQuad.GetComponent<Collider>());
        yield return new WaitForSeconds(0.1f);
        Destroy(flashQuad);
    }
}
