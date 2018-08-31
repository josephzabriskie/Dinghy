using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MyNetManager : NetworkManager {
	const int maxPlayers = 2;
	public PlayerConnectionObj[] playerSlots = new PlayerConnectionObj[2];

	public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
	{
		Debug.Log("OnServerAddPlayer: Adding new player");
		// find empty player slot
		for (int slot=0; slot < maxPlayers; slot++)
		{
			Debug.Log("Checking slot: " + slot.ToString());
			if (playerSlots[slot] == null)
			{
				var playerObj = (GameObject)GameObject.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
				var player = playerObj.GetComponent<PlayerConnectionObj>();

				Debug.Log("Setting player ID and adding player to slot: " + slot.ToString());
				player.playerId = slot;
				playerSlots[slot] = player;

				bool ret = NetworkServer.AddPlayerForConnection(conn, playerObj, playerControllerId);
				Debug.Log("Server AddPlayerForConnection(). Resl: " + ret.ToString());
				return;
			}
			Debug.Log("Slot " + slot.ToString() + " not null");
		}

		//TODO: graceful  disconnect
		conn.Disconnect();
	}

	public override void OnServerRemovePlayer(NetworkConnection conn, PlayerController playerController)
	{
		// remove players from slots
		var player = playerController.gameObject.GetComponent<PlayerConnectionObj>();
		playerSlots[player.playerId] = null;
		//CardManager.singleton.RemovePlayer(player);

		base.OnServerRemovePlayer(conn, playerController);
	}
	public override void OnServerDisconnect(NetworkConnection conn)
	{
		foreach (var playerController in conn.playerControllers)
		{
			var player = playerController.gameObject.GetComponent<PlayerConnectionObj>();
			playerSlots[player.playerId] = null;
			//CardManager.singleton.RemovePlayer(player);
		}

		base.OnServerDisconnect(conn);
	}
}
