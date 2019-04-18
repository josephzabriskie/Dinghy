using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellTypes;


public class GameGrid2D : MonoBehaviour {

	public GameObject CellPrefab;
	public Cell2D[,] cells;
	public PlayBoard2D parent = null;
	int sizex, sizey;
	public bool playerOwnedGrid = false;

	public void SetCellStruct(Vector2 pos, CellStruct s){
		this.cells[(int)pos.x, (int)pos.y].SetCellStruct(s);
	}

	public void PlaceCells(float w, float h){
		SpriteRenderer srbg = this.CellPrefab.transform.Find("Background").GetComponent<SpriteRenderer>(); // Grab background sprite size as cell size
		//Debug.Log(string.Format("STARTED GRID w:{0}, h:{1}", w, h));
		int fitx = (int)(w / (srbg.size.x));
		int fity = (int)(h / (srbg.size.y));
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
				Vector3 spot = start + new Vector3(offsetx * x, offsety * y, -0.01f);
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

	public void RXCellInput(Vector2 pos, CellStruct cs){
		//Debug.Log("RCCellInfo, send to parent. " + this.playerOwnedGrid.ToString() + pos.ToString() + cs.ToString());
		this.parent.RXGridInput(this.playerOwnedGrid, pos, cs);
	}

	public void RXCellHover(Vector2 pos, bool enter){
		parent.RXGridHover(playerOwnedGrid, pos, enter);
	}

	public CellStruct[,] GetCSArray(){
		CellStruct[,] output = new CellStruct[this.sizex, this.sizey];
		for(int x = 0; x < this.sizex; x++){
			for(int y = 0; y < this.sizey; y++){
				output[x,y] = this.cells[x,y].cStruct;
			}
		}
		return output;
	}

	public void SetCSArray(CellStruct[,] inCSArr){
		for(int x = 0; x < inCSArr.GetLength(0); x++){
			for(int y = 0; y < inCSArr.GetLength(1); y++){
				this.cells[x,y].SetCellStruct(inCSArr[x,y]);
			}
		}
	}

	public void ClearCSArray(){
		for(int x = 0; x < this.sizex; x++){
			for(int y = 0; y < this.sizey; y++){
				this.cells[x,y].SetCellStruct(new CellStruct(CBldg.hidden));
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
	////////////////////////////////////////////////////
	//Selection Functions
	public void SetSingleSelect(bool hovered, Vector2 loc){
		if(hovered){
			this.cells[(int)loc.x, (int)loc.y].SetHovered(true);
		}
		else{
			this.cells[(int)loc.x, (int)loc.y].SetSelected(true);
		}
	}

	public void SetRowSelect(bool hovered, int row){
		for(int x = 0; x < this.sizex; x++){
			if(hovered){
				this.cells[x,row].SetHovered(true);
			}
			else{
				this.cells[x,row].SetSelected(true);
			}
		}
	}

	public void SetEmptySquareSelect(bool hovered, Vector2 loc){
		for(int x = -1; x < 2; x+=2){
			for(int y = -1; y < 2; y+=2){
				Vector2 newLoc = new Vector2(loc.x + x, loc.y + y);
				if(!this.CheckLocInRange(newLoc)){
					continue;
				}
				if(hovered){
					this.cells[(int)newLoc.x, (int)newLoc.y].SetHovered(true);
				}
				else{
					this.cells[(int)newLoc.x, (int)newLoc.y].SetSelected(true);
				}
			}
		}
	}

	//Set square. Size is the diagonal count from center to edge.
	public void SetSquare3Select(bool hovered, Vector2 loc, int size){
		if(size < 0){ // Bad input
			return;
		}
		for(int x = -size; x < size + 1; x++){
			for(int y = -size; y < size + 1; y++){
				Vector2 newLoc = new Vector2(loc.x + x, loc.y + y);
				if(!this.CheckLocInRange(newLoc)){
					continue;
				}
				if(hovered){
					this.cells[(int)newLoc.x, (int)newLoc.y].SetHovered(true);
				}
				else{
					this.cells[(int)newLoc.x, (int)newLoc.y].SetSelected(true);
				}
			}
		}
	}

	public void SetAllSelect(bool hovered){
		for(int x = 0; x < this.sizex; x++){
			for(int y = 0; y < this.sizey; y++){
				if(hovered){
				this.cells[x,y].SetHovered(true);
				}
				else{
					this.cells[x,y].SetSelected(true);
				}
			}
		}
	}

	bool CheckLocInRange(Vector2 loc){
		return loc.x >= 0 && loc.x < this.sizex && loc.y >= 0 && loc.y < this.sizey;
	}
}
