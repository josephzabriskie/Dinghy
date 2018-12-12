using System.Collections.Generic;
using System.Linq;
using PlayerActions;
using UnityEngine;
using PlayboardTypes;

//Hey, don't use any unity stuff if you can avoid it here!
namespace CellTypes{
	public enum CState{
		hidden = 0,
		empty,
		towerTemp,
		tower, // Should be unused
		towerOffence,
		towerDefence,
		towerIntel,
		destroyedTower,
		destroyedTerrain,
		wall,
		wallDestroyed
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
	}

	// public class WallPrimary : PriorityCallback{
	// 	public WallPrimary(int priority, cellCallback cb, Vector2Int basecell) : base(priority, cb, basecell){
	// 	}
	// }

    public class Cell{
		public CState state;
		public PlayBoard pb;
		public int pNum;
		public Vector2Int loc;
		PriorityCB shootDef;
		PriorityCB buildDef;
		PriorityCB scoutDef;
		List<PriorityCB> shootCBs;
		List<PriorityCB> buildCBs;
		List<PriorityCB> scoutCBs;
        public int duration;
		public bool visible; //Needed?

		public Cell(CState state, int pNum, Vector2Int loc, PlayBoard pb){
			this.pb = pb; // This will never change
			this.pNum = pNum;
			this.loc = loc;
			this.shootCBs = new List<PriorityCB>();
			this.buildCBs = new List<PriorityCB>();
			this.scoutCBs = new List<PriorityCB>();
			this.ChangeState(state, init:true);
		}
		
		public void ChangeState(CState newState, bool init=false){
			//Debug.Log("Changing our state from" + this.state.ToString() + " to " + newState.ToString() + " loc " + this.loc.ToString());
			if (!init){
				this.TearDownSpecialCbs();
			}
			this.SetStateParams(newState);
			this.SetDefaultCB();
			this.SetupSpecialCBs();			
		}

		void SetDefaultCB(){
			switch(this.state){
				//Each State here has to set each of the defaults! If you don't it'll use the ones from the last state
			case CState.empty:
			case CState.wall:
			case CState.towerOffence:
			case CState.towerDefence:
			case CState.towerIntel:
				this.shootDef = new PriorityCB(0, DefShotCB);
				this.buildDef = new PriorityCB(0, DefBuiltCB);
				this.scoutDef = new PriorityCB(0, DefShotCB);
				break;
			case CState.destroyedTerrain:
			case CState.destroyedTower:
				this.shootDef = new PriorityCB(0, NullCB);
				this.buildDef = new PriorityCB(0, NullCB);
				this.scoutDef = new PriorityCB(0, NullCB);
				break;
			default:
				Debug.LogError("SetDefaultCB unhandled Case: " + this.state.ToString());
				break;
			}
		}

		//Add special CBs to other cells
		void SetupSpecialCBs(){
			if(this.state == CState.wall){
				Debug.Log("Adding the wall's special callbacks");
				this.pb.AddCellCallback(this.pNum, new Vector2Int(this.loc.x, this.loc.y -1), new PriorityCB(5, this.WallCB), PCBType.shoot);
				this.pb.AddCellCallback(this.pNum, new Vector2Int(this.loc.x, this.loc.y -2), new PriorityCB(6, this.WallCB), PCBType.shoot);
			}
		}

		void TearDownSpecialCbs(){
			if(this.state == CState.wall){
				Debug.Log("removing the wall's special callbacks");
				this.pb.RemCellCallback(this.pNum, new Vector2Int(this.loc.x, this.loc.y -1), new PriorityCB(5, this.WallCB), PCBType.shoot);
				this.pb.RemCellCallback(this.pNum, new Vector2Int(this.loc.x, this.loc.y -2), new PriorityCB(6, this.WallCB), PCBType.shoot);
			}
		}

		public bool AddCB(PriorityCB cb, PCBType cbt){
			Debug.Log("Cell " + this.loc.ToString() + " adding CB!");
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
			Debug.Log("Cell " + this.loc.ToString() + " removing CB!");
			switch(cbt){
			case PCBType.shoot:
				this.shootCBs.Remove(cb);
				break;
			case PCBType.build:
				this.buildCBs.Remove(cb);
				break;
			case PCBType.scout:
				this.scoutCBs.Remove(cb);
				break;
			default:
				Debug.LogError("Cell rem   nbCB: Unhandled state! " + cbt.ToString());
				return false;
			}
			return true;
		}

		void SetStateParams(CState newState){
			this.state = newState;
			//May need more here in the future...
		}
		
		//Public calls for basic actions
        public void onShoot(ActionReq ar){
			Debug.Log("Cell: " + this.loc.ToString() + " OnShoot");
			this.ExecPriorityList(this.shootCBs, this.shootDef, ar);
		}

		public void onBuild(ActionReq ar){
			Debug.Log("Cell: " + this.loc.ToString() + " onBuild");
			this.ExecPriorityList(this.buildCBs, this.buildDef, ar);
		}

		public void onScout(ActionReq ar){
			Debug.Log("Cell: " + this.loc.ToString() + " onScout");
			this.ExecPriorityList(this.scoutCBs, this.scoutDef, ar);
		}

		void ExecPriorityList(List<PriorityCB> pcbl, PriorityCB def, ActionReq ar){
			Debug.Log("Cell: Executing priorty list. " + pcbl.Count().ToString());
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
			Debug.Log("Cellinfo: Loc: " + this.loc.ToString() + ", State: " + this.state.ToString());
			Debug.Log("Cellinfo cont: cbshot: " + this.shootCBs.Count().ToString() + ", cbbuild:" +this.buildCBs.Count().ToString() + ", cbscout:" + this.scoutCBs.Count().ToString());
		}

		/////////////Callbacks
		bool WallCB(ActionReq ar){
			Debug.Log("CB handler: WallCB loc" + this.loc.ToString());
			this.ChangeState(CState.wallDestroyed);
			return true;
		}

		bool DefBuiltCB(ActionReq ar){
			Debug.Log("CB handler: DefBuiltCB loc " + this.loc.ToString());
			switch(ar.a){
			case pAction.buildOffenceTower:
				this.ChangeState(CState.towerOffence);
				break;
			case pAction.buildDefenceTower:
				this.ChangeState(CState.towerDefence);
				break;
			case pAction.buildIntelTower:
				this.ChangeState(CState.towerIntel);
				break;
			case pAction.buildWall:
				this.ChangeState(CState.wall);
				break;
			default:
				Debug.LogError("EmptyBuildCB: Default case unhandled. " + ar.a.ToString());
				return false;
			}
			return true;
		}

		bool DefShotCB(ActionReq ar){
			Debug.Log("CB handler: DefShotCB loc " + this.loc.ToString());
			if (new List<CState>(){CState.towerDefence, CState.towerOffence, CState.towerIntel}.Contains(this.state)){
			this.ChangeState(CState.destroyedTower);
			}
			else if (this.state == CState.empty);
			this.ChangeState(CState.destroyedTerrain);
			return true;
		}

		//TODO dos this need to be unique for shot,built,scouted?
		bool NullCB(ActionReq ar){
			Debug.Log("CB handler: NullCB loc " + this.loc.ToString() + ". Do nothing. State: " + this.state.ToString());
			return true;
		}

		bool DefScoutedCB(ActionReq ar){
			//Debug.Log("Handling getting scouted at loc " + this.loc.ToString());
			Debug.LogWarning("CB handler: DefScoutedCB. Warning: not implemented");
			return true;
		}
	}
}