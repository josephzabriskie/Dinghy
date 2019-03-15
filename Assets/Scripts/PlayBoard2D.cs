using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellTypes;
using CellUIInfo;
using PlayerActions;

//!! Important This script should be high priority in execution order
public class PlayBoard2D : MonoBehaviour {
	//For Setting up
	public GameObject gridPrefab;
	float midSpace = 1.0f;
	//Other
	GameGrid2D playerGrid = null;
	GameGrid2D enemyGrid = null;
	public int sizex;
	public int sizey;
	Transform shotOrig;

	void Awake(){
		this.InstantiateGrids();
		shotOrig = this.transform.Find("ShotOrigin").transform;
	}

	// void Start(){
	// 	this.InstantiateGrids();
	// 	shotOrig = this.transform.Find("ShotOrigin").transform;
	// }

	public void SetGridStates(CellStruct[,] pGrid, CellStruct[,] eGrid){
		this.playerGrid.SetCSArray(pGrid);
		this.enemyGrid.SetCSArray(eGrid);
	}

	public void RXGridInput(bool pGrid, InputType it, Vector2 pos, CellStruct cStruct){
		InputProcessor.instance.RXInput(pGrid, it, pos, cStruct);
	}

	public void SetCellStruct(bool pGrid, Vector2 pos, CellStruct cStruct){
		GameGrid2D g = (pGrid) ? this.playerGrid : this.enemyGrid;
		g.SetCellStruct(pos, cStruct);
	}
	
	public void SetCellsSelect(bool pGrid, bool sel, bool hovered, ActionReq ar){
		GameGrid2D g = (pGrid) ? this.playerGrid : this.enemyGrid;
		switch(ar.a){
		case pAction.fireBasic:
		case pAction.fireAgain:
		case pAction.buildWall:
		case pAction.buildOffenceTower:
		case pAction.buildDefenceTower:
		case pAction.buildIntelTower:
		case pAction.scout:
		case pAction.placeMine:
		case pAction.buildReflector:
		case pAction.firePiercing:
		case pAction.towerTakeover:
			g.SetSingleSelect(sel, hovered, ar.loc[0]);
			break;
		case pAction.fireRow:
			g.SetRowSelect(sel, hovered, (int)ar.loc[0].y);
			break;
		case pAction.fireSquare:
			g.SetEmptySquareSelect(sel, hovered, ar.loc[0]);
			break;
		case pAction.placeMole:
			g.SetSquare3Select(sel, hovered, ar.loc[0], 1); // 3x3 square
			break;
		case pAction.buildDefenceGrid:
			g.SetSquare3Select(sel, hovered, ar.loc[0], 2); // 5x5 square
			break;
		case pAction.blockingShot: // these guys don't have any targeting or loc in their action
		case pAction.hellFire:
		case pAction.flare:
			break;
		case pAction.noAction:
			if(hovered){
				g.SetSingleSelect(sel, hovered, ar.loc[0]);
			}
			break;
		default:
			Debug.Log("Unhandled pAction Type: " + ar.a.ToString());
			break;
		}
	}

	public void ClearGrids(){
		this.playerGrid.ClearCSArray();
		this.enemyGrid.ClearCSArray();
	}

	public void ClearSelectionState(bool hoveronly){
		this.playerGrid.ClearSelectionState(hoveronly);
		this.enemyGrid.ClearSelectionState(hoveronly);
	}

	public int[] GetGridSize(){
		int[] ret = {this.sizex, this.sizey};
		return ret;
	}

	public CellStruct[][,] GetGridStates(){
		CellStruct [][,] ret = new CellStruct[2][,];
		ret[0] = this.playerGrid.GetCSArray(); // player's grid always idx 0
		ret[1] = this.enemyGrid.GetCSArray(); // enemy's grid always idx 1
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
		this.playerGrid = Instantiate(gridPrefab, center1, Quaternion.identity).GetComponent<GameGrid2D>();
		this.enemyGrid = Instantiate(gridPrefab, center2, Quaternion.identity).GetComponent<GameGrid2D>();
		//Debug.Log("Width/Height " + width.ToString() + " " + height.ToString());
		this.playerGrid.PlaceCells(width, height);
		int[] size = this.playerGrid.GetGridSize();
		//Debug.Log("size/size " + size[0].ToString() + " " + size[1].ToString());
		this.sizex = size[0];
		this.sizey = size[1];
		this.playerGrid.parent = this;
		this.playerGrid.playerOwnedGrid = true;
		this.enemyGrid.PlaceCells(width, height);
		this.enemyGrid.parent = this;
		this.enemyGrid.playerOwnedGrid = false;
		this.enemyGrid.Flip(); //This one's facing the player, needs to be flipped
	}

	public Vector3 GetShotOrigin(){ //Called by someone who wants to know where shots originate on this board
		Debug.Log("Returning: " + shotOrig.position.ToString());
		return shotOrig.position;
	}
}
