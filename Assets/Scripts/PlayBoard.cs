using CellTypes;
using PlayerActions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PlayboardTypes{
	public class PlayBoard {

		const int playercnt = 2; //So far we're only designing for 2 players
		Cell[][,] cells;
		int sizex;
		int sizey;
		
		public PlayBoard(int sizex, int sizey){
			Debug.Log("Hey, I'm making a Playboard: " + sizex.ToString() + "X" + sizey.ToString());
			this.sizex = sizex;
			this.sizey = sizey;
			this.InitializeCells();
		}

		void InitializeCells(){
			//Clear out all cells if they exist TODO
			this.cells = new Cell[playercnt][,];
			for(int p = 0; p < playercnt; p++){
				this.cells[p] = new Cell[this.sizex, this.sizey];
				for(int x = 0; x < this.sizex; x++){
					for(int y = 0; y < this.sizey; y++){
						this.cells[p][x,y] = new Cell(CState.empty, p, new Vector2Int(x,y), this);
					}
				}
			}
		}
		//////////////////////Public functions for logic core calls
		//Return value will always put requesting player's grid in idx 0, enemy grid in idx 1
		public CState[][,] GetPlayerGameState(int playerIdx){
			int enemyIdx = (playerIdx + 1) % playercnt;
			CState[][,] boardOut = new CState[playercnt][,];
			boardOut[0] = this.GetGridSide(playerIdx); //playerGrid
			boardOut[1] = this.GetGridSide(enemyIdx); //enemyGrid
			return boardOut; 
		}
		//Used only to help out GetPlayerGameState
		CState[,] GetGridSide(int idx){
			CState [,] gridOut = new CState[sizex,sizey];
			for(int x = 0; x < this.sizex; x++){
				for(int y = 0; y < this.sizey; y++){
					gridOut[x,y] = this.cells[idx][x,y].state;
				}
			}
			return gridOut;
		}

		public bool CheckPlayerLose(int p){
			List<CState> s = new List<CState>(){CState.towerOffence, CState.towerDefence, CState.towerIntel};
			Debug.Log("GameOverChecking for player: " + p.ToString());
			bool playerlose = true;
			for(int x = 0; x < this.sizex; x++){ //TODO replace these nested loops with a foreach (think that should work on jagged array)
				for(int y = 0; y < this.sizey; y++){
					if (s.Contains(this.cells[p][x,y].state)){
						playerlose = false; // as long as they have one tower, they're still in it!
					}
				}
			}
			return playerlose;
		}

		public void ApplyActions(List<ActionReq> ars){
			Debug.Log("PlayBoard processing Actions. Got " + ars.Count);
			int i = 0;
			foreach (ActionReq ar in ars){
				Debug.Log("Handling action: " + i.ToString() + ", " + ar.ToString());
				i++;
				int enemyIdx = (ar.p + 1) % playercnt;
				//Make sure that each action only exists once in these lists
				List<pAction> buildActions = new List<pAction>(){pAction.buildOffenceTower, pAction.buildDefenceTower, pAction.buildIntelTower, pAction.buildWall};
				List<pAction> shootActions = new List<pAction>(){pAction.fireBasic};
				List<pAction> scoutActions = new List<pAction>(){pAction.scout};
				if(buildActions.Contains(ar.a)){
					this.GetCell(ar.t, new Vector2Int((int)ar.coords[0].x,(int)ar.coords[0].y)).onBuild(ar);
				}
				else if (shootActions.Contains(ar.a)){
					this.GetCell(ar.t, new Vector2Int((int)ar.coords[0].x,(int)ar.coords[0].y)).onShoot(ar);
				}
				else if (scoutActions.Contains(ar.a)){
					this.GetCell(ar.t, new Vector2Int((int)ar.coords[0].x,(int)ar.coords[0].y)).onScout(ar);
				}
				else{
					Debug.LogError("Unhandled Player request!  " + ar.a.ToString());
				}
			}
		}
		
		Cell GetCell(int targetPlayer, Vector2Int loc){ // TODO use this instead of 'this.cells[p][x,y]'
			return this.cells[targetPlayer][loc.x, loc.y];
		}
		///////////////////Public functions for Cell calls
		public void SetCellState(int p, Vector2Int loc, CState state){
			if (!this.CheckLocInRange(loc))
				return;
			Debug.Log("PB: SetCell " + loc.x.ToString() + "," + loc.y.ToString() + " to " + state.ToString());
			this.GetCell(p, loc).ChangeState(state);
		}

		public void AddCellCallback(int p, Vector2Int loc, PriorityCB cb, PCBType cbt){
			if (!this.CheckLocInRange(loc))
				return;
			Debug.Log("PB: AddCellCallback: loc: " + loc.ToString() + ", type: " + cbt.ToString());
			this.GetCell(p, loc).AddCB(cb, cbt);
		}
		
		//Cells will call these to add CBs to other cells
		public void RemCellCallback(int p, Vector2Int loc, PriorityCB cb, PCBType cbt){
			if (!this.CheckLocInRange(loc))
				return;
			Debug.Log("PB: RemCellCallback: loc: " + loc.ToString() + ", type: " + cbt.ToString());
			this.GetCell(p, loc).RemCB(cb, cbt);
		}

		bool CheckLocInRange(Vector2Int loc){
			return loc.x >= 0 && loc.x < this.sizex && loc.y >= 0 && loc.y < this.sizey;
		}
	}
}