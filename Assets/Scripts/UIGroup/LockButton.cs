using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LockButton : MonoBehaviour {
	Button button;

	void Start () {
		this.button = GetComponent<Button>();
	}

	// public void RegisterCallback(InputProcessor ip){
	// 	this.button.onClick.AddListener(ip.LockAction);
	// }

	public void DeregisterCallbacks(){
		this.button.onClick.RemoveAllListeners();
	}

	public void SetEnabled(bool en){
		this.button.interactable = en;
	}
}
