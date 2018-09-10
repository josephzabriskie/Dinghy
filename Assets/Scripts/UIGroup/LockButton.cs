using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LockButton : MonoBehaviour {
	Button button;
	Text buttonText;

	void Start () {
		this.button = GetComponent<Button>();
		this.buttonText = GetComponentInChildren<Text>();
	}

	public void RegisterCallback(PlayerConnectionObj pobj){
		this.button.onClick.AddListener(pobj.LockAction);
	}

	public void SetLocked(bool l){
		this.button.interactable = l;
	}
}
