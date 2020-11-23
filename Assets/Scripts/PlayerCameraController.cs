using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMouseController : MonoBehaviour
{
    public Transform m_PlayerBody;
    public float m_MouseSensitivity = 100.0f;

    float m_RotationX = 0.0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float m_X = 0.0f;
        float m_Y = 0.0f;

        if ( Mouse.current != null )
        {
            Vector2 m_Delta = Mouse.current.delta.ReadValue() / 15.0f;

            m_X += m_Delta.x;
            m_Y += m_Delta.y;
        }

        if ( Gamepad.current != null )
        {
            Vector2 m_Value = Gamepad.current.rightStick.ReadValue();

            m_X += m_Value.x;
            m_Y += m_Value.y;
        }

        m_X *= m_MouseSensitivity * Time.deltaTime;
        m_Y *= m_MouseSensitivity * Time.deltaTime;

        m_RotationX -= m_Y;
        m_RotationX = Mathf.Clamp(m_RotationX, -90.0f, 90.0f);

        transform.localRotation = Quaternion.Euler(m_RotationX, 0.0f, 0.0f);
        m_PlayerBody.Rotate(Vector3.up * m_X);
    }
}
