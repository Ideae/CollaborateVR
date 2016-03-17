using UnityEngine;
using Photon;

public class RandomMatchmaker : PunBehaviour
{
  private PhotonView myPhotonView;
  private int spawnPosIndex = 0;
  // Use this for initialization
  void Start()
  {
    //PhotonNetwork.logLevel = PhotonLogLevel.Full;
    PhotonNetwork.ConnectUsingSettings("0.1");

  }

  void OnGUI()
  {
    GUILayout.Label(PhotonNetwork.connectionStateDetailed.ToString());

    if (PhotonNetwork.connectionStateDetailed == PeerState.Joined)
    {

    }
    else
    {
      GUILayout.Label("Loading...");
    }
  }

  public override void OnJoinedLobby()
  {
    PhotonNetwork.JoinRandomRoom();
  }

  public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
  {
    Debug.Log("Can't join random room!");
    PhotonNetwork.CreateRoom(null);

  }
  
  public override void OnJoinedRoom()
  {
    var spawnPosObjs = GameObject.FindGameObjectsWithTag("spawnpos");
    Vector3 pos = spawnPosObjs[spawnPosIndex % spawnPosObjs.Length].transform.position;
    GameObject player = PhotonNetwork.Instantiate("PhotonResources/Prefabs/PlayerPhoton", pos, Quaternion.identity, 0);
    myPhotonView = player.GetComponent<PhotonView>();
    photonView.RPC("IncrementSpawnPos", PhotonTargets.AllBuffered);
  }

  [PunRPC]
  public void IncrementSpawnPos()
  {
    spawnPosIndex++;
  }
}