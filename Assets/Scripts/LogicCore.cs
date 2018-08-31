using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using CellInfo;
using PlayerActions;

namespace PlayerActions{
	public enum pAction{
		noAction,
		placeTower,
		fireBasic
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

//Logic core's job is to take action requests as inputs and then compute the new game state
public class LogicCore : NetworkBehaviour {
	public MyNetManager mnm;

	public PlayerConnectionObj[] playersObjs;
	public ActionReq[] playerActions;
	CState[,] p0own; // player 1's own grid
	CState[,] p0other; // player 1's enemy grid (what they see of it)
	CState[,] p1own; // player 2's own grid
	CState[,] p1other; // player 2's enemy grid (what they see of it)
	//CState [][,] pOwn;
	//CState[][,] pOther;
	int pNum = 2;

	PlayBoard pb;
	// Grid size right now is hard coded. Should be passed in from the main menu
	//Based on playboard sclae x:8, y:15
	int sizex = 8;
	int sizey = 8;


	// Use this for initialization
	void Start () {
		this.playerActions = new ActionReq[2]{new ActionReq(0, pAction.noAction, new Vector2[]{}), new ActionReq(1, pAction.noAction, new Vector2[]{})};
		this.IntializeGame();
	}

	void IntializeGame(){
		// Set state of internal grids to default, send state to players
		//Fill in all state arrays, default is hidden
		// for(int i = 0; i < this.pNum; i++){
		// 	this.pOwn[i] = new CState[this.sizex, this.sizey];
		// 	FillGrid(this.pOwn[i], CState.empty);
		// 	this.pOther[i] = new CState[this.sizex, this.sizey];
		// }

		this.p0own = new CState[this.sizex, this.sizey];
		this.p0other = new CState[this.sizex, this.sizey];
		this.p1own = new CState[this.sizex, this.sizey];
		this.p1other = new CState[this.sizex, this.sizey];
		//reveal players own grid
		FillGrid(this.p0own, CState.empty);
		FillGrid(this.p1own, CState.empty);
	}

	// helper function that I'd like to make local, but this version of c# doesn't support that :(
	void FillGrid(CState[,] grid, CState s){
		Debug.Log("Filling grid with: " + s.ToString());
		for (int i = 0; i < this.sizex; i++){
			for (int j = 0; j < this.sizey; j++){
				grid[i,j] = s;
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		bool ready = true;
		for(int i = 0; i < 2; i++){
			if (this.playerActions[i].a == pAction.noAction)
				ready = false;
		}
		if (ready){
			Debug.Log("We see that we've got two non empty action requests, process them");
			this.EvalActions();
		}
	}

	public void RXActionReq(ActionReq req){
		Debug.Log("Registering player " + req.p.ToString() + " action. Process when we have both");
		this.playerActions[req.p] = req;			
	}

	public void ReportGridState(int player){
		Debug.Log("Reporting Grid states to player '" + player + "'");
		if (player == 0){
			this.mnm.playerSlots[player].RpcUpdateGrids(GridUtils.Serialize(this.p0own), GridUtils.Serialize(this.p0other), this.sizex, this.sizey);
		}
		else if(player == 1){
			this.mnm.playerSlots[player].RpcUpdateGrids(GridUtils.Serialize(this.p1own), GridUtils.Serialize(this.p1other), this.sizex, this.sizey);
		}
		else{
			Debug.LogError("Player unsupported: " + player);
		}
	}

	void EvalActions(){
		//Evaluate Actions
		Debug.Log("Processing Actions");
		for(int i = 0; i < this.playerActions.Length; i++){
			switch(this.playerActions[i].a){
				case pAction.noAction:
					Debug.Log("Player " + i.ToString() + " gave us a 'noAction' request, do nothing");
					break;
				case pAction.placeTower:
					
					break;
				default:
					Debug.LogError("unrecognized action?? " + this.playerActions[i].a.ToString());
					break;
			}
		}
		//Then clear Actions
		for(int i = 0; i < 2; i++){
			this.playerActions[i] = new ActionReq(i, pAction.noAction, null);
		}
		//Now forward the results on to the world
		//This would be nice to iterate over, but there's only two players for now
		Debug.Log("Pushing updates to players");
		//Player1

		this.mnm.playerSlots[0].RpcUpdateGrids(GridUtils.Serialize(this.p0own), GridUtils.Serialize(this.p0other), this.sizex, this.sizey);
		//Player2
		this.mnm.playerSlots[1].RpcUpdateGrids(GridUtils.Serialize(this.p1own), GridUtils.Serialize(this.p1other), this.sizex, this.sizey);
	}

	//public void 
}
