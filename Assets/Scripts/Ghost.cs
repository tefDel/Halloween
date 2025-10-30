using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Ghost : MonoBehaviour
{
    [Header("Movimiento")]
    public NavMeshAgent agent;
    public float chaseSpeed = 2f;
    public float attackDistance = 1.2f;
    public float jumpscareDistance = 0.8f;
    public GameObject visualRoot;
    [Header("Animaciones")]
    public Animator animator;
    [Header("Orientación")]
    public Transform cameraFocusPoint;
    [Header("Jumpscare")]
    public Transform faceTarget; // punto frente al rostro del fantasma

    private bool isStunned = false;
    private bool hasAttacked = false;
    private bool hasTriggeredJumpscare = false;

    // buffer para evitar cambios bruscos de estado al cruzar el límite
    private float resumeMovementBuffer = 0.15f;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        animator.SetBool("isIdle", true);

        // Desactivar rotación automática del NavMesh para controlarla manualmente
        agent.updateRotation = false;

        // Usar stoppingDistance para ayudar a que el agente frene antes del objetivo
        agent.stoppingDistance = attackDistance;
    }

    void Update()
    {
        // Si está aturdido o ya hizo jumpscare, no hace nada (pero dejamos animaciones según estado)
        if (isStunned || hasTriggeredJumpscare)
            return;

        Transform cam = Camera.main?.transform;
        if (cam == null) return;

        // Distancia horizontal (plano XZ)
        float flatDistance = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(cam.position.x, 0f, cam.position.z)
        );

        // Si está dentro de rango de jumpscare, priorizar jumpscare
        if (flatDistance <= jumpscareDistance && !hasTriggeredJumpscare)
        {
            // detener inmediatamente para la animación de jumpscare
            agent.isStopped = true;
            StartCoroutine(TriggerJumpscare(cam));
            return;
        }

        // Si está dentro de rango de ataque, detener y atacar
        if (flatDistance <= attackDistance && !hasAttacked)
        {
            agent.isStopped = true;
            hasAttacked = true;
            TriggerAttack();
        }
        else if (flatDistance > attackDistance + resumeMovementBuffer)
        {
            agent.isStopped = false;
        }

        // Solo perseguir si no está detenido por ataque/jumpscare/aturdimiento
        if (!agent.isStopped)
        {
            agent.speed = chaseSpeed;
            agent.SetDestination(cam.position);
        }

        if (cameraFocusPoint != null)
        {
            Vector3 lookDir = cameraFocusPoint.position - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6f);
            }
        }


        // Animaciones de movimiento (solo si el agente se mueve)
        bool isMoving = agent.velocity.sqrMagnitude > 0.01f && !agent.isStopped;
        animator.SetBool("isRunning", isMoving);
        animator.SetBool("isIdle", !isMoving);
    }

    IEnumerator TriggerJumpscare(Transform cam)
    {
        hasTriggeredJumpscare = true;
        agent.isStopped = true;

        // Aseguramos que mire a la cámara (rotación inmediata)
        Vector3 lookDir = cam.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(lookDir);

        // Posiciona la cámara frente al rostro del fantasma (si está disponible)
        if (Camera.main != null && faceTarget != null)
        {
            Camera.main.transform.position = faceTarget.position;
            Camera.main.transform.rotation = faceTarget.rotation;
        }

        // Animación de ataque / jumpscare
        animator.SetBool("isAttacking", true);

        // Espera para que la animación haga su efecto (ajusta el tiempo según tu animación)
        yield return new WaitForSeconds(1.5f);

        animator.SetBool("isAttacking", false);
        Debug.Log("💀 Jumpscare activado - el jugador fue atrapado");
    }

    public void TriggerAttack()
    {
        // Nos aseguramos de que la animación de ataque se ejecute aunque el agent esté detenido
        animator.SetBool("isAttacking", true);
        StartCoroutine(ResetAttack());
    }

    IEnumerator ResetAttack()
    {
        // Duración de la animación de ataque (ajusta según tu clip)
        yield return new WaitForSeconds(1.2f);

        animator.SetBool("isAttacking", false);
        hasAttacked = false;

        // Si el jugador sigue dentro del rango de ataque, mantenemos detenido; si no, reanudamos movimiento
        Transform cam = Camera.main?.transform;
        if (cam != null)
        {
            float flatDistance = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(cam.position.x, 0f, cam.position.z)
            );

            if (flatDistance > attackDistance + resumeMovementBuffer)
                agent.isStopped = false;
            else
                agent.isStopped = true; // mantén detenido si aún está en rango
        }
        else
        {
            agent.isStopped = false;
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
        agent.isStopped = true;

        animator.SetBool("isDead", true);
        animator.SetBool("isIdle", false);
        animator.SetBool("isRunning", false);

        Debug.Log("👻 Fantasma aturdido...");

        yield return new WaitForSeconds(5f);

        isStunned = false;
        agent.isStopped = false;
        animator.SetBool("isDead", false);
        animator.SetBool("isIdle", true);
        hasAttacked = false;

        Debug.Log("👻 Fantasma recuperado...");
    }

    // 🔹 Mantengo tu método original

    public void SetVisible(bool visible)
    {
        if (visualRoot != null)
            visualRoot.SetActive(visible);
    }

}