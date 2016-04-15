using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon;

public class LineData
{
  public int lineID, vertexCount;
  public byte colorByte;
  public LineRenderer lineRenderer;


  public LineData(int lineID, byte colorByte, LineRenderer lineRenderer)
  {
    this.lineID = lineID;
    this.colorByte = colorByte;
    this.lineRenderer = lineRenderer;
    this.vertexCount = 1;
  }
}

public class UserActionSystem
{
  public Dictionary<int, UserActionList> actionListsDict = new Dictionary<int, UserActionList>();

  public UserActionSystem()
  {
    
  }

  private UserActionList GetActionList(int playerId)
  {
    if (!actionListsDict.ContainsKey(playerId))
    {
      actionListsDict[playerId] = new UserActionList(playerId);
    }
    return actionListsDict[playerId];
  }

  public void AddUpdatingAction(int playerId, int actionNum, UserAction action)
  {
    var actionList = GetActionList(playerId);
    actionList.AddUpdatingAction(actionNum, action);
  }

  public UserAction GetUpdatingAction(int playerId, int actionNum)
  {
    var actionList = GetActionList(playerId);
    return actionList.GetUpdatingAction(actionNum);
  }

  public void CompleteAction(int playerId, int actionNum)
  {
    var actionList = GetActionList(playerId);
    actionList.CompleteAction(actionNum);
  }

  public void UndoTopAction(int playerId)
  {
    var actionList = GetActionList(playerId);
    actionList.UndoTopAction();
  }
  public void RedoNextAction(int playerId)
  {
    var actionList = GetActionList(playerId);
    actionList.RedoNextAction();
  }
}

public class UserActionList
{
  public int playerId;
  private Dictionary<int, UserAction> updatingActions = new Dictionary<int, UserAction>();
  private List<UserAction> actionList = new List<UserAction>();
  private UserAction topAction;

  public UserActionList(int playerId)
  {
    this.playerId = playerId;
  }

  public UserAction GetUpdatingAction(int actionNum)
  {
    if (updatingActions.ContainsKey(actionNum))
    {
      return updatingActions[actionNum];
    }
    return null;
  }

  public void AddUpdatingAction(int actionNum, UserAction action)
  {
    updatingActions.Add(actionNum, action);
  }

  public void CompleteAction(int actionNum)
  {
    if (updatingActions.ContainsKey(actionNum))
    {
      UserAction action = updatingActions[actionNum];
      if (topAction != null)
      {
        int index = actionList.IndexOf(topAction);
        //remove actions above currentAction if they exist, so you can no longer redo them
        if (index != -1 && index < actionList.Count - 1)
        {
          actionList.RemoveRange(index + 1, actionList.Count - index - 1);
        }
      }
      actionList.Add(action);
      topAction = action;
      updatingActions.Remove(actionNum);

    }
  }

  public void UndoTopAction()
  {
    if (topAction != null)
    {
      int index = actionList.IndexOf(topAction);
      topAction.UndoAction();
      if (index > 0)
      {
        index -= 1;
        topAction = actionList.ElementAt(index);
      }
    }
  }

  public void RedoNextAction()
  {
    if (topAction != null)
    {
      int index = actionList.IndexOf(topAction);
      if (index != -1 && index != actionList.Count - 1)
      {
        topAction = actionList.ElementAt(index + 1);
        topAction.PerformAction(); //redo action function instead?
      }
    }
  }
}

public abstract class UserAction
{
  public abstract void PerformAction();
  public abstract void UndoAction();
}

public class DrawStrokeAction : UserAction
{
  public LineData lineData;
  public override void PerformAction()
  {
    lineData.lineRenderer.gameObject.SetActive(true);
  }
  public override void UndoAction()
  {
    lineData.lineRenderer.gameObject.SetActive(false);
  }
}

public class LineWhiteboard : PunBehaviour {
  public GameObject lineRendererPrefab;
  public Color drawColor = Color.black;

  public UserActionSystem actionSystem = new UserActionSystem();

  public static byte brushSize = 1;
  public static byte drawColorIndex = 1;

  //todo: duplicate in photonwhiteboard: switch to 8bit color (but provide some presets)?
  public static Color[] colorArray = new Color[]
  {
    Color.white,
    Color.black,
    Color.blue,
    Color.green,
    Color.yellow,
    new Color(1.0f,0.6f,0.0f,1.0f),
    Color.red,
    new Color(0.5f,0f,0.5f,1.0f),
  };
  // Use this for initialization
  void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}


  [PunRPC]
  public void UndoRPC(PhotonMessageInfo info)
  {
    actionSystem.UndoTopAction(info.sender.ID);
  }
  [PunRPC]
  public void RedoRPC(PhotonMessageInfo info)
  {
    actionSystem.RedoNextAction(info.sender.ID);
  }

  private Vector3 oldHitPoint;
  //Line Implementation
  public void DrawLineOnBoard(Vector3 hitpoint, PSWand.ButtonState btnState, int currentLineID)
  {
    if (btnState == PSWand.ButtonState.ButtonUp)
    {
      oldHitPoint = -Vector3.one;
      photonView.RPC("FinishLineOnBoardCallback", PhotonTargets.All, new object[] { currentLineID });
    }
    else if (oldHitPoint != hitpoint) //todo: floating point error? check that distance is great enough instead?
    {
      var coll = transform.GetComponent<Collider>();
      //float x = (hitpoint.x - coll.bounds.min.x)/coll.bounds.size.x;
      //float y = (hitpoint.y - coll.bounds.min.y)/coll.bounds.size.y;
      float x = Vector3.Dot(transform.right, hitpoint - transform.position);
      float y = Vector3.Dot(transform.up, hitpoint - transform.position);
      if (btnState == PSWand.ButtonState.ButtonDown)
      {
        photonView.RPC("StartLineOnBoardCallback", PhotonTargets.All, new object[] {x, y, currentLineID, drawColorIndex, brushSize });
      }
      else if (btnState == PSWand.ButtonState.ButtonHeld)
      {
        photonView.RPC("ContinueLineOnBoardCallback", PhotonTargets.All, new object[] { x, y, currentLineID });
      }
      oldHitPoint = hitpoint;
    }
  }

  private float outwardsDist = 0.01f;
  [PunRPC]
  public void StartLineOnBoardCallback(float x, float y, int currentLineID, byte colorByte, byte widthByte, PhotonMessageInfo info)
  {
    print("StartLineOnBoardCallback");
    Vector3 point = transform.position + transform.right*x + transform.up*y - transform.forward * outwardsDist;
    GameObject g = (GameObject)Instantiate(lineRendererPrefab, point, Quaternion.identity);
    LineData lineData = new LineData(currentLineID, colorByte, g.GetComponent<LineRenderer>());
    
    //lineDatas[currentLineID] = lineData;
    lineData.lineRenderer.useWorldSpace = true;
    lineData.lineRenderer.SetVertexCount(1);
    lineData.lineRenderer.SetPosition(0, point);//should its transform position be at the origin for this to work?
    float width = widthByte / 200f;
    print("width: " + width);
    lineData.lineRenderer.SetWidth(width, width);
    Color col = colorArray[colorByte];
    lineData.lineRenderer.SetColors(col, col);

    DrawStrokeAction action = new DrawStrokeAction();
    action.lineData = lineData;
    actionSystem.AddUpdatingAction(info.sender.ID, currentLineID, action);

  }
  [PunRPC]
  public void ContinueLineOnBoardCallback(float x, float y, int currentLineID, PhotonMessageInfo info)
  {
    //print("ContinueLineOnBoardCallback");
    DrawStrokeAction action = (DrawStrokeAction) actionSystem.GetUpdatingAction(info.sender.ID, currentLineID);
    if (action != null)
    {
      Vector3 point = transform.position + transform.right * x + transform.up * y - transform.forward * outwardsDist;
      LineData lineData = action.lineData;
      lineData.vertexCount++;
      lineData.lineRenderer.SetVertexCount(lineData.vertexCount);
      lineData.lineRenderer.SetPosition(lineData.vertexCount - 1, point);
    }

  }
  [PunRPC]
  public void FinishLineOnBoardCallback(int currentLineID, PhotonMessageInfo info)
  {
    //print("FinishLineOnBoardCallback");
    actionSystem.CompleteAction(info.sender.ID, currentLineID);
  }

  //public void 
}
