using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;

public class BoardTextTool : ToolBase {
  private DictationRecognizer m_DictationRecognizer;
  private LineWhiteboard currentDrawingBoard = null;
  private Canvas currentCanvas = null;
  private GameObject textbuttonPrefab;
  private Text currentText;
  private static int currentTextID = 0;

  public override void Awake()
  {
    base.Awake();
    m_DictationRecognizer = new DictationRecognizer();
    textbuttonPrefab = Resources.Load<GameObject>("PhotonResources/Prefabs/TextButton");

    m_DictationRecognizer.DictationResult += (text, confidence) =>
    {
      //Debug.LogFormat("Dictation result: {0}", text);
      currentText.text = text;
      currentText.color = Color.black;
      CleanupDictation();
    };

    m_DictationRecognizer.DictationHypothesis += (text) =>
    {
      //Debug.LogFormat("Dictation hypothesis: {0}", text);
      currentText.text = text;
      currentText.color = Color.green;
    };

    m_DictationRecognizer.DictationComplete += (completionCause) =>
    {
      if (completionCause != DictationCompletionCause.Complete)
        Debug.LogErrorFormat("Dictation completed unsuccessfully: {0}.", completionCause);
      CleanupDictation();
    };

    m_DictationRecognizer.DictationError += (error, hresult) =>
    {
      Debug.LogErrorFormat("Dictation error: {0}; HResult = {1}.", error, hresult);
      CleanupDictation();
    };
  }

  private void CleanupDictation()
  {
    currentDrawingBoard = null;
    currentCanvas = null;
    currentText = null;
    m_DictationRecognizer.Stop();
  }

  public override void StartTool()
  {
    RaycastHit? hitInfo = GetRaycastHit();
    if (hitInfo != null && hitInfo.Value.collider.gameObject != null)
    {
      var board = hitInfo.Value.collider.gameObject.GetComponent<LineWhiteboard>();
      if (board != null)
      {
        currentDrawingBoard = board;
        currentCanvas = board.GetComponentInChildren<Canvas>();
        currentTextID++;
        Vector3 point = GetBoardPoint(hitInfo.Value.point, board);
        Rect r = currentCanvas.GetComponent<RectTransform>().rect;
        float x = Vector3.Dot(board.transform.right, point - board.transform.position) / (board.transform.localScale.x/2);
        float y = Vector3.Dot(board.transform.up, point - board.transform.position)/(board.transform.localScale.y / 2);
        print(x + " : " + y);
        x *= r.width/2;
        y *= r.height/2;

        GameObject g = GameObject.Instantiate(textbuttonPrefab);
        g.transform.SetParent(currentCanvas.transform, false);
        RectTransform rt = g.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, y);
        
        //rt.localScale = new Vector3(1, 1, 1);
        currentText = g.GetComponentInChildren<Text>();
        currentText.text = "Speak into Microphone...";
        currentText.color = Color.red;
        m_DictationRecognizer.Start();
        //currentDrawingBoard.DrawLineOnBoard(point, PSWand.ButtonState.ButtonDown, currentTextID);


      }
    }
  }

  public override void ContinueTool()
  {
  }

  public override void FinishTool()
  {
  }
}
