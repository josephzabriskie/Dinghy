using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellInfo;
using MatchSequence;
using PlayerActions;
using ActionProc;
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
	public PlayBoard pb;

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

	public void RXInput(bool pGrid, Vector2 pos, CState state){
		//Don't need to ensure local player, grid only assigned to localplayer
		if (this.actionLocked){
			Debug.Log("Got input, but we're already locked... ignoring");
			return;
		}
		//ActionReq ar;
		switch(this.apc){
		case ActionProcState.reject:
			Debug.Log("APC Reject: Ignoring input from grid");
			//Do nothing, we ignore the request
			break;
		case ActionProcState.multiTower:
			if (!pGrid){
				Debug.Log("APC multitower: not our grid, don't do nuthin");
				break;
			}
			//In this state, action select buttons should be disabled, all input treated as tower placement
			//Does this location already exist within our queuedactions?
			int idx = this.queuedActions.FindIndex(x => x.a == pAction.placeTower && x.coords[0] == pos);
			//Debug.Log("APC multitower: check for dup result " + idx.ToString());
			if (idx >=0 ){ // We've already got this guy selected, deselect it
				//Debug.Log("APC multitower: Already got this one, toggle off");
				this.queuedActions[idx] = new ActionReq(this.report.playerId, pAction.noAction, null);
				this.pb.SetCellState(true, pos, CState.empty);
				break;
			}
			idx = this.queuedActions.FindIndex(x => x.a == pAction.noAction);
			//Debug.Log("APC multitower: check for open result " + idx.ToString());
			if(idx >=0){ // We've still have room for a new request
				//Debug.Log("APC multitower: we have room at idx " + idx.ToString());
				this.queuedActions[idx] = new ActionReq(this.report.playerId, pAction.placeTower, new Vector2[]{pos});
				this.pb.SetCellState(true, pos, CState.towerTemp);
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
				break;
			case pAction.scout:
				if (pGrid){
					break; //Don't scout yourself...
				}
				this.queuedActions[0] = new ActionReq(this.report.playerId, pAction.scout, new Vector2[]{pos});
				this.uic.ActionDisplayUpdate(this.queuedActions[0]);
				break;
			case pAction.placeTower:
				if(!pGrid){
					break; //Don't build on their side...
				}
				this.queuedActions[0] = new ActionReq(this.report.playerId, pAction.placeTower, new Vector2[]{pos});
				this.uic.ActionDisplayUpdate(this.queuedActions[0]);
				break;
			}
			break;
			//Todo here, highlight selected square coords
		default:
			break;
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
