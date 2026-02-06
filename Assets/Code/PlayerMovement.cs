using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 6f;
    public float gravity = -9.81f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 100f;
    public float maxPitch = 90f; // Keep pitch limited to avoid neck breaking

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.25f;
    public LayerMask groundMask;

    private CharacterController controller;
    private Camera playerCamera;

    private float currentYaw; // Now full 360 degrees
    private float currentPitch;
    private float yVelocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        CheckGround();
        HandleRotation();
        HandleMovementAndJump();
    }

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask
        );

        if (isGrounded && yVelocity < 0f)
        {
            yVelocity = -2f; // keeps player grounded
        }
    }

    void HandleMovementAndJump()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Get camera's forward and right vectors (ignore vertical component for movement)
        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 cameraRight = playerCamera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Move relative to camera direction
        Vector3 move = cameraRight * x + cameraForward * z;
        move = Vector3.ClampMagnitude(move, 1f); // Prevent diagonal speed boost

        // Jump
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            yVelocity = jumpForce;
        }

        // Gravity
        yVelocity += gravity * Time.deltaTime;

        // Combine horizontal + vertical movement
        Vector3 velocity = move * moveSpeed;
        velocity.y = yVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Yaw - FULL 360 DEGREES (no clamping)
        currentYaw += mouseX;

        // Normalize yaw to keep it within reasonable range (optional, prevents overflow)
        if (currentYaw > 360f) currentYaw -= 360f;
        if (currentYaw < -360f) currentYaw += 360f;

        // Apply yaw to character controller (horizontal rotation)
        transform.localRotation = Quaternion.Euler(0f, currentYaw, 0f);

        // Pitch - still limited for natural head movement
        currentPitch -= mouseY;
        currentPitch = Mathf.Clamp(currentPitch, -maxPitch, maxPitch);
        playerCamera.transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
    }
}