﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LockButton : MonoBehaviour {
	Button button;

	void Start () {
		this.button = GetComponent<Button>();
	}

	public void RegisterCallback(PlayerConnectionObj pobj){
		this.button.onClick.AddListener(pobj.LockAction);
	}

	public void DeregisterCallbacks(){
		this.button.onClick.RemoveAllListeners();
	}

	public void SetLocked(bool l){
		this.button.interactable = l;
	}
}
