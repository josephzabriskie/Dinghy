using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerActions;
using UnityEngine.UI;

public class ActionDisplay : MonoBehaviour {

	public Text shoottxt;
	public Text actiontxt;

	public void UpdateShoottxt(ActionReq newAR){
		if (newAR.a == pAction.noAction){
			this.Clear();
		}
		else {
			this.shoottxt.text = "Shoot: " + newAR.a.ToString() + " at " + newAR.loc[0].ToString();
		}
	}
	public void UpdateActiontxt(ActionReq newAR){
		if (newAR.a == pAction.noAction){
			this.Clear();
		}
		else {
			string locstr = newAR.loc.Length > 0 ? newAR.loc[0].ToString() : "null";
			this.actiontxt.text = "Action: " + newAR.a.ToString() + " at " + locstr;
		}
	}

	public void Clear(){
		this.shoottxt.text = "No shoot";
		this.actiontxt.text = "No action";
	}
}
