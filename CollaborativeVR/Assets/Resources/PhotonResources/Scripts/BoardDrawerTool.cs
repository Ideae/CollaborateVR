using UnityEngine;
using System.Collections;

public class BoardDrawerTool : ToolBase {
  private LineWhiteboard currentDrawingBoard = null;
  private int currentLineID = 0;
  // Use this for initialization
  void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

	}

  public override void StartTool()
  {
    print("StartTool");
    RaycastHit? hitInfo = GetRaycastHit();
    if (hitInfo != null && hitInfo.Value.collider.gameObject != null)
    {
      var board = hitInfo.Value.collider.gameObject.GetComponent<LineWhiteboard>();
      if (board != null)
      {
        currentDrawingBoard = board;
        currentLineID++; //todo: request this lineID from the masterClient (who will increment it) so no two clients try to use the same one
        currentDrawingBoard.DrawLineOnBoard(hitInfo.Value.point, PSWand.ButtonState.ButtonDown, currentLineID);
        
      }
    }
  }

  public override void ContinueTool()
  {
    RaycastHit? hitInfo = GetRaycastHit();
    if (hitInfo != null && hitInfo.Value.collider.gameObject != null)
    {
      var board = hitInfo.Value.collider.gameObject.GetComponent<LineWhiteboard>();
      if (currentDrawingBoard != null && currentDrawingBoard == board)
      {
        currentDrawingBoard.DrawLineOnBoard(hitInfo.Value.point, PSWand.ButtonState.ButtonHeld, currentLineID);
      }
    }
  }

  public override void FinishTool()
  {
    if (currentDrawingBoard != null)
    {
      currentDrawingBoard.DrawLineOnBoard(Vector3.zero, PSWand.ButtonState.ButtonUp, currentLineID);
    }
    currentDrawingBoard = null;
  }
}
