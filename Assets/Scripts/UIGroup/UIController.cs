using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayerActions;

//Class handles player requests for modifications to the UI
public class UIController : MonoBehaviour {
	TimerUI tui;
	ActionDisplay ad;
	LockButton lb;
	DebugPanel dbp;
	GameStateDisplay gsd;
	ActionSelectGroup asg;
	GameOverDisplay god;

	void Start () {
		this.tui = this.GetComponentInChildren<TimerUI>();
		this.ad = this.GetComponentInChildren<ActionDisplay>();
		this.lb = this.GetComponentInChildren<LockButton>();
		this.dbp = this.GetComponentInChildren<DebugPanel>();
		this.gsd = this.GetComponentInChildren<GameStateDisplay>();
		this.asg = this.GetComponentInChildren<ActionSelectGroup>();		
		this.god = this.GetComponentInChildren<GameOverDisplay>();
		this.GameOverDisplayHide(); // Hide this till needed. Todo Warning, may depend on script execution order
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
	//Functions to control our action select button group
	//Register
	public void ActionSelectButtonsRegister(PlayerConnectionObj pobj){
		this.asg.RegisterCallbacks(pobj);
	}
	//Deregister
	public void ActionSelectButtonsDeregister(){
		this.asg.DeregisterCallbacks();
	}
	//Enable/disable
	public void ActionSelectButtonsEnable(bool en){
		this.asg.SetButtonEnabled(en);
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
	public void ActionDisplayUpdate(ActionReq ar){
		this.ad.UpdateAction(ar);
	}
	//ClearAction
	public void ActionDisplayClear(){
		this.ad.ClearAction();
	}
	//#############################################
	//Functions to control our lock button
	//SetEnabled
	public void LockButtonEnabled(bool b){
		this.lb.SetEnabled(b);
	}
	//registerAction
	public void LockButtonRegister(PlayerConnectionObj pobj){
		this.lb.RegisterCallback(pobj);
	}
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
