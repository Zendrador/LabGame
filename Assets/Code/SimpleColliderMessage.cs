using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleColliderMessage : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private Text messageText;

    [Header("Message Settings")]
    [SerializeField] private string message = "New Objective Added!";
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private KeyCode closeKey = KeyCode.E;
    [SerializeField] private bool requireKeyPress = false;

    [Header("Collider Settings")]
    [SerializeField] private bool isTrigger = true;
    [SerializeField] private bool oneTimeOnly = false;
    [SerializeField] private LayerMask playerLayer = 1; // Default layer
    [SerializeField] private bool showOnEnter = true;
    [SerializeField] private bool hideOnExit = true;

    private bool hasBeenTriggered = false;
    private bool isShowing = false;
    private float hideTimer = 0f;

    void Start()
    {
        // Hide message panel at start
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }

        // Set up collider
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = isTrigger;
        }
    }

    void Update()
    {
        // Handle auto-hide timer
        if (isShowing && !requireKeyPress && displayDuration > 0)
        {
            hideTimer += Time.deltaTime;
            if (hideTimer >= displayDuration)
            {
                HideMessage();
            }
        }

        // Handle key press to close message
        if (isShowing && requireKeyPress && Input.GetKeyDown(closeKey))
        {
            HideMessage();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (showOnEnter && ShouldTrigger(other))
        {
            ShowMessage();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (hideOnExit && ShouldTrigger(other) && !requireKeyPress)
        {
            HideMessage();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isTrigger && showOnEnter && ShouldTrigger(collision.collider))
        {
            ShowMessage();
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (!isTrigger && hideOnExit && ShouldTrigger(collision.collider) && !requireKeyPress)
        {
            HideMessage();
        }
    }

    private bool ShouldTrigger(Collider other)
    {
        // Check if one-time trigger has already been used
        if (oneTimeOnly && hasBeenTriggered) return false;

        // Check layer mask
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            // Check if it's the player (optional tag check)
            if (other.CompareTag("Player"))
            {
                return true;
            }
            // If no tag is set, allow any object on the player layer
            return true;
        }

        return false;
    }

    public void ShowMessage()
    {
        if (isShowing) return;

        // Mark as triggered if one-time only
        if (oneTimeOnly) hasBeenTriggered = true;

        isShowing = true;
        hideTimer = 0f;

        // Set message text
        if (messageText != null)
        {
            messageText.text = message;
        }

        // Activate panel
        if (messagePanel != null)
        {
            messagePanel.SetActive(true);
        }

        Debug.Log("Message shown: " + message);
    }

    public void HideMessage()
    {
        if (!isShowing) return;

        // Deactivate panel
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }

        isShowing = false;
        hideTimer = 0f;

        Debug.Log("Message hidden");
    }

    // Public methods to control from other scripts
    public void SetMessage(string newMessage)
    {
        message = newMessage;
        if (isShowing && messageText != null)
        {
            messageText.text = message;
        }
    }

    public bool IsMessageShowing()
    {
        return isShowing;
    }

    public void ShowCustomMessage(string customMessage, float duration = 3f)
    {
        string originalMessage = message;
        float originalDuration = displayDuration;

        message = customMessage;
        displayDuration = duration;

        ShowMessage();

        // Restore original settings after showing
        message = originalMessage;
        displayDuration = originalDuration;
    }

    // For debugging
    void OnDrawGizmos()
    {
        if (isTrigger)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            if (GetComponent<BoxCollider>() != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(Vector3.zero, GetComponent<BoxCollider>().size);
            }
            else if (GetComponent<SphereCollider>() != null)
            {
                Gizmos.DrawSphere(transform.position, GetComponent<SphereCollider>().radius);
            }
        }
    }
}