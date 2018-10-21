using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameStateDisplay : MonoBehaviour {
	Text displayText;

	void Start () {
		this.displayText = GetComponentInChildren<Text>();
	}

	public void SetDisplay(string msg){
		this.displayText.text = msg;
	}
}
