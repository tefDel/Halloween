using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Ghost : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Movement")]
    public NavMeshAgent agent;
    public float chaseSpeed = 2f;
    public float attackDistance = 1.2f;

    [Header("Animation")]
    public Animator animator;

    private bool isStunned = false;
    private bool hasAttacked = false;

    void Start()
    {
        currentHealth = maxHealth;
        animator.SetBool("isIdle", true);
        agent.updateRotation = false;
    }

    void Update()
    {
        if (isStunned) return;

        if (Camera.main != null)
        {
            Vector3 targetPosition = Camera.main.transform.position;
            agent.speed = chaseSpeed;
            agent.SetDestination(targetPosition);

            Debug.Log("Persiguiendo a: " + targetPosition);

            // Animaciones
            float distance = Vector3.Distance(transform.position, targetPosition);
            bool isMoving = agent.velocity.magnitude > 0.1f;

            animator.SetBool("isRunning", isMoving);
            animator.SetBool("isIdle", !isMoving);

            if (distance <= attackDistance && !hasAttacked)
            {
                hasAttacked = true;
                TriggerAttack();
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isStunned) return;

        currentHealth -= damage;
        Debug.Log($"👻 Fantasma dañado! HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            StartCoroutine(StunAndRecover());
        }
    }

    IEnumerator StunAndRecover()
    {
        isStunned = true;
        agent.isStopped = true;

        animator.SetBool("isDead", true);
        animator.SetBool("isIdle", false);
        animator.SetBool("isRunning", false);

        yield return new WaitForSeconds(10f);

        currentHealth = maxHealth;
        isStunned = false;
        agent.isStopped = false;

        animator.SetBool("isDead", false);
        animator.SetBool("isIdle", true);
        hasAttacked = false;

        Debug.Log("👻 Fantasma recuperado y sigue buscando...");
    }

    public void TriggerAttack()
    {
        animator.SetBool("isAttacking", true);
        StartCoroutine(ResetAttack());
    }

    IEnumerator ResetAttack()
    {
        yield return new WaitForSeconds(1.5f);
        animator.SetBool("isAttacking", false);
    }
}