﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using PlayerActions;
using CellTypes;
using MatchSequence;
using UnityEngine.UI;
using System.Linq;
using ActionProc;
using PlayboardTypes;

public class PlayerConnectionObj : NetworkBehaviour {
	GameObject baseboard;
	[SyncVar]
	public int playerId;
	public int enemyId;
	LogicCore lc = null;
	PlayBoard2D pb = null;
	public CellStruct[,] latestPlayerGrid;
	public CellStruct[,] latestEnemyGrid;
	public List<Vector2> latestCapitolLocs;
	ActionAvail latestActionAvail;

	bool isReady = false;

	void Start () {
		//Debug.Log("PlayerConnectionObj Start. Local: " + isLocalPlayer.ToString());
		UIController.instance.DBPWrite("Player " +  this.playerId.ToString() + " joined!");
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
			//Debug.Log("pco init: This is the local player");
			//distinguish name? bad idea?
			gameObject.name = gameObject.name + "Local";
			//Find our UIC and do it's setup
			UIController.instance = GameObject.FindGameObjectWithTag("UIGroup").GetComponent<UIController>();
			UIController.instance.DBPWrite("Player " +  this.playerId.ToString() + " joined!");
			//Find our local playboard, and get grids
			this.pb = GameObject.FindGameObjectWithTag("PlayBoard").GetComponent<PlayBoard2D>(); // Find the playboard in the scene	
			this.pb.pobj = this;
			//this.ip = GameObject.FindGameObjectWithTag("InputProcessor").GetComponent<InputProcessor>();
			InputProcessor.instance.RegisterReport(this);

			this.isReady = true; //Now we can recieve RPC's ready's set
			this.CmdRequestGameStateUpdate();
			this.CmdRequestGridUpdate();
		}
	}

	public void ClearGrids(){ // mostly just used to make sure we clean up on disconnect
		this.pb.ClearGrids();
	}

	////////////////////////////////// COMMANDs 
	///Special functions that only get executed on the server
	[Command]
	void CmdSendPlayerActions( ActionReq[] qa){
		//Debug.Log("Player '" + this.playerId.ToString() + "' obj on server sending RX action to logic core!");
		//Debug.Log("CmdSendPlayerActions on server player " + this.playerId.ToString());
		// for(int i =0; i < qa.Count(); i ++){
		// 	Debug.Log("Got " + i.ToString() + ": " + qa[i].coords[0].ToString());
		// }
		this.lc.RXActionReq(this.playerId, new List<ActionReq>(qa));
	}
	[Command]
	void CmdRequestGridUpdate(){ // This guy should only be called internally on startup
		//Debug.Log("Player '" + this.playerId.ToString() + "' requesting CmdRequestGridUpdate");
		this.lc.ReportGridState(this.playerId); // this simply gets the logic core to call our RPC update grid
	}
	[Command]
	void CmdRequestGameStateUpdate(){ // This guy should only be called internally on startup
		//Debug.Log("Player '" + this.playerId.ToString() + "' requesting CmdRequestGameStateUpdate");
		this.lc.ReportGameState(this.playerId);
	}
	[Command]
	public void CmdSendPlayerLock(){ //Public so that Input processor can call it
		//Debug.Log("Sending action lock in for player " + this.playerId);
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
	public void RpcUpdatePlayBoard(GameBoardInfo gbi){
		if (!isLocalPlayer || !this.ReadyGuard()){// Ignore info if not local or Start not called yet
			return;
		}
		Debug.Log("Player: " + this.playerId + " got Playboard update");
		//Debug.Log("Ours: " + our.Length.ToString() + " :: Theirs: "  + other.Length.ToString());
		//Debug.Log("Got AA list: " + gbi.aaArray.Count().ToString());
		this.latestCapitolLocs = gbi.capitolTowers.ToList();
		UIController.instance.ActionSelectButtonGrpActionAvailUpdate(gbi.aaArray.ToList());
		UIController.instance.ActionSelectGroupUpdateActionInfo(gbi.aaArray.ToList());
		this.latestPlayerGrid = GUtils.Deserialize(gbi.ourGrid, gbi.gridSize[0], gbi.gridSize[1]);
		this.latestEnemyGrid =  GUtils.Deserialize(gbi.theirGrid, gbi.gridSize[0], gbi.gridSize[1]);
		UIController.instance.ActionSelectGroupUpdateFactionProgress(new FactionProgress(gbi.factionProgress), this.latestPlayerGrid, this.latestEnemyGrid);
		if(gbi.hitSunk){
			UIController.instance.HitSunkDisplayFlash();
		}
		this.pb.UpdateBoardFancy(gbi.lastArs.ToList(), this.latestPlayerGrid, this.latestEnemyGrid);
	}

	[ClientRpc]
	public void RpcUpdateGameState(StateInfo si){
		if (!isLocalPlayer || !this.ReadyGuard()){
			return;
		}
		switch(si.ms){
		case MatchState.waitForPlayers:
			Debug.Log("RPC Game state: wait for players");
			InputProcessor.instance.SetActionProcState(ActionProcState.reject);
			UIController.instance.GameStateUpdate("Hey We're waiting for players");
			break;
		case MatchState.placeTowers:
			Debug.Log("RPC Game state: placeTowers");
			// you need to have filled in the action request on the server within in 60s
			// After that it'll be read, set or not
			InputProcessor.instance.SetActionProcState(ActionProcState.multiTower);
			UIController.instance.GameStateUpdate("Hey Time to place towers: You've got 60s");
			UIController.instance.TimerStart(si.time);
			UIController.instance.ActionSelectButtonGrpEnable(true);
			break;
		case MatchState.actionSelect:
			Debug.Log("RPC Game state: actionSelect");
			InputProcessor.instance.SetActionProcState(ActionProcState.basicActions);
			UIController.instance.GameStateUpdate("Now you just enter an action every 30s");
			UIController.instance.TimerStart(si.time);
			UIController.instance.ActionSelectButtonGrpEnable(true);
			break;
		case MatchState.resolveState:
			Debug.Log("RPC Game state: resolveState");
			InputProcessor.instance.SetActionProcState(ActionProcState.reject);
			InputProcessor.instance.pb2d.ClearSelectionState(false); // Clear selected squares while resolving
			InputProcessor.instance.ClearActionContext();
			UIController.instance.TimerClear();
			UIController.instance.GameStateUpdate("Hey we're resolving real quick");
			UIController.instance.ActionSelectButtonGrpEnable(false);
			break;
		case MatchState.gameEnd:
			Debug.Log("Rpc Game state: gameEnd");
			InputProcessor.instance.SetActionProcState(ActionProcState.reject);
			UIController.instance.TimerClear();
			UIController.instance.GameStateUpdate("Game Over!");
			UIController.instance.ActionSelectButtonGrpEnable(false);
			UIController.instance.GameOverDisplayShow(si.won);
			break;
		default:
			Debug.LogError("RPC Game state: Uh oh, default. State is: " + si.ms.ToString());
			break;
		}
	}

	[ClientRpc]
	public void RpcReportActionReqs(){
		if (!isLocalPlayer || !this.ReadyGuard()){
			return;
		}
		ActionReq[] qa = InputProcessor.instance.GetQueuedActions().ToArray();
		//Debug.Log("RpcReportActionReqs from player " + this.playerId.ToString());
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
		//Debug.Log("RpcResetPlayerLock id: " + this.playerId.ToString());
		InputProcessor.instance.UnlockAction();
	}
	
	//Note about checking for local player. At the time of writing, the architecture of the networked game
	//has all playerconnection objects existing on all clients and the server at once. Ideally, we don't
	//need this. The client can synchronize their local playerconnobj with the server and no one else. Then
	//we won't need this "islocalPlayer" nonsense.
}
