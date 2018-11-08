using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionSelectGroup : MonoBehaviour {
    ActionSelectButton[] buttons;
    public InputProcessor ip; // Really don't like that I have to put this here... TODO FIX ME/. IP should find us and register itself

	void Start () {
        this.buttons = GetComponentsInChildren<ActionSelectButton>();
        this.RegisterCallbacks();
	}

    void RegisterCallbacks(){
        for (int i = 0; i < this.buttons.Length; i++){
		    this.buttons[i].RegisterCallback(this.ip);
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
