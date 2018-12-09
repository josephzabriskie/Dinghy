using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellUIInfo;
using CellTypes;

namespace CellUIInfo{
	public enum SelState{
		def = 0,
		select,
		selectHover
	}
	public enum InputType{
		hover,
		click
	}
}

public class Cell2D : MonoBehaviour {
	SpriteRenderer srbg; // sr of the bg
	SpriteRenderer srmain; // sr of the covering image
	//CoverState
	public Sprite fog;
	public Sprite tower;
	public Sprite towerOffence;
	public Sprite towerDefence;
	public Sprite towerIntel;
	public Sprite towerTemp;
	public Sprite destroyedTower;
	public Sprite destroyedTerrain;
	public Sprite wall;
	public Sprite wallDestroyed;
	//bgState
	public Sprite defaultBG;
	public Sprite selectBG;
	public Sprite selectHoverBG;
	//Other
	public Vector2 coords;
	public CState cState;
	public SelState selState;
	public GameGrid2D parentGrid;
	//temp
	Color defaultBGColor;

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
			else{
				Debug.LogError("Cell2D init: Unhandled name " + sr.name);
			}
		}
		this.srbg = srlist[0];
		this.srmain = srlist[1];
		this.SetMainState(CState.hidden);
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
		this.ReportInput(InputType.hover);
	}

	void OnMouseExit(){
		// don't report this...
	}

	void OnMouseDown(){
		Debug.Log("Clicked on: " + this.coords.ToString() + "State currently " + this.cState.ToString());
		this.ReportInput(InputType.click);
	}

	void ReportInput(InputType it){
		this.parentGrid.RXCellInput(this.coords, it, this.cState, this.selState);
	}

	public void SetMainState(CState newState){
		this.cState = newState;
		switch(this.cState){
			case CState.empty:
				this.srmain.sprite = null;
				break;
			case CState.hidden:
				this.srmain.sprite = this.fog;
				break;
			case CState.tower:
				this.srmain.sprite = this.tower;
				break;
			case CState.towerTemp:
				this.srmain.sprite = this.towerTemp;
				break;
			case CState.destroyedTerrain:
				this.srmain.sprite = this.destroyedTerrain;
				break;
			case CState.destroyedTower:
				this.srmain.sprite = this.destroyedTower;
				break;
			case CState.towerOffence:
				this.srmain.sprite = this.towerOffence;
				break;
			case CState.towerDefence:
				this.srmain.sprite = this.towerDefence;
				break;
			case CState.towerIntel:
				this.srmain.sprite = this.towerIntel;
				break;
			case CState.wall:
				this.srmain.sprite = this.towerIntel;
				break;
			case CState.wallDestroyed:
				this.srmain.sprite = this.towerIntel;
				break;
			default:
				Debug.LogError("Unhandled state: " + this.cState.ToString());
				break;
		}
	}

	public void SetSelectState(SelState s){
		this.selState = s;
		switch(this.selState){
		case SelState.def:
			this.srbg.sprite = this.defaultBG;
			break;
		case SelState.select:
			this.srbg.sprite = this.selectBG;
			break;
		case SelState.selectHover:
		this.srbg.sprite = this.selectHoverBG;
			break;
		}
	}

	public CState getState(){
		return this.cState;
	}
}
