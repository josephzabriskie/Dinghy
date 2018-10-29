using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionSelectGroup : MonoBehaviour {
    ActionSelectButton[] buttons;
	void Start () {
        this.buttons = GetComponentsInChildren<ActionSelectButton>();
	}

    public void RegisterCallbacks(PlayerConnectionObj pobj){
        for (int i = 0; i < this.buttons.Length; i++){
		    this.buttons[i].RegisterCallback(pobj);
        }
	}

    public void DeregisterCallbacks(){
        for (int i = 0; i < this.buttons.Length; i++){
		    this.buttons[i].DeregisterCallbacks();
        }
    }

    public void SetButtonEnabled(bool en){
        for(int i = 0; i < this.buttons.Length; i++){
            this.buttons[i].SetEnabled(en);
        }
    }
}
