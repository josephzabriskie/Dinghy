using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlrConnObj_Test : NetworkBehaviour {

	public GameObject playerUnit;
	public string playerName = "Anonymous";
	TextMesh tm;

	void Start () {
		//Since the player object is invisible, not part of the world
		//give me something physical to move around
		//if (hasAuthority || hasLocalAuthority) //other options
		if (isLocalPlayer == false){
			//this object belongs to another player
			return;
		}
		Debug.Log("PlrConnObj_Test::Start -- Spawning my own personal obj");

		//Instantiate() only reates an object on the LOCAL computer.
		//Even if it has a network identity/transform, it still will
		//not exist on the network (and therefore not on any other client)
		//unless NetworkServer.Spawn() is called on this object.
		//Instantiate(playerUnit);

		//Command the server (politely) to SPAWN our unit
		CmdSpawnMyUnit();

		//tm = this.GetComponentInChildren<TextMesh>();
		//tm.text = playerName;
	}
	
	void SetPlayerName(string n){
		playerName = n;
		tm.text = n;
	}

	void Update () {
		if (isLocalPlayer == false){
			return;
		}

		if (Input.GetKeyDown(KeyCode.Q)){
			string n = "Joe" + Random.Range(1, 100);
			Debug.Log("sending the server a request to change our name to: " + n);
			//CmdChangePlayerName(n);
		}
		if(Input.GetKeyDown(KeyCode.S)){
			CmdSpawnMyUnit();
		}
	}

	////////////////////////////////// COMMANDs 
	///Special functions that only get executed on the server
	[Command]
	void CmdSpawnMyUnit(){
		Debug.Log("Server: I've been told to spawn an object");
		//We are guaranteed to be executing code on the server right now.
		GameObject go = Instantiate(playerUnit);

		//To give a client authority to move the spawned object, get the network Id
		//and assign it to the connection to client. Or... below
		//go.GetComponent<NetworkIdentity>().AssignClientAuthority(connectionToClient);

		//Now that the object exists on the server, propagate it to
		//all the clients (and also wire up the network identity)
		//NetworkServer.Spawn(go);

		//This combines set client authority and spawn
		NetworkServer.SpawnWithClientAuthority(go, connectionToClient);
	}
	[Command]
	void CmdChangePlayerName(string n){
		Debug.Log("Server: CmdChangePlayerName: " + n);
		//Maybe we should check that the name doesn't have bad words?
		//If there is a bad word in the name, do we just ignore or do we still call the RPC but with original name?
		SetPlayerName(n); 
		// Now tell all clients the new name
		RpcChangePlayerName(this.playerName);
	}

	////////////////////////////////// RPCs
	///RPCs - Special functions only executed on the clients
	[ClientRpc]
	void RpcChangePlayerName(string n){
		Debug.Log("Server: RpcChangePlayerName: asked to change the player name on a particular PlayerConnectionObj: " + n);
		SetPlayerName(n);
	}
}
