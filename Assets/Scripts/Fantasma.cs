using UnityEngine;

public class Fantasma : MonoBehaviour
{
    //public ParticleSystem captureEffect; // Opcional
    //public AudioClip captureSound; // Opcional

    public void OnCaptured()
    {
        //Debug.Log($"El fantasma {gameObject.name} fue capturado!");

        // Efectos opcionales
        //if (captureEffect != null)
            //captureEffect.Play();

        //if (captureSound != null)
        //{
            //AudioSource.PlayClipAtPoint(captureSound, transform.position);
        //}

        // Destruir después de un delay
        Destroy(gameObject, 0.5f);
    }
}
