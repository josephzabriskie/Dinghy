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
		placeTowers, // Players will have 1m to select their tower starting positions
		actionSelect, 
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
		Debug.Log("Starting Logic Core!");
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
		}
	}

	public void SetPlayerLock(int playerNum){
		Debug.Log("LogicCore got setPlayerLock for player " + playerNum.ToString());
		this.playerLocks[playerNum] = true;
	}
	////////////////////////////////////

	/////////////////////////////////
	//Player response tracking functions
	void RessetRespTrack(){
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
		this.RessetRespTrack();
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

	//Call this externally if we lose a player
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
		bool done = false;
		while(!done){
			switch(this.currMS){
			case MatchState.waitForPlayers:
				Debug.Log("We're entering waiting for players state");
				this.UpdatePlayersGameState();
				done = true;
				break;
			case MatchState.placeTowers:
				Debug.Log("We're entering PlaceTowers state!");
				this.UpdatePlayersGameState();
				StartCoroutine(this.PlaceTowerIE(15, MatchState.actionSelect));
				done = true;
				break;
			case MatchState.actionSelect:
				Debug.Log("We're entering actionSelect state!");
				this.UpdatePlayersGameState();
				this.ResetPlayerLocks();
				done = true;
				break;
			default:
				Debug.LogError("Don't expect to hit default state in LogicCore's GameProcess, State: " + this.currMS.ToString());
				done = true;
				break;
			}
		}
	}

	void UpdatePlayersGameState(){
		for(int i = 0; i < this.pNum; i++){
			if(this.mnm.playerSlots[i]){
				this.ReportGameState(i);
			}
		}
	}

	public void ReportGameState(int p){
		Debug.Log("Reporting game state to player " + p.ToString());
		if (this.mnm.playerSlots[p]){
			this.mnm.playerSlots[p].RpcUpdateGameState(this.currMS);
		}
	}

	IEnumerator PlaceTowerIE(int time, MatchState nextState)
	{
		float currTime = time;
		while (currTime >= 0)
		{
			if((int)currTime%10 == 0 || (int)currTime <= 5){
				Debug.Log(this.currMS.ToString() + " :testIE time left: " + currTime.ToString());
			}
			if(this.playerLocks.All(x => x)){
				Debug.Log("All of our locks are true!");
				break;
			}
			// else{
			// 	Debug.Log("PlayerLocks Currently " + this.playerLocks[0].ToString() + " " + this.playerLocks[1].ToString());
			// }
			yield return new WaitForSeconds(1.0f);
			currTime -= 1.0f;
		}
		//So our locks have been set, now we wait for input
		currTime = 10;
		this.GetActionReqs();
		while(currTime >= 0){
			if((int)currTime%10 == 0 || currTime <= 5){
				Debug.Log(this.currMS.ToString() + ":testIE time left: " + currTime.ToString());
			}
			if (this.playerResps.All(x => x)){
				Debug.Log("All of our responses are in!");
				break;
			}
			yield return new WaitForSeconds(0.1f);
			currTime -= 0.1f;
		}
		if (this.playerResps.All(x => x)){ // Nice, we've got all of our input we need
			this.EvalActions();
			this.currMS = nextState;
			this.GameProcess();
		}
		else{ // Uhoh, we didn't get input there ... Place tower state needs that. What do we do here?
			Debug.Log("Don't have all responses yet! " + this.playerResps[0] + this.playerResps[1]);
			//Pause game state?
			//Kick out player who didn't give input?
			//Place random towers for that player?
		}
	}

	// Update is called once per frame
	void Update () {

	}

	void GetActionReqs(){
		for(int i = 0; i < this.pNum; i++){
			Debug.Log("GetActionReqs: i = " + i.ToString());
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
					int enemyIdx = (currentAR.p + 1) % this.pNum;
					coord = currentAR.coords[0];
					this.pHidden[enemyIdx][(int)coord.x, (int)coord.y] = false;
					break;
				default:
					Debug.LogError("unrecognized action?? " + currentAR.a.ToString());
					break;
			}
		}
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
