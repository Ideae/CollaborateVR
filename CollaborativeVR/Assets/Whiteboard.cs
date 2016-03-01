﻿using System;
using UnityEngine;
using System.Collections;

public class Whiteboard : MonoBehaviour
{
  public Color drawColor = Color.black;
  public int width = 800;
  private int height;
  private Texture2D boardTexture;
  public Texture2D brushTexture, shapeTexture;

  private Renderer renderer;
  // Use this for initialization
  void Start ()
  {
    float aspect = transform.localScale.y/transform.localScale.x;
	  height = (int)(width*aspect);//fix
    boardTexture = new Texture2D(width, height);
    renderer = GetComponent<Renderer>();
    renderer.material.mainTexture = boardTexture;
    oldX = oldY = -1;

    if (brushTexture == null)
    {
      brushTexture = (Texture2D)Resources.Load("Textures/brush_256/White1");
    }
    if (shapeTexture == null)
    {
      shapeTexture = (Texture2D) Resources.Load("WhiteCircle64");
    }
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

  void OnMouseUp()
  {
    oldX = oldY = -1;
  }

  public int step = 10;
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
        DrawBrush(x, y);
        if (oldX != -1 && oldY != -1 && shapeTexture.width < 127)
        {
          Vector2 dir = new Vector2(x - oldX, y - oldY);
          float dist = dir.magnitude;
          dir = dir.normalized;
          step = Mathf.RoundToInt(Mathf.Log(shapeTexture.width, 1.2f) - 7);
          for (int i = step; i < dist; i += step)
          {
            DrawBrush(Mathf.RoundToInt(oldX + dir.x * i), Mathf.RoundToInt(oldY + dir.y * i));
          }
        }
        oldX = x;
        oldY = y;
      }
    }
  }

  void DrawBrush(int x, int y)
  {
    int minx = Mathf.Max(0, x - shapeTexture.width / 2);
    int miny = Mathf.Max(0, y - shapeTexture.height / 2);
    int maxx = Mathf.Min(boardTexture.width, x + shapeTexture.width / 2);
    int maxy = Mathf.Min(boardTexture.height, y + shapeTexture.height / 2);//handle odd case?
    int w = x - minx, h = y - miny;
    int minBX = brushTexture.width / 2 - w;
    int minBY = brushTexture.height / 2 - h;
    int minSX = shapeTexture.width / 2 - w;
    int minSY = shapeTexture.height / 2 - h;
    int diffX = maxx - minx;
    int diffY = maxy - miny;

    for (int i = 0; i < diffX; i++)
    {
      for (int j = 0; j < diffY; j++)
      {
        int xx = i + minx, yy = j + miny;
        Color oldCol = boardTexture.GetPixel(xx, yy);
        Color brushCol = brushTexture.GetPixel(i + minBX, j + minBY);
        Color shapeCol = shapeTexture.GetPixel(i + minSX, j + minSY);
        //Color temp = (Color.white - (shapeCol));
        //Color temp2 = new Color(Math.Min(1f, temp.r + drawColor.r), Math.Min(1f, temp.g + drawColor.g), Math.Min(1f, temp.b + drawColor.b), Math.Min(1f, temp.a + drawColor.a));
        //Color final = temp2 * oldCol; //brushCol * shapeCol
        Color t = brushCol * shapeCol;//*brushCol.a;

        Color final = drawColor * drawColor.a * t.r + oldCol * (1f - t.r * drawColor.a);

        boardTexture.SetPixel(xx, yy, final);
      }
    }
    //boardTexture.SetPixel(x, y, drawColor);
    boardTexture.Apply();
  }
}
