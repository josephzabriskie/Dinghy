using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using CellUIInfo;
using CellTypes;
using PlayerActions;
using MatchSequence;
using System.Linq;
using PlayboardTypes;
using ActionProc;

//game object represenation of the logic processor on the server. Holds a reference to the actual gameboard
namespace MatchSequence{
	//Feel free to divide these down as needed (like place towers in to pre, mid, post place towers)
	public enum MatchState{
		waitForPlayers,
		placeTowers, // Players will have an amount of time to select their tower starting positions
		actionSelect, // Players will have an amount of time to select their next action
		resolveState, // Give the clients some time to resolve game state before next action
		gameEnd,
	}
	public struct StateInfo{
		public MatchState ms;
		public int time; // Time
		public bool won; // Winner
		public StateInfo(MatchState ms, int time, bool won){
			this.ms = ms;
			this.time = time;
			this.won = won;
		}
	}
}

//Logic core's job is to take action requests as inputs and then compute the new game state
public class LogicCore : NetworkBehaviour {
	//Need to added from scene
	public MyNetManager mnm;
	public PlayBoard2D pb2D;
	//Other
	public PlayerConnectionObj[] playersObjs;
	public List<ActionReq> playerActions;
	public List<bool> playerLocks;
	public List<bool> playerResps; // Keeps track of which players have send in action requests since we last cleared
	public List<bool> playerWin;
	public MatchState currMS = MatchState.waitForPlayers;
	public MatchState pausedMS = MatchState.placeTowers;
	public int stateTime;
	PlayBoard PB;
	int pNum = 2; // max player number
	Coroutine currentCoroutine = null;
	public int sizex;
	public int sizey;

	// Use this for initialization
	void Start () {
		//Debug.Log("Starting Logic Core!");
		int[] size = this.pb2D.GetGridSize();
		this.sizex = size[0];
		this.sizey = size[1];
		this.playerActions = new List<ActionReq>();
		this.playerLocks = new List<bool>(new bool[this.pNum]);
		this.playerResps = new List<bool>(new bool[this.pNum]);
		this.playerWin = new List<bool>(new bool[this.pNum]); // Defaults to false
		this.ResetGame();
	}

	bool testcb(ActionReq ar){
		return true;
	}
	
	/////////////////////////////////
	//Player Lock functions
	//Call these to indicate that the player's ready with their actions
	void ResetPlayerLocks(){
		//Debug.Log("Logic Core: Resetting all Player Locks");
		for(int i = 0; i < this.playerLocks.Count; i++){
			this.playerLocks[i] = false;
			if(this.mnm.playerSlots[i]){
				this.mnm.playerSlots[i].RpcResetPlayerLock();
			}
		}
	}

	public void SetPlayerLock(int playerNum){
		//Debug.Log("LogicCore got setPlayerLock for player " + playerNum.ToString());
		this.playerLocks[playerNum] = true;
	}
	////////////////////////////////////

	/////////////////////////////////
	//Player response tracking functions
	void ResetRespTrack(){
		//Debug.Log("Logic Core: reset response tracker");
		for(int i = 0; i < this.playerResps.Count; i++){
			this.playerResps[i] = false;
		}
	}

	public void SetRespTrack(int playerNum){
		this.playerResps[playerNum] = true;
	}
	/////////////////////////////////

	void ResetGame(){
		// Set state of internal grids to default, send state to players
		//Fill in all state arrays, default is hidden
		this.currMS = MatchState.waitForPlayers;
		this.PB = new PlayBoard(this.sizex, this.sizey);
		this.ResetPlayerLocks();
		this.ResetRespTrack();
	}

	//Call this externally when we have both players connected
	public void StartGameProcess(){
		if (this.currMS == MatchState.waitForPlayers){
			Debug.Log("Told to start our GameProcess, and we were paused");
			this.currMS = this.pausedMS;
			this.pausedMS = MatchState.waitForPlayers;
			this.GameProcess();
		}
	}

	//Call this externally if we lose a player (like from my net manager)
	public void PauseGameProcess(){
		if (this.currMS != MatchState.waitForPlayers){
			Debug.Log("Told to pause our GameProcess");
			this.pausedMS = this.currMS;
			this.currMS = MatchState.waitForPlayers;
			this.GameProcess();
		}
	}
	
	//Our main game play process, this one's important
	void GameProcess(){
		switch(this.currMS){
		case MatchState.waitForPlayers:
			Debug.Log("We're entering waiting for players state");
			this.UpdatePlayersGameState();
			break;
		case MatchState.placeTowers:
			Debug.Log("We're entering PlaceTowers state!");
			this.stateTime = 60;
			this.PB.validator.SetAPC(ActionProcState.multiTower);
			this.ClearCurrentCoroutine();
			this.UpdatePlayersGameState();
			this.ResetRespTrack();
			this.ResetPlayerLocks();
			this.currentCoroutine = StartCoroutine(this.PlaceTowerIE(this.stateTime, MatchState.actionSelect));
			break;
		case MatchState.actionSelect:
			Debug.Log("We're entering actionSelect state!");
			this.stateTime = 60;
			this.PB.validator.SetAPC(ActionProcState.basicActions);
			this.ClearCurrentCoroutine();
			this.UpdatePlayersGameState();
			this.ResetRespTrack();
			this.ResetPlayerLocks();
			this.currentCoroutine = StartCoroutine(this.SingleInputIE(this.stateTime, MatchState.resolveState));
			break;
		case MatchState.resolveState:
			Debug.Log("We're entering resolveState state!");
			this.stateTime = 0; // Time here is 0, players don't need to know how long we're here since timer shouldnt get set
			this.ClearCurrentCoroutine();
			this.UpdatePlayersGameState();
			this.currentCoroutine = StartCoroutine(this.ResolveIE(1, MatchState.actionSelect));
			break;
		case MatchState.gameEnd:
			Debug.Log("We're entering gameEnd state!");
			this.stateTime = 0;
			this.ClearCurrentCoroutine();
			this.UpdatePlayersGameState();
			//What next?
			break;
		default:
			Debug.LogError("Don't expect to hit default state in LogicCore's GameProcess, State: " + this.currMS.ToString());
			break;
		}
	}

	void ClearCurrentCoroutine(){
		if(this.currentCoroutine != null){
			StopCoroutine(this.currentCoroutine);
			this.currentCoroutine = null;
		}
	}

	void UpdatePlayersGameState(){
		for(int i = 0; i < this.pNum; i++){
			if(this.mnm.playerSlots[i]){
				this.ReportGameState(i);
			}
		}
	}

	///////////////////////////////////////////////////State IE's
	//These State IE's control the logic core's states. Each state transition should clear and then
	//kick off a new IE that will later pump GameProcess()

	//IE state for initial tower placement
	IEnumerator PlaceTowerIE(int time, MatchState nextState){
		float currTime = time;
		const float interval = 0.1f;
		while (currTime >= 0){ // Wait for locks first
			// if((int)currTime%10 == 0 || (int)currTime <= 5){
			// 	Debug.Log(this.currMS.ToString() + " :testIE time left: " + currTime.ToString());
			// }
			if(this.playerLocks.All(x => x)){
				Debug.Log("All " + this.playerLocks.Count() + " of our locks are true!");
				break;
			}
			yield return new WaitForSeconds(interval);
			currTime -= interval;
			this.stateTime = (int)currTime;
		}
		//So our locks have been set or we ran out of time, now we wait for input
		this.stateTime = 0; // If a player asks for time, tell them 0 after locks are rx'd
		currTime = 5; //Now get responses for a bit
		this.GetActionReqs();
		while(currTime >= 0){ // Then wait for responses to GetActionReqs
			// if((int)currTime%10 == 0 || currTime <= 5){
			// 	Debug.Log(this.currMS.ToString() + ":testIE time left: " + currTime.ToString());
			// }
			if (this.playerResps.All(x => x)){
				Debug.Log("All " + this.playerResps.Count() + " of our responses are in!");
				break;
			}
			this.GetActionReqs(); // This is ok to call multiple times like this. Input rx is gated to 1 response before reset
			yield return new WaitForSeconds(interval);
			currTime -= interval;
		}
		if (this.playerResps.All(x => x)){ // Nice, we've got all of our input we need
			this.EvalActions();
		}
		else{ // Uhoh, we didn't get input there ... Place tower state needs that. What do we do here? TODO
			Debug.Log("Don't have all responses yet! " + this.playerResps[0] + this.playerResps[1]);
			//Pause game state? Kick out player who didn't give input? Place random towers for that player?
		}
		this.currMS = nextState;
		this.GameProcess();
	}

	//This waits for both players to lock in their single action each turn then evals those actions
	IEnumerator SingleInputIE(int time, MatchState nextState){
		float currTime = time;
		const float interval = 0.1f;
		while(currTime >=0){ // Wait for locks
			if(this.playerLocks.All(x => x)){
				Debug.Log("All " + this.playerLocks.Count() + " of our locks are true!");
				break;
			}
			yield return new WaitForSeconds(interval);
			currTime -=interval;
			this.stateTime = (int)currTime;
		}
		//So our locks have been set or we ran out of time, now we wait for input
		this.stateTime = 0;
		currTime = 5;
		this.GetActionReqs();
		while(currTime >= 0){ // Then wait for responses to GetActionReqs
			if (this.playerResps.All(x => x)){
				Debug.Log("All " + this.playerResps.Count() + " of our responses are in!");
				break;
			}
			this.GetActionReqs(); // This is ok to call multiple times like this. Input rx is gated to 1 response before reset
			yield return new WaitForSeconds(interval);
			currTime -= interval;
		}
		if (this.playerResps.All(x => x)){ // Nice, we've got all of our input we need
			this.EvalActions();
		}
		else{ // TODO what do we do here?
			Debug.Log("Don't have all responses yet! " + this.playerResps[0] + this.playerResps[1]);
		}
		this.currMS = nextState;
		this.GameProcess();
	}

	//This is just a temporary small timer for resolving animations on clients before the next turn starts
	IEnumerator ResolveIE(int time, MatchState nextState){
		//For now just sleep for a bit then go to next state
		yield return new WaitForSeconds(time);
		if (this.GameOverCheck()){
			this.currMS = MatchState.gameEnd;
		}
		else{
			this.currMS = nextState;
		}
		this.GameProcess();
	}
	////////////////////////////////////////////////End state IEs

	bool GameOverCheck(){
		Debug.Log("GameOverChecking");
		bool[] playerlose = {true, true};
		playerlose[0] = this.PB.CheckPlayerLose(0);
		playerlose[1] = this.PB.CheckPlayerLose(1);
		for(int i = 0; i < playerlose.Length; i++){
			if(playerlose[i]){
				this.playerWin[(i + 1) % this.pNum] = true;
			}
		}
		if (playerWin.Any(x => x)){
			Debug.Log("Hey!! We got a Winner over here! P0: " + playerWin[0].ToString() + " P1: " + playerWin[1].ToString());
			return true;
		}
		Debug.Log("Nope, both players still have at least 1 tower");
		return false;
	}

	void GetActionReqs(){
		for(int i = 0; i < this.pNum; i++){
			//Debug.Log("GetActionReqs: i = " + i.ToString());
			if (!this.playerResps[i]){
				this.mnm.playerSlots[i].RpcReportActionReqs();
			}
		}
	}

	//TODO how do we verify that this request set actually came from player x?
	public void RXActionReq(int playerId, List<ActionReq> reqs){
		Debug.Log("LogicCore RXActionReq: Got input action reqs");
		// for(int i =0; i < reqs.Count; i ++){
		// 	Debug.Log("Gotem " + i.ToString() + ": " + reqs[i].coords[0].ToString());
		// }
		//Allow the player to input no actions, but don' process
		if (!(reqs.Count > 0)){ // boom, we've got at least 1 req
			Debug.LogWarning("RXActionReq: We recieved an empty request list");
			this.SetRespTrack(playerId);
			return;
		}
		if (!reqs.All(x => x.p == playerId)){ // Verify that all player reqs are for the same player
			Debug.LogError("RXActionReq: Not all requests match player num: " + playerId.ToString());
			return;
		}
		if(this.playerResps[playerId]){
			Debug.Log("RXActionReq: Player input already recieved, ignore: " + playerId.ToString());
			return;
		}
		//At this point we're satisfied, say we've recieved a player responses and add them
		this.SetRespTrack(playerId);
		this.playerActions.AddRange(reqs);
		Debug.Log("Added to playerActions. Count now " + this.playerActions.Count);
	}

	//Called by logic core after eval actions
	//Called by pcobj on start
	public void ReportGridState(int p){
		Debug.Log("Reporting Grid states to player '" + p + "'");
		CellStruct[][,] state = this.PB.GetPlayerGameState(p, false);
		CellStruct[] pOwnGrid = GUtils.Serialize(state[0]);
		CellStruct[] pOtherGrid = GUtils.Serialize(state[1]);
		List<ActionAvail> aaList=  this.PB.GetActionAvailable(p);
		this.mnm.playerSlots[p].RpcUpdateGrids(pOwnGrid, pOtherGrid, this.sizex, this.sizey, aaList.ToArray());
	}

	//This guy is public for a reason (unlike most of my public stuff...) Player objs can request the game state
	//Called by
	public void ReportGameState(int p){
		Debug.Log("Reporting game state to player " + p.ToString());
		if (this.mnm.playerSlots[p]){
			this.mnm.playerSlots[p].RpcUpdateGameState(new StateInfo(this.currMS, this.stateTime, this.playerWin[p]));
		}
	}

	void EvalActions(){
		//Evaluate Actions
		this.PB.ApplyValidActions(this.playerActions);
		//Then clear Actions
		this.playerActions.Clear();
		//Now forward the results on to the world
		//This would be nice to iterate over, but there's only two players for now
		Debug.Log("Pushing updates to players");
		for (int i = 0; i < this.pNum; i++){
			this.ReportGridState(i);
		}
	}
}
