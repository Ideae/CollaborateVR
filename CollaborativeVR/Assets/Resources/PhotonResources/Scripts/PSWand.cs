using System;
using System.Collections.Generic;
using UnityEngine;

public class PSWand : MonoBehaviour
{

  public Color hoverColor = Color.green;
  public float maxDistance = 100f, forceFactor = 100f, torqueFactor = 10f;
  public Color selectionColor = Color.blue;
  public float triggerThreshold = 0.3f;

  private PSMoveController controller;
  private float holdDistance;
  private GameObject hoveredObject, heldObject;
  private Vector3 oldObjectPosition, effectiveVelocity;
  private float oldTriggerValue = 0f;
  private bool moveButtonHeld = false;
  private PhotonWhiteboard currentDrawingBoard = null;

  private Dictionary<Type, ToolBase> toolDict = new Dictionary<Type, ToolBase>();  
  private ToolBase currentTool;


  public enum ButtonState
  {
    ButtonHeld,
    ButtonDown,
    ButtonUp,
  }

  private void Start()
  {
    
    controller = GetComponent<PSMoveController>();
    controller.OnButtonPSPressed += Controller_OnButtonPSPressed;
    controller.OnButtonMovePressed += MoveButtonPressed;
    controller.OnButtonMoveReleased += MoveButtonReleased;
    controller.OnButtonSquarePressed += ClearAll;
    //controller.OnButtonCrossPressed += ;
    controller.OnButtonTrianglePressed += UndoAction;
    controller.OnButtonCirclePressed += RedoAction;

    currentTool = AddTool<BoardDrawerTool>();
    AddTool<BoardLineTool>();

  }

  private ToolBase AddTool<T>() where T : ToolBase
  {
    Type t = typeof (T);
    if (!toolDict.ContainsKey(t))
    {
      toolDict[t] = gameObject.AddComponent<T>();
    }
    return toolDict[t];

  }

  private void UndoAction(object sender, System.EventArgs e)
  {
    RaycastHit? hit = GetRaycastHit();
    if (hit != null)
    {
      var lineWhiteboard = hit.Value.collider.gameObject.GetComponent<LineWhiteboard>();
      if (lineWhiteboard != null)
      {
        lineWhiteboard.photonView.RPC("UndoRPC", PhotonTargets.All, new object[] {});
      }
    }
  }

  private void RedoAction(object sender, System.EventArgs e)
  {
    RaycastHit? hit = GetRaycastHit();
    if (hit != null)
    {
      var lineWhiteboard = hit.Value.collider.gameObject.GetComponent<LineWhiteboard>();
      if (lineWhiteboard != null)
      {
        lineWhiteboard.photonView.RPC("RedoRPC", PhotonTargets.All, new object[] { });
      }
    }
  }
  private void ClearAll(object sender, System.EventArgs e)
  {
    RaycastHit? hit = GetRaycastHit();
    if (hit != null)
    {
      var lineWhiteboard = hit.Value.collider.gameObject.GetComponent<LineWhiteboard>();
      if (lineWhiteboard != null)
      {
        lineWhiteboard.photonView.RPC("ClearAllRPC", PhotonTargets.All, new object[] { });
      }
    }
  }

  private void Controller_OnButtonPSPressed(object sender, System.EventArgs e)
  {
    controller.ResetYaw();
  }

  private void MoveButtonPressed(object sender, EventArgs e)
  {
    print("MoveButtonPressed");
    //if (moveButtonHeld) return;
    moveButtonHeld = true;
    currentTool.StartTool();

    //teleport
    //RaycastHit? hitInfo = GetRaycastHit();
    //if (hitInfo != null)
    //{
    //  if (hitInfo.Value.collider.gameObject.name == "Floor")
    //  {
    //    transform.parent.position = new Vector3(hitInfo.Value.point.x, transform.parent.position.y,
    //      hitInfo.Value.point.z);
    //  }
    //}
  }

  private void MoveButtonReleased(object sender, EventArgs e)
  {
    moveButtonHeld = false;
    currentTool.FinishTool();
    //RaycastHit? hitInfo = GetRaycastHit();
    //DrawTool(hitInfo, ButtonState.ButtonUp);
  }

  private void GrabObjectEventHandler()
  {
    var r = GetRaycastRigidbody();
    if (r != null)
    {
      heldObject = r.gameObject;
      hoveredObject = null;
      SetColor(heldObject, selectionColor);
      r.isKinematic = true;
      holdDistance = Vector3.Distance(transform.position, r.gameObject.transform.position);
    }
  }

  private void ReleaseObjectEventHandler()
  {
    if (heldObject != null)
    {
      var r = heldObject.GetComponent<Rigidbody>();
      r.velocity = Vector3.zero;
      r.isKinematic = false;
      r.velocity = effectiveVelocity;
      SetColor(heldObject, Color.white);
      heldObject = null;
    }
  }
  
  private void Update()
  {
    bool seen = controller.IsTracking;

    if (Input.GetKeyDown(KeyCode.Alpha1))
    {
      currentTool = toolDict[typeof (BoardDrawerTool)];
    }
    else if (Input.GetKeyDown(KeyCode.Alpha2))
    {
      currentTool = toolDict[typeof(BoardLineTool)];
    }


    //controller.SetRumble(seen ? 0f : 0.5f);

      //if (controller.TriggerValue > triggerThreshold && oldTriggerValue <= triggerThreshold)
      //{
      //  GrabObjectEventHandler();
      //}
      //else if (controller.TriggerValue < triggerThreshold && oldTriggerValue > triggerThreshold)
      //{
      //  ReleaseObjectEventHandler();
      //}
      //oldTriggerValue = controller.TriggerValue;
  }

  private void FixedUpdate()
  {
    RaycastHit? hitInfo = GetRaycastHit();
    if (moveButtonHeld)
    {
      //DrawTool(hitInfo, ButtonState.ButtonHeld);
      currentTool.ContinueTool();
    }
    //SelectTool(raycastObject);
    //TranslateTool();
  }

  private void DrawTool(RaycastHit? hitInfo, ButtonState btnState)
  {
    if (btnState == ButtonState.ButtonUp)
    {
      //finish line
      if (currentDrawingBoard != null)
      {
        currentDrawingBoard.OnMouseUp();
      }
      currentDrawingBoard = null;
    }
    else if (hitInfo != null && hitInfo.Value.collider.gameObject != null)
    {
      var board = hitInfo.Value.collider.gameObject.GetComponent<PhotonWhiteboard>();
      if (board != null)
      {
        if (btnState == ButtonState.ButtonHeld && currentDrawingBoard == board)//controller.GetPSButton(PSMoveButton.Move))
        {
          //continue line
          currentDrawingBoard.DrawOnBoard(hitInfo.Value.point);
        }
        else if (btnState == ButtonState.ButtonDown)
        {
          //start new line
          currentDrawingBoard = board;
        }
      }
    }
  }

  private void SelectTool(GameObject raycastObject)
  {
    if (heldObject != null) return;
    //var r = GetRaycastRigidbody();
    var r = (raycastObject == null) ? null : raycastObject.GetComponent<Rigidbody>();
    if (r != null)
    {
      if (hoveredObject != null && r.gameObject != hoveredObject)
      {
        SetColor(hoveredObject, Color.white);
      }
      hoveredObject = r.gameObject;
      SetColor(hoveredObject, hoverColor);
    }
    else if (hoveredObject != null)
    {
      SetColor(hoveredObject, Color.white);
      hoveredObject = null;
    }
  }

  private void TranslateTool()
  {
    if (heldObject != null)
    {
      var newObjectPosition = heldObject.transform.position;
      if (newObjectPosition != oldObjectPosition)
      {
        effectiveVelocity = (newObjectPosition - oldObjectPosition) / Time.deltaTime;
        oldObjectPosition = newObjectPosition;
      }
      var selectedRigidbody = heldObject.GetComponent<Rigidbody>();
      var target = transform.position + transform.forward * holdDistance;
      selectedRigidbody.MovePosition(target);
    }
  }

  public RaycastHit? GetRaycastHit()
  {
    RaycastHit hitInfo;
    if (Physics.Raycast(transform.position, transform.forward, out hitInfo, maxDistance))
    {
      return hitInfo;
    }
    return null;
  }

  private Rigidbody GetRaycastRigidbody()
  {
    var ray = new Ray(transform.position, transform.forward);
    RaycastHit hitInfo;
    if (Physics.Raycast(ray, out hitInfo, maxDistance))
    {
      var r = hitInfo.collider.gameObject.GetComponent<Rigidbody>();
      return r;
    }
    return null;
  }

  private void SetColor(GameObject g, Color c)
  {
    var renderer = g.GetComponent<Renderer>();
    if (renderer != null)
    {
      renderer.material.color = c;
    }
  }
}