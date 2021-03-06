﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MyNetManager : NetworkManager {
	const int maxPlayers = 2;
	int currentPlayers = 0;
	public LogicCore logicCore;
	public PlayBoard2D pb;
	public PlayerConnectionObj[] playerSlots = new PlayerConnectionObj[2];


	public override void OnServerAddPlayer(NetworkConnection conn, AddPlayerMessage extraMessage)
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

				NetworkServer.AddPlayerForConnection(conn, playerObj);
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

	public override void OnServerRemovePlayer(NetworkConnection conn, NetworkIdentity playerNetId){
		//Debug.Log("OnServerRemovePlayer: removing player");
		// remove players from slots
		var player = playerNetId.gameObject.GetComponent<PlayerConnectionObj>();
		playerSlots[player.playerId] = null;
		currentPlayers--;
		if(currentPlayers != maxPlayers){
			logicCore.PauseGameProcess();
		}
		base.OnServerRemovePlayer(conn, playerNetId);
	}

	public override void OnServerDisconnect(NetworkConnection conn){
		//Debug.Log("OnServerDisconnect: removing player");
		currentPlayers--;
		var player = conn.playerController.gameObject.GetComponent<PlayerConnectionObj>();
		playerSlots[player.playerId] = null;
		if(currentPlayers != maxPlayers){
			logicCore.PauseGameProcess();
		}
		base.OnServerDisconnect(conn);
	}

	public override void OnClientConnect(NetworkConnection conn){
		//Debug.Log("OnClientConnect");
		base.OnClientConnect(conn);
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
