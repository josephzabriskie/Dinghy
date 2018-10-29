using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayerActions;

public class ActionSelectButton : MonoBehaviour {
	Button button;
    public pAction action;

	void Start () {
		this.button = GetComponent<Button>();
	}

	public void RegisterCallback(PlayerConnectionObj pobj){
		this.button.onClick.AddListener(delegate{pobj.SetActionContext(action);});
	}

	public void DeregisterCallbacks(){
		this.button.onClick.RemoveAllListeners();
	}

    public void SetEnabled(bool en){
        this.button.interactable = en;
    }
}
