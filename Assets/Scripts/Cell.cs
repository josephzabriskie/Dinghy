using System.Collections.Generic;
using System.Linq;
using PlayerActions;
using UnityEngine;
using PlayboardTypes;

//Hey, don't use any unity stuff if you can avoid it here!
namespace CellTypes{
	public enum CBldg{ //Representation of what's built in a tile
		hidden = 0,
		empty,
		towerTemp,
		tower, // Should be unused
		towerOffence,
		towerDefence,
		towerIntel,
		//destroyedTower,
		//destroyedTerrain,
		wall,
		//wallDestroyed,
		blocked,
		mine,
		//destroyedMine,
		defenceGrid,
		//destroyedDefenceGrid
	}
	public struct CellStruct{ // Here is the data type that will be passed back to the player
		public CBldg bldg; //The building in the cell (can be empty)
		public bool destroyed;
		//Explicit Constructor
		public CellStruct(CBldg bldg, bool destroyed){
			this.bldg = bldg;
			this.destroyed = destroyed;
		}
		//Default constructor? Useful?
		public CellStruct(CBldg bldg){
			this.bldg = bldg;
			this.destroyed = false;
		}
	}

	public enum PCBType{ //prioritycallback type
		shoot,
		build,
		scout
	}

	public delegate bool cellCallback(ActionReq ar);
	public class PriorityCB{
		public PriorityCB(int priority, cellCallback cb){
			this.p = priority;
			this.f = cb;
		}
		public int p; // priority
		public cellCallback f; //callback function

		public override string ToString(){
        	return "PriorityCallback: p: " + p.ToString() + ", f: " + f.GetInvocationList().ToString();
    	}
	}

    public class Cell{
		CBldg bldg;
		public PlayBoard pb;
		public int pNum;
		public Vector2Int loc;
		PriorityCB shootDef;
		PriorityCB buildDef;
		PriorityCB scoutDef;
		List<PriorityCB> shootCBs;
		List<PriorityCB> buildCBs;
		List<PriorityCB> scoutCBs;
		///////CellParams
		bool destroyed; // if we ded
		bool vis; // visible to Enemy
		//For Scouting actions
		bool scouted;
        int scoutDuration; // How long visible to enemy
		//For couting hits on defence grid
		int defenceGridHits;
		bool defenceGridActive; // multiple shots on grid in one round are blocked, don't count

		//Called externally to decrement/increment counting elements on each turn
		//Take action here if needed
		public void IncrementCounters(){
			//Scouted Counters
			if(this.scouted){
				this.scoutDuration--;
			}
			if(this.scoutDuration <=0){ // Scouting worn off
				this.scouted = false;
			}
			//Grid de-activate
			this.defenceGridActive = false;
		}

		public CellStruct GetCellStruct(bool showAll=false){
			CBldg s;
			bool ded;
			if(showAll || this.vis || this.scouted){ // In this case area is visible, fill in all data accurately
				s = this.bldg;
				ded = this.destroyed;
			}
			else{ //We're supposed to be hidden, show nothing!
				s = CBldg.hidden;
				ded = false;
			}
			return new CellStruct(s, ded);
		}

		public Cell(CBldg bldg, int pNum, Vector2Int loc, PlayBoard pb, bool visibleToEnemy){
			this.pb = pb; // This will never change
			this.pNum = pNum;
			this.loc = loc;
			this.vis = visibleToEnemy;
			this.shootCBs = new List<PriorityCB>();
			this.buildCBs = new List<PriorityCB>();
			this.scoutCBs = new List<PriorityCB>();
			this.ChangeCellBldg(bldg, init:true);
		}
		
		//////////////////////////
		//State Changing functions
		public void ChangeCellBldg(CBldg newBldg, bool init=false){
			//Debug.Log("Changing our bldg from" + this.bldg.ToString() + " to " + newBldg.ToString() + " loc " + this.loc.ToString());
			if (!init){
				this.TearDownSpecialCbs();
			}
			this.SetCellParams(newBldg);
			this.SetDefaultCB();
			this.SetupSpecialCBs();			
		}

		public void DestroyCell(bool destroy){
			this.destroyed = destroy;
			if(destroy){ // blow it up
				this.vis = true; // on boom, make visible
				this.TearDownSpecialCbs();
			}
			else{// un-blow it up?
				this.SetupSpecialCBs();
			}
		}

		//////////////////////////
		void SetDefaultCB(){
			switch(this.bldg){
				//Each bldg here has to set each of the defaults! If you don't it'll use the ones from the last bldg
			case CBldg.empty:
			case CBldg.wall:
			case CBldg.towerOffence:
			case CBldg.towerDefence:
			case CBldg.towerIntel:
			case CBldg.mine:
			case CBldg.defenceGrid:
				this.shootDef = new PriorityCB(0, DefShotCB);
				this.buildDef = new PriorityCB(0, DefBuiltCB);
				this.scoutDef = new PriorityCB(0, DefScoutedCB);
				break;
			// case CState.destroyedTerrain:
			// case CState.destroyedTower:
			// case CState.wallDestroyed:
			// case CState.destroyedMine:
			// case CState.destroyedDefenceGrid:
			case CBldg.blocked:
			// 	this.shootDef = new PriorityCB(0, NullCB);
			// 	this.buildDef = new PriorityCB(0, NullCB);
			// 	this.scoutDef = new PriorityCB(0, NullCB);
			// 	break;
			default:
				Debug.LogError("SetDefaultCB unhandled Case: " + this.bldg.ToString() + ", setting CB's to null");
				this.shootDef = new PriorityCB(0, NullCB);
				this.buildDef = new PriorityCB(0, NullCB);
				this.scoutDef = new PriorityCB(0, NullCB);
				break;
			}
		}

		//Add special CBs to other cells
		void SetupSpecialCBs(){
			if(this.bldg == CBldg.wall){
				//Debug.Log("Adding the wall's special callbacks");
				this.pb.AddCellCallback(this.pNum, new Vector2Int(this.loc.x, this.loc.y -1), new PriorityCB(5, this.WallCB), PCBType.shoot);
				this.pb.AddCellCallback(this.pNum, new Vector2Int(this.loc.x, this.loc.y -2), new PriorityCB(6, this.WallCB), PCBType.shoot);
			}
			if(this.bldg == CBldg.defenceGrid){
				for(int x = -2; x < 3; x++){
					for(int y = -2; y < 3; y++){
						if(!(x == 0 && y == 0)){
							this.pb.AddCellCallback(this.pNum, new Vector2Int(this.loc.x + x, this.loc.y + y), new PriorityCB(7, this.DefenceGridCB), PCBType.shoot);
						}
					}
				}
			}
		}

		void TearDownSpecialCbs(){
			if(this.bldg == CBldg.wall){
				//Debug.Log("removing the wall's special callbacks");
				this.pb.RemCellCallback(this.pNum, new Vector2Int(this.loc.x, this.loc.y -1), new PriorityCB(5, this.WallCB), PCBType.shoot);
				this.pb.RemCellCallback(this.pNum, new Vector2Int(this.loc.x, this.loc.y -2), new PriorityCB(6, this.WallCB), PCBType.shoot);
			}
			if(this.bldg == CBldg.defenceGrid){
				for(int x = -2; x < 3; x++){
					for(int y = -2; y < 3; y++){
						if(!(x == 0 && y == 0)){
							this.pb.RemCellCallback(this.pNum, new Vector2Int(this.loc.x + x, this.loc.y + y), new PriorityCB(7, this.DefenceGridCB), PCBType.shoot);
						}
					}
				}
			}
		}

		public bool AddCB(PriorityCB cb, PCBType cbt){
			//Debug.Log("Cell " + this.loc.ToString() + " adding CB!");
			switch(cbt){
			case PCBType.shoot:
				this.shootCBs.Add(cb);
				break;
			case PCBType.build:
				this.buildCBs.Add(cb);
				break;
			case PCBType.scout:
				this.scoutCBs.Add(cb);
				break;
			default:
				Debug.LogError("Cell AddCB: Unhandled state! " + cbt.ToString());
				return false;
			}
			return true;
		}

		public bool RemCB(PriorityCB cb, PCBType cbt){
			//Debug.Log("Cell " + this.loc.ToString() + " removing CB type " + cbt.ToString());
			List<PriorityCB> lst = null;
			if(cbt == PCBType.shoot){ lst = this.shootCBs; } // Avante Garde Formatting
			else if(cbt == PCBType.build){ lst = this.buildCBs; }
			else if(cbt == PCBType.scout){ lst = this.scoutCBs; }
			else{
				Debug.LogError("Cell rem nbCB: Unhandled state! " + cbt.ToString());
				return false;
			}
			PriorityCB rem = lst.SingleOrDefault(i => i.p == cb.p && i.f == cb.f);
			if(rem != null){
				lst.Remove(rem);
			}
			else{
				Debug.LogError("Found none or more than 1 matching item to remove: len" + lst.Count.ToString() + " pcb: " + rem.ToString());
				return false;
			}
			return true;
		}

		void SetCellParams(CBldg newBldg){
			this.bldg = newBldg;
			switch(this.bldg){
			// case CState.destroyedTerrain:
			// case CState.wallDestroyed:
			// case CState.destroyedTower:
			// case CState.destroyedMine:
			// case CState.destroyedDefenceGrid:
			case CBldg.blocked:
				//Debug.Log("SetCellParams: Since we're destroyed, reveal us: " + this.newBldg.ToString());
				this.vis = true; // Set visibility permanently
				break;
			case CBldg.defenceGrid:
				this.defenceGridHits = 0;
				this.defenceGridActive = false;
				break;
			case CBldg.hidden:
				Debug.LogError("SetCellParams: Don't ever expect to be in this state: " + this.bldg.ToString());
				break;
			default:
				//Debug.Log("SetStateParams: No new state params for incoming state: " + this.state.ToString());
				break;
			}
		}
		
		//Public calls for basic actions
        public void onShoot(ActionReq ar){
			//Debug.Log("Cell: " + this.loc.ToString() + " OnShoot");
			this.ExecPriorityList(this.shootCBs, this.shootDef, ar);
		}

		public void onBuild(ActionReq ar){
			//Debug.Log("Cell: " + this.loc.ToString() + " onBuild");
			this.ExecPriorityList(this.buildCBs, this.buildDef, ar);
		}

		public void onScout(ActionReq ar){
			//Debug.Log("Cell: " + this.loc.ToString() + " onScout");
			this.ExecPriorityList(this.scoutCBs, this.scoutDef, ar);
		}

		void ExecPriorityList(List<PriorityCB> pcbl, PriorityCB def, ActionReq ar){
			//Debug.Log("Cell: Executing priorty list. " + pcbl.Count().ToString());
			List<PriorityCB> list = new List<PriorityCB>();
			list.AddRange(pcbl); // Add special callbacks
			list.Add(def); // Add default callback
			list.OrderByDescending(cb => cb.p).ToList();
			foreach(PriorityCB pcb in list){
				if(pcb.f(ar)){
					break;
				}
			}
		}

		public void PrintSelfInfo(){
			Debug.Log("Cellinfo: Loc: " + this.loc.ToString() + ", Bldg: " + this.bldg.ToString());
			Debug.Log("Cellinfo cont: cbshot: " + this.shootCBs.Count().ToString() + ", cbbuild:" +this.buildCBs.Count().ToString() + ", cbscout:" + this.scoutCBs.Count().ToString());
		}

		/////////////Callbacks
		bool WallCB(ActionReq ar){
			//Debug.Log("CB handler: WallCB loc" + this.loc.ToString());
			this.DestroyCell(true);
			return true;
		}

		bool DefenceGridCB(ActionReq ar){
			const int maxHits = 3;
			//Todo, we need a way to show this to the player!
			if (!this.defenceGridActive){
				this.defenceGridHits++;
				this.defenceGridActive = true;
			}
			Debug.LogWarning("CB handler: DefenceGridCB blocked shot #" + this.defenceGridHits.ToString() + " at loc" + this.loc.ToString());
			if (this.defenceGridHits >= maxHits){
				this.DestroyCell(true);
			}
			return true;
		}

		bool DefBuiltCB(ActionReq ar){
			//Debug.Log("CB handler: DefBuiltCB loc " + this.loc.ToString());
			switch(ar.a){
			case pAction.buildOffenceTower:
				this.ChangeCellBldg(CBldg.towerOffence);
				break;
			case pAction.buildDefenceTower:
				this.ChangeCellBldg(CBldg.towerDefence);
				break;
			case pAction.buildIntelTower:
				this.ChangeCellBldg(CBldg.towerIntel);
				break;
			case pAction.buildWall:
				this.ChangeCellBldg(CBldg.wall);
				break;
			case pAction.placeMine:
				this.ChangeCellBldg(CBldg.mine);
				break;
			case pAction.buildDefenceGrid:
				this.ChangeCellBldg(CBldg.defenceGrid);
				break;
			default:
				Debug.LogError("EmptyBuildCB: Default case unhandled. " + ar.a.ToString());
				return false;
			}
			return true;
		}

		bool DefShotCB(ActionReq ar){
			//Debug.Log("CB handler: DefShotCB loc " + this.loc.ToString());
			//Always check if we're a mine first, must punish
			if(this.bldg == CBldg.mine){
				this.DestroyCell(true);
				this.pb.SetActionCooldown(ar.p, pAction.fireBasic, 3);
			}
			if(ar.a == pAction.blockingShot){
				if (this.bldg == CBldg.empty){
					this.ChangeCellBldg(CBldg.blocked);
				}
				else{
					Debug.LogError("Trying to block a non empty cell! " + ar.ToString());
				}
			}
			else{
				this.DestroyCell(true);
			}
			return true;
		}

		bool DefScoutedCB(ActionReq ar){
			const int maxDuration = 4;
			Debug.Log("Handling getting scouted at loc " + this.loc.ToString());
			this.scouted = true;
			this.scoutDuration = maxDuration;
			return true;
		}

		//TODO does this need to be unique for shot,built,scouted?
		bool NullCB(ActionReq ar){
			//Debug.Log("CB handler: NullCB loc " + this.loc.ToString() + ". Do nothing. Bldg: " + this.bldg.ToString());
			return true;
		}
	}
}