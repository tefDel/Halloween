using UnityEngine;

public class HorrorFloatingBook : MonoBehaviour
{
    [Header("Flotación normal")]
    [SerializeField] private float floatHeight = 0.08f;
    [SerializeField] private float floatSpeed = 1.2f;

    [Header("Momento de terror")]
    [SerializeField] private float horrorDelay = 8f;
    [SerializeField] private float horrorTransition = 3f;

    [Header("Movimiento escalofriante")]
    [SerializeField] private float horrorFloatHeight = 0.15f;
    [SerializeField] private float horrorRotation = 8f;
    [SerializeField] private float horrorSpeed = 0.7f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float timer;

    void Start()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // ==========================================
        // FLOTACIÓN NORMAL
        // ==========================================

        float normalFloat =
            Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        Vector3 targetPosition =
            startPosition + Vector3.up * normalFloat;

        Quaternion targetRotation = startRotation;

        // ==========================================
        // TRANSICIÓN AL HORROR
        // ==========================================

        if (timer > horrorDelay)
        {
            float horrorTimer = timer - horrorDelay;

            float transition =
                Mathf.Clamp01(horrorTimer / horrorTransition);

            // Suavizamos la transición
            transition = Mathf.SmoothStep(0f, 1f, transition);

            // Movimiento de flotación más profundo
            float horrorFloat =
                Mathf.Sin(Time.time * horrorSpeed) *
                horrorFloatHeight;

            targetPosition =
                startPosition +
                Vector3.up * Mathf.Lerp(
                    normalFloat,
                    horrorFloat,
                    transition
                );

            // Rotación lenta y antinatural
            float rotationX =
                Mathf.Sin(Time.time * horrorSpeed * 0.8f)
                * horrorRotation * transition;

            float rotationZ =
                Mathf.Cos(Time.time * horrorSpeed * 0.6f)
                * (horrorRotation * 0.5f) * transition;

            targetRotation =
                startRotation *
                Quaternion.Euler(
                    rotationX,
                    0f,
                    rotationZ
                );
        }

        // Aplicamos el movimiento
        transform.localPosition = targetPosition;
        transform.localRotation = targetRotation;
    }
}