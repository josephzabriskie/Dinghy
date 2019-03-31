﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellUIInfo;
using CellTypes;

namespace CellUIInfo{
	public enum InputType{
		clickDown,
		hoverEnter,
		hoverExit
	}
}

public class Cell2D : MonoBehaviour {
	//Sprite Renderers
	SpriteRenderer srbg; // sr of the bg
	SpriteRenderer srmain; // sr of the covering image
	SpriteRenderer srdestroyed; // sr of the destroyed image
	SpriteRenderer srdefected; // sr when we defect
	SpriteRenderer srmoleArea; //sr for mole area 
	SpriteRenderer srdefenceGridArea; // sr for def grid area
	SpriteRenderer srmolePanel; // Background to read mole text
	SpriteRenderer srScouted;
	//BldgStatesprites
	public Sprite fog;
	public Sprite tower;
	public Sprite towerOffence;
	public Sprite towerDefence;
	public Sprite towerIntel;
	public Sprite towerTemp;
	public Sprite wall;
	public Sprite blocked;
	public Sprite mine;
	public Sprite defenceGrid;
	public Sprite reflector;
	public Sprite reflectorHidden;
	public Sprite defenceGridArea; // area tha defence grid protects
	//bgState
	public Sprite defaultBG;
	public Sprite selectBG;
	public Sprite selectHoverBG;
	//Other
	public Vector2 coords;
	public CellStruct cStruct;
	public GameGrid2D parentGrid;
	//Hit States
	public Sprite destroyed;
	public Sprite destroyedOld;
	public Sprite defenceGridBlock;
	//Molestuff
	public Sprite moleArea; // Area that the mole detects
	public bool mole;
	public int molecount;
	GameObject moleTextObj;
	TextMesh moleText;
	//Take over/Defection
	public bool defected;
	public Sprite defectSprite;
	//Selections states
	bool hovered = false;
	bool selected = false;
	//Scouted sprite
	public Sprite scouted;

	// Use this for initialization
	void Start () {
		SpriteRenderer[] srlist = this.GetComponentsInChildren<SpriteRenderer>();
		foreach(SpriteRenderer sr in srlist){
			if(sr.name == "Background"){
				this.srbg = sr;
			}
			else if(sr.name == "MainState"){
				this.srmain = sr;
			}
			else if(sr.name == "Destroyed"){
				this.srdestroyed = sr;
			}
			else if(sr.name == "Defected"){
				this.srdefected = sr;
			}
			else if(sr.name == "MoleArea"){
				this.srmoleArea = sr;
			}
			else if(sr.name == "DefenceGridArea"){
				this.srdefenceGridArea = sr;
			}
			else if(sr.name == "MolePanel"){
				this.srmolePanel = sr;
			}
			else if(sr.name == "Scouted"){
				this.srScouted = sr;
			}
			else{
				Debug.LogError("Cell2D init: Unhandled name " + sr.name);
			}
		}
		this.moleTextObj = this.transform.Find("MolePanel/MoleText").gameObject;
		this.moleText = this.moleTextObj.GetComponent<TextMesh>();
		//this.moleTextObj.SetActive(false);
		this.SetCellStruct(new CellStruct(CBldg.hidden));
		this.SetBGColor(Color.white);
	}

	public void SetBGColor(Color c){
		this.srbg.color = c;
	}

	public void Flip(){
		this.transform.Rotate(0.0f, 0.0f, 180.0f);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnMouseEnter(){
		this.parentGrid.RXCellInput(this.coords, InputType.hoverEnter, this.cStruct);
	}

	void OnMouseExit(){
		this.parentGrid.RXCellInput(this.coords, InputType.hoverExit, this.cStruct);
	}

	void OnMouseDown(){
		Debug.Log("Clicked on: " + this.coords.ToString() + "State currently " + this.cStruct.ToString());
		this.parentGrid.RXCellInput(this.coords, InputType.clickDown, this.cStruct);
	}

	public void SetCellStruct(CellStruct newCS){
		this.cStruct = newCS;
		switch(this.cStruct.bldg){
		case CBldg.empty:
			this.srmain.sprite = null;
			break;
		case CBldg.hidden:
			this.srmain.sprite = this.fog;
			break;
		case CBldg.tower:
			this.srmain.sprite = this.tower;
			break;
		case CBldg.towerTemp:
			this.srmain.sprite = this.towerTemp;
			break;
		case CBldg.towerOffence:
			this.srmain.sprite = this.towerOffence;
			break;
		case CBldg.towerDefence:
			this.srmain.sprite = this.towerDefence;
			break;
		case CBldg.towerIntel:
			this.srmain.sprite = this.towerIntel;
			break;
		case CBldg.wall:
			this.srmain.sprite = this.wall;
			break;
		case CBldg.blocked:
			this.srmain.sprite = this.blocked;
			break;
		case CBldg.mine:
			this.srmain.sprite = this.mine;
			break;
		case CBldg.defenceGrid:
			this.srmain.sprite = this.defenceGrid;
			break;
		case CBldg.reflector:
			this.srmain.sprite = newCS.reflected ? this.reflector : this.reflectorHidden;
			break;
		default:
			Debug.LogError("Unhandled state: " + this.cStruct.ToString());
			break;
		}
		//Set destroyed state
		if(newCS.destroyed){
			this.srdestroyed.sprite = newCS.lastHit? this.destroyed : this.destroyedOld;

		}
		else if(newCS.defenceGridBlock){
			this.srdestroyed.sprite = this.defenceGridBlock;
		}
		else{
			this.srdestroyed.sprite = null;
		}
		//Update mole status
		this.mole = newCS.mole;
		if(this.mole){
			this.molecount = newCS.molecount;
			//this.moleTextObj.SetActive(true);
			this.srmolePanel.gameObject.SetActive(true);
			this.moleText.text = this.molecount.ToString();
			this.srmoleArea.sprite = this.moleArea;
		}
		else{
			this.molecount = 0;
			this.srmolePanel.gameObject.SetActive(false);
			//this.moleTextObj.SetActive(false);
			this.srmoleArea.sprite = null;
		}
		//Update takeover info
		this.defected = newCS.defected;
		if(this.defected){
			this.srdefected.sprite = this.defectSprite;
		}
		else{
			this.srdefected.sprite = null;
		}
		//Update DefenceGrid
		if(newCS.bldg == CBldg.defenceGrid && !newCS.destroyed){
			this.srdefenceGridArea.sprite = this.defenceGridArea;
		}
		else{
			this.srdefenceGridArea.sprite = null;
		}
		//Update Scouted
		if(newCS.scouted){
			this.srScouted.sprite = scouted;
		}
		else{
			this.srScouted.sprite = null;
		}

	}

	public void SetHovered(bool hov){
		this.hovered = hov;
		this.UpdateBGState();
	}

	public void SetSelected(bool sel){
		this.selected = sel;
		this.UpdateBGState();
	}

	void UpdateBGState(){
		if(this.hovered){
			this.srbg.sprite = this.selectHoverBG;
		}
		else if(this.selected){
			this.srbg.sprite = this.selectBG;
		}
		else{
			this.srbg.sprite = this.defaultBG;
		}
	}

	public CellStruct GetCellStruct(){
		return this.cStruct;
	}
}
