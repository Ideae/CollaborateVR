using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityStandardAssets.Characters.FirstPerson;
using Photon;

public class PlayerPhoton : PunBehaviour
{
  public float mouseRotateSpeed = 0.2f;
  //public float smooth = 4f;

  private Camera cam;
  private GameObject head;
  private CharacterController characterCont;
  private FirstPersonController fpController;
  private Rigidbody myRigidbody;
  private Transform myTransform;
  
  //private Vector3 playerPosition;
  private Point oldMousePoint;

  [DllImport("user32.dll")]
  public static extern bool SetCursorPos(int X, int Y);
  [DllImport("user32.dll")]
  public static extern bool GetCursorPos(out Point pos);


  private Vector3 correctPlayerPos, correctHeadPos;
  private Quaternion correctPlayerRot, correctHeadRot;
  private float speed = 5f;
  [HideInInspector]
  public static int spawnPosIndex = 0;

  void Start()
  {
    myTransform = transform;
    myRigidbody = GetComponent<Rigidbody>();
    characterCont = GetComponent<CharacterController>();
    fpController = GetComponent<FirstPersonController>();

    foreach (var tempCam in FindObjectsOfType<Camera>())
    {
      if (tempCam.transform.parent == null)
      {
        tempCam.gameObject.SetActive(false);
      }
    }

    var spawnPosObjs = GameObject.FindGameObjectsWithTag("spawnpos");
    Vector3 pos = spawnPosObjs[spawnPosIndex % spawnPosObjs.Length].transform.position;


    cam = transform.FindChild("Camera").GetComponent<Camera>();
    head = transform.FindChild("Head").gameObject;
    if (photonView.isMine)
    {
      characterCont.enabled = true;
      fpController.enabled = true;
      cam.gameObject.SetActive(true);
      head.SetActive(false);

      //start process of sending all whiteboard textures sequentially, with manuallly buffered RPCs
      if (!PhotonNetwork.player.isMasterClient)
      {
        whiteboardIndex = 0;
        whiteboards = FindObjectsOfType<PhotonWhiteboard>().ToList();
        SendNextWhiteboardTexture();
      }
    }

    print("PLAYERID: " + PhotonNetwork.player.ID);
  }

  

  private List<PhotonWhiteboard> whiteboards;
  private int whiteboardIndex = 0;

  void SendNextWhiteboardTexture()
  {
    print("SendNextWhiteboardTexture");
    if (whiteboardIndex >= whiteboards.Count) return;
    print("whiteboardIndex >= whiteboards.Count");
    PhotonWhiteboard board = whiteboards.ElementAt(whiteboardIndex);
    whiteboardIndex++;
    PhotonTransmitter networkTransmitter = board.GetComponent<PhotonTransmitter>();
    networkTransmitter.OnDataCompletelyReceived += board.ReceivedTextureHandler;
    networkTransmitter.OnDataCompletelyReceived += (a, b) => SendNextWhiteboardTexture();
    board.photonView.RPC("CmdSendTexture", PhotonNetwork.masterClient, new object[] { PhotonNetwork.player.ID });
    //CmdSendTexture(whiteboards.ElementAt(0).Key);
  }



  void Update()
  {
    if (photonView.isMine)
    {
      HandleMouseRotation();
      //RepositionUI();
    }
    else
    {
      //print(transform.position + " : " + correctPlayerPos);

      transform.position = Vector3.Lerp(transform.position, this.correctPlayerPos, Time.deltaTime * speed);
      transform.rotation = Quaternion.Lerp(transform.rotation, this.correctPlayerRot, Time.deltaTime * speed);
      head.transform.position = Vector3.Lerp(head.transform.position, this.correctHeadPos, Time.deltaTime * speed);
      head.transform.rotation = Quaternion.Lerp(head.transform.rotation, this.correctHeadRot, Time.deltaTime * speed);
    }
  }
  public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
  {
    if (stream.isWriting)
    {
      // We own this player: send the others our data
      stream.SendNext(transform.position);
      stream.SendNext(transform.rotation);
      stream.SendNext(cam.transform.position);
      stream.SendNext(cam.transform.rotation);
    }
    else
    {
      // Network player, receive data
      this.correctPlayerPos = (Vector3)stream.ReceiveNext();
      this.correctPlayerRot = (Quaternion)stream.ReceiveNext();
      this.correctHeadPos = (Vector3)stream.ReceiveNext();
      this.correctHeadRot = (Quaternion)stream.ReceiveNext();
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

  private PhotonWhiteboard activeBoard;
  private void RepositionUI()
  {
    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    RaycastHit hit;
    if (Physics.Raycast(ray, out hit))
    {
      var g = hit.collider.gameObject;
      if (g.tag.Equals("whiteboard") && (activeBoard == null || activeBoard.gameObject != g))
      {
        activeBoard = g.GetComponent<PhotonWhiteboard>();
        var canvas = FindObjectOfType<Canvas>();
        var rect = canvas.GetComponent<RectTransform>();
        Vector3 offset = new Vector3(0, g.transform.localScale.y/2 + rect.rect.height*rect.localScale.y/2, 0);
        canvas.transform.position = activeBoard.transform.position + offset;
        canvas.transform.rotation = activeBoard.transform.rotation;
      }
    }
  }
}

//public struct Point
//{
//  public int X;
//  public int Y;
//
//  public Point(int x, int y)
//  {
//    this.X = x;
//    this.Y = y;
//  }
//}