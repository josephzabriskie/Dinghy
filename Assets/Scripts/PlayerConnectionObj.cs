using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayerActions;
using CellInfo;
using UnityEngine.UI;

public class PlayerConnectionObj : NetworkBehaviour {
	GameObject baseboard;
	[SyncVar]
	public int playerId;
	public LogicCore lc = null;
	//public DebugPanel dbp;
	public UIController uic;

	public GameGrid myGG;
	public GameGrid theirGG;

	//public Button lockButton; //Button to disable when locked //TODO make private, just doing for visibility
	//public Text actionDisplay; //Write to this when we've got a new action, clear
	bool actionLocked = false;
	ActionReq queuedAction;
	
	void Start () {
		//Get our DebugPanel
		this.uic = GameObject.FindGameObjectWithTag("UIGroup").GetComponent<UIController>();
		this.uic.DBPWrite("Player " +  this.playerId.ToString() + " joined!");
		if (isServer){ //We're the object on the server, need to link up to logic core
			Debug.Log("pco init: This is the server");
			GameObject logicCoreObj = GameObject.FindGameObjectWithTag("LogicCore");
			if (logicCoreObj != null){
				this.lc = logicCoreObj.GetComponent<LogicCore>();
				Debug.Log("pco init: Logic core found by server instance of player: " + this.lc.ToString());
			}
			else
				Debug.Log("pco init: Found no logic Core server instance of player");
		}
		if (isLocalPlayer){ // We're the local player, need to grab out grids, set their owner, set color
			Debug.Log("pco init: This is the local player");
			//Setup our default no action
			this.queuedAction = new ActionReq(this.playerId, pAction.noAction, null);
			//Find our lock button and register an on press action
			this.uic.LockButtonRegister(this);
			this.uic.ActionDisplayUpdate(this.queuedAction);
			//Find our local playboard, and get grids
			PlayBoard pbs = GameObject.FindGameObjectWithTag("PlayBoard").GetComponent<PlayBoard>(); // Find the playboard in the scene	
			this.myGG = pbs.getMyGrid();
			this.theirGG = pbs.getTheirGrid();
			this.myGG.pco = this;
			this.theirGG.pco = this;
			this.myGG.playerOwnedGrid=true;
			this.theirGG.playerOwnedGrid=false;
			this.CmdRequestGridUpdate();
		}
	}

	public void LockAction(){
		if(this.queuedAction.a != pAction.noAction){
			this.actionLocked = true;
			this.uic.LockButtonEnabled(false);
		}
	}

	//Unlock and wipe queued Action
	public void UnlockAction(){
		this.queuedAction = new ActionReq(this.playerId, pAction.noAction, null);
		this.actionLocked = false;
		this.uic.LockButtonEnabled(true);
	}

	void UpdateAction(ActionReq newAR){
		if (!this.actionLocked){
			this.queuedAction = newAR;
			this.uic.ActionDisplayUpdate(this.queuedAction);
		}
		else{
			//Do stuff that informs the player that they're locked in
		}
	}

	//Communicate with gamegrid
	public void RXGridInput(bool pGrid, Vector2 pos, CState state){
		ActionReq ar;
		if (pGrid){ // If clicked on our grid, spawn a tower
			ar = new ActionReq(this.playerId, pAction.placeTower, new Vector2[]{pos});
		}
		else{ // Else scout enemy spot
			ar = new ActionReq(this.playerId, pAction.scout, new Vector2[]{pos});
		}
		this.UpdateAction(ar);
		Debug.Log("Player " + this.playerId.ToString() + ": RXGrid, forward action to server through cmd. " + ar.p.ToString());
		CmdSendPlayerActions(ar);
	}

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
	//Serialized RPC with ours and other's grid state
	[ClientRpc]
	public void RpcUpdateGrids(CState[] our, CState[] other, int dim1, int dim2){
		if (!isLocalPlayer){ // only care if we're the local player
			Debug.Log("Ignoring update to our grid, not the client, don't care");
			return;
		}
		Debug.Log("Player: " + this.playerId + " got update to our grids.");
		Debug.Log("Ours: " + our.Length.ToString() + " :: Theirs: "  + other.Length.ToString());
		this.myGG.SetArrayState(GUtils.Deserialize(our, dim1, dim2));
		this.theirGG.SetArrayState(GUtils.Deserialize(other, dim1, dim2));
	}
}
