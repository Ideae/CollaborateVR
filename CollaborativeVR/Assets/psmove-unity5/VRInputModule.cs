using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Text;

public class VRInputModule : BaseInputModule 
{
    const float RAYCAST_OFFSET = 1.0f;

    // UI is on collision layer #6 (bit index 5)
    private static int UI_COLLISION_LAYER_MASK = 1 << 5;

    // multiplier controls how fast slider/scrollbar moves with respect to input axis value
    public float ScrollSpeedMultiplier = 0.01f;

    // internal vars
    private PointerEventData m_hoverData;
    private GameObject m_currentHoverObject;
    private GameObject m_currentPressedObject;
    private GameObject m_currentDraggingObject;
    private bool m_guiRaycastHit;

    // use screen midpoint as locked pointer location, enabling look location to be the "mouse"
    private PointerEventData GetLookPointerEventData() 
    {
        VRCursorController cursor= VRCursorController.GetInstance();
      if (cursor == null) return null;
        Vector3 lookPosition = cursor.GetCursorRaycastPosition(RAYCAST_OFFSET);
        RaycastHit testRayResult = new RaycastHit();

        if (m_hoverData == null) 
        {
            m_hoverData = new PointerEventData(eventSystem);
        }

        m_hoverData.Reset();
        m_hoverData.delta = Vector2.zero;
        m_hoverData.position = lookPosition;
        m_hoverData.scrollDelta = Vector2.zero;

        Vector3 lookDirection = cursor.GetCursorRaycastDirection();
        Ray testRay = new Ray(lookPosition, lookDirection);

        RaycastResult raycastResult = new RaycastResult();
        raycastResult.Clear();

        if (Physics.Raycast(testRay, out testRayResult, RAYCAST_OFFSET+10.0f, UI_COLLISION_LAYER_MASK))
        {
            raycastResult.gameObject = testRayResult.collider.gameObject;
            raycastResult.depth = 0;
            raycastResult.distance = testRayResult.distance;
        }

        m_hoverData.pointerCurrentRaycast = raycastResult;

        if (m_hoverData.pointerCurrentRaycast.gameObject != null) 
        {
            m_guiRaycastHit = true;
        }
        else 
        {
            m_guiRaycastHit = false;
        }

        return m_hoverData;
    }
      
    // clear the current selection
    private void ClearSelection() 
    {
        if (eventSystem.currentSelectedGameObject != null) 
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }

    // select a game object
    private void Select(
        GameObject go) 
    {
        ClearSelection();

        if (ExecuteEvents.GetEventHandler<ISelectHandler>(go)) 
        {
            eventSystem.SetSelectedGameObject(go);
        }
    }

    // send update event to selected object
    // needed for InputField to receive keyboard input
    private bool SendUpdateEventToSelectedObject() 
    {
        if (eventSystem.currentSelectedGameObject == null)
            return false;

        BaseEventData data = GetBaseEventData ();
        ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);

        return data.used;
    }

    public override bool IsModuleSupported()
    {
        return true;
    }

    // Process is called by UI system to process events
    public override void Process() 
    {
        VRCursorController cursor= VRCursorController.GetInstance();
      if (cursor == null) return;
        // send update events if there is a selected object - this is important for InputField to receive keyboard events
        SendUpdateEventToSelectedObject();

        // see if there is a UI element that is currently being looked at
        PointerEventData lookData = GetLookPointerEventData();
        m_currentHoverObject = lookData.pointerCurrentRaycast.gameObject;

        // handle enter and exit events (highlight)
        // using the function that is already defined in BaseInputModule
        HandlePointerExitAndEnter(lookData, m_currentHoverObject);

        // Cursor Pressed Handling
        if (cursor.GetCursorPressed()) 
        {
            ClearSelection();

            lookData.pressPosition = lookData.position;
            lookData.pointerPressRaycast = lookData.pointerCurrentRaycast;
            lookData.pointerPress = null;

            if (m_currentHoverObject != null) 
            {
                GameObject newPressed = null;
                m_currentPressedObject = m_currentHoverObject;

                newPressed = ExecuteEvents.ExecuteHierarchy(m_currentPressedObject, lookData, ExecuteEvents.pointerDownHandler);

                if (newPressed == null) 
                {
                    // some UI elements might only have click handler and not pointer down handler
                    newPressed = ExecuteEvents.ExecuteHierarchy(m_currentPressedObject, lookData, ExecuteEvents.pointerClickHandler);

                    if (newPressed != null) 
                    {
                        m_currentPressedObject = newPressed;
                    }
                } 
                else 
                {
                    m_currentPressedObject = newPressed;

                    // we want to do click on button down at same time, unlike regular mouse processing
                    // which does click when mouse goes up over same object it went down on
                    // reason to do this is head tracking might be jittery and this makes it easier to click buttons
                    ExecuteEvents.Execute(newPressed, lookData, ExecuteEvents.pointerClickHandler);
                }

                if (newPressed != null) 
                {
                    lookData.pointerPress = newPressed;
                    m_currentPressedObject = newPressed;

                    Select(m_currentPressedObject);
                }

                if (ExecuteEvents.Execute(m_currentPressedObject, lookData, ExecuteEvents.beginDragHandler))
                {
                    lookData.pointerDrag = m_currentPressedObject;
                    m_currentDraggingObject = m_currentPressedObject;
                }
            }
        }

        // Cursor release handling
        if (cursor.GetCursorReleased())
        {
            if (m_currentDraggingObject) 
            {
                ExecuteEvents.Execute(m_currentDraggingObject,lookData,ExecuteEvents.endDragHandler);

                if (m_currentHoverObject != null) 
                {
                    ExecuteEvents.ExecuteHierarchy(m_currentHoverObject,lookData,ExecuteEvents.dropHandler);
                }

                lookData.pointerDrag = null;
                m_currentDraggingObject = null;
            }

            if (m_currentPressedObject) 
            {
                ExecuteEvents.Execute(m_currentPressedObject,lookData,ExecuteEvents.pointerUpHandler);

                lookData.rawPointerPress = null;
                lookData.pointerPress = null;
                m_currentPressedObject = null;
            }
        }

        // Drag handling
        if (m_currentDraggingObject != null) 
        {
            ExecuteEvents.Execute(m_currentDraggingObject,lookData,ExecuteEvents.dragHandler);
        }

        // Scroll Handling
        if (eventSystem.currentSelectedGameObject != null) 
        {
            float scrollAmount = cursor.GetCursorScroll();

            if (scrollAmount > 0.01f || scrollAmount < -0.01f) 
            {
                Slider slider = eventSystem.currentSelectedGameObject.GetComponent<Slider>();

                if (slider != null) 
                {
                    float multiplier = slider.maxValue - slider.minValue;

                    slider.value += scrollAmount*ScrollSpeedMultiplier*multiplier;
                } 
                else 
                {
                    Scrollbar scrollBar = eventSystem.currentSelectedGameObject.GetComponent<Scrollbar>();

                    if (scrollBar != null) 
                    {
                        scrollBar.value += scrollAmount*ScrollSpeedMultiplier;
                    }
                }
            }
        }

        // Tab Handling
        if (eventSystem.currentSelectedGameObject != null && Input.GetKeyUp(KeyCode.Tab))
        {
            AxisEventData axisData = new AxisEventData(this.eventSystem);

            axisData.moveDir = MoveDirection.Down;

            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, axisData, ExecuteEvents.moveHandler);
        }
    }

    public override string ToString()
    {
        VRCursorController cursor = VRCursorController.GetInstance();
        Vector3 position = cursor.GetCursorPosition();
        StringBuilder outputText = new StringBuilder();

        outputText.AppendFormat("Cursor Position: ({0}, {1}, {2})\n", position.x, position.y, position.z);
        outputText.AppendFormat("Cursor Pressed: {0}\n", cursor.GetCursorPressed() ? "yes" : "no");
        outputText.AppendFormat("Cursor Scroll Amount: {0}\n", cursor.GetCursorScroll());
        outputText.AppendFormat("Raycast Hit: {0}\n", m_guiRaycastHit ? "yes" : "no");        
        outputText.AppendFormat("Hover Object: {0}\n",
            m_currentHoverObject != null ? m_currentHoverObject.name : "(null)");
        outputText.AppendFormat("Pressed Object: {0}\n",
            m_currentPressedObject != null ? m_currentPressedObject.name : "(null)");
        outputText.AppendFormat("Dragging Object: {0}\n",
            m_currentDraggingObject != null ? m_currentDraggingObject.name : "(null)");

        return outputText.ToString();
    }
}
