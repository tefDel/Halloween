using UnityEngine;
using System.Collections;

public class Ghost : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 6f;
    public float attackDistance = 1.2f;
    public float jumpscareDistance = 0.8f;
    public float obstacleAvoidanceDistance = 1.5f;
    public LayerMask obstacleMask;
    public GameObject visualRoot;

    [Header("Animaciones")]
    public Animator animator;
    public RuntimeAnimatorController animatorController;

    [Header("Jumpscare")]
    public Transform faceTarget;

    [Header("Screamer Final")]
    public AudioSource screamerAudio;
    public Light[] flickerLights;
    public float flickerDuration = 2f;
    public string sceneToReload = "FatalFrane";

    private bool isStunned = false;
    private bool hasAttacked = false;
    private bool hasTriggeredJumpscare = false;
    private bool hasPlayedAttackAnimation = false;
    private bool hasTriggeredFinalAttack = false;  // Prevents repeating the final attack

    [Header("Debug")]
    public bool debugMode = false;

    void Awake()
    {
        Debug.Log($"🔵 AWAKE llamado - GameObject activo: {gameObject.activeSelf}");
        TryInitializeAnimator("Awake");
    }

    void OnEnable()
    {
        Debug.Log($"🟢 ON_ENABLE llamado - GameObject activo: {gameObject.activeSelf}");
        TryInitializeAnimator("OnEnable");
    }

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
                Debug.LogError("❌ Animator no encontrado en Ghost");
            else
                Debug.Log("✅ Animator encontrado: " + animator.name);
        }
        if (animator != null && animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"⚠ {gameObject.name}: Animator sin controller. Animaciones deshabilitadas.");
            animator = null;
        }
        else if (animator != null)
        {
            Debug.Log("✅ Animator Controller activo: " + animator.runtimeAnimatorController.name);
        }
    }

    void TryInitializeAnimator(string calledFrom)
    {
        Debug.Log($"🔧 [{calledFrom}] Intentando inicializar Animator...");

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError($"❌ [{calledFrom}] No se encontró Animator");
                return;
            }
            Debug.Log($"✅ [{calledFrom}] Animator encontrado: {animator.name}");
        }
        else
        {
            Debug.Log($"ℹ [{calledFrom}] Animator ya estaba asignado: {animator.name}");
        }

        // Verificar el estado del animator
        Debug.Log($"📊 [{calledFrom}] Animator.enabled: {animator.enabled}");
        Debug.Log($"📊 [{calledFrom}] Animator.gameObject.activeSelf: {animator.gameObject.activeSelf}");
        Debug.Log($"📊 [{calledFrom}] RuntimeController ANTES: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL")}");

        // Forzar asignación del controlador
        if (animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
            Debug.Log($"🔄 [{calledFrom}] Controller asignado manualmente");
        }
        else
        {
            Debug.LogError($"❌ [{calledFrom}] animatorController es NULL en el Inspector!");
        }

        // Verificar después de asignar
        Debug.Log($"📊 [{calledFrom}] RuntimeController DESPUÉS: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL")}");

        // Verificar parámetros
        if (animator.runtimeAnimatorController != null)
        {
            Debug.Log($"✅ [{calledFrom}] Parámetros disponibles:");
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                Debug.Log($"   - {param.name} ({param.type})");
            }
        }
    }

    void Update()
    {
        // ⭐ DIAGNÓSTICO: Verificar estado del animator en cada frame
        if (animator == null)
        {
            Debug.LogError("❌ UPDATE: animator es NULL");
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError($"❌ UPDATE: runtimeAnimatorController es NULL (Frame: {Time.frameCount})");
            // Intentar reinicializar
            TryInitializeAnimator("Update-Retry");
            return;
        }

        if (isStunned || hasTriggeredJumpscare) return;

        Transform cam = Camera.main?.transform;
        if (cam == null) return;

        Vector3 ghostPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 camPos = new Vector3(cam.position.x, 0f, cam.position.z);
        float flatDistance = Vector3.Distance(ghostPos, camPos);

        Vector3 direction = (cam.position - transform.position).normalized;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(-direction);
            if (visualRoot != null)
                visualRoot.transform.rotation = Quaternion.Slerp(visualRoot.transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }

        if (flatDistance <= jumpscareDistance && !hasTriggeredJumpscare)
        {
            Debug.Log("👻 Activando jumpscare: distancia = " + flatDistance);
            StartCoroutine(TriggerJumpscare(cam));
            return;
        }

        if (flatDistance <= attackDistance)
        {
            if (!hasAttacked)
            {
                Debug.Log("👊 Activando ataque normal: distancia = " + flatDistance);
                hasAttacked = true;
                TriggerAttack();
            }
        }

        // ⭐ MODIFIED: Only auto-set idle/running if NOT attacking and NOT in debug mode
        if (!hasPlayedAttackAnimation && !debugMode)
        {
            if (flatDistance > attackDistance)
            {
                if (!Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.3f, direction, out _, obstacleAvoidanceDistance, obstacleMask))
                {
                    transform.position += direction * moveSpeed * Time.deltaTime;
                }

                SafeSetBool("isRunning", true);
                SafeSetBool("isIdle", false);
            }
            else
            {
                SafeSetBool("isRunning", false);
                SafeSetBool("isIdle", true);
            }
        }
    }

    private void SafeSetBool(string paramName, bool value)
    {
        if (animator == null)
        {
            Debug.LogWarning($"⚠ SafeSetBool({paramName}): animator es NULL");
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"⚠ SafeSetBool({paramName}): runtimeAnimatorController es NULL");
            return;
        }

        try
        {
            animator.SetBool(paramName, value);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al setear {paramName}: {e.Message}");
        }
    }

    public void TriggerAttack()
    {
        if (!hasPlayedAttackAnimation && !hasTriggeredFinalAttack)  // ⭐ MODIFIED: Only allow if not already triggered globally
        {
            Debug.Log("🎬 Activando animación de ataque final (única vez)");

            // ⭐ NEW: Flip the ghost 180 degrees (turn around)
            Transform cam = Camera.main?.transform;
            if (cam != null)
            {
                Vector3 directionToCam = (cam.position - transform.position).normalized;
                directionToCam.y = 0f;  // Keep it flat
                Vector3 flippedDirection = -directionToCam;  // Opposite direction
                Quaternion targetRot = Quaternion.LookRotation(flippedDirection);

                // Apply rotation instantly for a sharp flip
                if (visualRoot != null)
                    visualRoot.transform.rotation = targetRot;
                else
                    transform.rotation = targetRot;

                Debug.Log("🔄 Ghost flipped 180 degrees for attack");
            }

            // ⭐ MODIFIED: Deactivate idle and running BEFORE activating attacking
            SafeSetBool("isIdle", false);
            SafeSetBool("isRunning", false);
            SafeSetBool("isAttacking", true);

            // ⭐ NEW: Start camera drag to faceTarget
            if (Camera.main != null && faceTarget != null)
            {
                StartCoroutine(DragCameraToFace());
            }

            hasPlayedAttackAnimation = true;
            hasTriggeredFinalAttack = true;  // ⭐ NEW: Mark as triggered to prevent repeats
            StartCoroutine(ResetAttackAnimation());
        }
    }

    IEnumerator ResetAttackAnimation()
    {
        yield return new WaitForSeconds(1.5f);

        SafeSetBool("isAttacking", false);

        // ⭐ MODIFIED: Do NOT restore idle/running or reset flags—end the game instead
        Debug.Log("💀 Animation ended—reloading scene to end game");
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToReload);

        // No reset of hasPlayedAttackAnimation or hasAttacked, as game ends
    }

    IEnumerator TriggerJumpscare(Transform cam)
    {
        hasTriggeredJumpscare = true;

        Vector3 lookDir = cam.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            if (visualRoot != null)
                visualRoot.transform.rotation = targetRot;
            else
                transform.rotation = targetRot;
        }

        if (Camera.main != null && faceTarget != null)
        {
            Camera.main.transform.position = faceTarget.position;
            Camera.main.transform.rotation = faceTarget.rotation;
        }

        Debug.Log("😱 Activando animación de jumpscare");
        SafeSetBool("isAttacking", true);

        if (screamerAudio != null)
        {
            Debug.Log("🔊 Reproduciendo sonido de screamer");
            screamerAudio.Play();
        }

        StartCoroutine(FlickerLights());

        yield return new WaitForSeconds(2f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToReload);
    }

    IEnumerator FlickerLights()
    {
        float elapsed = 0f;
        while (elapsed < flickerDuration)
        {
            foreach (Light light in flickerLights)
            {
                if (light != null)
                    light.enabled = !light.enabled;
            }

            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
            elapsed += Time.deltaTime;
        }

        foreach (Light light in flickerLights)
        {
            if (light != null)
                light.enabled = true;
        }
    }

    public void Stun()
    {
        if (!isStunned)
            StartCoroutine(StunAndRecover());
    }

    IEnumerator StunAndRecover()
    {
        isStunned = true;

        SafeSetBool("isDead", true);
        SafeSetBool("isIdle", false);
        SafeSetBool("isRunning", false);

        Debug.Log("💤 Fantasma aturdida");

        yield return new WaitForSeconds(5f);

        isStunned = false;
        SafeSetBool("isDead", false);
        SafeSetBool("isIdle", true);

        hasAttacked = false;
        Debug.Log("💥 Fantasma recuperada");
    }

    public void SetVisible(bool visible)
    {
        if (visualRoot != null)
        {
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                r.enabled = visible;
            }
        }
    }

    IEnumerator DragCameraToFace()
    {
        Transform cam = Camera.main.transform;
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;
        float duration = 1.5f;  // Match animation time; adjust if needed
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            cam.position = Vector3.Lerp(startPos, faceTarget.position, t);
            cam.rotation = Quaternion.Slerp(startRot, faceTarget.rotation, t);
            yield return null;
        }

        // Ensure exact final position/rotation
        cam.position = faceTarget.position;
        cam.rotation = faceTarget.rotation;

        Debug.Log("📹 Camera dragged to ghost's face");
    }
}