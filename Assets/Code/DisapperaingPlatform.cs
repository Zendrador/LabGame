using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class SafeDisappearingPlatform : MonoBehaviour
{
    [Header("Movement Configuration")]
    [Tooltip("Direction the platform moves (in world space)")]
    public Vector3 moveDirection = new Vector3(0, -1, 0); // Default: Down

    [Tooltip("How far the platform moves in the specified direction")]
    public float moveDistance = 5f;

    [Tooltip("Speed at which the platform moves")]
    public float moveSpeed = 2f;

    [Tooltip("Delay before platform starts moving after detection")]
    public float movementDelay = 0.5f;

    [Header("Detection Settings")]
    [Tooltip("Radius for detecting the player")]
    public float detectionRadius = 3f;

    [Tooltip("Player layer for detection")]
    public LayerMask playerLayer;

    [Tooltip("Use trigger collider instead of radius detection")]
    public bool useTriggerDetection = false;

    [Header("Platform Behavior")]
    [Tooltip("Should the platform return to its original position?")]
    public bool returnAfterMove = true;

    [Tooltip("Delay before platform returns to start position")]
    public float returnDelay = 3f;

    [Tooltip("Speed at which platform returns")]
    public float returnSpeed = 1.5f;

    [Tooltip("Should platform be destroyed after moving?")]
    public bool destroyAfterUse = false;

    [Header("Visual Feedback")]
    [Tooltip("Material to use when platform is normal")]
    public Material normalMaterial;

    [Tooltip("Material to use when platform is warning")]
    public Material warningMaterial;

    [Tooltip("Material to use when platform is moving")]
    public Material movingMaterial;

    [Tooltip("Particle system to play when warning")]
    public ParticleSystem warningParticles;

    [Tooltip("Particle system to play when moving")]
    public ParticleSystem moveParticles;

    [Header("Audio")]
    public AudioClip detectionSound;
    public AudioClip movementSound;
    public AudioClip returnSound;

    // Private variables
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Renderer platformRenderer;
    private Collider platformCollider;
    private AudioSource audioSource;

    private enum PlatformState
    {
        Idle,
        PlayerDetected,
        Warning,
        Moving,
        WaitingToReturn,
        Returning,
        Destroyed
    }

    private PlatformState currentState = PlatformState.Idle;
    private float stateTimer = 0f;
    private bool playerIsOnPlatform = false;

    void Start()
    {
        InitializePlatform();
    }

    void InitializePlatform()
    {
        // Store initial position
        startPosition = transform.position;

        // Calculate target position based on direction and distance
        Vector3 normalizedDirection = moveDirection.normalized;
        targetPosition = startPosition + (normalizedDirection * moveDistance);

        // Get components
        platformRenderer = GetComponent<Renderer>();
        platformCollider = GetComponent<Collider>();

        // Add or get AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Set initial material
        if (platformRenderer != null && normalMaterial != null)
            platformRenderer.material = normalMaterial;

        // Stop any particle systems
        if (warningParticles != null)
            warningParticles.Stop();
        if (moveParticles != null)
            moveParticles.Stop();
    }

    void Update()
    {
        switch (currentState)
        {
            case PlatformState.Idle:
                CheckForPlayer();
                break;

            case PlatformState.PlayerDetected:
                stateTimer += Time.deltaTime;

                // Check if player is still on platform
                CheckIfPlayerIsOnPlatform();

                // If player left or delay passed, start warning
                if (!playerIsOnPlatform || stateTimer >= movementDelay)
                {
                    StartWarning();
                }
                break;

            case PlatformState.Warning:
                stateTimer += Time.deltaTime;

                // After warning duration, start moving
                if (stateTimer >= movementDelay)
                {
                    StartMoving();
                }
                break;

            case PlatformState.Moving:
                MovePlatformToTarget();
                break;

            case PlatformState.WaitingToReturn:
                stateTimer += Time.deltaTime;

                if (stateTimer >= returnDelay)
                {
                    StartReturning();
                }
                break;

            case PlatformState.Returning:
                ReturnPlatformToStart();
                break;
        }

        // Visual feedback update
        UpdateVisualFeedback();
    }

    void CheckForPlayer()
    {
        if (useTriggerDetection)
            return; // Trigger will handle this

        // Check if player is in detection radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);

        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag("Player") || (playerLayer.value & (1 << col.gameObject.layer)) != 0)
            {
                OnPlayerDetected(col.gameObject);
                break;
            }
        }
    }

    void CheckIfPlayerIsOnPlatform()
    {
        // IMPORTANT: This prevents the platform from moving while player is standing on it
        Vector3 checkCenter = transform.position;
        float checkHeight = platformCollider.bounds.size.y * 0.6f;

        // Create a box slightly above the platform to check for player
        Vector3 checkBoxCenter = checkCenter + Vector3.up * checkHeight;
        Vector3 checkBoxSize = new Vector3(
            platformCollider.bounds.size.x * 0.9f,
            0.1f,
            platformCollider.bounds.size.z * 0.9f
        );

        // Check for player presence
        Collider[] colliders = Physics.OverlapBox(checkBoxCenter, checkBoxSize / 2, transform.rotation, playerLayer);

        playerIsOnPlatform = false;
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player") || (playerLayer.value & (1 << col.gameObject.layer)) != 0)
            {
                // Additional check: ensure player is actually standing on platform
                CharacterController playerController = col.GetComponent<CharacterController>();
                if (playerController != null)
                {
                    // Get the bottom of the player's CharacterController
                    float playerBottom = col.transform.position.y - playerController.height / 2;
                    float platformTop = platformCollider.bounds.max.y;

                    if (playerBottom <= platformTop + 0.2f)
                    {
                        playerIsOnPlatform = true;
                        break;
                    }
                }
                else
                {
                    playerIsOnPlatform = true;
                    break;
                }
            }
        }
    }

    void OnPlayerDetected(GameObject player)
    {
        if (currentState != PlatformState.Idle) return;

        currentState = PlatformState.PlayerDetected;
        stateTimer = 0f;

        // Play detection sound
        if (detectionSound != null)
        {
            audioSource.PlayOneShot(detectionSound);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!useTriggerDetection) return;

        if (other.CompareTag("Player") || (playerLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            if (currentState == PlatformState.Idle)
            {
                OnPlayerDetected(other.gameObject);
            }
        }
    }

    void StartWarning()
    {
        currentState = PlatformState.Warning;
        stateTimer = 0f;

        // Play warning particles
        if (warningParticles != null)
        {
            warningParticles.Play();
        }
    }

    void StartMoving()
    {
        currentState = PlatformState.Moving;
        stateTimer = 0f;

        // Play movement sound
        if (movementSound != null)
        {
            audioSource.PlayOneShot(movementSound);
        }

        // Play movement particles
        if (moveParticles != null)
        {
            moveParticles.Play();
        }
    }

    void MovePlatformToTarget()
    {
        // Move platform smoothly toward target
        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

        // Check if reached target
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;

            if (destroyAfterUse)
            {
                DestroyPlatform();
            }
            else if (returnAfterMove)
            {
                currentState = PlatformState.WaitingToReturn;
                stateTimer = 0f;
            }
            else
            {
                currentState = PlatformState.Destroyed;
            }

            // Stop movement particles
            if (moveParticles != null)
            {
                moveParticles.Stop();
            }
        }
    }

    void StartReturning()
    {
        currentState = PlatformState.Returning;

        // Play return sound
        if (returnSound != null)
        {
            audioSource.PlayOneShot(returnSound);
        }
    }

    void ReturnPlatformToStart()
    {
        // Move platform back to start position
        float step = returnSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, startPosition, step);

        // Check if returned to start
        if (Vector3.Distance(transform.position, startPosition) < 0.01f)
        {
            transform.position = startPosition;
            ResetPlatform();
        }
    }

    void ResetPlatform()
    {
        currentState = PlatformState.Idle;
        stateTimer = 0f;

        // Reset material
        if (platformRenderer != null && normalMaterial != null)
            platformRenderer.material = normalMaterial;

        // Stop all particles
        if (warningParticles != null)
            warningParticles.Stop();
        if (moveParticles != null)
            moveParticles.Stop();
    }

    void DestroyPlatform()
    {
        currentState = PlatformState.Destroyed;

        // Disable collider first
        if (platformCollider != null)
            platformCollider.enabled = false;

        // Make platform invisible
        if (platformRenderer != null)
            platformRenderer.enabled = false;

        // Schedule destruction
        Destroy(gameObject, 2f);
    }

    void UpdateVisualFeedback()
    {
        // Update materials based on state
        if (platformRenderer == null) return;

        switch (currentState)
        {
            case PlatformState.Idle:
                if (normalMaterial != null)
                    platformRenderer.material = normalMaterial;
                break;

            case PlatformState.Warning:
                if (warningMaterial != null)
                    platformRenderer.material = warningMaterial;
                break;

            case PlatformState.Moving:
            case PlatformState.Returning:
                if (movingMaterial != null)
                    platformRenderer.material = movingMaterial;
                break;
        }
    }

    // This is CRITICAL for preventing player from falling through
    void OnCollisionStay(Collision collision)
    {
        // When player is standing on platform, ensure they move with it
        if (collision.gameObject.CompareTag("Player"))
        {
            CharacterController playerController = collision.gameObject.GetComponent<CharacterController>();
            if (playerController != null)
            {
                // Calculate platform movement this frame
                Vector3 platformMovement = transform.position - startPosition;

                // Move player with platform (CharacterController doesn't use rigidbody physics)
                playerController.Move(platformMovement);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw movement direction and distance
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(startPosition, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.5f);
        }
        else
        {
            Gizmos.color = Color.cyan;
            Vector3 calculatedTarget = transform.position + (moveDirection.normalized * moveDistance);
            Gizmos.DrawLine(transform.position, calculatedTarget);
            Gizmos.DrawWireSphere(calculatedTarget, 0.5f);
        }

        // Draw player check area
        if (platformCollider != null)
        {
            Gizmos.color = Color.green;
            Vector3 checkBoxCenter = transform.position + Vector3.up * (platformCollider.bounds.size.y * 0.6f);
            Vector3 checkBoxSize = new Vector3(
                platformCollider.bounds.size.x * 0.9f,
                0.1f,
                platformCollider.bounds.size.z * 0.9f
            );
            Gizmos.DrawWireCube(checkBoxCenter, checkBoxSize);
        }
    }
}