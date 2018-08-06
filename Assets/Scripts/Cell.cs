using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour {
	SpriteRenderer sr;
	public Sprite fog;
	public Vector2 coords;

	public enum CState{
		hidden,
		empty,
		tower
	}
	public CState state;

	// Use this for initialization
	void Start () {
		this.sr = this.GetComponent<SpriteRenderer>();
		this.SetState(CState.hidden);
	}

	public void SetColor(Color c){
		this.sr.color = c;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnMouseEnter(){
		sr.color = Color.black;
	}

	void OnMouseExit(){
		sr.color = Color.white;
	}

	public void SetState(CState newState){
		
	}

	public CState getState(){
		return CState.hidden;
	}
}
