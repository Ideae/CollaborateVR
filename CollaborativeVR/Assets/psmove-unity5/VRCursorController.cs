using UnityEngine;
using System.Collections;
using System;

public class VRCursorController : MonoBehaviour 
{
    private static VRCursorController m_instance;

    private PSMoveController m_moveComponent;
    private bool m_bWasPressed;
    private bool m_bIsPressed;

    public static VRCursorController GetInstance()
    {
        return m_instance;
    }

    public bool GetCursorPressed()
    {
        return m_bIsPressed && !m_bWasPressed;
    }

    public bool GetCursorReleased()
    {
        return !m_bIsPressed && m_bWasPressed;
    }

    public Vector3 GetCursorPosition()
    {
        return m_instance.transform.position;
    }

    public float GetCursorScroll()
    {
        float scrollAmount = 0;

        if (m_moveComponent != null)
        {
            if (m_moveComponent.IsTriangleButtonDown)
            {
                scrollAmount = 1.0f;
            }
            else if (m_moveComponent.IsCircleButtonDown)
            {
                scrollAmount = -1.0f;
            }
        }

        return scrollAmount;
    }

    public Vector3 GetCursorRaycastPosition(
        float raycastOffset)
    {
        return m_instance.transform.position - GetCursorRaycastDirection() * raycastOffset;
    }

    public Vector3 GetCursorRaycastDirection()
    {
        Vector3 raycastDirection = m_instance.transform.forward;

        return raycastDirection;
    }

    void Awake()
    {
        m_instance = this;
    }

    void Start() 
    {
        m_moveComponent = gameObject.GetComponent<PSMoveController>();
        m_bWasPressed = false;
        m_bIsPressed = false;
    }

    void OnDestroy()
    {
        m_instance = null;
    }
    
    void Update () 
    {
        if (m_moveComponent != null)
        {
            m_bWasPressed = m_bIsPressed;
          m_bIsPressed = m_moveComponent.IsMoveButtonDown; //m_moveComponent.TriggerValue > 0.1f;
        }
    }
}
