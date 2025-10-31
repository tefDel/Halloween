using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class GhostCameraController : MonoBehaviour
{
    [Header("Cámara")]
    public Camera mainCamera;

    [Header("UI de la cámara fantasma")]
    public Canvas ghostCameraCanvas;

    [Header("Sistema fantasma")]
    public float captureRange = 15f;
    public string ghostTag = "Ghost";

    [Header("Controles PC")]
    public KeyCode toggleCameraKey = KeyCode.C;
    public KeyCode capturePhotoKey = KeyCode.Space;

    [Header("Controles VR")]
    public UnityEngine.XR.InputFeatureUsage<bool> vrToggleButton = UnityEngine.XR.CommonUsages.primaryButton;
    public UnityEngine.XR.InputFeatureUsage<bool> vrCaptureButton = UnityEngine.XR.CommonUsages.triggerButton;

    private bool isCameraActive = false;
    private XRBaseController rightController;
    private bool isVRMode = false;
    private float lastToggleTime = 0f;
    private float lastCaptureTime = 0f;
    private float buttonCooldown = 0.3f;

    void Awake()
    {
        AutoAssignReferences();
    }

    void Start()
    {
        UnityEngine.XR.InputDevice rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        isVRMode = rightHandDevice.isValid;

        Debug.Log(isVRMode ? "🥽 Modo VR detectado" : "🖥️ Modo PC detectado");
        Debug.Log("📡 Update activo"); // ✅ Solo se imprime una vez

        SetAllGhostsVisible(false);
        SetCameraActive(false);
    }

    void AutoAssignReferences()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

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

            DetectGhostInSight(); // 👻 Detectar si se apunta a un fantasma
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

    void ToggleCamera()
    {
        SetCameraActive(!isCameraActive);
    }

    void SetCameraActive(bool active)
    {
        isCameraActive = active;
        Debug.Log("🟢 SetCameraActive llamado con: " + active);

        if (ghostCameraCanvas != null)
            ghostCameraCanvas.gameObject.SetActive(active);

        SetAllGhostsVisible(active);

        if (isVRMode)
        {
            float intensity = active ? 0.3f : 0.2f;
            float duration = active ? 0.2f : 0.1f;
            SendHapticImpulse(rightController, intensity, duration);
        }

        Debug.Log(active ? "📷 Modo cámara ACTIVADO" : "📷 Modo cámara DESACTIVADO");
    }

    void CapturePhoto()
    {
        Debug.Log("📸 Intentando capturar foto");

        if (mainCamera == null)
        {
            Debug.LogWarning("⚠️ Main Camera no asignada!");
            return;
        }

        int ghostsCaptured = 0;
        Ghost[] allGhosts = FindObjectsOfType<Ghost>();

        foreach (Ghost ghost in allGhosts)
        {
            if (IsInCameraView(ghost.transform))
            {
                ghost.Stun();
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

    void DetectGhostInSight()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, captureRange))
        {
            Debug.Log("🔦 Raycast golpeó: " + hit.transform.name);

            if (hit.transform.CompareTag(ghostTag))
            {
                Ghost ghost = hit.transform.GetComponent<Ghost>();
                if (ghost != null)
                {
                    ghost.SetVisible(true);
                    Debug.Log("👻 Fantasma visible activado");
                }
            }
        }
        else
        {
            Debug.Log("🌫️ Raycast no golpeó nada");
        }
    }

    bool IsInCameraView(Transform target)
    {
        if (mainCamera == null) return false;

        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(target.position);

        if (viewportPoint.z > 0 &&
            viewportPoint.x > 0.25f && viewportPoint.x < 0.75f &&
            viewportPoint.y > 0.25f && viewportPoint.y < 0.75f)
        {
            float distance = Vector3.Distance(mainCamera.transform.position, target.position);
            if (distance <= captureRange)
            {
                if (Physics.Raycast(mainCamera.transform.position,
                    target.position - mainCamera.transform.position,
                    out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
                {
                    return hit.transform == target || hit.transform.IsChildOf(target);
                }
            }
        }
        return false;
    }

    void SetAllGhostsVisible(bool visible)
    {
        Debug.Log("👻 SetAllGhostsVisible llamado con: " + visible);

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
        if (mainCamera == null) yield break;

        GameObject flashQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        flashQuad.transform.SetParent(mainCamera.transform);
        flashQuad.transform.localPosition = new Vector3(0, 0, mainCamera.nearClipPlane + 0.01f);
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