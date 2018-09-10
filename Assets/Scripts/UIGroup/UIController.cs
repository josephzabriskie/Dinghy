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

	void Start () {
		this.tui = this.GetComponentInChildren<TimerUI>();
		this.ad = this.GetComponentInChildren<ActionDisplay>();
		this.lb = this.GetComponentInChildren<LockButton>(); // Warning, no script here... Just a button
		this.dbp = this.GetComponentInChildren<DebugPanel>();
	}
	//#############################################
	//Functions to control our timer UI
	//Start
	public void TimerStart(){
		this.tui.StartTimer();
	}
	//Stop
	public void TimerStop(){
		this.tui.StopTimer();
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
		this.lb.SetLocked(b);
	}
	//registerAction
	public void LockButtonRegister(PlayerConnectionObj pobj){
		this.lb.RegisterCallback(pobj);
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
}
