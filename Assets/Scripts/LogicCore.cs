using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using CellInfo;
using PlayerActions;
using MatchSequence;
using System.Linq;

namespace PlayerActions{
	public enum pAction{
		noAction,
		placeTower,
		fireBasic,
		scout,
	}

	public struct ActionReq
	{
		public int p; //player number
		public pAction a;
		public Vector2[] coords;

		public ActionReq(int inPlayer, pAction inAction, Vector2[] inCoords)
		{
			p=inPlayer;
			a=inAction;
			coords=inCoords;
		}
	}
}

namespace MatchSequence{
	//Feel free to divide these down as needed (like place towers in to pre, mid, post place towers)
	public enum MatchState{
		waitForPlayers,
		placeTowers, // Players will have an amount of time to select their tower starting positions
		actionSelect, // Players will have an amount of time to select their next action
		resolveState, // Give the clients some time to resolve game state before next action
		gameEnd,
	}
}

//Logic core's job is to take action requests as inputs and then compute the new game state
public class LogicCore : NetworkBehaviour {
	public MyNetManager mnm;

	public PlayerConnectionObj[] playersObjs;
	public List<ActionReq> playerActions;
	public List<bool> playerLocks;
	public List<bool> playerResps; // Keeps track of which players have send in action requests since we last cleared
	public MatchState currMS = MatchState.waitForPlayers;
	public MatchState pausedMS = MatchState.placeTowers;
	public int stateTime;
	CState [][,] pGrid;
	bool[][,] pHidden;
	int pNum = 2; // max player number
	Coroutine currentCoroutine = null;

	PlayBoard pb;
	// Grid size right now is hard coded. Should be passed in from the main menu
	//Based on playboard scale x:8, y:17
	int sizex = 8;
	int sizey = 8;


	// Use this for initialization
	void Start () {
		//this.playerActions = new ActionReq[this.pNum]{new ActionReq(0, pAction.noAction, new Vector2[]{}), new ActionReq(-1, pAction.noAction, new Vector2[]{})};
		//Debug.Log("Starting Logic Core!");
		this.playerActions = new List<ActionReq>();
		this.playerLocks = new List<bool>(new bool[this.pNum]);
		this.playerResps = new List<bool>(new bool[this.pNum]);
		this.ResetGame();
	}
	
	/////////////////////////////////
	//Player Lock functions
	//Call these to indicate that the player's ready with their actions
	void ResetPlayerLocks(){
		for(int i = 0; i < this.playerLocks.Count; i++){
			this.playerLocks[i] = false;
			if(this.mnm.playerSlots[i]){
				this.mnm.playerSlots[i].RpcResetPlayerLock();
			}
		}
	}

	public void SetPlayerLock(int playerNum){
		Debug.Log("LogicCore got setPlayerLock for player " + playerNum.ToString());
		this.playerLocks[playerNum] = true;
	}
	////////////////////////////////////

	/////////////////////////////////
	//Player response tracking functions
	void ResetRespTrack(){
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
		this.pGrid = new CState[this.pNum][,];
		this.pHidden = new bool[this.pNum][,];
		this.ResetPlayerLocks();
		this.ResetRespTrack();
		for (int i = 0; i < this.pNum; i++){
			this.pGrid[i] = new CState[this.sizex, this.sizey];
			GUtils.FillGrid(this.pGrid[i], CState.empty);
			this.pHidden[i] = new bool[this.sizex, this.sizey];
			GUtils.FillBoolGrid(this.pHidden[i], true);
		}
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
			this.ClearCurrentCoroutine();
			this.UpdatePlayersGameState();
			this.ResetRespTrack();
			this.ResetPlayerLocks();
			this.currentCoroutine = StartCoroutine(this.PlaceTowerIE(this.stateTime, MatchState.actionSelect));
			break;
		case MatchState.actionSelect:
			Debug.Log("We're entering actionSelect state!");
			this.stateTime = 30;
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
			this.currentCoroutine = StartCoroutine(this.ResolveIE(3, MatchState.actionSelect));
			break;
		case MatchState.gameEnd:
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

	//This guy is public for a reason (unlike most of my public stuff...) Player objs can request the game state
	public void ReportGameState(int p){
		Debug.Log("Reporting game state to player " + p.ToString());
		if (this.mnm.playerSlots[p]){
			this.mnm.playerSlots[p].RpcUpdateGameState(this.currMS, this.stateTime);
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
		this.currMS = nextState;
		this.GameOverCheck();
		this.GameProcess();
	}
	////////////////////////////////////////////////End state IEs

	bool GameOverCheck(){
		Debug.Log("GameOverChecking");
		bool[] playerLoss = {true, true};
		for(int p = 0; p < this.pNum; p++){
			for(int x = 0; x < this.pGrid[p].GetLength(0); x++){
				for(int y = 0; y < this.pGrid[p].GetLength(1); y++){
					if (this.pGrid[p][x,y] == CState.tower){
						playerLoss[p] = false; // as long as they have one tower, they're still in it!
					}
				}
			}
		}
		if (playerLoss.Any(x => x)){
			Debug.Log("Hey!! We got a loser over here! P0: " + playerLoss[0].ToString() + " P1: " + playerLoss[1].ToString());
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

	public void RXActionReq(List<ActionReq> reqs){
		Debug.Log("LogicCore RXActionReq: Got input action reqs");
		// for(int i =0; i < reqs.Count; i ++){
		// 	Debug.Log("Gotem " + i.ToString() + ": " + reqs[i].coords[0].ToString());
		// }
		//First verify that there's any requests to add
		if (!(reqs.Count > 0)){ // boom, we've got at least 1 req
			Debug.LogError("RXActionReq: We recieved an empty request list");
			return;
		}
		int playernum = reqs[0].p;
		if (!reqs.All(x => x.p == playernum)){ // Verify that all player reqs are for the same player
			Debug.LogError("RXActionReq: Not all requests match player num: " + playernum.ToString());
			return;
		}
		if(this.playerResps[playernum]){
			Debug.Log("RXActionReq: Player input already recieved, ignore: " + playernum.ToString());
			return;
		}
		//At this point we're satisfied, say we've recieved a player responses and add them
		this.SetRespTrack(playernum);
		this.playerActions.AddRange(reqs);
		Debug.Log("Added to playerActions. Count now " + this.playerActions.Count);
	}

	public void ReportGridState(int p){
		Debug.Log("Reporting Grid states to player '" + p + "'");
		int enemyIdx = (p + 1) % this.pNum; // Calculate the other player index, in our case we know it's 1/0, this may have to change if more players...
		CState[] pOwnGrid = GUtils.Serialize(this.pGrid[p]);
		CState[] pOtherGrid = GUtils.Serialize(GUtils.ApplyHiddenMask(this.pGrid[enemyIdx], this.pHidden[enemyIdx]));
		this.mnm.playerSlots[p].RpcUpdateGrids(pOwnGrid, pOtherGrid, this.sizex, this.sizey);
	}

	void EvalActions(){
		//Evaluate Actions
		Debug.Log("Processing Actions. Got " + this.playerActions.Count);
		for(int i = 0; i < this.playerActions.Count; i++){
			Vector2 coord;
			ActionReq currentAR = this.playerActions[i];
			int enemyIdx = (currentAR.p + 1) % this.pNum;
			switch(currentAR.a){
				case pAction.noAction:
					Debug.Log("Player " + currentAR.p.ToString() + " gave us a 'noAction' request, do nothing");
					break;
				case pAction.placeTower://Use for beginning of game and normal single action
					Debug.Log("Player " + currentAR.p.ToString() + " gave us a 'placeTower' request, do it");
					coord = currentAR.coords[0];
					this.pGrid[currentAR.p][(int)coord.x, (int)coord.y] = CState.tower;
					break;
				case pAction.scout:
					Debug.Log("Player " + currentAR.p.ToString() + " gave us a 'scout' request, do it");
					coord = currentAR.coords[0];
					this.pHidden[enemyIdx][(int)coord.x, (int)coord.y] = false;
					break;
				case pAction.fireBasic:
					Debug.Log("Player " + currentAR.p.ToString() + " gave us a 'fireBasic' request, do it");
					coord = currentAR.coords[0];
					if (this.pGrid[enemyIdx][(int)coord.x,(int)coord.y] == CState.tower){
						this.pGrid[enemyIdx][(int)coord.x,(int)coord.y] = CState.destroyedTower;
					}
					else{
						this.pGrid[enemyIdx][(int)coord.x,(int)coord.y] = CState.destroyedTerrain;
					}
					this.pHidden[enemyIdx][(int)coord.x,(int)coord.y] = false;
					break;
				default:
					Debug.LogError("Unhandled action?? " + currentAR.a.ToString());
					break;
			}
		}
		//Then clear Actions
		this.playerActions.Clear();
		//Now forward the results on to the world
		//This would be nice to iterate over, but there's only two players for now
		Debug.Log("Dont Processing Pushing updates to players");
		for (int i = 0; i < this.pNum; i++){
			this.ReportGridState(i);
		}
	}
}
