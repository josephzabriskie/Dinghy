using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellTypes;


public abstract class Cell2D : MonoBehaviour {
	//main data
	public Vector2 coords;
	public CellStruct cStruct;
	public GameGrid2D parentGrid;

	public CellStruct GetCellStruct(){
		return this.cStruct;
	}

	// Public functions, you shouldn't add new public functions to anything but the base class
	public abstract void SetCellStruct(CellStruct newCS);

	public abstract void SetHovered(bool hov);

	public abstract void SetSelected(bool sel);

	public void Flip(){
		this.transform.Rotate(0.0f, 0.0f, 180.0f);
	}

	//Private functions
	void OnMouseEnter(){
		parentGrid.RXCellHover(coords, true);
	}

	void OnMouseExit(){
		parentGrid.RXCellHover(coords, false);
	}

	void OnMouseDown(){
		Debug.Log("Clicked on: " + this.coords.ToString() + "State currently " + this.cStruct.ToString());
		this.parentGrid.RXCellInput(this.coords, this.cStruct);
	}

}
