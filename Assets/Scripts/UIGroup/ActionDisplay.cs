using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerActions;
using UnityEngine.UI;

public class ActionDisplay : MonoBehaviour {

	Text txt;

	void Start () {
		this.txt = GetComponentInChildren<Text>();
	}

	public void UpdateAction(ActionReq newAR){
		if (newAR.a == pAction.noAction){
			this.ClearAction();
		}
		else {
			this.txt.text = "Action: " + newAR.a.ToString() + " at " + newAR.coords[0].ToString();
		}
	}

	public void ClearAction(){
		this.txt.text = "No Action";
	}
}
