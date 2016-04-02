using System;
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
  private bool moveButtonDown = false;

  private void Start()
  {
    controller = GetComponent<PSMoveController>();
    controller.OnButtonPSPressed += Controller_OnButtonPSPressed;
    controller.OnButtonMovePressed += TeleportEventHandler;
    controller.OnButtonMoveReleased += MoveButtonReleased;
    //controller.OnButtonCrossPressed += ;
    //controller.OnButtonCirclePressed += ;
    //controller.OnButtonSquarePressed += ;
    //controller.OnButtonTrianglePressed += ;
  }
  
  private void Controller_OnButtonPSPressed(object sender, System.EventArgs e)
  {
    controller.ResetYaw();
  }

  private void TeleportEventHandler(object sender, EventArgs e)
  {
    moveButtonDown = true;
    var ray = new Ray(transform.position, transform.forward);
    RaycastHit hitInfo;
    if (Physics.Raycast(ray, out hitInfo, maxDistance))
    {
      if (hitInfo.collider.gameObject.name == "Floor")
      {
        transform.parent.position = new Vector3(hitInfo.point.x, transform.parent.position.y, hitInfo.point.z);
      }
    }
  }

  private void MoveButtonReleased(object sender, EventArgs e)
  {
    moveButtonDown = false;
  }

  private void GrabObjectEventHandler()
  {
    var r = GetRigidbodyFromRaycast();
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
    var ray = new Ray(transform.position, transform.forward);
    RaycastHit hitInfo;
    GameObject raycastObject = null;
    if (Physics.Raycast(ray, out hitInfo, maxDistance))
    {
      raycastObject = hitInfo.collider.gameObject;
      DrawTool(raycastObject, hitInfo.point);
    }
    
    //SelectTool(raycastObject);
    //TranslateTool();
  }

  private void DrawTool(GameObject raycastObject, Vector3 hitpoint)
  {
    
    if (raycastObject != null)
    {
      var board = raycastObject.GetComponent<PhotonWhiteboard>();
      if (board != null)
      {
        if (moveButtonDown)//controller.GetPSButton(PSMoveButton.Move))
        {
          board.DrawOnBoard(hitpoint);
        }
        else
        {
          board.OnMouseUp();
        }
      }
    }
  }

  private void SelectTool(GameObject raycastObject)
  {
    if (heldObject != null) return;
    //var r = GetRigidbodyFromRaycast();
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


  private Rigidbody GetRigidbodyFromRaycast()
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