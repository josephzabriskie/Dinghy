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
		towerTakeover, //Takeover enemy tower on hit. Tower now counts towards your cost requirements
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
    	{	string locStr = "";
			if(loc==null){
				locStr = "null";
			}
			else{
				foreach(Vector2 l in loc){
					locStr += l.ToString();
				}
			}
        	return "P: " + p + " T: " + t + " A: " + a + " loc: " + locStr;
    	}
	}
}

namespace PlayboardTypes{

	public enum Faction{
		NoFaction,
		Offence,
		Defence,
		Intel
	}

	public struct ActionParam{
		public pAction action;
		public Faction faction; // Faction this action is associated with
		public int factionCost; // number of towers in faction to use
		public int cooldown; // cooldown
		public int maxUses; // max uses
		public bool enabled; //If this is allowed at all
		public ActionParam(pAction action, Faction faction, int factionCost, int cooldown, int maxUses, bool enabled){
			this.action = action;
			this.faction = faction;
			this.factionCost = factionCost;
			this.cooldown = cooldown; // 0 means no cooldown
			this.maxUses = maxUses; // 0 means no more uses, -1 means no limit
			this.enabled = enabled; // false means player can't cause this
		}
		public override string ToString(){
			return String.Format("APM: A:{0} f:{1}, c:{2}, cd:{3}, mu: {4}",
				this.action, this.faction, this.factionCost, this.cooldown, this.maxUses);
		}
	}

	//This can be requested by a player so they know what the server thinks they can do.
	public struct ActionAvail{
		public bool available;
		public bool costsMet;
		public int cooldown;
		public int usesLeft;
		public ActionParam actionParam;
		public ActionAvail(bool costsMet, int cooldown, int usesLeft, ActionParam actionParam){
			this.available = costsMet && cooldown == 0 && usesLeft != 0; //Just cal this for easier use
			this.costsMet = costsMet;
			this.cooldown = cooldown;
			this.usesLeft = usesLeft;
			this.actionParam = actionParam;
		}
		public override string ToString(){
			return String.Format("Action Avail: av: {3} cm:{0}, cd: {1}, ul: {2}. APM {4}",
				this.costsMet, this.cooldown, this.usesLeft, this.available, this.actionParam);
		}
	}

	//class for keeping track of the maximum progress, can return list of progress indexed by
	public class FactionProgress{ // How to make a structure that contains count of all faction towers?
		static List<Faction> allFactions = ((Faction[])Enum.GetValues(typeof(Faction))).ToList();
		public Dictionary<Faction, int> progress;
		public FactionProgress(){
			progress = new Dictionary<Faction, int>{};
			foreach(Faction f in allFactions){
				progress.Add(f, 0);
			}
		}
		public FactionProgress(int[] inProgress){
			progress = new Dictionary<Faction, int>{};
			foreach(Faction f in allFactions){
				progress.Add(f, 0);
			}
			for(int i = 0; i < inProgress.Count(); i++){
				progress[(Faction)i] = inProgress[i];
			}
		}
		public int GetProgress(Faction faction){
			return this.progress[faction];
		}
		//Set progress to exact value
		public void SetProgress(Faction faction, int progress){
			this.progress[faction] = progress;
		}
		//Update faction progress if input progress is greater than current
		public void UpdateProgress(Dictionary<Faction, int> inProgress){
			foreach(Faction f in inProgress.Keys){
				if(this.progress[f] < inProgress[f]){
					this.progress[f] = inProgress[f];
				}
			}
		}
		//Return array that can be indexed by (int)Faction enum
		public int[] GetArray(){
			int[] resl = new int[this.progress.Count];
			for(int i = 0; i < allFactions.Count; i++){
				resl[i] = this.progress[(Faction)i];
			}
			return resl;
		}
	}

	public class PlayerActionTracker{
		//Useful static values
		static List<pAction> allActions = ((pAction[])Enum.GetValues(typeof(pAction))).ToList(); // How to get all pActions defined in the enum!
		static Dictionary<pAction, ActionParam> actionParams = new Dictionary<pAction, ActionParam>{
			//Action					ActionParam(	action 						faction            cost.cd.uses.enabled)
			{pAction.noAction, 			new ActionParam(pAction.noAction, 			Faction.NoFaction,	0, 0, -1,	true)},
			{pAction.buildOffenceTower, new ActionParam(pAction.buildOffenceTower,	Faction.Offence,	0, 1, 7,	true)},
			{pAction.buildDefenceTower, new ActionParam(pAction.buildDefenceTower,	Faction.Defence,	0, 1, 7,	true)},
			{pAction.buildIntelTower, 	new ActionParam(pAction.buildIntelTower, 	Faction.Intel,		0, 1, 7,	true)},
			{pAction.buildWall, 		new ActionParam(pAction.buildWall,			Faction.Defence,	3, 2, 5,	true)},
			{pAction.fireBasic, 		new ActionParam(pAction.fireBasic,			Faction.Offence,	0, 0, -1,	true)},
			{pAction.scout, 			new ActionParam(pAction.scout,				Faction.Intel,		3, 0, 0,	true)},
			{pAction.fireAgain,			new ActionParam(pAction.fireAgain,			Faction.Offence,	3, 2, -1,	true)},
			{pAction.fireRow,			new ActionParam(pAction.fireRow,			Faction.Offence,	7, 0, 1,	false)},
			{pAction.fireSquare,		new ActionParam(pAction.fireSquare,			Faction.Offence,	1, 4, -1,	true)},
			{pAction.blockingShot,		new ActionParam(pAction.blockingShot,		Faction.Offence,	5, 3, 4,	false)},// this one's odd, what should the cost be?
			{pAction.hellFire,			new ActionParam(pAction.hellFire,			Faction.Offence,	7, 6, -1,	true)},
			{pAction.flare,				new ActionParam(pAction.flare,				Faction.Intel,		3, 3, -1,	true)},
			{pAction.placeMine,			new ActionParam(pAction.placeMine,			Faction.Defence,	5, 1, 4,	false)},
			{pAction.buildDefenceGrid,	new ActionParam(pAction.buildDefenceGrid, 	Faction.Defence,	7, 6, 3,	true)},
			{pAction.buildReflector,	new ActionParam(pAction.buildReflector,	 	Faction.Defence,	5, 2, 6,	true)},
			{pAction.fireReflected,		new ActionParam(pAction.fireReflected,		Faction.NoFaction,	0, 0, 0,	false)}, // Player can't cause this
			{pAction.firePiercing,		new ActionParam(pAction.firePiercing,		Faction.Intel,		5, 0, 0,	false)},
			{pAction.placeMole,			new ActionParam(pAction.placeMole,			Faction.Intel,		5, 4, 2,	true)},
			{pAction.towerTakeover,		new ActionParam(pAction.towerTakeover,		Faction.Intel,		7, 5, -1,	true)},
		};
		static Dictionary<Faction, CBldg> factionBldgMap = new Dictionary<Faction, CBldg>{
			{Faction.Offence, CBldg.towerOffence},
			{Faction.Defence, CBldg.towerDefence},
			{Faction.Intel, CBldg.towerIntel},
		};

		//Instance Members
		Dictionary<pAction, int> actionCooldowns; //Use for tracking cooldowns
		List<pAction> actionHistory; //Use for counting uses
		public FactionProgress factionProgress;

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
			this.factionProgress = new FactionProgress();
		}

		//Get list of actions that can be used based on the current game state
		List<pAction> CheckCostsMet(CellStruct[][,] gState){
			Dictionary<Faction, int> curProg = new Dictionary<Faction, int>(); // Current progress based on this game state
			foreach(Faction f in factionBldgMap.Keys){
				int count = GUtils.Serialize(gState[0]).Count(cell => cell.bldg == factionBldgMap[f] && !cell.destroyed && !cell.defected);
				count += GUtils.Serialize(gState[1]).Count(cell => cell.bldg == factionBldgMap[f] && !cell.destroyed && cell.defected);
				curProg.Add(f, count);
			}
			this.factionProgress.UpdateProgress(curProg);
			List<pAction> retList = new List<pAction>();
			foreach(pAction action in actionParams.Keys){
				ActionParam apm = actionParams[action];
				if(apm.enabled && this.factionProgress.GetProgress(apm.faction) >= apm.factionCost){
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
		
		public List<ActionAvail> GetActionAvailibility(CellStruct[][,] playerGameState){
			List<ActionAvail> retList = new List<ActionAvail>();
			List<pAction> costsMetActions = CheckCostsMet(playerGameState); // Check cost of actions met
			foreach(pAction action in allActions){
				retList.Add(new ActionAvail(costsMetActions.Contains(action), this.actionCooldowns[action], this.GetUsesLeft(action), actionParams[action]));
			}
			return retList;
		}

		//Validate Actions = input action list, output actionlist that meet costs, off cooldown, not over max use
		List<ActionReq> ValidateActions(List<ActionReq> inList, CellStruct[][,] playerGameState){
			List<ActionReq> retList = new List<ActionReq>();
			List<pAction> costsMetActions = CheckCostsMet(playerGameState); // Check cost of actions met
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
		public List<ActionReq> ValidateAndTrackActions(List<ActionReq> inList, CellStruct[][,] playerGameState){
			List<ActionReq> valActions = this.ValidateActions(inList, playerGameState);
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
		public List<Vector2>[] capitolTowerLocs; // Location list of the very first tower's placed for each player
		ActionProcState actionProcState;
		public bool[] hitSunk; //Does either player need a hitsunk message. (e.g get hitSunk[1] to tell if player 1 has sunk a ship this turn)
		Dictionary<Vector2, bool>[] sunkDicts;

		public PlayBoard(int sizex, int sizey){
			//Debug.Log("Hey, I'm making a Playboard: " + sizex.ToString() + "X" + sizey.ToString());
			this.sizex = sizex;
			this.sizey = sizey;
			this.capitolTowerLocs = new List<Vector2>[]{new List<Vector2>(){},new List<Vector2>(){}};
			this.hitSunk = new bool[playercnt];
			this.sunkDicts = new Dictionary<Vector2, bool>[playercnt]; // Dictionary of capitol and if it's sunk
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
		//Set showall to false if you're looking to get data to hand back to players
		public CellStruct[][,] GetPlayerGameState(int playerIdx, bool showall){
			int enemyIdx = (playerIdx + 1) % playercnt;
			CellStruct[][,] boardOut = new CellStruct[playercnt][,];
			if(showall){
				boardOut[0] = this.GetGridSide(playerIdx, CellPerspective.All); //playerGrid
				boardOut[1] = this.GetGridSide(enemyIdx, CellPerspective.All); //enemyGrid	
			}
			else{
				boardOut[0] = this.GetGridSide(playerIdx, CellPerspective.PlayersOwn); //playerGrid
				boardOut[1] = this.GetGridSide(enemyIdx, CellPerspective.PlayersEnemy); //enemyGrid
			}
			return boardOut; 
		}

		////////////////////////Helper functions for game logic
		//Get specific side of grid (indexed by playerID)
		CellStruct[,] GetGridSide(int idx, CellPerspective perspective){
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
		List<Vector2> GetLocsOfBldgs(int p,  List<CBldg> bldgs, CellPerspective perspective, bool negate=false){
			List<Vector2> ret = new List<Vector2>();
			foreach(Cell c in this.cells[p]){
				if(!negate){ // I think I can simplify this logic...
					if (bldgs.Contains(c.GetCellStruct(perspective).bldg)){
						ret.Add(c.loc);
					}
				}
				else{
					if (!bldgs.Contains(c.GetCellStruct(perspective).bldg)){
						ret.Add(c.loc);
					}
				}
			}
			return ret;
		}

		public bool CheckPlayerLose(int p){
			List<CBldg> s = new List<CBldg>(){CBldg.towerOffence, CBldg.towerDefence, CBldg.towerIntel};
			//Debug.Log("GameOverChecking for player: " + p.ToString());
			bool playerlose = true;
			foreach(Cell c in this.cells[p]){
				CellStruct cs = c.GetCellStruct(0);
				if (s.Contains(cs.bldg) && !cs.destroyed && !cs.defected){
					playerlose = false; // as long as they have one tower, they're still in it!
				}
			}
			return playerlose;
		}

		public List<ActionAvail> GetActionAvailable(int playerId){
			return this.pats[playerId].GetActionAvailibility(this.GetPlayerGameState(playerId, true));
		}

		public FactionProgress GetFactionProgress(int playerId){
			return this.pats[playerId].factionProgress;
		}

		void UpdateSunkDicts(){
			for(int p = 0; p < playercnt; p++){
				int otherPlayer = (p + 1) % playercnt;
				Dictionary<Vector2, bool> newTowerSunk = this.validator.GetTowerChainsSunk(this.GetGridSide(p, CellPerspective.All), new Vector2(sizex, sizey), this.capitolTowerLocs[p]);
				if (this.sunkDicts[p] == null){ // first time through it's easier, nothing can be sunk at this point, just assign dict
					this.sunkDicts[p] = newTowerSunk;
					continue; // continue, no point checking if sunk. Nothing can be sunk at this point
				}
				this.hitSunk[otherPlayer] = false; // False until detected
				foreach(Vector2 cap in capitolTowerLocs[p]){
					if(!this.sunkDicts[p][cap] && newTowerSunk[cap]){ // If sunk state has changed from false to true, the other player's got
						this.hitSunk[otherPlayer] = true;
					}
				}
				this.sunkDicts[p] = newTowerSunk;
			}
		}

		void ClearLastHitFlag(){
			for(int p = 0; p < playercnt; p++){
				foreach(Cell c in this.cells[p]){
					c.lastHit = false;
				}
			}
		}
		

		//Here we do the cleanup/calcs/etc that needs to happen at the end of apply
		void FinalizeApply(){
			//Increment the counters in the cells
			for(int p = 0; p < playercnt; p++){
				foreach(Cell c in this.cells[p]){
					//Increment the counters in the cells
					c.IncrementCounters();
					//Moles do their thing at the end of every action apply
					if(c.mole){
						c.molecount = this.GetMoleCount(p, c.loc);
					}
				}
			}
			UpdateSunkDicts();
		}


		//We expect this to be called only once per round
		public void ApplyValidActions(List<ActionReq> ars){
			Debug.Log("PlayBoard processing Actions. Got " + ars.Count);
			List<ActionReq> validARs = new List<ActionReq>();
			foreach(ActionReq ar in ars){ // Trust no one, validate it allllll
				if (this.validator.Validate(ar, this.GetGridSide(ar.p, CellPerspective.PlayersOwn), this.GetGridSide((ar.p + 1) % playercnt, CellPerspective.PlayersEnemy), new Vector2(sizex, sizey), this.capitolTowerLocs[ar.p])){
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
			player0ARs = this.pats[0].ValidateAndTrackActions(player0ARs, this.GetPlayerGameState(0, true));
			player1ARs = this.pats[1].ValidateAndTrackActions(player1ARs, this.GetPlayerGameState(1, true));
			ars.Clear();
			ars.AddRange(player0ARs);
			ars.AddRange(player1ARs);
			//Validation is done, now we can apply these as like normal
			//First clear the old flags from last turn
			ClearLastHitFlag();
			this.ApplyActions(ars);
			//Now update the timed parameters of each cell
			this.FinalizeApply();
		}

		void ApplyActions(List<ActionReq> ars){
			//To CodeMonkey: each action must appear no more than once in these lists
			List<pAction> buildActions = new List<pAction>(){pAction.buildOffenceTower, pAction.buildDefenceTower, pAction.buildIntelTower, pAction.buildWall,
				pAction.placeMine, pAction.buildDefenceGrid, pAction.buildReflector, pAction.placeMole};
			List<pAction> shootActions = new List<pAction>(){pAction.fireBasic, pAction.fireAgain, pAction.fireRow, pAction.fireSquare, pAction.blockingShot,
				pAction.hellFire, pAction.fireReflected, pAction.firePiercing, pAction.towerTakeover};
			List<pAction> scoutActions = new List<pAction>(){pAction.scout, pAction.flare};
			//Here we order the list to make sure that building happens first
			List<ActionReq> buildARs = ars.Where(ar => buildActions.Contains(ar.a)).ToList();
			//If this is the multi tower placement state, we want to save off the locations of the first tower to return to the pobjs
			if(this.actionProcState == ActionProcState.multiTower){
				this.capitolTowerLocs[0].AddRange(buildARs.Where(ar => ar.p == 0).Select(ar =>ar.loc[0])); // Here we assume that we've only grabbed tower placements, hopefully valid...
				this.capitolTowerLocs[1].AddRange(buildARs.Where(ar => ar.p == 1).Select(ar =>ar.loc[0]));
				Debug.Log("Got towerCapitols: len0: " + this.capitolTowerLocs[0].Count.ToString());
				Debug.Log("Got towerCapitols: len1: " + this.capitolTowerLocs[1].Count.ToString());
			}
			List<ActionReq> otherARs = ars.Where(ar => !buildARs.Contains(ar)).ToList();
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
					List<Vector2> emptyLocs =  this.GetLocsOfBldgs(ar.t, new List<CBldg>(){CBldg.empty}, CellPerspective.All);
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
					List<Vector2> allLocs =  this.GetLocsOfBldgs(ar.t, new List<CBldg>(){}, CellPerspective.All, negate:true);
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
					List<Vector2> flareLocs =  this.GetLocsOfBldgs(ar.t, new List<CBldg>(){}, CellPerspective.All, negate:true);
					for(int i = 0; i < 2; i++){
						if(flareLocs.Count() == 0){
							break;
						}
						int randVal = UnityEngine.Random.Range(0,flareLocs.Count());
						ret.Add(new ActionReq(ar.p, ar.t, ar.a, new Vector2[]{flareLocs[randVal]}));
						flareLocs.RemoveAt(randVal);
					}
					break;
				default:
					ret.Add(ar);
					break;
				}
			}
			return ret;
		}

		public void SetAPC(ActionProcState apc){
			this.actionProcState = apc;
			this.validator.SetAPC(apc);
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