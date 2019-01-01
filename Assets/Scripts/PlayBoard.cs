﻿using CellTypes;
using PlayerActions;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActionProc;

namespace PlayerActions{
	public enum pAction{
		noAction,
		buildTower, //Should be unused, place type of tower is new action
		buildOffenceTower,
		buildDefenceTower,
		buildIntelTower,
		buildWall,
		fireBasic, // Our every-round shot
		scout,
		fireAgain, // Our second shot (different reqs)
		fireRow, // this won't be broken
		fireSquare
	}
	public struct ActionReq	{
		public int p; //player number
		public int t; //target player number
		public pAction a;
		public Vector2[] loc;
		public ActionReq(int inPlayer, int targetPlayer, pAction inAction, Vector2[] inCoords){
			p=inPlayer;
			t=targetPlayer;
			a=inAction;
			loc=inCoords;
		}
		public override string ToString()
    	{
        	return "P: " + p + " T: " + t + " A: " + a + " loc: " + loc;
    	}
	}
}

namespace PlayboardTypes{

	public struct ActionParam{
		public int offenceCost; // offenceCost
		public int defenceCost; // defenceCost
		public int intelCost; // intelCost
		public int cooldown; // cooldown
		public int maxUses; // max uses
		public ActionParam(int offenceCost, int defenceCost, int intelCost, int cooldown, int maxUses){
			this.offenceCost = offenceCost;
			this.defenceCost = defenceCost;
			this.intelCost = intelCost;
			this.cooldown = cooldown; // 0 means no cooldown
			this.maxUses = maxUses; // 0 means no more uses, -1 means no limit
		}
		public override string ToString(){
			return String.Format("APM: o:{0}, d:{1}, i:{2}, cd:{3}, mu: {4}",
				this.offenceCost, this.defenceCost, this.intelCost, this.cooldown, this.maxUses);
		}
	}

	//This can be requested by a player so they know what the server thinks they can do.
	public struct ActionAvail{
		public pAction action;
		public bool available;
		public bool costsMet;
		public int cooldown;
		public int usesLeft;
		public ActionParam actionParam;
		public ActionAvail(pAction action, bool costsMet, int cooldown, int usesLeft, ActionParam actionParam){
			this.action = action;
			this.available = costsMet && cooldown == 0 && usesLeft != 0; //Just cal this for the player
			this.costsMet = costsMet;
			this.cooldown = cooldown;
			this.usesLeft = usesLeft;
			this.actionParam = actionParam;
		}
		public override string ToString(){
			return String.Format("Action Avail: a:{0}, av: {4} cm:{1}, cd: {2}, ul: {3}. APM {5}",
				this.action, this.costsMet, this.cooldown, this.usesLeft, this.available, this.actionParam);
		}
	}

	public class PlayerActionTracker{
		static List<pAction> allActions = ((pAction[])Enum.GetValues(typeof(pAction))).ToList(); // How to get all pActions defined in the enum!
		static Dictionary<pAction, ActionParam> actionParams = new Dictionary<pAction, ActionParam>{
										//	ActionParam(costs..cd..uses) Note: you can set uses to 0 if you want disabled
			{pAction.noAction, 			new ActionParam(0,0,0, 0, -1)},
			{pAction.buildTower, 		new ActionParam(0,0,0, 0, -1)},
			{pAction.buildOffenceTower, new ActionParam(0,0,0, 0, 7)},
			{pAction.buildDefenceTower, new ActionParam(0,0,0, 0, 7)},
			{pAction.buildIntelTower, 	new ActionParam(0,0,0, 0, 7)},
			{pAction.buildWall, 		new ActionParam(0,2,0, 2, -1)},
			{pAction.fireBasic, 		new ActionParam(0,0,0, 0, -1)},
			{pAction.scout, 			new ActionParam(0,0,2, 0, -1)},
			{pAction.fireAgain,			new ActionParam(3,0,0, 3, -1)},
			{pAction.fireRow,			new ActionParam(0,0,0, 0, -1)},
			{pAction.fireSquare,		new ActionParam(0,0,0, 0, -1)}
		};
		Dictionary<pAction, int> actionCooldowns; //Use for tracking cooldowns
		List<pAction> actionHistory; //Use for counting uses

		//Constructor
		public PlayerActionTracker(){
			foreach(pAction action in allActions){
				if(!actionParams.ContainsKey(action)){
					Debug.LogError("Action Param missing pAction: " + action.ToString());
				}
			}
			this.actionCooldowns = new Dictionary<pAction, int>();
			foreach(pAction action in allActions){
				this.actionCooldowns.Add(action, 0);
			}
			this.actionHistory = new List<pAction>();
		}

		static List<pAction> CheckCostsMet(CState[,] pGrid){
			int offenceCount = GUtils.Serialize(pGrid).Count(s => s == CState.towerOffence);
			int defenceCount = GUtils.Serialize(pGrid).Count(s => s == CState.towerDefence);
			int intelCount = GUtils.Serialize(pGrid).Count(s => s == CState.towerIntel);
			List<pAction> retList = new List<pAction>();
			foreach(pAction action in actionParams.Keys){
				ActionParam apm = actionParams[action];
				if(apm.offenceCost <= offenceCount && apm.defenceCost <= defenceCount && apm.intelCost <= intelCount){
					retList.Add(action);
				}
			}
			return retList;
		}

		int GetUseCount(pAction action){
			return this.actionHistory.Count(a => a == action);
		}

		int GetUsesLeft(pAction action){
			int actionUses = this.GetUseCount(action);
			if(actionParams[action].maxUses >= 0 && actionParams[action].maxUses < actionUses){
				Debug.LogError("Player used action: " + action.ToString() + " " + actionUses.ToString() + " times when max was " + actionParams[action].maxUses.ToString());
			}
			return actionParams[action].maxUses <= 0 ? -1 : actionParams[action].maxUses - actionUses; //Calc uses left (-1 means unlimited)
		}

		public bool HasUsesLeft(pAction action){
			int usesLeft = this.GetUsesLeft(action);
			return usesLeft != 0; // Less than zero we ignore, more than 0 means uses are left
		}
		
		public List<ActionAvail> GetActionAvailibility(CState[,] pGrid){
			List<ActionAvail> retList = new List<ActionAvail>();
			List<pAction> costsMetActions = CheckCostsMet(pGrid); // Check cost of actions met
			foreach(pAction action in allActions){
				retList.Add(new ActionAvail(action, costsMetActions.Contains(action), this.actionCooldowns[action], this.GetUsesLeft(action), actionParams[action]));
			}
			return retList;
		}

		//Validate Actions = input action list, output actionlist that meet costs, off cooldown, not over max use
		List<ActionReq> ValidateActions(List<ActionReq> inList, CState[,] pGrid){
			List<ActionReq> retList = new List<ActionReq>();
			List<pAction> costsMetActions = CheckCostsMet(pGrid); // Check cost of actions met
			foreach(ActionReq ar in inList){
				//Is cooldown 0, Is cost met, max uses not met yet?
				if (this.actionCooldowns[ar.a] == 0 && costsMetActions.Contains(ar.a) && this.HasUsesLeft(ar.a)){
					retList.Add(ar);
				}
				else{
					Debug.LogWarning("Action validation by tracker failed: " + ar.ToString());
				}
			}
			return retList;
		}

		//TrackActions = take list of action requests, record them (so we can see usage), update cooldowns
		void TrackActions(List<ActionReq> inList){
			List<pAction> keys = this.actionCooldowns.Keys.ToList();
			foreach(pAction action in keys){ //Update cooldowns first (had to do it this way since C# doesn't like any modification of list while iterating over keys)
				if (this.actionCooldowns[action] > 0){
					this.actionCooldowns[action] -= 1;
				}
			}
			foreach(ActionReq ar in inList){ //Track the action and set cooldown
				this.actionHistory.Add(ar.a);
				this.actionCooldowns[ar.a] = actionParams[ar.a].cooldown;
			}
		}

		//Validate and track actions = Just do the validate then track in sequence
		public List<ActionReq> ValidateAndTrackActions(List<ActionReq> inList, CState[,] pGrid){
			List<ActionReq> valActions = this.ValidateActions(inList, pGrid);
			this.TrackActions(valActions);
			return valActions;
		}
	}

	public class PlayBoard {

		const int playercnt = 2; //So far we're only designing for 2 players
		Cell[][,] cells;
		int sizex;
		int sizey;
		public Validator validator;
		PlayerActionTracker[] pats;
		
		public PlayBoard(int sizex, int sizey){
			//Debug.Log("Hey, I'm making a Playboard: " + sizex.ToString() + "X" + sizey.ToString());
			this.sizex = sizex;
			this.sizey = sizey;
			this.InitializeCells();
			validator = new Validator(ActionProcState.reject);
			pats = new PlayerActionTracker[playercnt]{new PlayerActionTracker(), new PlayerActionTracker()};
		}

		void InitializeCells(){
			//Clear out all cells if they exist TODO
			this.cells = new Cell[playercnt][,];
			for(int p = 0; p < playercnt; p++){
				this.cells[p] = new Cell[this.sizex, this.sizey];
				for(int x = 0; x < this.sizex; x++){
					for(int y = 0; y < this.sizey; y++){
						this.cells[p][x,y] = new Cell(CState.empty, p, new Vector2Int(x,y), this, false);
					}
				}
			}
		}
		//////////////////////Public functions for logic core calls
		//Return value will always put requesting player's grid in idx 0, enemy grid in idx 1
		public CState[][,] GetPlayerGameState(int playerIdx){
			int enemyIdx = (playerIdx + 1) % playercnt;
			CState[][,] boardOut = new CState[playercnt][,];
			boardOut[0] = this.GetGridSide(playerIdx, showAll:true); //playerGrid
			boardOut[1] = this.GetGridSide(enemyIdx); //enemyGrid
			return boardOut; 
		}
		//Used only to help out GetPlayerGameState
		CState[,] GetGridSide(int idx, bool showAll=false){
			CState [,] gridOut = new CState[sizex,sizey];
			for(int x = 0; x < this.sizex; x++){
				for(int y = 0; y < this.sizey; y++){
					gridOut[x,y] = cells[idx][x,y].GetState(showAll:showAll);
				}
			}
			return gridOut;
		}

		public bool CheckPlayerLose(int p){
			List<CState> s = new List<CState>(){CState.towerOffence, CState.towerDefence, CState.towerIntel};
			//Debug.Log("GameOverChecking for player: " + p.ToString());
			bool playerlose = true;
			for(int x = 0; x < this.sizex; x++){ //TODO replace these nested loops with a foreach (think that should work on jagged array)
				for(int y = 0; y < this.sizey; y++){
					if (s.Contains(this.cells[p][x,y].GetState(showAll:true))){
						playerlose = false; // as long as they have one tower, they're still in it!
					}
				}
			}
			return playerlose;
		}

		public List<ActionAvail> GetActionAvailable(int playerId){
			return this.pats[playerId].GetActionAvailibility(this.GetGridSide(playerId, showAll:true));
		}

		void IncrementCellCounters(){
			for(int p = 0; p < playercnt; p++){
				for(int x = 0; x < this.sizex; x++){
					for(int y = 0; y < this.sizey; y++){
						this.cells[p][x,y].IncrementCounters();
					}
				}
			}
		}


		//We expect this to be called once per round. We'll update the ticking elements at the end of this func
		public void ApplyActions(List<ActionReq> ars){
			Debug.Log("PlayBoard processing Actions. Got " + ars.Count);
			List<ActionReq> validARs = new List<ActionReq>();
			foreach(ActionReq ar in ars){ // Trust no one, validate it allllll
				if (this.validator.Validate(ar, this.GetGridSide(ar.p, showAll:true), this.GetGridSide((ar.p + 1) % playercnt), new Vector2(sizex, sizey))){
					validARs.Add(ar);
					Debug.Log("validated ar! :) " + ar.ToString());
				}
				else{
					Debug.LogWarning("Got bad AR, dont' use: " + ar.ToString());
				}
			}
			ars = validARs;
			//Give the action tracker these actions! It can remove actions that it deems illegal as well
			//Doing this for each player is kinda clunky, TODO revisit this section
			List<ActionReq> player0ARs = ars.Where(ar => ar.p == 0).ToList();
			List<ActionReq> player1ARs = ars.Where(ar => ar.p == 1).ToList();
			player0ARs = this.pats[0].ValidateAndTrackActions(player0ARs, this.GetGridSide(0, showAll:true));
			player1ARs = this.pats[1].ValidateAndTrackActions(player1ARs, this.GetGridSide(1, showAll:true));
			ars.Clear();
			ars.AddRange(player0ARs);
			ars.AddRange(player1ARs);
			//To CodeMonkey: each action must appear no more than once in these lists
			List<pAction> buildActions = new List<pAction>(){pAction.buildOffenceTower, pAction.buildDefenceTower, pAction.buildIntelTower, pAction.buildWall};
			List<pAction> shootActions = new List<pAction>(){pAction.fireBasic, pAction.fireAgain, pAction.fireRow, pAction.fireSquare};
			List<pAction> scoutActions = new List<pAction>(){pAction.scout};
			//Here we order the list to make sure that building happens first
			var buildARs = ars.Where(ar => buildActions.Contains(ar.a));
			var otherARs = ars.Where(ar => !buildARs.Contains(ar));
			ars = buildARs.Concat(otherARs).ToList();
			//Now we need to expande certain actions, usually ones that will trigger onXXXX() in many cells
			ars = this.ActionExpansion(ars);
			foreach (ActionReq ar in ars){
				if(buildActions.Contains(ar.a)){
					this.GetCell(ar.t, new Vector2Int((int)ar.loc[0].x,(int)ar.loc[0].y)).onBuild(ar);
				}
				else if (shootActions.Contains(ar.a)){
					this.GetCell(ar.t, new Vector2Int((int)ar.loc[0].x,(int)ar.loc[0].y)).onShoot(ar);
				}
				else if (scoutActions.Contains(ar.a)){
					this.GetCell(ar.t, new Vector2Int((int)ar.loc[0].x,(int)ar.loc[0].y)).onScout(ar);
				}
				else{
					Debug.LogError("Unhandled Player request!  " + ar.a.ToString());
				}
			}
			//Now update the timed parameters of each cell
			this.IncrementCellCounters();
		}

		List<ActionReq> ActionExpansion(List<ActionReq> inList){
			//Some ARs need to be expanded to multiple cell onXXXX calls, we do this after we've validated and tracked the ARs
			List<ActionReq> ret = new List<ActionReq>();
			foreach(ActionReq ar in inList){
				switch(ar.a){
				case pAction.fireRow:
					for(int x = 0; x < this.sizex; x++){
						ret.Add(new ActionReq(ar.p, ar.t, ar.a, new Vector2[]{ new Vector2(x,(int)ar.loc[0].y)}));
					}
					break;
				case pAction.fireSquare:
					List<Vector2> locs = new List<Vector2>();
					for(int x = -1; x < 2; x+=2){
						for(int y = -1; y < 2; y+=2){
							locs.Add(new Vector2(ar.loc[0].x + x, ar.loc[0].y + y));
						}
					}
					foreach(Vector2 loc in locs){
						if(this.CheckLocInRange(new Vector2Int((int)loc.x, (int)loc.y))){ //TODO make alllllll vector2's into vector2ints!
							ret.Add(new ActionReq(ar.p, ar.t, ar.a, new Vector2[]{loc}));
						}
					}
					break;
				default:
					ret.Add(ar);
					break;
				}
			}
			return ret;
		}
		
		Cell GetCell(int targetPlayer, Vector2Int loc){ // TODO use this instead of 'this.cells[p][x,y]'
			return this.cells[targetPlayer][loc.x, loc.y];
		}
		///////////////////Public functions for Cell calls
		public void SetCellState(int p, Vector2Int loc, CState state){
			if (!this.CheckLocInRange(loc))
				return;
			//Debug.Log("PB: SetCell " + loc.x.ToString() + "," + loc.y.ToString() + " to " + state.ToString());
			this.GetCell(p, loc).ChangeState(state);
		}

		public void AddCellCallback(int p, Vector2Int loc, PriorityCB cb, PCBType cbt){
			if (!this.CheckLocInRange(loc))
				return;
			//Debug.Log("PB: AddCellCallback: loc: " + loc.ToString() + ", type: " + cbt.ToString());
			this.GetCell(p, loc).AddCB(cb, cbt);
		}
		
		//Cells will call these to add CBs to other cells
		public void RemCellCallback(int p, Vector2Int loc, PriorityCB cb, PCBType cbt){
			if (!this.CheckLocInRange(loc))
				return;
			//Debug.Log("PB: RemCellCallback: loc: " + loc.ToString() + ", type: " + cbt.ToString());
			this.GetCell(p, loc).RemCB(cb, cbt);
		}

		bool CheckLocInRange(Vector2Int loc){
			return loc.x >= 0 && loc.x < this.sizex && loc.y >= 0 && loc.y < this.sizey;
		}
	}
}