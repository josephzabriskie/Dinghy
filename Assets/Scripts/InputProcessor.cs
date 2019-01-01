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
		basicActions, // Here we store only one action in first index. Save 1 send 1
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
	Validator v;

	void Start () {
		this.queuedActions = new List<ActionReq>();
		this.v = new Validator(ActionProcState.reject);
	}

	public void RegisterReport(PlayerConnectionObj r){
		this.report = r;
	}

	public void SetActionContext(pAction pa){
		this.actionContext = pa;
		this.uic.ActionSelectGroupHighlightPanel(pa);
	}

	public void ClearActionContext(){
		this.SetActionContext(pAction.noAction);
	}

	public void SetActionProcState(ActionProcState s){
		this.apc = s;
		this.v.SetAPC(s);
		switch(s){
		case ActionProcState.reject:
			this.queuedActions.Clear();
			break;
		case ActionProcState.multiTower:
			this.queuedActions.Clear();
			this.queuedActions.AddRange(new List<ActionReq>{ // currently hold 3. 1 for each initial tower allowed to place
				new ActionReq(this.report.playerId, this.report.playerId, pAction.noAction, null),
				new ActionReq(this.report.playerId, this.report.playerId, pAction.noAction, null),
				new ActionReq(this.report.playerId, this.report.playerId, pAction.noAction, null)});
			this.uic.ActionDisplayClear();
			break;
		case ActionProcState.basicActions:
			this.queuedActions.Clear();
			this.queuedActions.AddRange(new List<ActionReq> {
				new ActionReq(this.report.playerId, this.report.playerId, pAction.noAction, null),
				new ActionReq(this.report.playerId, this.report.playerId, pAction.noAction, null)});
			this.uic.ActionDisplayUpdateShoot(this.queuedActions[0]);
			this.uic.ActionDisplayUpdateAction(this.queuedActions[1]);
			break;
		default:
			Debug.LogError("SetActionProcState: Unhandled!!! What? Got: " + s.ToString());
			break;
		}
	}

	public List<ActionReq> GetQueuedActions(){
		List<ActionReq> retList = new List<ActionReq>{};
		foreach(ActionReq ar in this.queuedActions){
			if(this.v.Validate(ar, this.report.latestPlayerGrid, this.report.latestEnemyGrid, new Vector2(pb.sizex, pb.sizey))){
				retList.Add(ar);
			}
			else{
				Debug.LogWarning("Validation failed! Don't send AR: " + ar.ToString());
			}
		}
		return retList;
	}

	public void ClearQueuedActions(){
		this.queuedActions.Clear();
	}

	public void RXInput(bool pGrid, InputType it, Vector2 pos, CState state){
		//Don't need to ensure local player, grid only assigned to localplayer
		if (this.actionLocked){
			//Debug.Log("Got input, but we're already locked... ignoring");
			return;
		}
		if(it == InputType.hoverEnter){ // Handle hover input. Probably will depend on current action context, don't worry about that now
			//Debug.Log("Hover ON at: " + pos.ToString());
			ActionReq hoverAR = new ActionReq(0, 0, this.actionContext, new Vector2[]{pos});// All that matters here is that context and pos are correct
			this.pb.SetCellsSelect(pGrid, true, true, hoverAR);
		}
		else if(it == InputType.hoverExit){
			//Debug.Log("Hover OFF at: " + pos.ToString());
			ActionReq hoverAR = new ActionReq(0, 0, this.actionContext, new Vector2[]{pos}); // All that matters here is that context and pos are correct
			this.pb.SetCellsSelect(pGrid, false, true, hoverAR);
		}
		else if(it == InputType.clickDown){ // Handle click input
			switch(this.apc){
			case ActionProcState.reject:
				Debug.Log("APC Reject: Ignoring input from grid");
				//Do nothing, we ignore the request
				break;
			case ActionProcState.multiTower:
				if (!pGrid){ // Only build on our side
					break; // Our validator would catch this, but we do some more logic below that relies on this assumption
				}
				int idx;
				Dictionary<pAction, CState> buildDict = new Dictionary<pAction, CState>{
					{pAction.buildOffenceTower, CState.towerOffence},
					{pAction.buildDefenceTower, CState.towerDefence},
					{pAction.buildIntelTower, CState.towerIntel}};
				CState[][,] currentGrids = this.pb.GetGridStates();
				ActionReq MultiTowerAR = new ActionReq(this.report.playerId, this.report.playerId, this.actionContext, new Vector2[]{pos});
				if(this.v.Validate(MultiTowerAR, currentGrids[0], currentGrids[1], new Vector2(pb.sizex, pb.sizey))){
					idx = this.queuedActions.FindIndex(x => x.a == pAction.noAction);
					if(idx >=0){
						Debug.Log("Input processor: Valid Move: Add new AR at " + idx.ToString() + ", ar: " + MultiTowerAR.ToString());
						this.queuedActions[idx] = MultiTowerAR;
						this.pb.SetCellMainState(true, pos, buildDict[this.actionContext]);
						break;
					}
				}
				else{
					Debug.Log("Input processor: Invalid request, don't add to list");
				}
				if (this.v.StateIn(currentGrids[0], pos, buildDict.Values.ToList(), new Vector2(pb.sizex, pb.sizey))){ // Now, if there is a tower here already, remove it
					idx = this.queuedActions.FindIndex(x => buildDict.Keys.ToList().Contains(x.a) && x.loc != null && x.loc[0] == pos);
					if(idx >= 0){
						Debug.Log("Input processor: Removing AR that is in this place");
						this.queuedActions[idx] = new ActionReq(this.report.playerId, this.report.playerId, pAction.noAction, null);
						this.pb.SetCellMainState(true, pos, CState.empty);
					}
				}
				break;
			case ActionProcState.basicActions:
				ActionReq singleAR = new ActionReq(this.report.playerId, this.report.playerId, pAction.noAction, null); // NoAction should always fail eval
				switch(this.actionContext){ // May not need this to be a switch statement after refactor... lol TODO
				case pAction.noAction:
					break; // don't do nuthin if no action context
				case pAction.fireBasic: // All of these single targeted actions can be handled the same way
				case pAction.scout: // All differences in placement rules are handled by the validator
				case pAction.buildTower:
				case pAction.buildDefenceTower:
				case pAction.buildOffenceTower:
				case pAction.buildIntelTower:
				case pAction.buildWall:
				case pAction.fireAgain:
				case pAction.fireRow: // this is multi location, but still single targeted shot
				case pAction.fireSquare:
					int target = pGrid ? this.report.playerId : this.report.enemyId;
					singleAR = new ActionReq(this.report.playerId, target, this.actionContext, new Vector2[]{pos});
					break;
				default:
					Debug.LogError("Input processor unhandled actionContext: " + this.actionContext.ToString());	
					break;
				}
				if(this.v.Validate(singleAR, this.report.latestPlayerGrid, this.report.latestEnemyGrid, new Vector2(pb.sizex, pb.sizey))){
					Debug.Log("Validated action: " + singleAR.ToString());
					if(singleAR.a == pAction.fireBasic){
						this.pb.SetCellsSelect(pGrid, false, false, this.queuedActions[0]); //clear old action selection
						this.queuedActions[0] = singleAR;
						this.pb.SetCellsSelect(pGrid, true, false, this.queuedActions[0]); //add new action select
						this.uic.ActionDisplayUpdateShoot(singleAR);
					}
					else{
						this.pb.SetCellsSelect(pGrid, false, false, this.queuedActions[1]); //clear old action selection
						this.queuedActions[1] = singleAR;
						this.pb.SetCellsSelect(pGrid, true, false, this.queuedActions[1]); //add new action selectf
						this.uic.ActionDisplayUpdateAction(singleAR);
					}
				}
				else{
					Debug.Log("Bad action: " + singleAR.ToString());
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
			//Debug.Log("LockactionPressed pid: " + this.report.playerId.ToString());
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
