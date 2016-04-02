using UnityEngine;
using System.Collections;

public class ToolBase : MonoBehaviour
{
  public PSWand psWand;

  public virtual void Awake()
  {
    psWand = GetComponent<PSWand>();
  }
  //void Start () {
	//
	//}
	//void Update () {
	//
	//}

  public virtual void StartTool()
  {
    
  }

  public virtual void ContinueTool()
  {
    
  }

  public virtual void FinishTool()
  {
    
  }

  public RaycastHit? GetRaycastHit()
  {
    RaycastHit hitInfo;
    if (Physics.Raycast(transform.position, transform.forward, out hitInfo, psWand.maxDistance))
    {
      return hitInfo;
    }
    return null;
  }
}
