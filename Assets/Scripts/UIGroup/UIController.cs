using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayerActions;
using PlayboardTypes;
using CellTypes;

//Class handles player requests for modifications to the UI
//Also now a singletonio
public class UIController : MonoBehaviour {
	public static UIController instance;
	TimerUI tui;
	ActionDisplay ad;
	LockButton lb;
	DebugPanel dbp;
	GameStateDisplay gsd;
	ActionSelectGroup asg;
	GameOverDisplay god;
	List <ActionSelectButton> lasb;

	void Awake(){
		if(instance == null){
			//Debug.Log("Setting UIController Singleton");
			instance = this;
		}
		else if(instance != this){
			Destroy(gameObject);
			Debug.LogError("Singleton UIController instantiated multiple times, destroy all but first one to awaken");
		}
	}

	void Start () {
		this.tui = this.GetComponentInChildren<TimerUI>();
		this.ad = this.GetComponentInChildren<ActionDisplay>();
		this.lb = this.GetComponentInChildren<LockButton>();
		this.dbp = this.GetComponentInChildren<DebugPanel>();
		this.gsd = this.GetComponentInChildren<GameStateDisplay>();
		this.asg = this.GetComponentInChildren<ActionSelectGroup>();
		this.god = this.GetComponentInChildren<GameOverDisplay>();
		this.lasb = new List<ActionSelectButton>();
		this.GameOverDisplayHide(); // Hide this till needed. Todo Warning, may depend on script execution order
	}

	//#############################################
	//Functions to control all action select buttons
	//Add
	public void ActionSelectButtonGrpAdd(ActionSelectButton asb){
		this.lasb.Add(asb);
	}
	//Remove -- probably not needed, why add a button to remove it?
	public void ActionSelectButtonGrpRmv(ActionSelectButton asb){
		this.lasb.Remove(asb);
	}
	//Highlight
	public void ActionSelectButtonGrpHighlight(pAction action){
		foreach(ActionSelectButton asb in this.lasb){
			if(asb.action == action){
				asb.Highlight(true);
			}
			else{
				asb.Highlight(false);
			}
		}
	}
	//Enable/disable all
	public void ActionSelectButtonGrpEnable(bool en){
		foreach(ActionSelectButton asb in this.lasb){
			asb.Enable(en);
		}
	}
	//UpdateActionAvail info
	public void ActionSelectButtonGrpActionAvailUpdate(List<ActionAvail> actionAvail){
		foreach(ActionSelectButton asb in this.lasb){
			asb.UpdationActionAvail(actionAvail.Find(aa => aa.actionParam.action == asb.action));
		}
	}

	//#############################################
	//Functions to control our gameover display
	//Show
	public void GameOverDisplayShow(bool won){
		this.god.Show(won);
	}
	//Hide
	public void GameOverDisplayHide(){
		this.god.Hide();
	}
	//#############################################
	//Functions to control our action select bar group
	//Update ActionInfo
	public void ActionSelectGroupUpdateActionInfo(List<ActionAvail> aaList){
		this.asg.UpdateActionInfo(aaList);
	}
	public void ActionSelectGroupUpdateFactionProgress(FactionProgress factionProgress, CellStruct[,] playerState, CellStruct[,] enemyState){
		this.asg.UpdateFactionProgress(factionProgress, playerState, enemyState);
	}
	//#############################################
	//Functions to control our timer UI
	//Start
	public void TimerStart(int time){
		this.tui.StartTimer(time);
	}
	//Stop
	public void TimerStop(){
		this.tui.StopTimer();
	}
	//Clear
	public void TimerClear(){
		this.tui.ClearTimer();
	}
	//#############################################
	//Functions to control our Action Display
	//UpdateAction
	public void ActionDisplayUpdateAction(ActionReq ar){
		this.ad.UpdateActiontxt(ar);
	}
	//UpdateShoot
	public void ActionDisplayUpdateShoot(ActionReq ar){
		this.ad.UpdateShoottxt(ar);
	}
	//ClearAction
	public void ActionDisplayClear(){
		this.ad.Clear();
	}
	//#############################################
	//Functions to control our lock button
	//SetEnabled
	public void LockButtonEnabled(bool b){
		this.lb.SetEnabled(b);
	}
	// //registerAction
	// public void LockButtonRegister(InputProcessor ip){
	// 	this.lb.RegisterCallback(ip);
	// }
	//deregisterAction
	public void LockButtonDeregister(){
		this.lb.DeregisterCallbacks();
	}
	//#############################################
	//Functions to write to our debug panel, ignore if no DBP available
	//Write
	public void DBPWrite(string msg){
		if (this.dbp != null){
			this.dbp.Log(msg);
		}
	}
	//Clear
	public void DBPClear(){
		if(this.dbp != null){
			this.dbp.Clear();
		}
	}
	//#############################################
	//Functions to update our game state display
	public void GameStateUpdate(string msg){
		this.gsd.SetDisplay(msg);
	}
}
