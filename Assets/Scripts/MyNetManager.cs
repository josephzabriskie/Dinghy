using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MyNetManager : NetworkManager {
	const int maxPlayers = 2;
	int currentPlayers = 0;
	public LogicCore logicCore;
	public PlayerConnectionObj[] playerSlots = new PlayerConnectionObj[2];
	public GameObject localPlayerObj; // The local player

	void Awake(){
		LogFilter.currentLogLevel = LogFilter.Debug;
	}

	public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
	{
		//Debug.Log("OnServerAddPlayer: Adding new player");
		// find empty player slot
		for (int slot=0; slot < maxPlayers; slot++){
			//Debug.Log("Checking slot: " + slot.ToString());
			if (playerSlots[slot] == null){
				var playerObj = (GameObject)GameObject.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
				//Debug.Log("Starting new player in slot "  + slot.ToString());
				//playerObj.GetComponent<PlayerConnectionObj>().Start();
				var player = playerObj.GetComponent<PlayerConnectionObj>();

				Debug.Log("Setting player ID and adding player to slot: " + slot.ToString());
				player.playerId = slot;
				playerSlots[slot] = player;

				NetworkServer.AddPlayerForConnection(conn, playerObj, playerControllerId);
				currentPlayers++;
				if (currentPlayers == maxPlayers){
					logicCore.StartGameProcess();
				}				
				return;
			}
			//Debug.Log("Slot " + slot.ToString() + " not null");
		}

		conn.Disconnect();
	}

	public override void OnServerRemovePlayer(NetworkConnection conn, PlayerController playerController){
		//Debug.Log("OnServerRemovePlayer: removing player");
		// remove players from slots
		var player = playerController.gameObject.GetComponent<PlayerConnectionObj>();
		playerSlots[player.playerId] = null;
		currentPlayers--;
		if(currentPlayers != maxPlayers){
			logicCore.PauseGameProcess();
		}
		base.OnServerRemovePlayer(conn, playerController);
	}

	public override void OnServerDisconnect(NetworkConnection conn){
		//Debug.Log("OnServerDisconnect: removing player");
		foreach (var playerController in conn.playerControllers)
		{
			currentPlayers--;
			var player = playerController.gameObject.GetComponent<PlayerConnectionObj>();
			playerSlots[player.playerId] = null;
		}
		if(currentPlayers != maxPlayers){
			logicCore.PauseGameProcess();
		}
		base.OnServerDisconnect(conn);
	}

	public override void OnClientConnect(NetworkConnection conn){
		//Debug.Log("OnClientConnect");
		base.OnClientConnect(conn);
	}

	public override void OnStartClient(NetworkClient client){
		//Debug.Log("OnStartClient...");
		//Player object not instantiated yet...
		// GameObject obj = GameObject.Find("PlayerConnObject(Clone)");
		// Debug.Log("Looking for player obj. " + obj.ToString());
		base.OnStartClient(client);
	}

	public override void OnStopClient(){ // you can use this to do stuff when the player disconencts
		//Debug.Log("OnStopClient()");
		// you can use this to do stuff when the player disconencts
		PlayerConnectionObj pobj = GameObject.Find("PlayerConnObject(Clone)Local").GetComponent<PlayerConnectionObj>();
		pobj.uic.GameStateUpdate("Disconnected");
		pobj.uic.TimerStop();
		pobj.uic.ActionDisplayClear();
		pobj.uic.LockButtonDeregister();
		pobj.ClearGrids();
		base.OnStopClient();
	}

	public override void OnClientDisconnect(NetworkConnection conn){
		//Debug.Log("A client is disconnecting");
		base.OnClientDisconnect(conn);
	}

	public override void OnServerConnect(NetworkConnection conn){
		//Debug.Log("OnServerConnect()");
		base.OnServerConnect(conn);
	}
}
