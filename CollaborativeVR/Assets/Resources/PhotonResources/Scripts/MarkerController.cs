using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MarkerController : MonoBehaviour {

  void Start()
  {
    for (int i = 0; i < transform.childCount; i++)
    {
      var child = transform.GetChild(i);
      //print(child.gameObject.name);
      child.GetComponent<Image>().color = PhotonWhiteboard.colorArray[i];
    }
  }

  public void ChooseColor(int colorIndex)
  {
    if (colorIndex < PhotonWhiteboard.colorArray.Length)
    {
      PhotonWhiteboard.drawColorID = (byte) colorIndex;
    }
  }

}
