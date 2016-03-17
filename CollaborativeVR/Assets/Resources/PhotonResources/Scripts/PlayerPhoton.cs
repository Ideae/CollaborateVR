using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityStandardAssets.Characters.FirstPerson;
using Photon;

public class PlayerPhoton : PunBehaviour
{
  public float mouseRotateSpeed = 0.2f;
  //public float smooth = 4f;

  private Camera cam;
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


  private Vector3 correctPlayerPos;
  private Quaternion correctPlayerRot;
  private float speed = 5f;

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

    cam = transform.FindChild("Camera").GetComponent<Camera>();
    if (photonView.isMine)
    {
      characterCont.enabled = true;
      fpController.enabled = true;
      cam.gameObject.SetActive(true);
    }
  }

  void Update()
  {
    if (photonView.isMine)
    {
      HandleMouseRotation();
    }
    else
    {
      //print(transform.position + " : " + correctPlayerPos);

      transform.position = Vector3.Lerp(transform.position, this.correctPlayerPos, Time.deltaTime * speed);
      transform.rotation = Quaternion.Lerp(transform.rotation, this.correctPlayerRot, Time.deltaTime * speed);
    }
  }
  public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
  {
    if (stream.isWriting)
    {
      // We own this player: send the others our data
      stream.SendNext(transform.position);
      stream.SendNext(transform.rotation);
    }
    else
    {
      // Network player, receive data
      this.correctPlayerPos = (Vector3)stream.ReceiveNext();
      this.correctPlayerRot = (Quaternion)stream.ReceiveNext();
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