using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayerActions;
using CellInfo;
using MatchSequence;
using UnityEngine.UI;
using System.Linq;

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
	bool isReady = false;

	//these guys are used to define how we process input and where we store it
	enum ActionProcState{ // How do we save and send input?
		reject, // Default reject input, don't save, don't send
		multiTower, // This is multi selection mode at start of game. Save up to 3, send 3
		singleAction, // Here we store only one action in first index. Save 1 send 1
	}
	ActionProcState apc = ActionProcState.reject;
	List<ActionReq> queuedActions; // Used for game start tower placement

	void Start () {
		Debug.Log("PlayerConnectionObj Start. Local: " + isLocalPlayer.ToString());
		this.uic = GameObject.FindGameObjectWithTag("UIGroup").GetComponent<UIController>();
		this.uic.DBPWrite("Player " +  this.playerId.ToString() + " joined!");
		if (isServer){ //We're the object on the server, need to link up to logic core
			//Debug.Log("pco init: This is the server");
			GameObject logicCoreObj = GameObject.FindGameObjectWithTag("LogicCore");
			if (logicCoreObj != null){
				this.lc = logicCoreObj.GetComponent<LogicCore>();
				//Debug.Log("pco init: Logic core found by server instance of player: " + this.lc.ToString());
			}
			//else
				//Debug.Log("pco init: Found no logic Core server instance of player");
		}
		if (isLocalPlayer){ // We're the local player, need to grab out grids, set their owner, set color
			Debug.Log("pco init: This is the local player");
			//distinguish name? bad idea?
			gameObject.name = gameObject.name + "Local";
			//Find our UIC and do it's setup
			this.uic = GameObject.FindGameObjectWithTag("UIGroup").GetComponent<UIController>();
			this.uic.DBPWrite("Player " +  this.playerId.ToString() + " joined!");
			this.uic.LockButtonRegister(this);
			//this.uic.ActionDisplayUpdate(this.queuedActions[0]);
			this.queuedActions = new List<ActionReq>();
			//Find our local playboard, and get grids
			PlayBoard pbs = GameObject.FindGameObjectWithTag("PlayBoard").GetComponent<PlayBoard>(); // Find the playboard in the scene	
			this.myGG = pbs.getMyGrid();
			this.theirGG = pbs.getTheirGrid();
			this.myGG.pco = this;
			this.theirGG.pco = this;
			this.myGG.playerOwnedGrid=true;
			this.theirGG.playerOwnedGrid=false;
			this.isReady = true;
			this.CmdRequestGameStateUpdate();
			this.CmdRequestGridUpdate();
		}
	}

	public void LockAction(){
		if (this.queuedActions.Any(req => req.a != pAction.noAction)){
			Debug.Log("LockactionPressed pid: " + this.playerId.ToString());
			this.actionLocked = true;
			this.uic.LockButtonEnabled(false);
			this.CmdSendPlayerLock();
		}
	}

	public void ClearGrids(){ // mostly just used to make sure we clean up on disconnect
		this.myGG.ClearArrayState();
		this.theirGG.ClearArrayState();
	}

	//Unlock and wipe queued Action - Do I need this?
	public void UnlockAction(){
		this.actionLocked = false;
		this.uic.LockButtonEnabled(true);
	}

	// void UpdateAction(ActionReq newAR){
	// 	if (!this.actionLocked){
	// 		//this.queuedAction = newAR;
	// 		//this.uic.ActionDisplayUpdate(this.queuedAction);
	// 	}
	// 	else{
	// 		//Do stuff that informs the player that they're locked in
	// 	}
	// }

	/////////////////////////////////Get and process input from grids
	//Communicate with gamegrid
	public void RXGridInput(bool pGrid, Vector2 pos, CState state){
		//Don't need to ensure local player, grid only assigned to localplayer
		if (this.actionLocked){
			Debug.Log("Got input, but we're already locked... ignoring");
			return;
		}
		//ActionReq ar;
		switch(this.apc){
		case ActionProcState.reject:
			Debug.Log("APC Reject: Ignoring input from grid");
			//Do nothing, we ignore the request
			break;
		case ActionProcState.multiTower:
			if (!pGrid){
				Debug.Log("APC multitower: not our grid, don't do nuthin");
				break;
			}
			//Does this location already exist within our queuedactions?
			int idx = this.queuedActions.FindIndex(x => x.a == pAction.placeTower && x.coords[0] == pos);
			//Debug.Log("APC multitower: check for dup result " + idx.ToString());
			if (idx >=0 ){ // We've already got this guy selected, deselect it
				//Debug.Log("APC multitower: Already got this one, toggle off");
				this.queuedActions[idx] = new ActionReq(this.playerId, pAction.noAction, null);
				this.myGG.SetCellState(pos, CState.empty);
				break;
			}
			idx = this.queuedActions.FindIndex(x => x.a == pAction.noAction);
			//Debug.Log("APC multitower: check for open result " + idx.ToString());
			if(idx >=0){ // We've still have room for a new request
				//Debug.Log("APC multitower: we have room at idx " + idx.ToString());
				this.queuedActions[idx] = new ActionReq(this.playerId, pAction.placeTower, new Vector2[]{pos});
				this.myGG.SetCellState(pos, CState.towertemp);
			}
			else{ // No room for another tower selection, ignore
				//Debug.Log(" APC multitower: no room left! ignoring");
			}
			break;
		case ActionProcState.singleAction:
			if(pGrid){ // If we click on our side, that means place tower
				this.queuedActions[0] = new ActionReq(this.playerId, pAction.placeTower, new Vector2[]{pos});
			}
			else{ // If we click on their side, that means reveal/shoot
				this.queuedActions[0] = new ActionReq(this.playerId, pAction.scout, new Vector2[]{pos});
			}
			break;
		default:
			break;
		}
	}

	////////////////////////////////// COMMANDs 
	///Special functions that only get executed on the server
	[Command]
	void CmdSendPlayerActions( ActionReq[] qa){
		//Debug.Log("Player '" + this.playerId.ToString() + "' obj on server sending RX action to logic core!");
		Debug.Log("CmdSendPlayerActions on server player " + this.playerId.ToString());
		// for(int i =0; i < qa.Count(); i ++){
		// 	Debug.Log("Got " + i.ToString() + ": " + qa[i].coords[0].ToString());
		// }
		this.lc.RXActionReq(new List<ActionReq>(qa));
	}
	[Command]
	void CmdRequestGridUpdate(){ // This guy should only be called internally on startup, not local player guarded
		//Debug.Log("Player '" + this.playerId.ToString() + "' requesting CmdRequestGridUpdate");
		this.lc.ReportGridState(this.playerId); // this simply gets the logic core to call our RPC update grid
	}
	[Command]
	void CmdRequestGameStateUpdate(){ // This guy should only be called internally on startup, not local player guarded
		//Debug.Log("Player '" + this.playerId.ToString() + "' requesting CmdRequestGameStateUpdate");
		this.lc.ReportGameState(this.playerId);
	}
	[Command]
	void CmdSendPlayerLock(){
		Debug.Log("Sending action lock in for player " + this.playerId);
		this.lc.SetPlayerLock(this.playerId);
	}

	////////////////////////////////// RPCs
	//RPC's can be called before start is called. isReady guards against that.
	bool ReadyGuard(){
		if(!this.isReady){
			Debug.Log("Caught RPC before player 'isReady'. Advise return early");
			return false;
		}
		return true;
	}

	//Serialized RPC with ours and other's grid state
	[ClientRpc]
	public void RpcUpdateGrids(CState[] our, CState[] other, int dim1, int dim2){
		if (!isLocalPlayer || !this.ReadyGuard()){// Ignore info if not local or Start not called yet
			return;
		}
		//Debug.Log("Player: " + this.playerId + " got update to our grids.");
		//Debug.Log("Ours: " + our.Length.ToString() + " :: Theirs: "  + other.Length.ToString());
		this.myGG.SetArrayState(GUtils.Deserialize(our, dim1, dim2));
		this.theirGG.SetArrayState(GUtils.Deserialize(other, dim1, dim2));
	}

	[ClientRpc]
	public void RpcUpdateGameState(MatchState ms, int timeleft){
		if (!isLocalPlayer || !this.ReadyGuard()){
			return;
		}
		switch(ms){
		case MatchState.waitForPlayers:
			Debug.Log("RPC Game state: wait for players");
			this.apc = ActionProcState.reject;
			this.uic.GameStateUpdate("Hey We're waiting for players");
			break;
		case MatchState.placeTowers:
			Debug.Log("RPC Game state: placeTowers");
			// you need to have filled in the action request on the server within in 60s
			// After that it'll be read, set or not
			this.apc = ActionProcState.multiTower;
			this.queuedActions.Clear();
			this.queuedActions.AddRange(new List<ActionReq>{ // currently hold 3. 1 for each initial tower allowed to place
				new ActionReq(this.playerId, pAction.noAction, null),
				new ActionReq(this.playerId, pAction.noAction, null),
				new ActionReq(this.playerId, pAction.noAction, null)});
			this.uic.GameStateUpdate("Hey Time to place towers: You've got 60s");
			this.uic.TimerStart(timeleft);
			break;
		case MatchState.actionSelect:
			Debug.Log("RPC Game state: actionSelect");
			this.apc = ActionProcState.singleAction;
			this.queuedActions.Clear();
			this.queuedActions.AddRange(new List<ActionReq> {new ActionReq(this.playerId, pAction.noAction, null)});
			this.apc = ActionProcState.singleAction;
			this.uic.GameStateUpdate("Now you just enter an action every 30s");
			this.uic.TimerStart(timeleft);
			break;
		default:
			Debug.Log("RPC Game state: Uh oh default");
			break;
		}
	}

	[ClientRpc]
	public void RpcReportActionReqs(){
		if (!isLocalPlayer || !this.ReadyGuard()){
			return;
		}
		Debug.Log("RpcReportActionReqs from player " + this.playerId.ToString());
		ActionReq[] qa = this.queuedActions.ToArray();
		// for(int i =0; i < qa.Count(); i ++){
		// 	Debug.Log("Send " + i.ToString() + ": " + qa[i].coords[0].ToString());
		// }
		this.CmdSendPlayerActions(qa);
	}

	[ClientRpc]
	public void RpcResetPlayerLock(){
		if(!isLocalPlayer || !this.ReadyGuard()){
			return;
		}
		Debug.Log("RpcResetPlayerLock id: " + this.playerId.ToString());
		this.UnlockAction();
	}
	
	//Note about checking for local player. At the time of writing, the architecture of the networked game
	//has all playerconnection objects existing on all clients and the server at once. Ideally, we don't
	//need this. The client can synchronize their local playerconnobj with the server and no one else. Then
	//we won't need this "islocalPlayer" nonsense.
}
