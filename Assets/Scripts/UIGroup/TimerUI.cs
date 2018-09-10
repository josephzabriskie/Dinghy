using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerUI : MonoBehaviour {
	Text timerText;
	Coroutine currentCoroutine = null;

	void Start(){
		this.timerText = GetComponentInChildren<Text>();
		this.StartTimer();
	}

	public void StartTimer(){
		this.StopTimer();
		this.currentCoroutine = StartCoroutine(this.Countdown());
	}

	public void StopTimer(){
		if (this.currentCoroutine != null){
			StopCoroutine(this.currentCoroutine);
		}
		this.currentCoroutine = null;
	}

	IEnumerator Countdown(int time = 60)
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
