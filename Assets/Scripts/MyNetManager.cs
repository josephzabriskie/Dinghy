using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MyNetManager : NetworkManager {
	const int maxPlayers = 2;
	int currentPlayers = 0;
	public LogicCore logicCore;
	public PlayBoard2D pb;
	public PlayerConnectionObj[] playerSlots = new PlayerConnectionObj[2];

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

				Debug.Log("Server adding player. ID: " + slot.ToString());
				player.playerId = slot;
				player.enemyId = (slot + 1) % maxPlayers;
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
		UIController.instance.GameStateUpdate("Disconnected");
		UIController.instance.TimerStop();
		UIController.instance.ActionDisplayClear();
		//UIController.instance.LockButtonDeregister();
		this.pb.ClearGrids();
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
