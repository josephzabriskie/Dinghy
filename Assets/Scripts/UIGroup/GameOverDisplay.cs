using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerActions;
using UnityEngine.UI;

public class GameOverDisplay : MonoBehaviour {

	Text txt;
    public string wintext = "Game Over\nYou Win!";
    public string losetext = "Game Over\nYou Lose!";

	void Start () {
		this.txt = GetComponentInChildren<Text>();
	}

	public void Show(bool won){
		if (won){
			this.txt.text = this.wintext;
		}
		else {
			this.txt.text = losetext;
		}
        this.gameObject.SetActive(true);
	}

	public void Hide(){
		this.gameObject.SetActive(false);
	}
}
