﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellInfo;

namespace CellInfo{
	public enum CState{
		hidden,
		empty,
		towerTemp,
		tower,
		destroyedTower,
		destroyedTerrain
	}
}

public class Cell : MonoBehaviour {
	SpriteRenderer srbg; // sr of the bg
	SpriteRenderer srcover; // sr of the covering image
	public Sprite fog;
	public Sprite tower;
	public Sprite towerTemp;
	public Sprite destroyedTower;
	public Sprite destroyedTerrain;
	public Vector2 coords;
	public CState state;
	public GameGrid parentGrid;

	//temp
	Color defaultBGColor;

	// Use this for initialization
	void Start () {
		SpriteRenderer[] srlist = this.GetComponentsInChildren<SpriteRenderer>();
		this.srbg = srlist[0];
		this.srcover = srlist[1];
		this.SetState(CState.hidden);
		this.SetBGColor(Color.white);
	}

	public void SetBGColor(Color c){
		this.srbg.color = c;
		this.defaultBGColor = c;
	}

	public void Flip(){
		this.transform.Rotate(0.0f, 0.0f, 180.0f);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnMouseEnter(){
		srbg.color = Color.black;
	}

	void OnMouseExit(){
		srbg.color = this.defaultBGColor;
	}

	void OnMouseDown(){
		//this.SetColor(Color.red);
		Debug.Log("Clicked on: " + this.coords.ToString() + "State currently " + this.state.ToString());
		this.parentGrid.RXCellInput(this.coords, this.state);
	}


	public void SetState(CState newState){
		this.state = newState;
		switch(this.state){
			case CState.empty:
				this.srcover.sprite = null;
				break;
			case CState.hidden:
				this.srcover.sprite = this.fog;
				break;
			case CState.tower:
				this.srcover.sprite = this.tower;
				break;
			case CState.towerTemp:
				this.srcover.sprite = this.towerTemp;
				break;
			case CState.destroyedTerrain:
				this.srcover.sprite = this.destroyedTerrain;
				break;
			case CState.destroyedTower:
				this.srcover.sprite = this.destroyedTower;
				break;
			default:
				Debug.LogError("Unhandled state: " + this.state.ToString());
				break;
		}
	}

	public CState getState(){
		return this.state;
	}
}
