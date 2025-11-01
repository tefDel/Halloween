using UnityEngine;
using System.Collections;

public class Ghost : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 0f;
    public float rotationSpeed = 6f;
    public float attackDistance = 1.2f;
    public float jumpscareDistance = 0.8f;
    public float obstacleAvoidanceDistance = 1.5f;
    public LayerMask obstacleMask;
    public GameObject visualRoot;

    [Header("Fade en VR")]
    public GameObject blackFadeQuad;

    [Header("Animaciones")]
    public Animator animator;
    public RuntimeAnimatorController animatorController;

    [Header("Jumpscare")]
    public Transform faceTarget;
    public Transform xrOrigin; // ⭐ Asigna el XR Origin aquí (el objeto padre de la cámara VR)

    [Header("Screamer Final")]
    public AudioSource screamerAudio;
    public Light[] flickerLights;
    public float flickerDuration = 2f;
    public string sceneToReload = "FatalFrane";
    private bool hasPlayedScreamer = false;
    [Header("Luz guía")]
    public Light guideLight; // ← Asigna esta luz en el Inspector


    private bool isStunned = false;
    private bool hasAttacked = false;
    private bool hasTriggeredJumpscare = false;
    private bool isAttacking = false; // ⭐ ADDED: Explicit attack state tracking


    [Header("Debug")]
    public bool debugMode = false;

   

    // New variables for camera movement during attack
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool isCameraMoving = false;
    [Header("Destino de jumpscare")]
    public Transform ghostApproachTarget;

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
        Debug.Log("🚀 === GHOST START ===");

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

        // ⭐ Auto-detect XR Origin if not assigned
        Debug.Log("🔍 Verificando XR Origin...");
        Debug.Log($"   xrOrigin asignado en Inspector: {(xrOrigin != null ? xrOrigin.name : "NULL")}");

        if (xrOrigin == null && Camera.main != null)
        {
            Debug.Log("🔍 Buscando XR Origin automáticamente...");
            // Try to find XR Origin by going up the hierarchy
            Transform current = Camera.main.transform;
            int level = 0;
            while (current.parent != null && level < 10)
            {
                current = current.parent;
                level++;
                Debug.Log($"   Nivel {level}: {current.name}");

                if (current.name.Contains("XR") || current.name.Contains("Origin") || current.name.Contains("OVR") || current.name.Contains("CameraRig"))
                {
                    xrOrigin = current;
                    Debug.Log($"✅ XR Origin detectado automáticamente: {xrOrigin.name} (nivel {level})");
                    break;
                }
            }

            if (xrOrigin == null)
            {
                Debug.LogWarning("⚠️ No se detectó XR Origin automáticamente. Asígnalo manualmente en el Inspector.");
                Debug.LogWarning($"   Jerarquía de Camera.main: {GetHierarchyPath(Camera.main.transform)}");
            }
        }
        else if (xrOrigin != null)
        {
            Debug.Log($"✅ XR Origin ya asignado: {xrOrigin.name}");
        }

        // Store original XR Origin position
        if (xrOrigin != null)
        {
            originalCameraPosition = xrOrigin.position;
            originalCameraRotation = xrOrigin.rotation;
            Debug.Log($"💾 Posición original del XR Origin guardada:");
            Debug.Log($"   Position: {originalCameraPosition}");
            Debug.Log($"   Rotation: {originalCameraRotation.eulerAngles}");
        }
        else
        {
            Debug.LogError("❌ XR Origin es NULL después de Start! No se podrá mover la cámara.");
        }

        Debug.Log("🚀 === FIN GHOST START ===");
    }

    // Helper method to get full hierarchy path
    string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
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
        // ⭐ FIXED: Block all Update logic during attack
        if (isAttacking)
        {
            return;
        }

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

        // ⭐ FIXED: Only set movement animations when not attacking
        if (!debugMode)
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
    public void ActivateMovement()
    {
        // Si aún no tiene velocidad asignada, empieza con la base
        if (moveSpeed <= 0f)
            moveSpeed = 0.7f; // velocidad base inicial

        Debug.Log($"👻 Fantasma activada: velocidad inicial = {moveSpeed}");
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
            bool currentValue = animator.GetBool(paramName);
            Debug.Log($"🔍 SafeSetBool({paramName}): current = {currentValue}, setting to {value}");
            animator.SetBool(paramName, value);
            Debug.Log($"✅ SafeSetBool({paramName}): set to {value} successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al setear {paramName}: {e.Message}");
        }
    }

    public void TriggerAttack()
    {
        // ⭐ FIXED: Set isAttacking flag immediately
        isAttacking = true;
        if (guideLight != null && guideLight.enabled)
        {
            guideLight.enabled = false;
            Debug.Log("💡 Luz guía apagada al iniciar el screamer");
        }
        Debug.Log("🎬 Activando animación de ataque");

        Transform cam = Camera.main?.transform;
        if (cam != null)
        {
            Vector3 directionToCam = (cam.position - transform.position).normalized;
            directionToCam.y = 0f;
            Vector3 flippedDirection = -directionToCam;
            Quaternion targetRot = Quaternion.LookRotation(flippedDirection);

            if (visualRoot != null)
                visualRoot.transform.rotation = targetRot;
            else
                transform.rotation = targetRot;

            Debug.Log("🔄 Ghost flipped 180 degrees for attack");
        }

        SafeSetBool("isIdle", false);
        SafeSetBool("isRunning", false);
        SafeSetBool("isAttacking", true);
        // 💥 Reproducir sonido de grito al iniciar ataque
        if (screamerAudio != null && !hasPlayedScreamer)
        {
            screamerAudio.Play();
            hasPlayedScreamer = true;
            Debug.Log("🔊 Sonido de ataque reproducido una sola vez");
        }

        // ⭐ Mueve XR Origin hacia el faceTarget apenas inicia el ataque
        if (xrOrigin != null && faceTarget != null)
        {
            StartCoroutine(MoveGhostToCameraTarget(1.5f));
            // Puedes ajustar la duración
        }
        else
        {
            Debug.LogWarning("⚠ No se puede mover XR Origin: faltan referencias");
        }
        // Start coroutine to wait for animation completion
        StartCoroutine(WaitForAttackAnimation());
    }

    // ⭐ FIXED: Improved animation wait logic
    IEnumerator WaitForAttackAnimation()
    {
        Debug.Log("⏳ Esperando a que termine la animación de ataque");

        yield return null;
        yield return null;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float timeout = 0f;

        while (!stateInfo.IsName("Attack") && timeout < 1f)
        {
            yield return null;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            timeout += Time.deltaTime;
        }

        while (stateInfo.normalizedTime < 0.95f || animator.IsInTransition(0))
        {
            yield return null;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }

        Debug.Log("✅ Animación de ataque completada");

        // 💥 Activar pantalla negra inmediatamente
        if (blackFadeQuad != null)
        {
            blackFadeQuad.SetActive(true);
            Debug.Log("🕳 Pantalla negra activada en VR");
        }

        if (visualRoot != null)
        {
            foreach (Renderer r in visualRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (r.enabled)
                {
                    r.enabled = false;
                }
            }
            Debug.Log("👻 Fantasma vuelta invisible (renderers apagados)");
        }



        // ⏳ Esperar a que el sonido termine
        if (screamerAudio != null && screamerAudio.clip != null)
        {
            Debug.Log("🔊 Esperando a que termine el sonido...");
            yield return new WaitForSeconds(screamerAudio.clip.length);
        }

        // ⏳ Esperar 5 segundos con pantalla negra
        yield return new WaitForSeconds(4f);

        // 🔄 Reiniciar escena
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToReload);
    }



    // ⭐ Move XR Origin to faceTarget position for attack view
    IEnumerator MoveXROriginToFaceTarget(float duration)
    {
        Debug.Log("🔍 === INICIO MoveXROriginToFaceTarget ===");

        if (faceTarget == null || xrOrigin == null || Camera.main == null)
        {
            Debug.LogError("❌ Referencias faltantes para movimiento de XR Origin");
            yield break;
        }

        Vector3 cameraLocalPos = Camera.main.transform.localPosition;
        Vector3 targetPos = faceTarget.position - (xrOrigin.rotation * cameraLocalPos);
        Quaternion targetRot = faceTarget.rotation;

        // 💥 Movimiento brusco tipo jumpscare
        xrOrigin.position = targetPos + Random.insideUnitSphere * 0.03f; // Temblor inicial
        xrOrigin.rotation = targetRot;
        yield return new WaitForSeconds(0.05f); // Microdelay

        xrOrigin.position = targetPos;
        xrOrigin.rotation = targetRot;

        Debug.Log("💥 Movimiento brusco completado");
    }

    // New coroutine to move camera towards faceTarget during attack
    IEnumerator MoveCameraToFace(float duration)
    {
        isCameraMoving = true;
        Transform cam = Camera.main.transform;
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;
        Vector3 endPos = faceTarget.position;
        Quaternion endRot = faceTarget.rotation;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            cam.position = Vector3.Lerp(startPos, endPos, t);
            cam.rotation = Quaternion.Slerp(startRot, endRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cam.position = endPos;
        cam.rotation = endRot;
        Debug.Log("📹 Camera moved to faceTarget for attack");
    }

    // New coroutine to move camera back to original position
    IEnumerator MoveCameraBack(float duration)
    {
        Transform cam = Camera.main.transform;
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            cam.position = Vector3.Lerp(startPos, originalCameraPosition, t);
            cam.rotation = Quaternion.Slerp(startRot, originalCameraRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cam.position = originalCameraPosition;
        cam.rotation = originalCameraRotation;
        isCameraMoving = false;
        Debug.Log("📹 Camera moved back to original position");
    }


    IEnumerator TriggerJumpscare(Transform cam)
    {
        hasTriggeredJumpscare = true;

        // ⏳ Programar reinicio exacto 3 segundos después del inicio del jumpscare
        StartCoroutine(DelayedSceneReload(3f));

        // Rotar la fantasma hacia el jugador
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

        // Mover XR Origin al target del jumpscare
        if (xrOrigin != null && faceTarget != null)
        {
            Debug.Log("📹 Moving XR Origin to faceTarget for jumpscare");
            StartCoroutine(MoveXROriginToFaceTarget(0.1f));
        }
        else
        {
            Debug.LogWarning("⚠ XR Origin or faceTarget is null, cannot move for jumpscare");
        }

        // Activar luces parpadeantes
        StartCoroutine(FlickerLights());

        // Activar pantalla negra
        if (blackFadeQuad != null)
        {
            blackFadeQuad.SetActive(true);
            Debug.Log("🕳 Pantalla negra activada en VR");
        }

        yield break; // Finalizar la corutina sin esperar más
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

        // 💾 Guardar velocidad actual antes del estuneo
        float previousSpeed = moveSpeed;
        moveSpeed = 0f; // 🚫 Bloquear movimiento completamente

        // Reproducir animación de "muerte"/aturdimiento
        SafeSetBool("isDead", true);
        SafeSetBool("isIdle", false);
        SafeSetBool("isRunning", false);

        Debug.Log($"💤 Fantasma aturdida durante 5s (velocidad original = {previousSpeed})");

        // Esperar duración del estuneo
        yield return new WaitForSeconds(10f);

        // ✅ Recuperar estado y velocidad
        SafeSetBool("isDead", false);
        SafeSetBool("isIdle", true);

        isStunned = false;
        hasAttacked = false;

        // 🔁 Restaurar la velocidad que tenía antes
        moveSpeed = previousSpeed;

        Debug.Log($"💥 Fantasma recuperada, velocidad restaurada a {moveSpeed}");
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
    public void UpdateSpeed(int itemCount)
    {
        moveSpeed = 0.75f + (itemCount * 0.4f); 
        Debug.Log($"👻 Velocidad actual de la fantasma: {moveSpeed} (por {itemCount} ítems)");
    }

    IEnumerator MoveGhostToCameraTarget(float duration)
    {
        Debug.Log("👻 === TELETRANSPORTE DE FANTASMA AL TARGET MIRANDO AL JUGADOR CON GIRO 180° ===");

        if (ghostApproachTarget == null || visualRoot == null || Camera.main == null)
        {
            Debug.LogError("❌ ghostApproachTarget, visualRoot o Camera.main es NULL");
            yield break;
        }

        // Posición exacta del target
        Vector3 targetPos = ghostApproachTarget.position;

        // Mantener altitud actual de la fantasma
        targetPos.y = transform.position.y;

        // Calcular rotación para mirar al jugador
        Vector3 lookDirection = Camera.main.transform.position - targetPos;
        lookDirection.y = 0f;
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);

        // Aplicar giro de 180° en Y
        Quaternion flippedRotation = lookRotation * Quaternion.Euler(0f, 180f, 0f);

        // 💥 Teletransporte con microtemblor
        transform.position = targetPos + Random.insideUnitSphere * 0.03f;
        transform.rotation = flippedRotation;
        yield return new WaitForSeconds(0.05f);

        transform.position = targetPos;
        transform.rotation = flippedRotation;

        Debug.Log("✅ Fantasma apareció en el target mirando al jugador con giro 180°");
    }

    void ReloadScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToReload);
    }

    IEnumerator DelayedSceneReload(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReloadScene();
    }


}