using UnityEngine;
using System.Collections;

public class BoardBrushTool : ToolBase {
  private LineWhiteboard currentDrawingBoard = null;
  private static int currentLineID = 0;
  // Use this for initialization
  void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

	}

  public override void StartTool()
  {
    RaycastHit? hitInfo = GetRaycastHit();
    if (hitInfo != null && hitInfo.Value.collider.gameObject != null)
    {
      var board = hitInfo.Value.collider.gameObject.GetComponent<LineWhiteboard>();
      if (board != null)
      {
        currentDrawingBoard = board;
        currentLineID++;
        Vector3 point = GetBoardPoint(hitInfo.Value.point, board);
        currentDrawingBoard.DrawStrokeOnBoard(point, PSWand.ButtonState.ButtonDown, currentLineID);
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
        Vector3 point = GetBoardPoint(hitInfo.Value.point, board);
        currentDrawingBoard.DrawStrokeOnBoard(point, PSWand.ButtonState.ButtonHeld, currentLineID);
      }
    }
  }

  public override void FinishTool()
  {
    if (currentDrawingBoard != null)
    {
      currentDrawingBoard.DrawStrokeOnBoard(Vector3.zero, PSWand.ButtonState.ButtonUp, currentLineID);
    }
    currentDrawingBoard = null;
  }
}
