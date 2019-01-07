using CellTypes;
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
		fireSquare,
		blockingShot, // Randomly block # of unoccupied spaces on enemy grid
		hellFire, // Randomly shoot at 5 targets on enemy side!
		flare, //randomly shoot scouts at 2
		placeMine,
		buildDefenceGrid, // Guard against 3 shots den blow up
		buildReflector, // Shots that hit this are reflected to a random space on the opponents side
		fireReflected, //Player can't request this action, only a reflector can cause this. Normal shot, but destroys reflectors so no looping
		firePiercing, //Shot that goes through walls, boom on the way
		placeMole, // place a mole that 
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
    	{	string locStr = loc==null ? "null" : loc.Count()==0 ? "0 len" : loc.ToString();
        	return "P: " + p + " T: " + t + " A: " + a + " loc0: " + locStr;
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
			//								ActionParam(costs..cd..uses) Note: you can set uses to 0 to disable
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
			{pAction.fireSquare,		new ActionParam(0,0,0, 0, -1)},
			{pAction.blockingShot,		new ActionParam(0,0,0, 0, -1)},
			{pAction.hellFire,			new ActionParam(0,0,0, 0, -1)},
			{pAction.flare,				new ActionParam(0,0,0, 0, -1)},
			{pAction.placeMine,			new ActionParam(0,0,0, 0, -1)},
			{pAction.buildDefenceGrid,	new ActionParam(0,0,0, 0, -1)},
			{pAction.buildReflector,	new ActionParam(0,0,0, 0, -1)},
			{pAction.fireReflected,		new ActionParam(0,0,0, 0, 0)}, // Player can't cause this
			{pAction.firePiercing,		new ActionParam(0,0,0, 0, -1)},
			{pAction.placeMole,			new ActionParam(0,0,0, 0, -1)},
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

		static List<pAction> CheckCostsMet(CellStruct[,] pGrid){
			int offenceCount = GUtils.Serialize(pGrid).Count(cell => cell.bldg == CBldg.towerOffence && !cell.destroyed);
			int defenceCount = GUtils.Serialize(pGrid).Count(cell => cell.bldg == CBldg.towerDefence && !cell.destroyed);
			int intelCount = GUtils.Serialize(pGrid).Count(cell => cell.bldg == CBldg.towerIntel && !cell.destroyed);
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
		
		public List<ActionAvail> GetActionAvailibility(CellStruct[,] pGrid){
			List<ActionAvail> retList = new List<ActionAvail>();
			List<pAction> costsMetActions = CheckCostsMet(pGrid); // Check cost of actions met
			foreach(pAction action in allActions){
				retList.Add(new ActionAvail(action, costsMetActions.Contains(action), this.actionCooldowns[action], this.GetUsesLeft(action), actionParams[action]));
			}
			return retList;
		}

		//Validate Actions = input action list, output actionlist that meet costs, off cooldown, not over max use
		List<ActionReq> ValidateActions(List<ActionReq> inList, CellStruct[,] pGrid){
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

		//Function to artificially set cooldown of action
		public void SetCooldown(pAction action, int cooldown){
			if(cooldown < 0){
				Debug.LogError("Don't set cd to < 0: " + cooldown.ToString());
				return;
			}
			this.actionCooldowns[action] = cooldown;
		}

		//Validate and track actions = Just do the validate then track in sequence
		public List<ActionReq> ValidateAndTrackActions(List<ActionReq> inList, CellStruct[,] pGrid){
			List<ActionReq> valActions = this.ValidateActions(inList, pGrid);
			this.TrackActions(valActions);
			return valActions;
		}
	}

	public class PlayBoard {

		const int playercnt = 2; //So far we're only designing for 2 players
		Cell[][,] cells;
		public int sizex;
		public int sizey;
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
						this.cells[p][x,y] = new Cell(CBldg.empty, p, new Vector2Int(x,y), this, false);
					}
				}
			}
		}
		//////////////////////Public for logic core calls
		//Return value will always put requesting player's grid in idx 0, enemy grid in idx 1
		public CellStruct[][,] GetPlayerGameState(int playerIdx){
			int enemyIdx = (playerIdx + 1) % playercnt;
			CellStruct[][,] boardOut = new CellStruct[playercnt][,];
			boardOut[0] = this.GetGridSide(playerIdx, 1); //playerGrid
			boardOut[1] = this.GetGridSide(enemyIdx, 2); //enemyGrid
			return boardOut; 
		}

		////////////////////////Helper functions for game logic
		//Get specific side of grid (indexed by playerID)
		CellStruct[,] GetGridSide(int idx, int perspective){
			CellStruct [,] gridOut = new CellStruct[sizex,sizey];
			for(int x = 0; x < this.sizex; x++){
				for(int y = 0; y < this.sizey; y++){
					gridOut[x,y] = cells[idx][x,y].GetCellStruct(perspective);
				}
			}
			return gridOut;
		}

		//Made for mole area checking
		int GetMoleCount(int idx, Vector2Int loc){
			List<CBldg> untrackedBldgs = new List<CBldg>{CBldg.empty};
			int ret = 0;
			for(int x = -1; x < 2; x++){
				for(int y = -1; y < 2; y++){
					Vector2Int newLoc = new Vector2Int(loc.x + x, loc.y + y);
					Debug.Log("Molecounting at loc " + newLoc.ToString() + " player: " + idx.ToString());
					if(CheckLocInRange(newLoc) && !untrackedBldgs.Contains(cells[idx][newLoc.x,newLoc.y].GetCellStruct(0).bldg)){
						ret++;
					}
				}
			}
			return ret;
		}

		//Used to help our randomizer functions that don't want to hit
		List<Vector2> GetLocsOfBldgs(int idx,  List<CBldg> bldgs, int perspective, bool negate=false){
			List<Vector2> ret = new List<Vector2>();
			for(int x = 0; x < this.sizex; x++){
				for(int y = 0; y < this.sizey; y++){
					if(!negate){
						if (bldgs.Contains(cells[idx][x,y].GetCellStruct(perspective).bldg)){
							ret.Add(new Vector2(x,y));
						}
					}
					else{
						if (!bldgs.Contains(cells[idx][x,y].GetCellStruct(perspective).bldg)){
							ret.Add(new Vector2(x,y));
						}
					}
				}
			}
			return ret;
		}

		public bool CheckPlayerLose(int p){
			List<CBldg> s = new List<CBldg>(){CBldg.towerOffence, CBldg.towerDefence, CBldg.towerIntel};
			//Debug.Log("GameOverChecking for player: " + p.ToString());
			bool playerlose = true;
			for(int x = 0; x < this.sizex; x++){ //TODO replace these nested loops with a foreach (think that should work on jagged array)
				for(int y = 0; y < this.sizey; y++){
					CellStruct cs = this.cells[p][x,y].GetCellStruct(0);
					if (s.Contains(cs.bldg) && !cs.destroyed){
						playerlose = false; // as long as they have one tower, they're still in it!
					}
				}
			}
			return playerlose;
		}

		public List<ActionAvail> GetActionAvailable(int playerId){
			return this.pats[playerId].GetActionAvailibility(this.GetGridSide(playerId, 0));
		}

		//Here we do the cleanup/calcs/etc that needs to happen at the end of apply
		void FinalizeApply(){
			//Increment the counters in the cells
			for(int p = 0; p < playercnt; p++){
				for(int x = 0; x < this.sizex; x++){
					for(int y = 0; y < this.sizey; y++){
						//Increment the counters in the cells
						this.cells[p][x,y].IncrementCounters();
						//Moles do their thing at the end of every action apply
						if(this.cells[p][x,y].mole){
							this.cells[p][x,y].molecount = this.GetMoleCount(p, new Vector2Int(x,y));
						}
					}
				}
			}
			//Moles do their thing at the end of every action apply

		}


		//We expect this to be called only once per round
		public void ApplyValidActions(List<ActionReq> ars){
			Debug.Log("PlayBoard processing Actions. Got " + ars.Count);
			List<ActionReq> validARs = new List<ActionReq>();
			foreach(ActionReq ar in ars){ // Trust no one, validate it allllll
				if (this.validator.Validate(ar, this.GetGridSide(ar.p, 1), this.GetGridSide((ar.p + 1) % playercnt, 2), new Vector2(sizex, sizey))){
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
			player0ARs = this.pats[0].ValidateAndTrackActions(player0ARs, this.GetGridSide(0, 0));
			player1ARs = this.pats[1].ValidateAndTrackActions(player1ARs, this.GetGridSide(1, 0));
			ars.Clear();
			ars.AddRange(player0ARs);
			ars.AddRange(player1ARs);
			//Validation is done, now we can apply these as like normal
			this.ApplyActions(ars);
			//Now update the timed parameters of each cell
			this.FinalizeApply();
		}

		void ApplyActions(List<ActionReq> ars){
			//To CodeMonkey: each action must appear no more than once in these lists
			List<pAction> buildActions = new List<pAction>(){pAction.buildOffenceTower, pAction.buildDefenceTower, pAction.buildIntelTower, pAction.buildWall,
				pAction.placeMine, pAction.buildDefenceGrid, pAction.buildReflector, pAction.placeMole};
			List<pAction> shootActions = new List<pAction>(){pAction.fireBasic, pAction.fireAgain, pAction.fireRow, pAction.fireSquare, pAction.blockingShot,
				pAction.hellFire, pAction.fireReflected, pAction.firePiercing};
			List<pAction> scoutActions = new List<pAction>(){pAction.scout, pAction.flare};
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
					List<Vector2> squareLocs = new List<Vector2>();
					for(int x = -1; x < 2; x+=2){
						for(int y = -1; y < 2; y+=2){
							squareLocs.Add(new Vector2(ar.loc[0].x + x, ar.loc[0].y + y));
						}
					}
					foreach(Vector2 loc in squareLocs){
						if(this.CheckLocInRange(new Vector2Int((int)loc.x, (int)loc.y))){ //TODO make alllllll vector2's into vector2ints!
							ret.Add(new ActionReq(ar.p, ar.t, ar.a, new Vector2[]{loc}));
						}
					}
					break;
				case pAction.blockingShot:
					List<Vector2> emptyLocs =  this.GetLocsOfBldgs(ar.t, new List<CBldg>(){CBldg.empty}, 0);
					for(int i = 0; i < 3; i++){
						if(emptyLocs.Count() == 0){
							break;
						}
						int randVal = UnityEngine.Random.Range(0,emptyLocs.Count());
						ret.Add(new ActionReq(ar.p, ar.t, ar.a, new Vector2[]{emptyLocs[randVal]}));
						emptyLocs.RemoveAt(randVal);
					}
					break;
				case pAction.hellFire:
					List<Vector2> allLocs =  this.GetLocsOfBldgs(ar.t, new List<CBldg>(){}, 0, negate:true);
					for(int i = 0; i < 5; i++){
						if(allLocs.Count() == 0){
							break;
						}
						int randVal = UnityEngine.Random.Range(0,allLocs.Count());
						ret.Add(new ActionReq(ar.p, ar.t, ar.a, new Vector2[]{allLocs[randVal]}));
						allLocs.RemoveAt(randVal);
					}
					break;
				case pAction.flare:
					List<Vector2> noTowerLocs =  this.GetLocsOfBldgs(ar.t, new List<CBldg>(){}, 0, negate:true);
					for(int i = 0; i < 2; i++){
						if(noTowerLocs.Count() == 0){
							break;
						}
						int randVal = UnityEngine.Random.Range(0,noTowerLocs.Count());
						ret.Add(new ActionReq(ar.p, ar.t, ar.a, new Vector2[]{noTowerLocs[randVal]}));
						noTowerLocs.RemoveAt(randVal);
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
		///////////////////Public functions for Cell calls during their action resolution
		public void SetCellBldg(int p, Vector2Int loc, CBldg bldg){
			if (!this.CheckLocInRange(loc))
				return;
			//Debug.Log("PB: SetCellBldg " + loc.x.ToString() + "," + loc.y.ToString() + " to " + bldg.ToString());
			this.GetCell(p, loc).ChangeCellBldg(bldg);
		}

		public void CellSetDefGridBlock(int p, Vector2Int loc, bool block){
			Debug.LogWarning("Setting block boolean to " + block.ToString());
			this.GetCell(p,loc).defenceGridBlock = block;
		}

		//Cells can create and execute new actions in their resolution
		public void CellApplyActionReqs(List<ActionReq> actionReq){
			this.ApplyActions(actionReq);
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

		public void SetActionCooldown(int playerId, pAction action, int cooldown){
			this.pats[playerId].SetCooldown(action,cooldown);
		}

		bool CheckLocInRange(Vector2Int loc){
			return loc.x >= 0 && loc.x < this.sizex && loc.y >= 0 && loc.y < this.sizey;
		}
	}
}