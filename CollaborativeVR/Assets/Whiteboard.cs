using UnityEngine;
using System.Collections;

public class Whiteboard : MonoBehaviour
{
  public Color drawColor = Color.black;
  public int width = 800;
  private int height;
  private Texture2D texture;
  private Renderer renderer;
  // Use this for initialization
  void Start ()
	{
	  height = (int)(width*0.75);
    texture = new Texture2D(width, height);
    renderer = GetComponent<Renderer>();
    renderer.material.mainTexture = texture;

    
  }
	
	// Update is called once per frame
	void Update () {
	
	}

  void OnMouseDown()
  {
    DrawOnBoard();
  }
  void OnMouseDrag()
  {
    DrawOnBoard();
  }

  private int oldX, oldY;
  void DrawOnBoard()
  {
    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    RaycastHit hit;
    if (Physics.Raycast(ray, out hit))
    {
      var coll = transform.GetComponent<Collider>();
      int x = (int)(((hit.point.x - coll.bounds.min.x) / coll.bounds.size.x) * width);
      int y = (int)(((hit.point.y - coll.bounds.min.y) / coll.bounds.size.y) * height);

      if (oldX != x || oldY != y)
      {
        Debug.Log(hit.point.x + " , " + hit.point.y);

        texture.SetPixel(x, y, drawColor);
        texture.Apply();

        oldX = x;
        oldY = y;
      }

    }
  }
}
