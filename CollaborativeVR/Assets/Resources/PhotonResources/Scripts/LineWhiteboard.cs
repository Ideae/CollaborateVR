using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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

public class LineWhiteboard : PunBehaviour {
  public GameObject lineRendererPrefab;
  public Color drawColor = Color.black;
  public int brushSize = 1;
  public Dictionary<int, LineData> lineDatas = new Dictionary<int, LineData>();

  private byte drawColorByte = 1;

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
        photonView.RPC("StartLineOnBoardCallback", PhotonTargets.All, new object[] {x, y, currentLineID, drawColorByte });
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
  public void StartLineOnBoardCallback(float x, float y, int currentLineID, byte colorByte)
  {
    print("StartLineOnBoardCallback");
    Vector3 point = transform.position + transform.right*x + transform.up*y - transform.forward * outwardsDist;
    GameObject g = (GameObject)Instantiate(lineRendererPrefab, point, Quaternion.identity);
    LineData lineData = new LineData(currentLineID, colorByte, g.GetComponent<LineRenderer>());
    lineDatas[currentLineID] = lineData;
    lineData.lineRenderer.useWorldSpace = true;
    lineData.lineRenderer.SetVertexCount(1);
    lineData.lineRenderer.SetPosition(0, point);//should its transform position be at the origin for this to work?
    Color col = colorArray[colorByte];
    lineData.lineRenderer.SetColors(col, col);

  }
  [PunRPC]
  public void ContinueLineOnBoardCallback(float x, float y, int currentLineID)
  {
    //print("ContinueLineOnBoardCallback");
    if (lineDatas.ContainsKey(currentLineID))
    {
      Vector3 point = transform.position + transform.right * x + transform.up * y - transform.forward * outwardsDist;
      LineData lineData = lineDatas[currentLineID];
      lineData.vertexCount++;
      lineData.lineRenderer.SetVertexCount(lineData.vertexCount);
      lineData.lineRenderer.SetPosition(lineData.vertexCount - 1, point);
    }

  }
  [PunRPC]
  public void FinishLineOnBoardCallback(int currentLineID)
  {
    print("FinishLineOnBoardCallback");

  }
}
