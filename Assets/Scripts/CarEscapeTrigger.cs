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
    [HideInInspector]

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
    public TextMeshPro messageText;
    public float messageDuration = 3f;

    [Header("Final del Juego")]
    public string sceneToLoad = "GameOverScene";
    public float delayBeforeSceneLoad = 5f;


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

    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player") || other.transform.root == xrRig)
        {
            if (!AllItemsCollected())
            {
                ShowMissingItems();
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
    void ShowMessage(string text)
    {
        if (messageCanvas != null && messageText != null)
        {
            messageText.text = text;
            messageCanvas.enabled = true;
            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), messageDuration);
        }
    }

    void HideMessage()
    {
        if (messageCanvas != null)
            messageCanvas.enabled = false;
    }

    void ShowMissingItems()
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

        ShowMessage(message);
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

        yield return new WaitForSeconds(2f);


        if (carMoveTarget != null)
        {
            yield return StartCoroutine(MoveCarToTarget());
        }
        else
        {
            yield return new WaitForSeconds(delayBeforeSceneLoad);
        }

        SceneManager.LoadScene(sceneToLoad);
    }

    IEnumerator MoveCarToTarget()
    {
        isMoving = true;
        Vector3 startPos = carObject.transform.position;
        Vector3 endPos = carMoveTarget.position;
        float distance = Vector3.Distance(startPos, endPos);
        float elapsedTime = 0f;
        float duration = distance / carMoveSpeed;

        Vector3 direction = (endPos - startPos).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            carObject.transform.rotation = Quaternion.Slerp(
                carObject.transform.rotation,
                targetRotation,
                Time.deltaTime * 2f
            );
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float curvedT = speedCurve.Evaluate(t);

            carObject.transform.position = Vector3.Lerp(startPos, endPos, curvedT);

            yield return null;
        }

        carObject.transform.position = endPos;
        isMoving = false;


        yield return new WaitForSeconds(2f);
    }


    void DisablePlayerControls()
    {
        if (xrRig != null)
        {
            // CORRECCIÓN: Ahora con el namespace correcto
            LocomotionProvider locomotion = xrRig.GetComponentInChildren<LocomotionProvider>();
            if (locomotion != null)
                locomotion.enabled = false;

            // También deshabilitar los providers específicos
            var teleportation = xrRig.GetComponentInChildren<TeleportationProvider>();
            if (teleportation != null)
                teleportation.enabled = false;

            var continuous = xrRig.GetComponentInChildren<ContinuousMoveProviderBase>();
            if (continuous != null)
                continuous.enabled = false;

            var turn = xrRig.GetComponentInChildren<ContinuousTurnProviderBase>();
            if (turn != null)
                turn.enabled = false;
        }
    }


    public void CollectItem(string itemName)
    {
        if (!requiredItemNames.Contains(itemName)) return;

        collectedItemCount++;
        Debug.Log($"📦 {collectedItemCount}/{requiredItemNames.Count} Items recolectados");

        Ghost ghost = FindObjectOfType<Ghost>();
        if (ghost != null)
        {
            ghost.UpdateSpeed(collectedItemCount);
        }

        if (collectedItemCount >= requiredItemNames.Count)
        {
            Debug.Log("🚗 ¡Todos los items recolectados! Puedes ir al carro para escapar.");
        }
    }
    public void CollectItemByIndex(int index)
    {
        if (index >= 0 && index < itemsCollected.Count)
        {
            itemsCollected[index] = true;
            ShowMessage($"¡Recogiste: {requiredItemNames[index]}!\n\n{GetCollectedCount()}/{requiredItemNames.Count} items");

            GhostCameraController ghostCam = FindObjectOfType<GhostCameraController>();
            if (ghostCam != null)
                ghostCam.UpdateItemPanels(AllItemsCollected());
        }
    }


    int GetCollectedCount()
    {
        int count = 0;
        foreach (bool collected in itemsCollected)
        {
            if (collected) count++;
        }
        return count;
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