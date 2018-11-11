﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellInfo;

//!! Important This script should be high priority in execution order
public class PlayBoard : MonoBehaviour {
	//For Setting up
	public GameObject gridPrefab;
	float midSpace = 1.0f;
	//Other
	GameGrid playerGrid = null;
	GameGrid enemyGrid = null;
	public InputProcessor ip = null; // Link up the guy that will process our grid clicks
	int sizex;
	int sizey;

	void Start(){
		this.InstantiateGrids();
	}

	public void SetGridStates(CState[,] pGrid, CState[,] eGrid){
		this.playerGrid.SetArrayState(pGrid);
		this.enemyGrid.SetArrayState(eGrid);
	}

	public void RXGridInput(bool pGrid, Vector2 pos, CState state){
		this.ip.RXInput(pGrid, pos, state);
	}

	public void SetCellState(bool pGrid, Vector2 pos, CState state){
		GameGrid g = (pGrid) ? this.playerGrid : this.enemyGrid;
		g.SetCellState(pos, state);
	}

	public void ClearGrids(){
		this.playerGrid.ClearArrayState();
		this.enemyGrid.ClearArrayState();
	}

	public int[] GetGridSize(){
		int[] ret = {this.sizex, this.sizey};
		return ret;
	}

	void numberGrid(){
		//TBD auto number the grid
		//Should be done on InstantiateGrids
	}

	public void InstantiateGrids() {
		float width;
		float height;
		Vector3 center1;
		Vector3 center2;
		if (this.transform.localScale.x > this.transform.localScale.y){ // horizontal board
			width = (this.transform.localScale.x - this.midSpace) / 2.0f;
			height = this.transform.localScale.y;
			//Debug.Log(string.Format("W{0} > H{1}", width, height));
			center1 = this.transform.position - new Vector3(width/2.0f + this.midSpace/2.0f, 0, 0);
			center2 = this.transform.position + new Vector3(width/2.0f + this.midSpace/2.0f, 0, 0);
		}
		else{ // vertical board
			width = this.transform.localScale.x;
			height = (this.transform.localScale.y - this.midSpace) / 2.0f;
			//Debug.Log(string.Format("W{0} < H{1}", width, height));
			center1 = this.transform.position - new Vector3(0, height/2.0f + this.midSpace/2.0f, 0);
			center2 = this.transform.position + new Vector3(0, height/2.0f + this.midSpace/2.0f, 0);
		}
		//Debug.Log(string.Format("cent1 {0}, cent2 {1}", center1, center2));
		this.playerGrid = Instantiate(gridPrefab, center1, Quaternion.identity).GetComponent<GameGrid>();
		this.enemyGrid = Instantiate(gridPrefab, center2, Quaternion.identity).GetComponent<GameGrid>();
		this.playerGrid.PlaceCells(width, height);
		int[] size = this.playerGrid.GetGridSize();
		this.sizex = size[0];
		this.sizey = size[1];
		this.playerGrid.parent = this;
		this.playerGrid.playerOwnedGrid = true;
		this.enemyGrid.PlaceCells(width, height);
		this.enemyGrid.parent = this;
		this.enemyGrid.playerOwnedGrid = false;
		this.enemyGrid.Flip(); //This one's facing the player, needs to be flipped
	}
}
