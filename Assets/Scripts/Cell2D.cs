using System.Collections;
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
	public Sprite defenceGridBlock;
	//Molestuff
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
			else{
				Debug.LogError("Cell2D init: Unhandled name " + sr.name);
			}
		}
		this.moleTextObj = this.transform.Find("MoleText").gameObject;
		this.moleText = this.moleTextObj.GetComponent<TextMesh>();
		this.moleTextObj.SetActive(false);
		this.srbg = srlist[0];
		this.srmain = srlist[1];
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
			this.srmain.sprite = this.reflector;
			break;
		default:
			Debug.LogError("Unhandled state: " + this.cStruct.ToString());
			break;
		}
		//Set destroyed state
		if(newCS.destroyed){
			this.srdestroyed.sprite = this.destroyed;
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
			this.moleTextObj.SetActive(true);
			this.moleText.text = this.molecount.ToString();
		}
		else{
			this.molecount = 0;
			this.moleTextObj.SetActive(false);
		}
		//Update takeover info
		this.defected = newCS.defected;
		if(this.defected){
			this.srdefected.sprite = this.defectSprite;
		}
		else{
			this.srdefected.sprite = null;
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
