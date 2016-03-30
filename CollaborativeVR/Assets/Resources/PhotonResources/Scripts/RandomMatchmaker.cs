using UnityEngine;
using Photon;
using UnityEngine.VR;

public class RandomMatchmaker : PunBehaviour
{
  private PhotonView myPhotonView;
  
  // Use this for initialization
  void Start()
  {
    //PhotonNetwork.logLevel = PhotonLogLevel.Full;
    PhotonNetwork.ConnectUsingSettings("0.1");

  }

  void Update()
  {
    if (Input.GetKeyDown(KeyCode.V))
    {
      VRSettings.enabled = !VRSettings.enabled;
      
    }
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
    GameObject player = PhotonNetwork.Instantiate("PhotonResources/Prefabs/PlayerPhoton", Vector3.zero, Quaternion.identity, 0);
    myPhotonView = player.GetComponent<PhotonView>();
  }

  public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
  {
    if (PhotonNetwork.player.isMasterClient)
    {
      photonView.RPC("IncrementSpawnPos", PhotonTargets.All, new object[] { PlayerPhoton.spawnPosIndex + 1 });
    }
  }
  [PunRPC]
  public void IncrementSpawnPos(int newIndex)
  {
    PlayerPhoton.spawnPosIndex = newIndex;
  }
}