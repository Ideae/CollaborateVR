using UnityEngine;
using System.Collections;

public class SpaceBrushTool : ToolBase
{
  private LineWhiteboard currentDrawingBoard = null;
  private int currentLineID = 0;
  // Use this for initialization
  void Start()
  {
    currentDrawingBoard = GameObject.Find("SpaceContainer").GetComponent<LineWhiteboard>();
  }

  // Update is called once per frame
  void Update()
  {

  }

  public override void StartTool()
  {
    currentLineID++;
    currentDrawingBoard.DrawStrokeOnBoard(psWand.transform.position, PSWand.ButtonState.ButtonDown, currentLineID);
  }

  public override void ContinueTool()
  {
    currentDrawingBoard.DrawStrokeOnBoard(psWand.transform.position, PSWand.ButtonState.ButtonHeld, currentLineID);
  }

  public override void FinishTool()
  {
    currentDrawingBoard.DrawStrokeOnBoard(Vector3.zero, PSWand.ButtonState.ButtonUp, currentLineID);
  }
}
