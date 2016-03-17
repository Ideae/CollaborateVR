using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class MyNetworkManager : NetworkManager {

  public override void OnServerConnect(NetworkConnection conn)
  {
    conn.SetChannelOption(Channels.DefaultReliable, ChannelOption.MaxPendingBuffers, 500);
  }
}
