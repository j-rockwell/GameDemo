using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerMovementState
{
    Walk,
    Sprint,
    Crouch
}

public class PlayerController : MonoBehaviour
{
    [Header("Player Prefab Elements")]
    public CharacterController m_CharacterController;
    public Camera m_Camera;

    [Header("Ground Check for Physics")]
    public Transform m_GroundCheck;
    public float m_GroundDist = 0.4f;

    [Tooltip("When creating a level, any floor that you want to be considered for gravity must have the provided Layer Mask applied to it")]
    public LayerMask m_GroundMask;

    [Header("Movement Settings")]
    [Range(0.1f, 100.0f)]
    public float m_WalkSpeed = 4.0f;

    [Range(0.1f, 100.0f)]
    public float m_SprintSpeed = 8.0f;

    [Range(0.1f, 0.9f)]
    public float m_CrouchModifier = 0.5f;

    [Range(0.1f, 30.0f)]
    public float m_JumpSpeed = 3.5f;

    [Header("Camera Settings")]
    [Range(0.01f, 3.0f)]
    public float m_CameraPanOutSpeed = 0.08f;

    [Range(0.01f, 3.0f)]
    public float m_CameraPanInSpeed = 0.02f;

    [Range(0.1f, 30.0f)]
    public float m_WalkingBobSpeed = 12.0f;

    [Range(0.1f, 30.0f)]
    public float m_SprintingBobSpeed = 25.0f;

    [Range(0.01f, 3.0f)]
    public float m_BobbingAmount = 0.03f;

    private const float c_Gravity = -9.81f;

    PlayerInput m_PlayerInput;

    Vector3 m_PlayerVelocity;
    PlayerMovementState m_CurrentMovementState;
    bool m_Grounded;

    private float m_DefaultCameraFov;
    private float m_DefaultPosY;
    private float m_Timer;

    // Fire when the script is initially loaded and always runs first
    private void Awake()
    {
        m_CurrentMovementState = PlayerMovementState.Walk;
        m_PlayerInput = new PlayerInput();

        m_PlayerInput.Player.Jump.performed += ctx => OnJumpChanged(ctx);
        m_PlayerInput.Player.Sprint.performed += ctx => OnSprintChanged(ctx);
        m_PlayerInput.Player.Sprint.canceled += ctx => OnSprintChanged(ctx);
        m_PlayerInput.Player.Crouch.performed += ctx => OnCrouchChanged(ctx);
        m_PlayerInput.Player.Crouch.canceled += ctx => OnCrouchChanged(ctx);

        m_DefaultPosY = 0.0f;
        m_Timer = 0.0f;
    }

    // Fire when the script is enabled
    private void OnEnable()
    {
        m_PlayerInput.Enable();
    }

    // Fire when the script is disabled
    private void OnDisable()
    {
        m_PlayerInput.Disable();
    }

    // Fire when the script is initially started
    private void Start()
    {
        m_DefaultCameraFov = m_Camera.fieldOfView;
        m_DefaultPosY = m_Camera.transform.localPosition.y;
    }

    // Fire every frame
    private void Update()
    {
        m_Grounded = Physics.CheckSphere(m_GroundCheck.position, m_GroundDist, m_GroundMask);

        UpdateMovement(m_PlayerInput.Player.Move.ReadValue<Vector2>());
        UpdateFieldOfView();
        UpdateViewBobbing();

        if ( m_Grounded && m_PlayerVelocity.y < 0 )
           m_PlayerVelocity.y = -2.0f;

        if ( !m_Grounded && m_PlayerVelocity.y < 0.0f && m_CurrentMovementState == PlayerMovementState.Sprint )
            m_CurrentMovementState = PlayerMovementState.Walk;

        m_PlayerVelocity.y += c_Gravity * Time.deltaTime;

        m_CharacterController.Move(m_PlayerVelocity * Time.deltaTime);
    }

    // Handles movement updates from the input system
    private void UpdateMovement(Vector2 p_Data)
    {
        Vector3 m_Movement = (transform.right * p_Data.x + transform.forward * p_Data.y) * GetMovementSpeed();
        m_PlayerVelocity.x = m_Movement.x;
        m_PlayerVelocity.z = m_Movement.z;

        // Player is not holding W, if they are sprinting we need to set them to walking
        if ( p_Data.y != 1 && m_CurrentMovementState == PlayerMovementState.Sprint )
            m_CurrentMovementState = PlayerMovementState.Walk;
    }
    
    // Handles updating the camera field of view for the player depending on their movement state
    private void UpdateFieldOfView()
    {
        if ( m_CurrentMovementState == PlayerMovementState.Sprint )
            m_Camera.fieldOfView = Mathf.Lerp(m_Camera.fieldOfView, (m_DefaultCameraFov + 10.0f), m_CameraPanOutSpeed);
        else if ( m_CurrentMovementState == PlayerMovementState.Walk )
            m_Camera.fieldOfView = Mathf.Lerp(m_Camera.fieldOfView, m_DefaultCameraFov, m_CameraPanInSpeed);
        else if ( m_CurrentMovementState == PlayerMovementState.Crouch )
            m_Camera.fieldOfView = Mathf.Lerp(m_Camera.fieldOfView, (m_DefaultCameraFov - 5.0f), m_CameraPanOutSpeed);
    }

    // Handles updating view bobbing for the player depending on their movement state
    private void UpdateViewBobbing()
    {
        if (Mathf.Abs(m_PlayerVelocity.x) > 0.1f || Mathf.Abs(m_PlayerVelocity.z) > 0.1f)
        {
            m_Timer += Time.deltaTime * GetViewBobbingSpeed();
            m_Camera.transform.localPosition = new Vector3(m_Camera.transform.localPosition.x, m_DefaultPosY + Mathf.Sin(m_Timer) * m_BobbingAmount, m_Camera.transform.localPosition.z);
        }

        else
        {
            m_Timer = 0.0f;
            m_Camera.transform.localPosition = new Vector3(m_Camera.transform.localPosition.x, Mathf.Lerp(m_Camera.transform.localPosition.y, m_DefaultPosY, Time.deltaTime * m_WalkingBobSpeed), m_Camera.transform.localPosition.z);
        }
    }

    // Handles jump requests from the input system
    private void OnJumpChanged(InputAction.CallbackContext p_Ctx)
    {
        if ( !m_Grounded )
            return;

        m_PlayerVelocity.y = m_JumpSpeed;
    }

    // Handles sprint requests from the input system
    private void OnSprintChanged(InputAction.CallbackContext p_Ctx)
    {
        if ( !m_Grounded )
            return;

        float m_Value = p_Ctx.ReadValue<float>();
        bool m_RequestingSprint = (m_Value == 1);

        if ( m_RequestingSprint && m_CurrentMovementState != PlayerMovementState.Sprint )
            m_CurrentMovementState = PlayerMovementState.Sprint;
        else if ( !m_RequestingSprint && m_CurrentMovementState == PlayerMovementState.Sprint )
            m_CurrentMovementState = PlayerMovementState.Walk;
    }

    // Handles crouch requests from the input system
    private void OnCrouchChanged(InputAction.CallbackContext p_Ctx)
    {
        if ( !m_Grounded )
            return;

        float m_Value = p_Ctx.ReadValue<float>();
        bool m_RequestingCrouch = (m_Value == 1);

        if ( m_RequestingCrouch && m_CurrentMovementState != PlayerMovementState.Crouch )
        {
            m_CurrentMovementState = PlayerMovementState.Crouch;
        }

        else if ( m_CurrentMovementState != PlayerMovementState.Walk )
        {
            m_CurrentMovementState = PlayerMovementState.Walk;
        }
    }

    // Returns the movement speed for the player based on their current movement state
    private float GetMovementSpeed()
    {
        switch ( m_CurrentMovementState )
        {
            case PlayerMovementState.Walk:
                return m_WalkSpeed;
            case PlayerMovementState.Crouch:
                return (m_WalkSpeed * m_CrouchModifier);
            case PlayerMovementState.Sprint:
                return m_SprintSpeed;
            default:
                return 0.0f;
        }
    }

    // Returns the view bobbing speed for the player based on their current movement state
    private float GetViewBobbingSpeed()
    {
        switch ( m_CurrentMovementState )
        {
            case PlayerMovementState.Sprint:
                return m_SprintingBobSpeed;
            default:
                return m_WalkingBobSpeed;
        }
    }
}