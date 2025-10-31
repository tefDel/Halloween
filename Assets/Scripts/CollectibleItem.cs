using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CollectibleItem : MonoBehaviour
{
    [Header("Configuración del Item")]
    [Tooltip("Nombre del item (debe coincidir EXACTAMENTE con la lista en CarEscapeTrigger)")]
    public string itemName;

    [Header("Referencias")]
    private CarEscapeTrigger carEscapeTrigger;

    [Header("Efectos (Opcional)")]
    public AudioClip pickupSound;
    public ParticleSystem pickupParticles;

    void Start()
    {
        // Buscar el CarEscapeTrigger en la escena
        carEscapeTrigger = FindObjectOfType<CarEscapeTrigger>();

        if (carEscapeTrigger == null)
        {
            Debug.LogError("❌ No se encontró CarEscapeTrigger en la escena!");
        }

        // Verificar que el nombre coincida con la lista
        if (carEscapeTrigger != null && !carEscapeTrigger.requiredItemNames.Contains(itemName))
        {
            Debug.LogWarning($"⚠ El item '{itemName}' no está en la lista de items requeridos!");
        }
    }

    void OnEnable()
    {
        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
        }
    }

    void OnDisable()
    {
        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        CollectItem();
    }

    void CollectItem()
    {
        if (carEscapeTrigger != null)
        {
            // Notificar al CarEscapeTrigger que este item fue recogido
            carEscapeTrigger.CollectItem(itemName);

            // Efectos opcionales
            PlayPickupEffects();

            // Destruir el objeto después de recogerlo
            Destroy(gameObject, 0.1f);
        }
    }

    void PlayPickupEffects()
    {
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        if (pickupParticles != null)
        {
            Instantiate(pickupParticles, transform.position, Quaternion.identity);
        }
    }
}