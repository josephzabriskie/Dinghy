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

//Logic core's job is to take action requests as inputs and then compute the new game state
public class LogicCore : NetworkBehaviour {
	public MyNetManager mnm;

	public PlayerConnectionObj[] playersObjs;
	public ActionReq[] playerActions;
	CState [][,] pGrid;
	bool[][,] pHidden;
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
		this.pGrid = new CState[this.pNum][,];
		this.pHidden = new bool[this.pNum][,];
		for(int i = 0; i < this.pNum; i++){
			this.pGrid[i] = new CState[this.sizex, this.sizey];
			GUtils.FillGrid(this.pGrid[i], CState.empty);
			this.pHidden[i] = new bool[this.sizex, this.sizey];
			GUtils.FillBoolGrid(this.pHidden[i], true);
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

	public void ReportGridState(int p){
		Debug.Log("Reporting Grid states to player '" + p + "'");
		int enemyIdx = (p + 1) % this.pNum; // Calculate the other player index, in our case we know it's 1/0, this may have to change if more players...
		CState[] pOwnGrid = GUtils.Serialize(this.pGrid[p]);
		CState[] pOtherGrid = GUtils.Serialize(GUtils.ApplyHiddenMask(this.pGrid[enemyIdx], this.pHidden[enemyIdx]));
		this.mnm.playerSlots[p].RpcUpdateGrids(pOwnGrid, pOtherGrid, this.sizex, this.sizey);
	}

	void EvalActions(){
		//Evaluate Actions
		Debug.Log("Processing Actions");
		for(int i = 0; i < this.pNum; i++){
			Vector2 coord;
			switch(this.playerActions[i].a){
				case pAction.noAction:
					Debug.Log("Player " + i.ToString() + " gave us a 'noAction' request, do nothing");
					break;
				case pAction.placeTower:
					Debug.Log("Player " + i.ToString() + " gave us a 'placeTower' request, do it");
					coord = this.playerActions[i].coords[0];
					this.pGrid[i][(int)coord.x, (int)coord.y] = CState.tower;
					break;
				case pAction.scout:
					Debug.Log("Player " + i.ToString() + " gave us a 'scout' request, do it");
					int enemyIdx = (i + 1) % this.pNum;
					coord = this.playerActions[i].coords[0];
					this.pHidden[enemyIdx][(int)coord.x, (int)coord.y] = false;
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
			this.ReportGridState(i);
		}
	}

	//public void 
}
