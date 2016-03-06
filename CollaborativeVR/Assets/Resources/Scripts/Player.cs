using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.InteropServices;
using UnityStandardAssets.Characters.FirstPerson;

public class Player : NetworkBehaviour
{
  public float walkSpeed = 3f;
  public float mouseRotateSpeed = 0.2f;
  private Camera cam;
  private CharacterController characterCont;
  private FirstPersonController fpController;
  private Rigidbody rigid;
	void Start ()
	{
	  if (isLocalPlayer)
	  {
	    rigid = GetComponent<Rigidbody>();
	    characterCont = GetComponent<CharacterController>();
      characterCont.enabled = true;
	    fpController = GetComponent<FirstPersonController>();
	    fpController.enabled = true;
	    cam = transform.FindChild("Camera").GetComponent<Camera>();
      cam.gameObject.SetActive(true);
    }
	}

  private Point oldMousePoint;
	void Update () {
	  if (isLocalPlayer)
	  {
      HandleMouseRotation();
	    //HandlePlayerMovement();
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

  private void HandlePlayerMovement()
  {
    float horiz = Input.GetAxis("Horizontal");
    float vert = Input.GetAxis("Vertical");
    Vector3 v = new Vector3(horiz, 0f, vert) * walkSpeed;
    v = transform.rotation*v*Time.deltaTime;
    //Quaternion yRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    //characterCont.SimpleMove(v);
    //characterCont.Move(v);
    rigid.MovePosition(transform.position + v);
  }

  [DllImport("user32.dll")]
  public static extern bool SetCursorPos(int X, int Y);
  [DllImport("user32.dll")]
  public static extern bool GetCursorPos(out Point pos);
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