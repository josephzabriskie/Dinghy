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
	CState [][,] pOwn;
	CState[][,] pOther;
	int pNum = 2;

	PlayBoard pb;
	// Grid size right now is hard coded. Should be passed in from the main menu
	//Based on playboard sclae x:8, y:15
	int sizex = 8;
	int sizey = 8;


	// Use this for initialization
	void Start () {
		//this.playerActions = new ActionReq[this.pNum]{new ActionReq(0, pAction.noAction, new Vector2[]{}), new ActionReq(-1, pAction.noAction, new Vector2[]{})};
		this.playerActions = new ActionReq[this.pNum];
		this.IntializeGame();
	}

	void IntializeGame(){
		// Set state of internal grids to default, send state to players
		//Fill in all state arrays, default is hidden
		this.pOwn = new CState[this.pNum][,];
		this.pOther = new CState[this.pNum][,];
		for(int i = 0; i < this.pNum; i++){
			this.pOwn[i] = new CState[this.sizex, this.sizey];
			//Reveal player's own grid
			FillGrid(this.pOwn[i], CState.empty);
			this.pOther[i] = new CState[this.sizex, this.sizey];
		}
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
		this.mnm.playerSlots[player].RpcUpdateGrids(GridUtils.Serialize(this.pOwn[player]), GridUtils.Serialize(this.pOther[player]), this.sizex, this.sizey);
	}

	void EvalActions(){
		//Evaluate Actions
		Debug.Log("Processing Actions");
		for(int i = 0; i < this.pNum; i++){
			switch(this.playerActions[i].a){
				case pAction.noAction:
					Debug.Log("Player " + i.ToString() + " gave us a 'noAction' request, do nothing");
					break;
				case pAction.placeTower:
					Debug.Log("Player " + i.ToString() + " gave us a 'placeTower' request, do it");
					Vector2 coord = this.playerActions[i].coords[0];
					this.pOwn[i][(int)coord.x, (int)coord.y] = CState.tower;
					break;
				default:
					Debug.LogError("unrecognized action?? " + this.playerActions[i].a.ToString());
					break;
			}
			//Then clear Actions
			this.playerActions[i] = new ActionReq(i, pAction.noAction, null);
		}
		//Now forward the results on to the world
		//This would be nice to iterate over, but there's only two players for now
		Debug.Log("Pushing updates to players");
		for (int i = 0; i < this.pNum; i++){
			this.mnm.playerSlots[i].RpcUpdateGrids(GridUtils.Serialize(this.pOwn[i]), GridUtils.Serialize(this.pOther[i]), this.sizex, this.sizey);
		}
	}

	//public void 
}
