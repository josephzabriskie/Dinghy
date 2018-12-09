using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MatchSequence;
using PlayerActions;
using ActionProc;
using CellTypes;
using CellUIInfo;
using System.Linq;

namespace ActionProc{
	public enum ActionProcState{ // How do we process input at differnt times?
		reject, // Default reject input, don't save, don't send
		multiTower, // This is multi selection mode at start of game. Save up to 3, send 3
		singleAction, // Here we store only one action in first index. Save 1 send 1
	}
}

public class InputProcessor : MonoBehaviour {
	List<ActionReq> queuedActions; //A list in case we want to send more than 1 action
	PlayerConnectionObj report = null;
	bool actionLocked = false;
	ActionProcState apc = ActionProcState.reject;
	pAction actionContext = pAction.noAction;
	public UIController uic;
	public PlayBoard2D pb;

	void Start () {
		this.queuedActions = new List<ActionReq>();
	}

	public void RegisterReport(PlayerConnectionObj r){
		this.report = r;
	}

	public void SetActionContext(pAction pa){
		this.actionContext = pa;
	}

	public void SetActionProcState(ActionProcState s){
		this.apc = s;
		switch(s){
		case ActionProcState.reject:
			this.queuedActions.Clear();
			break;
		case ActionProcState.multiTower:
			this.queuedActions.Clear();
			this.queuedActions.AddRange(new List<ActionReq>{ // currently hold 3. 1 for each initial tower allowed to place
				new ActionReq(this.report.playerId, pAction.noAction, null),
				new ActionReq(this.report.playerId, pAction.noAction, null),
				new ActionReq(this.report.playerId, pAction.noAction, null)});
			this.uic.ActionDisplayUpdate(this.queuedActions[0]);
			break;
		case ActionProcState.singleAction:
			this.queuedActions.Clear();
			this.queuedActions.AddRange(new List<ActionReq> {new ActionReq(this.report.playerId, pAction.noAction, null)});
			this.uic.ActionDisplayUpdate(this.queuedActions[0]);
			break;
		default:
			Debug.LogError("SetActionProcState: Unhandled!!! What? Got: " + s.ToString());
			break;
		}
	}

	public List<ActionReq> GetQueuedActions(){
		return this.queuedActions;
	}

	public void ClearQueuedActions(){
		this.queuedActions.Clear();
	}

	public void RXInput(bool pGrid, InputType it, Vector2 pos, CState state, SelState selstate){
		//Don't need to ensure local player, grid only assigned to localplayer
		if (this.actionLocked){
			Debug.Log("Got input, but we're already locked... ignoring");
			return;
		}
		if(it == InputType.hover){ // Handle hover input. Probably will depend on current action context, don't worry about that now
			this.pb.ClearSelectionState(true);
			if(selstate != SelState.select){
				this.pb.SetCellBGState(pGrid, pos, SelState.selectHover);
			}
		}
		else if(it == InputType.click){ // Handle click input
			switch(this.apc){
			case ActionProcState.reject:
				Debug.Log("APC Reject: Ignoring input from grid");
				//Do nothing, we ignore the request
				break;
			case ActionProcState.multiTower:
				List<pAction> allowedActions = new List<pAction>(){pAction.buildOffenceTower, pAction.buildDefenceTower, pAction.buildIntelTower};
				List<CState> resultingState = new List<CState>(){CState.towerOffence, CState.towerDefence, CState.towerIntel};
				if (!pGrid){
					Debug.Log("Rejected input during Multitower placement, not our grid");
					break; // do nothing if not our grid, do nothing if no action selected
				}
 				if(!allowedActions.Contains(this.actionContext)){
					Debug.Log("Rejected input during Multitower placement, bad context: " + this.actionContext.ToString());
					break; // do nothing if not our grid, do nothing if no action selected
				}
				//Does this location already exist within our queuedactions?
				int idx = this.queuedActions.FindIndex(x => allowedActions.Contains(x.a) && x.coords[0] == pos);
				//Debug.Log("APC multitower: check for dup result " + idx.ToString());
				if (idx >=0 ){ // We've already got this guy selected, deselect it
					//Debug.Log("APC multitower: Already got this one, toggle off");
					this.queuedActions[idx] = new ActionReq(this.report.playerId, pAction.noAction, null);
					this.pb.SetCellMainState(true, pos, CState.empty);
					break;
				}
				//Do we already have a tower of this type?
				idx = this.queuedActions.FindIndex(x => x.a == this.actionContext);
				if (idx >= 0){
					Debug.Log("multiTower proc: Already have a " + this.actionContext.ToString());
					break;
				}
				idx = this.queuedActions.FindIndex(x => x.a == pAction.noAction);
				//Debug.Log("APC multitower: check for open result " + idx.ToString());
				if(idx >=0){ // We've still have room for a new request
					//Debug.Log("APC multitower: we have room at idx " + idx.ToString());
					this.queuedActions[idx] = new ActionReq(this.report.playerId, this.actionContext, new Vector2[]{pos});
					CState s = resultingState[allowedActions.IndexOf(this.actionContext)];
					this.pb.SetCellMainState(true, pos, s);
				}
				else{ // No room for another tower selection, ignore
					//Debug.Log(" APC multitower: no room left! ignoring");
				}
				break;
			case ActionProcState.singleAction:
				switch(this.actionContext){
				case pAction.noAction:
					break; // don't do nuthin if no action context
				case pAction.fireBasic:
					if(pGrid){
						break; //Don't want to shoot yourself...or do you?
					}
					this.queuedActions[0] = new ActionReq(this.report.playerId, pAction.fireBasic, new Vector2[]{pos});
					this.uic.ActionDisplayUpdate(this.queuedActions[0]);
					this.pb.ClearSelectionState(false);
					this.pb.SetCellBGState(pGrid, pos, SelState.select);
					break;
				case pAction.scout:
					if (pGrid){
						break; //Don't scout yourself...
					}
					this.queuedActions[0] = new ActionReq(this.report.playerId, pAction.scout, new Vector2[]{pos});
					this.uic.ActionDisplayUpdate(this.queuedActions[0]);
					this.pb.ClearSelectionState(false);
					this.pb.SetCellBGState(pGrid, pos, SelState.select);
					break;
				case pAction.buildTower:
					if(!pGrid){
						break; //Don't build on their side...
					}
					this.queuedActions[0] = new ActionReq(this.report.playerId, pAction.buildTower, new Vector2[]{pos});
					this.uic.ActionDisplayUpdate(this.queuedActions[0]);
					this.pb.ClearSelectionState(false);
					this.pb.SetCellBGState(pGrid, pos, SelState.select);
					break;
				case pAction.buildWall:
					if(!pGrid){
						break; // don't build wall on their side
					}
					this.queuedActions[0] = new ActionReq(this.report.playerId, pAction.buildWall, new Vector2[]{pos});
					this.uic.ActionDisplayUpdate(this.queuedActions[0]);
					this.pb.ClearSelectionState(false);
					this.pb.SetCellBGState(pGrid, pos, SelState.select);
					break;
				default:
					Debug.LogError("Input processor unhandled actionContext: " + this.actionContext.ToString());	
					break;
				}
				break;
			default:
				Debug.LogError("Input processor unhandled actionprocstate: " + this.apc.ToString());
				break;
			}
		}
		else{
			Debug.LogError("Hey in our RXInput we didn't get a handled input type: " + it.ToString());
		}
	}
	
	////////////////Lock Stuff
	public void LockAction(){
		if (this.queuedActions.Any(req => req.a != pAction.noAction)){
			Debug.Log("LockactionPressed pid: " + this.report.playerId.ToString());
			this.actionLocked = true;
			this.uic.LockButtonEnabled(false);
			this.report.CmdSendPlayerLock();
		}
	}

	//Unlock and wipe queued Action - Do I need this?
	public void UnlockAction(){
		this.actionLocked = false;
		this.uic.LockButtonEnabled(true);
	}
}
