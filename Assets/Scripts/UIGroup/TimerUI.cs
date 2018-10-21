using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerUI : MonoBehaviour {
	Text timerText;
	Coroutine currentCoroutine = null;
	string defaultText;

	void Start(){
		this.timerText = GetComponentInChildren<Text>();
		this.defaultText = this.timerText.text;
	}

	//Kills counting down coroutine if it's active, leaves the last number intact
	public void StopTimer(){
		if (this.currentCoroutine != null){
			StopCoroutine(this.currentCoroutine);
		}
		this.currentCoroutine = null;
	}

	public void StartTimer(int time){
		this.StopTimer();
		this.currentCoroutine = StartCoroutine(this.Countdown(time));
	}

	public void ClearTimer(){
		this.StopTimer();
		timerText.text = this.defaultText;
	}

	IEnumerator Countdown(int time)
	{
		int currTime = time;
		while (currTime >= 0)
		{
			timerText.text = currTime.ToString();
			yield return new WaitForSeconds(1.0f);
			currTime--;
		}
	}
}
