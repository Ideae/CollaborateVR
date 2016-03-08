using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.InteropServices;
using UnityStandardAssets.Characters.FirstPerson;

[NetworkSettings(channel = 1)]
public class Player : NetworkBehaviour
{
  //public float walkSpeed = 3f;
  public float mouseRotateSpeed = 0.2f;
  public float positionUpdateRate = 0.2f;
  public float smooth = 4f;

  private Camera cam;
  private CharacterController characterCont;
  private FirstPersonController fpController;
  private Rigidbody myRigidbody;
  private Transform myTransform;

  private Vector3 playerPosition;
  private Point oldMousePoint;

  [DllImport("user32.dll")]
  public static extern bool SetCursorPos(int X, int Y);
  [DllImport("user32.dll")]
  public static extern bool GetCursorPos(out Point pos);

  

  private static Player localPlayer;
  private Dictionary<uint, Whiteboard> whiteboards;
  public override void OnStartLocalPlayer()
  {
    base.OnStartLocalPlayer();
    localPlayer = this;
    whiteboards = new Dictionary<uint, Whiteboard>();
    var boards = FindObjectsOfType<Whiteboard>();
    foreach (var board in boards)
    {
      board.localPlayer = this;
      whiteboards[board.netId.Value] = board;
    }
  }

  void Start ()
	{
    myTransform = transform;
    myRigidbody = GetComponent<Rigidbody>();
    characterCont = GetComponent<CharacterController>();
    fpController = GetComponent<FirstPersonController>();
    cam = transform.FindChild("Camera").GetComponent<Camera>();
    if (isLocalPlayer)
	  {
      characterCont.enabled = true;
	    fpController.enabled = true;
      cam.gameObject.SetActive(true);
	    StartCoroutine(UpdatePosition());
	  }
	}

  void LerpPosition()
  {
    myTransform.position = Vector3.Lerp(myTransform.position, playerPosition, Time.deltaTime*smooth);
    //myTransform.position = Vector3.MoveTowards(myTransform.position, playerPosition, Time.deltaTime * smooth);
  }

  IEnumerator UpdatePosition()
  {
    while (enabled)
    {
      CmdSendPosition(transform.position);
      yield return new WaitForSeconds(positionUpdateRate);
    }
  }

  [Command]
  void CmdSendPosition(Vector3 pos)
  {
    playerPosition = pos;
    RpcReceivePosition(pos);
  }

  [ClientRpc]
  void RpcReceivePosition(Vector3 pos)
  {
    playerPosition = pos;
  }

  
  
  void Update () {
    if (isLocalPlayer)
    {
      HandleMouseRotation();
      //HandlePlayerMovement();
    }
    else
    {
      LerpPosition();
    }
	}

  private void HandleMouseRotation()
  {
    //mouse repositioning only works on windows: make compiler flags
    if (Input.GetMouseButtonDown(1))
    {
      GetCursorPos(out oldMousePoint);
      Cursor.visible = false;
      Cursor.lockState = CursorLockMode.Locked;
    }
    else if (Input.GetMouseButtonUp(1))
    {
      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;
      SetCursorPos(oldMousePoint.X, oldMousePoint.Y);
    }
    else if (Input.GetMouseButton(1))
    {
      float xAxis = Input.GetAxis("Mouse X");
      transform.Rotate(0f, xAxis, 0f);
      float yAxis = Input.GetAxis("Mouse Y");
      cam.transform.Rotate(-yAxis, 0f, 0f);
    }
  }

  public void CallDrawBrush(int x, int y, int px, int py, uint boardId)
  {
    Debug.Log("CallDrawBrush");
    CmdDrawBrush(x, y, px, py, boardId);
  }
  [Command]
  void CmdDrawBrush(int x, int y, int px, int py, uint boardId)
  {
    Debug.Log("CmdDrawBrush");
    RpcDrawBrush(x, y, px, py, boardId);
  }

  [ClientRpc]
  void RpcDrawBrush(int x, int y, int px, int py, uint boardId)
  {
    Debug.Log("RpcDrawBrush");
    if (localPlayer != null)
    {
      localPlayer.whiteboards[boardId].DrawOnBoardCallback(x, y, px, py);
    }
  }

  //private void HandlePlayerMovement()
  //{
  //  float horiz = Input.GetAxis("Horizontal");
  //  float vert = Input.GetAxis("Vertical");
  //  Vector3 v = new Vector3(horiz, 0f, vert) * walkSpeed;
  //  v = transform.rotation*v*Time.deltaTime;
  //  //Quaternion yRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
  //  //characterCont.SimpleMove(v);
  //  //characterCont.Move(v);
  //  myRigidbody.MovePosition(transform.position + v);
  //}
}

public struct Point
{
  public int X;
  public int Y;

  public Point(int x, int y)
  {
    this.X = x;
    this.Y = y;
  }
}