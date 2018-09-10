using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugPanel : MonoBehaviour {
	public Text textLog;
	string[] lines;
	

	// Use this for initialization
	void Start () {
		lines = new string[10];
		this.UpdateLines();
	}

	void UpdateLines(){
		string outstr = "";
		for(int i = 0; i <this.lines.Length; i++){
			outstr += "[" + i.ToString() + "]: " + this.lines[i] + "\n";
		}
		this.textLog.text = outstr;
	}

	public void Log(string msg){
		string saveStr, newStr;
		newStr = msg;
		for(int i = 0; i < this.lines.Length; i++){
			saveStr = this.lines[i];
			this.lines[i] = newStr;
			newStr = saveStr;
		}
		this.UpdateLines();
	}

	public void Clear(){
		lines = new string[10];
		this.UpdateLines();
	}
}