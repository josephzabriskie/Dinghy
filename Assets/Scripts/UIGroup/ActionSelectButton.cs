using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayerActions;

public class ActionSelectButton : MonoBehaviour {
	Button button = null;
    public pAction action;

	void Start () {
		this.GetButton();
	}

	public void RegisterCallback(InputProcessor ip){
		this.GetButton();
		this.button.onClick.AddListener(delegate{ip.SetActionContext(action);});
	}

	void GetButton(){ // There's a problem where group grabs this and trys to register before it's ready.
		if (this.button == null){
			this.button = gameObject.GetComponent<Button>();
		}
	}

	public void DeregisterCallbacks(){
		this.button.onClick.RemoveAllListeners();
	}

    public void SetEnabled(bool en){
        this.button.interactable = en;
    }
}
