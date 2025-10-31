using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using TMPro;

public class CarEscapeTrigger : MonoBehaviour
{
    [Header("XR Rig")]
    public Transform xrRig;
    public Transform xrCamera;

    [Header("Items Requeridos")]
    public int collectedItemCount = 0;
    public List<string> requiredItemNames = new List<string>
    {
        "Dark Diary",
        "LLave",
        "Agua",
        "Aceite",
        "Gasolina"
    };
    private List<bool> itemsCollected = new List<bool>();

    [Header("Carro - Transformaciones")]
    public Transform carSeatTransform;
    public Transform carMoveTarget;
    public GameObject carObject;

    [Header("Movimiento del Carro")]
    public float carMoveSpeed = 5f;
    public float carAcceleration = 2f;
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    public AudioSource carStartSound;

    [Header("UI Mensaje")]
    public Canvas messageCanvas;
    public GameObject panelFaltanItems;
    public GameObject panelTodoListo;
    public float messageDuration = 3f;

    [Header("Final del Juego")]
    public string sceneToLoad = "GameOverScene";
    public float delayBeforeSceneLoad = 5f;

    [Header("Blackout")]
    public GameObject blackFadeQuad;
    public float fadeDuration = 2f;

    [Header("Cámara dentro del carro")]
    public Transform cameraSeatTransform;
    public Transform cameraRoot;

    private bool hasTriggered = false;
    private bool isMoving = false;

    void Start()
    {
        if (xrRig == null)
        {
            GameObject rig = GameObject.Find("XR Origin") ?? GameObject.Find("XR Rig");
            if (rig != null)
                xrRig = rig.transform;
        }

        itemsCollected = new List<bool>(new bool[requiredItemNames.Count]);
        ShowMissingItemsVisual(); // Mostrar panel rojo al iniciar

    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player") || other.transform.root == xrRig)
        {
            if (!AllItemsCollected())
            {
                ShowMissingItemsWithText();
            }
            else
            {
                hasTriggered = true;
                StartCoroutine(EscapeSequence());
            }
        }
    }

    bool AllItemsCollected()
    {
        foreach (bool collected in itemsCollected)
        {
            if (!collected) return false;
        }
        return true;
    }

    void ShowMissingItemsVisual()
    {
        if (panelFaltanItems != null)
            panelFaltanItems.SetActive(true);

        if (panelTodoListo != null)
            panelTodoListo.SetActive(false);
    }

    void ShowReadyToEscape()
    {
        if (panelTodoListo != null)
            panelTodoListo.SetActive(true);

        if (panelFaltanItems != null)
            panelFaltanItems.SetActive(false);
    }


    void ShowMissingItemsWithText()
    {
        GhostCameraController ghostCam = FindObjectOfType<GhostCameraController>();
        if (ghostCam != null)
            ghostCam.UpdateItemPanels(false);

        string message = "Te faltan estos items:\n\n";
        for (int i = 0; i < requiredItemNames.Count; i++)
        {
            if (!itemsCollected[i])
            {
                message += "• " + requiredItemNames[i] + "\n";
            }
        }

        Debug.Log(message);
        ShowMissingItemsVisual();
    }

    IEnumerator EscapeSequence()
    {
        DisablePlayerControls();

        if (xrRig != null && carSeatTransform != null)
        {
            xrRig.position = carSeatTransform.position;
            xrRig.rotation = carSeatTransform.rotation;
            xrRig.SetParent(carObject.transform);
        }

        if (carStartSound != null)
            carStartSound.Play();

        yield return StartCoroutine(CameraShake(0.5f, 0.03f));

        if (cameraRoot != null && cameraSeatTransform != null)
        {
            cameraRoot.position = cameraSeatTransform.position;
            cameraRoot.rotation = cameraSeatTransform.rotation;
        }

        yield return StartCoroutine(MoveCarForward());

        if (blackFadeQuad != null)
            yield return StartCoroutine(FadeToBlack());

        yield return new WaitForSeconds(delayBeforeSceneLoad);
        SceneManager.LoadScene(sceneToLoad);
    }

    IEnumerator CameraShake(float duration, float intensity)
    {
        float elapsed = 0f;
        Vector3 originalPos = xrCamera.localPosition;

        while (elapsed < duration)
        {
            Vector3 offset = Random.insideUnitSphere * intensity;
            xrCamera.localPosition = originalPos + offset;
            elapsed += Time.deltaTime;
            yield return null;
        }

        xrCamera.localPosition = originalPos;
    }

    IEnumerator MoveCarForward()
    {
        float moveTime = 3f;
        float elapsed = 0f;
        Vector3 direction = carObject.transform.forward;

        while (elapsed < moveTime)
        {
            carObject.transform.position += direction * carMoveSpeed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator FadeToBlack()
    {
        Renderer quadRenderer = blackFadeQuad.GetComponent<Renderer>();
        if (quadRenderer == null) yield break;

        Material mat = quadRenderer.material;
        Color startColor = mat.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 1f);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            mat.color = Color.Lerp(startColor, endColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mat.color = endColor;
    }

    void DisablePlayerControls()
    {
        if (xrRig != null)
        {
            LocomotionProvider locomotion = xrRig.GetComponentInChildren<LocomotionProvider>();
            if (locomotion != null) locomotion.enabled = false;

            var teleportation = xrRig.GetComponentInChildren<TeleportationProvider>();
            if (teleportation != null) teleportation.enabled = false;

            var continuous = xrRig.GetComponentInChildren<ContinuousMoveProviderBase>();
            if (continuous != null) continuous.enabled = false;

            var turn = xrRig.GetComponentInChildren<ContinuousTurnProviderBase>();
            if (turn != null) turn.enabled = false;
        }
    }

    public void CollectItem(string itemName)
    {
        int index = requiredItemNames.IndexOf(itemName);
        if (index < 0 || index >= itemsCollected.Count) return;

        if (!itemsCollected[index])
        {
            itemsCollected[index] = true;
            collectedItemCount++;
            Debug.Log($"📦 {collectedItemCount}/{requiredItemNames.Count} Items recolectados");

            Ghost ghost = FindObjectOfType<Ghost>();
            if (ghost != null)
            {
                ghost.UpdateSpeed(collectedItemCount);
            }

            if (AllItemsCollected())
            {
                Debug.Log("🚗 ¡Todos los items recolectados! Puedes ir al carro para escapar.");
                ShowReadyToEscape();
            }
            else
            {
                ShowMissingItemsVisual(); // Mantener panel rojo si aún faltan
            }
        }
    }


    public bool IsItemCollected(string itemName)
    {
        int index = requiredItemNames.IndexOf(itemName);
        if (index >= 0 && index < itemsCollected.Count)
            return itemsCollected[index];
        return false;
    }

    void OnDrawGizmos()
    {
        if (carMoveTarget != null && carObject != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(carObject.transform.position, carMoveTarget.position);
            Gizmos.DrawWireSphere(carMoveTarget.position, 1f);
        }
    }
}
