using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellTypes;
using CellUIInfo;

public class GameGrid2D : MonoBehaviour {

	public GameObject CellPrefab;
	public Cell2D[,] cells;
	public PlayBoard2D parent = null;
	int sizex, sizey;
	public bool playerOwnedGrid = false;

	public void SetColor(Color c){
		for(int x = 0; x < this.sizex; x++){
			for(int y = 0; y < this.sizey; y++){
				this.cells[x,y].SetBGColor(c);
			}
		}
	}

	public void SetCellMainState(Vector2 pos, CState s){
		this.cells[(int)pos.x, (int)pos.y].SetMainState(s);
	}

	public void PlaceCells(float w, float h){
		//Debug.Log(string.Format("STARTED GRID w:{0}, h:{1}", w, h));
		int fitx = (int)(w / (this.CellPrefab.GetComponentInChildren<SpriteRenderer>().size.x)); //Warning there are multiple sprites as obj children, assuming here all the same size
		int fity = (int)(h / (this.CellPrefab.GetComponentInChildren<SpriteRenderer>().size.y));
		cells = new Cell2D[fitx,fity];
		this.sizex = fitx;
		this.sizey = fity;
		float offsetx = w/fitx;
		float offsety = h/fity;
		Vector3 start = this.transform.position + new Vector3(offsetx/2.0f - w/2.0f, offsety/2.0f - h/2.0f, 0.0f);
		//Debug.Log(string.Format("We can fit x {0}, y {1}", fitx, fity));
		//Debug.Log(string.Format("offset x: {0}, y: {1}", offsetx, offsety));
		//Debug.Log(string.Format("Start pos: {0}", start));
		int count = 0;
		for(int x = 0; x < fitx; x++){
			for(int y = 0; y < fity; y++){
				count++;
				Vector3 spot = start + new Vector3(offsetx * x, offsety * y, -0.2f);
				GameObject c = Instantiate(this.CellPrefab, spot, Quaternion.identity, this.transform);
				Cell2D tempCell = c.GetComponent<Cell2D>();
				tempCell.coords = new Vector2(x, y);
				this.cells[x,y] = tempCell;
				tempCell.parentGrid = this;
				//Debug.Log(string.Format("Spawned {0} at {1}", count, spot));
				//n.transform.parent = this.transform;
			}
		}
	}

	public int[] GetGridSize(){
		int[] ret = {this.sizex, this.sizey};
		return ret;
	}

	public void Flip(){ // This is a function intended to be called right after instantiation
		this.transform.Rotate(0.0f, 0.0f, 180.0f);
		for(int x = 0; x < this.sizex; x++){
			for(int y = 0; y < this.sizey; y++){
				this.cells[x,y].Flip();
			}
		}
	}

	public void RXCellInput(Vector2 pos, InputType it, CState cellState){
		//Debug.Log("RCCellInfo, send to parent. " + this.playerOwnedGrid.ToString() + pos.ToString() + cellState.ToString());
		this.parent.RXGridInput(this.playerOwnedGrid, it, pos, cellState);
	}

	public CState[,] GetArrayState(){
		CState[,] output = new CState[this.sizex, this.sizey];
		for(int x = 0; x < this.sizex; x++){
			for(int y = 0; y < this.sizey; y++){
				output[x,y] = this.cells[x,y].cState;
			}
		}
		return output;
	}

	public void SetArrayState(CState[,] inStates){
		for(int x = 0; x < inStates.GetLength(0); x++){
			for(int y = 0; y < inStates.GetLength(1); y++){
				this.cells[x,y].SetMainState(inStates[x,y]);
			}
		}
	}

	public void ClearArrayState(){
		for(int x = 0; x < this.sizex; x++){
			for(int y = 0; y < this.sizey; y++){
				this.cells[x,y].SetMainState(CState.hidden);
			}
		}
	}

	public void ClearSelectionState(bool hovered){
		for(int x = 0; x < this.sizex; x++){
			for(int y = 0; y < this.sizey; y++){
				if(hovered){ //Clear hovered values
					this.cells[x,y].SetHovered(false);
				}
				else{ //Clear cells
					this.cells[x,y].SetSelected(false);
				}
			}
		}
	}

	//SelectionUtilities
	public void SetSingleSelect(bool sel, Vector2 loc, bool hovered){
		if(hovered){
			this.cells[(int)loc.x, (int)loc.y].SetHovered(sel);
		}
		else{
			this.cells[(int)loc.x, (int)loc.y].SetSelected(sel);
		}
	}

	public void SetRowSelect(bool sel, Vector2 loc, bool hovered){
		

	}
}
