using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Matchmaker : NetworkManager {
	const int maxWaiting = 20;
	PlayerConnectionObj[] waitingSlots = new PlayerConnectionObj[maxWaiting];
	public GameObject logicCorePrefab;
	const int maxMatches = 10;
	List<Match> matches = new List<Match>();
	int debugCount = 0;

	public override void OnServerAddPlayer(NetworkConnection conn, AddPlayerMessage extraMessage){
		for (int slot=0; slot < maxWaiting; slot++){
			if (waitingSlots[slot] == null){
				GameObject playerObj = (GameObject)GameObject.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
				playerObj.name = "PConnObj-" + debugCount++.ToString();
				PlayerConnectionObj player = playerObj.GetComponent<PlayerConnectionObj>();

				Debug.Log("Matchmaker adding waiting player. ID: " + slot.ToString());
				NetworkServer.AddPlayerForConnection(conn, playerObj);
				Debug.Log("NetID: " + player.GetComponent<NetworkIdentity>().netId.ToString());
				waitingSlots[slot] = player;
				SearchForMatches();
				return;
			}
		}
		conn.Disconnect();	
	}

	void SearchForMatches(){
		Debug.Log("Searching for matches!");
		if(matches.Count < maxMatches){
			PlayerConnectionObj p0 = null;
			PlayerConnectionObj p1 = null;
			foreach(PlayerConnectionObj pobj in waitingSlots){
				if(!pobj){
					break;
				}
				if(!p0){
					Debug.Log("Setting p0 to " + pobj.ToString());
					p0 = pobj;
				}
				else{
					Debug.Log("Got 2 players, make new Match!");
					p1 = pobj;
					GameObject newLogicCore = Instantiate(logicCorePrefab, transform.position, Quaternion.identity);
					NetworkServer.Spawn(newLogicCore);
					int matchIndex = matches.Count;
					Match newMatch = new Match(p0, p1, newLogicCore, matchIndex);
					matches.Add(newMatch);
				}
			}
		}
		return;
	}
}

public class Match {
	PlayerConnectionObj player0;
	PlayerConnectionObj player1;
	GameObject logicCoreobj;
	int index;
	public Match(PlayerConnectionObj p0, PlayerConnectionObj p1, GameObject logicCoreObj, int matchIndex){
		Debug.Log("Creating a new match!");
		player0 = p0;
		player1 = p1;
		this.logicCoreobj = logicCoreObj;
		LogicCore lc = this.logicCoreobj.GetComponent<LogicCore>();
		lc.matchIndex = matchIndex;
		index = matchIndex;
		player0.ServerInit(logicCoreObj,0,1); //Don't like that the init's here are separate
		player1.ServerInit(logicCoreObj,1,0);
		player0.RpcInit(logicCoreobj,0,1);
		player1.RpcInit(logicCoreobj,1,0);
		lc.SpawnStart(player0, player1);
		lc.StartGameProcess();
	}
}