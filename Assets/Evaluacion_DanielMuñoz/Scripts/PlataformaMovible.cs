using UnityEngine;

public class PlataformaMovible : MonoBehaviour
{
    public float speed = 2f; // Velocidad de movimiento
    public float rangoM = 5f; // Rango de movimiento en el eje seleccionado
    public bool moverEnEjeX = true; // Si es verdadero, la plataforma se moverá en el eje X; si es falso, en el eje Z

    private Vector3 pInicial; // Posición inicial de la plataforma
    private bool avanzando = true; // Dirección del movimiento para el eje Z

    void Start()
    {
        pInicial = transform.position; // Guardamos la posición inicial
    }

    void FixedUpdate()
    {
        if (moverEnEjeX)
        {
            // Movimiento oscilante en el eje X
            float movimiento = Mathf.PingPong(Time.time * speed, rangoM * 2 - 0.1f) - rangoM + 0.05f;
            transform.position = new Vector3(pInicial.x + movimiento, transform.position.y, transform.position.z);
        }
        else
        {
            // Movimiento alternado en el eje Z
            if (avanzando)
            {
                transform.position += Vector3.forward * speed * Time.fixedDeltaTime;

                if (transform.position.z >= pInicial.z + rangoM - 0.1f) // Pequeño margen
                {
                    avanzando = false;
                }
            }
            else
            {
                transform.position += Vector3.back * speed * Time.fixedDeltaTime;

                if (transform.position.z <= pInicial.z - rangoM + 0.1f) // Pequeño margen
                {
                    avanzando = true;
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            Debug.Log("Jugador ha entrado en la plataforma");
            collision.transform.SetParent(transform); // Hacer al jugador hijo de la plataforma
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            Debug.Log("Jugador ha salido de la plataforma");
            collision.transform.SetParent(null); // Desvincular al jugador de la plataforma
        }
    }
}
