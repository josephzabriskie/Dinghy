using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayerActions;
using CellInfo;

public class PlayerConnectionObj : NetworkBehaviour {
	GameObject baseboard;
	[SyncVar]
	public int playerId;
	public GameObject lcgo;
	public LogicCore lc = null;
	public string playerName = "Anonymous";

	public GameGrid myGG;
	public GameGrid theirGG;
	
	// Use this for initialization
	void Start () {
		Debug.Log("pco init: New Player Joined! ID: " + this.playerId.ToString());
		if(!isServer && !isLocalPlayer){
			Debug.Log("I ain't the server or the player owner, don't set any thing up");
			return;
		}

		if (isServer){ //We're the object on the server, need to link up to logic core
			Debug.Log("pco init: This is the server");
			this.lcgo = GameObject.FindGameObjectWithTag("LogicCore");
			if (this.lcgo != null){
				this.lc =this.lcgo.GetComponent<LogicCore>();
				Debug.Log("pco init: Logic core found by serverinstance of player: " + this.lc.ToString());
			}
			else
				Debug.Log("pco init: Found no logic Core serverinstance of player");
		}
		if (isLocalPlayer){ // We're the local player, need to grab out grids, set their owner, set color
			Debug.Log("pco init: This is the local player");
			//Set up play board
			GameObject pb = GameObject.FindGameObjectWithTag("PlayBoard"); // Find the playboard in the scene	
			if (!pb){
				Debug.LogError("pco init: Couldn't find 'PlayBoard'!");
			}
			PlayBoard pbs = pb.GetComponent<PlayBoard>(); // get playboard script
			this.myGG = pbs.getMyGrid();
			this.theirGG = pbs.getTheirGrid();
			this.myGG.pco = this;
			this.theirGG.pco = this;
			this.myGG.playerOwnedGrid=true;
			this.theirGG.playerOwnedGrid=false;
			this.CmdRequestGridUpdate();
			//this.myGG.SetColor(Color.green);
			//this.theirGG.SetColor(Color.magenta);
		}
	}

	//Communicate with gamegrid
	public void RXGridInput(bool pGrid, Vector2 pos, CState state){
		ActionReq ar = new ActionReq(this.playerId, pAction.placeTower, new Vector2[]{pos});
		Debug.Log("Player " + this.playerId.ToString() + ": RXGrid, forward action to server through cmd. " + ar.p.ToString());
		CmdSendPlayerActions(ar);
	}

	// public void AssignClientAuth(NetworkConnection conn){
	// 	this.network
	// }

	////////////////////////////////// COMMANDs 
	///Special functions that only get executed on the server
	[Command]
	void CmdSendPlayerActions(ActionReq req){
		Debug.Log("Player obj on server sending RX action to logic core!");
		this.lc.RXActionReq(req);
	}
	[Command]
	void CmdRequestGridUpdate(){
		Debug.Log("Player '" + this.playerId + "' requesting grid Update");
		this.lc.ReportGridState(this.playerId); // this simply gets the logic core to call our RPC update grid
	}


	////////////////////////////////// RPCs
	///RPCs - Special functions only executed on the clients
	[ClientRpc]
	void RpcChangePlayerName(string n){
		// Debug.Log("Server: RpcChangePlayerName: asked to change the player name on a particular PlayerConnectionObj: " + n);
		/// SetPlayerName(n);
	}

	[ClientRpc]
	public void RpcUpdateGrids(CState[] our, CState[] other, int dim1, int dim2){
		if (!isLocalPlayer){
			Debug.Log("Ignoring update to our grid, not the client, don't care");
			return;
		}
		Debug.Log("Player: " + this.playerId + " got update to our grids.");
		Debug.Log("Ours: " + our.Length.ToString() + " :: Theirs: "  + other.Length.ToString());
		this.myGG.SetArrayState(GridUtils.Deserialize(our, dim1, dim2));
		this.theirGG.SetArrayState(GridUtils.Deserialize(other, dim1, dim2));
	}
}
