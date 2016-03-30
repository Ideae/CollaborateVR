using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MarkerController : MonoBehaviour
{
  public GameObject colorPanel, brushSizePanel;
  void Start()
  {
    SetupColorPanel();
    SetupBrushSizePanel();
  }

  private void SetupColorPanel()
  {
    for (int i = 0; i < colorPanel.transform.childCount; i++)
    {
      var child = colorPanel.transform.GetChild(i);
      //print(child.gameObject.name);
      child.GetComponent<Image>().color = PhotonWhiteboard.colorArray[i];
    }
  }

  private void SetupBrushSizePanel()
  {
    for (int i = 0; i < brushSizePanel.transform.childCount; i++)
    {
      var child = brushSizePanel.transform.GetChild(i);
      //child.GetComponent<Image>().color = PhotonWhiteboard.colorArray[i];
      //Image img = child.GetComponent<Image>();
      var tex = PhotonWhiteboard.GetShapeTexture(i+1);
      if (tex != null)
      {
        var img = child.FindChild("Image").GetComponent<Image>();
        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
        img.sprite = s;
        img.rectTransform.sizeDelta = new Vector2(tex.width,tex.height) * 2;
        //img.material.color = Color.white;

      }
      
    }
  }

  public void ChooseColor(int colorIndex)
  {
    if (colorIndex < PhotonWhiteboard.colorArray.Length)
    {
      PhotonWhiteboard.drawColorID = (byte) colorIndex;
    }
  }

  public void ChooseBrushSize(int sizeIndex)
  {
    PhotonWhiteboard.SetBrushSize((byte)sizeIndex);
  }

}
