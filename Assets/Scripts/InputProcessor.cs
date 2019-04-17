using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MatchSequence;
using PlayerActions;
using ActionProc;
using CellTypes;

using System.Linq;

namespace ActionProc{
	public enum ActionProcState{ // How do we process input at differnt times?
		reject, // Default reject input, don't save, don't send
		multiTower, // This is multi selection mode at start of game. Save up to 3, send 3
		basicActions, // Here we store only one action in first index. Save 1 send 1
	}
}

//Hay, this should be a singleton
public class InputProcessor : MonoBehaviour {
	public static InputProcessor instance; // singletonio

	List<ActionReq> queuedActions; //A list in case we want to send more than 1 action
	PlayerConnectionObj report = null;
	bool actionLocked = false;
	ActionProcState apc = ActionProcState.reject;
	pAction actionContext = pAction.noAction;
	public PlayBoard2D pb2d;
	Validator v;

	void Awake(){
		if(instance == null){
			//Debug.Log("Setting InputProcessor Singleton");
			instance = this;
		}
		else if(instance != this){
			Destroy(gameObject);
			Debug.LogError("Singleton InputProcessor instantiated multiple times, destroy all but first to awaken");
		}
	}

	void Start () {
		this.queuedActions = new List<ActionReq>();
		this.v = new Validator(ActionProcState.reject);
	}

	public void RegisterReport(PlayerConnectionObj r){
		this.report = r;
	}

	//Called when it's time to send out actions by player connection obj
	public List<ActionReq> GetQueuedActions(){
		List<ActionReq> retList = new List<ActionReq>{};
		foreach(ActionReq ar in this.queuedActions){
			if(this.v.Validate(ar, this.report.latestPlayerGrid, this.report.latestEnemyGrid, new Vector2(pb2d.sizex, pb2d.sizey), this.report.latestCapitolLocs)){
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
			UIController.instance.ActionDisplayClear();
			break;
		case ActionProcState.basicActions:
			this.queuedActions.Clear();
			this.queuedActions.AddRange(new List<ActionReq> {
				new ActionReq(this.report.playerId, this.report.playerId, pAction.noAction, null),
				new ActionReq(this.report.playerId, this.report.playerId, pAction.noAction, null)});
			UIController.instance.ActionDisplayUpdateShoot(this.queuedActions[0]);
			UIController.instance.ActionDisplayUpdateAction(this.queuedActions[1]);
			break;
		default:
			Debug.LogError("SetActionProcState: Unhandled!!! What? Got: " + s.ToString());
			break;
		}
	}

	public void ClearActionContext(){
		this.SetActionContext(pAction.noAction);
	}
	
	//Buttons should either call this so we know what we're supposed to do when clicking on the grid or...
	public void SetActionContext(pAction action){
		List<pAction> noTarget = new List<pAction>{pAction.hellFire, pAction.blockingShot, pAction.flare};
		this.actionContext = action;
		this.pb2d.actionContext = this.actionContext;
		UIController.instance.ActionSelectButtonGrpHighlight(action);
		if(noTarget.Contains(action)){ //No target for this action, just set it and move on with your life
			switch(this.apc){
			case ActionProcState.reject:
			case ActionProcState.multiTower:
				Debug.Log("APC Reject: Ignoring input from SetTargetlessAction");
				break;
			case ActionProcState.basicActions:
				Debug.Log("Setting single action: " + action.ToString());
				//Right now, all non targeted actions hit enemy, may need to change in the future
				ActionReq newAR = new ActionReq(this.report.playerId, this.report.enemyId, action, new Vector2[0]);
				if(this.v.Validate(newAR, this.report.latestPlayerGrid, this.report.latestEnemyGrid, new Vector2(pb2d.sizex, pb2d.sizey), this.report.latestCapitolLocs)){ //TODO do we really need to validate here?
					Debug.Log("Validated action: " + newAR.ToString());
					this.queuedActions[1] = newAR;
					this.pb2d.SetActions(this.queuedActions); //clear old action selection
					UIController.instance.ActionDisplayUpdateAction(newAR);
				}
				else{
					Debug.Log("Bad action: " + newAR.ToString());
				}
				break;
			default:
				Debug.LogError("SetActionContext unhandled actionprocstate: " + this.apc.ToString());
				break;
			}
		}
	}

	public void RXInput(bool pGrid, Vector2 pos, CellStruct cs){
		//Don't need to ensure local player, grid only assigned to localplayer
		if (this.actionLocked){
			//Debug.Log("Got input, but we're already locked... ignoring");
			return;
		}
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
			Dictionary<pAction, CellStruct> buildDict = new Dictionary<pAction, CellStruct>{
				{pAction.buildOffenceTower, new CellStruct(CBldg.towerOffence)},
				{pAction.buildDefenceTower, new CellStruct(CBldg.towerDefence)},
				{pAction.buildIntelTower, new CellStruct(CBldg.towerIntel)}};
			CellStruct[][,] currentGrids = this.pb2d.GetGridStates();
			ActionReq MultiTowerAR = new ActionReq(this.report.playerId, this.report.playerId, this.actionContext, new Vector2[]{pos});
			if(this.v.Validate(MultiTowerAR, currentGrids[0], currentGrids[1], pb2d.GetGridVec2Size(), this.report.latestCapitolLocs)){
				idx = this.queuedActions.FindIndex(x => x.a == pAction.noAction);
				if(idx >=0){
					Debug.Log("Input processor: Valid Move: Add new AR at " + idx.ToString() + ", ar: " + MultiTowerAR.ToString());
					this.queuedActions[idx] = MultiTowerAR;
					this.pb2d.SetCellStruct(true, pos, buildDict[this.actionContext]);
					break;
				}
			}
			else{
				Debug.Log("Input processor: Invalid request, don't add to list");
			}
			if (this.v.BldgIn(currentGrids[0], pos, buildDict.Values.ToList().Select(cstruct => cstruct.bldg).ToList())){ // Now, if there is a tower here already, remove it
				idx = this.queuedActions.FindIndex(x => buildDict.Keys.ToList().Contains(x.a) && x.loc != null && x.loc[0] == pos);
				if(idx >= 0){
					Debug.Log("Input processor: Removing AR that is in this place");
					this.queuedActions[idx] = new ActionReq(this.report.playerId, this.report.playerId, pAction.noAction, null);
					this.pb2d.SetCellStruct(true, pos, new CellStruct(CBldg.empty));
				}
			}
			break;
		case ActionProcState.basicActions:
			ActionReq singleAR = new ActionReq(this.report.playerId, this.report.playerId, pAction.noAction, null); // NoAction should always fail eval
			int target = pGrid ? this.report.playerId : this.report.enemyId;
			switch(this.actionContext){ // May not need this to be a switch statement after refactor... lol TODO
			case pAction.noAction:
				break; // don't do nuthin if no action context
			case pAction.blockingShot://These guys don't need a target, don't do anything here, assume handled by set action context
			case pAction.hellFire:
			case pAction.flare:
				break;
			case pAction.fireBasic: // All of these single targeted actions can be handled the same way
			case pAction.scout: // All differences in placement rules are handled by the validator
			case pAction.buildDefenceTower:
			case pAction.buildOffenceTower:
			case pAction.buildIntelTower:
			case pAction.buildWall:
			case pAction.fireAgain:
			case pAction.fireRow: // this is multi location, but still single targeted shot
			case pAction.fireSquare: // this is multi location, but still single targeted shot
			case pAction.placeMine:
			case pAction.buildDefenceGrid:
			case pAction.buildReflector:
			case pAction.firePiercing:
			case pAction.placeMole:
			case pAction.towerTakeover:
				singleAR = new ActionReq(this.report.playerId, target, this.actionContext, new Vector2[]{pos});
				break;
			case pAction.fireReflected:
				Debug.LogError("Player input should never be able to make this action! " + this.actionContext.ToString());
				break;
			default:
				Debug.LogError("Input processor unhandled actionContext: " + this.actionContext.ToString());	
				break;
			}
			if(this.v.Validate(singleAR, this.report.latestPlayerGrid, this.report.latestEnemyGrid, pb2d.GetGridVec2Size(), this.report.latestCapitolLocs)){
				Debug.Log("Validated action: " + singleAR.ToString());
				if(singleAR.a == pAction.fireBasic){
					this.queuedActions[0] = singleAR;
				}
				else{
					this.queuedActions[1] = singleAR;
				}
				this.pb2d.SetActions(this.queuedActions); //add new action select
				UIController.instance.ActionDisplayUpdateShoot(singleAR);
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
	
	////////////////Lock Stuff
	public void LockAction(){
		if (this.queuedActions.Any(req => req.a != pAction.noAction)){
			//Debug.Log("LockactionPressed pid: " + this.report.playerId.ToString());
			this.actionLocked = true;
			UIController.instance.LockButtonEnabled(false);
			this.report.CmdSendPlayerLock();
		}
	}

	//Unlock and wipe queued Action - Do I need this?
	public void UnlockAction(){
		this.actionLocked = false;
		UIController.instance.LockButtonEnabled(true);
	}
}
