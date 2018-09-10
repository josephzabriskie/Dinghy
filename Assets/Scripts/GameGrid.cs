using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellInfo;

public class GameGrid : MonoBehaviour {

	public GameObject CellPrefab;
	public Cell[,] cells;
	//CState[,] cellStates; // We can just pass the state through, this class doesn't really care about it
	public PlayerConnectionObj pco = null;
	int sizex, sizey;
	public bool playerOwnedGrid = false;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
	}

	public void SetColor(Color c){
		for(int x = 0; x < this.sizex; x++){
			for(int y = 0; y < this.sizey; y++){
				this.cells[x,y].SetBGColor(c);
			}
		}
	}

	public void PlaceCells(float w, float h){
		//Debug.Log(string.Format("STARTED GRID w:{0}, h:{1}", w, h));
		int fitx = (int)(w / (this.CellPrefab.GetComponent<SpriteRenderer>().size.x));
		int fity = (int)(h / (this.CellPrefab.GetComponent<SpriteRenderer>().size.y));
		cells = new Cell[fitx,fity];
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
				Cell tempCell = c.GetComponent<Cell>();
				tempCell.coords = new Vector2(x, y);
				this.cells[x,y] = tempCell;
				tempCell.parentGrid = this;
				//Debug.Log(string.Format("Spawned {0} at {1}", count, spot));
				//n.transform.parent = this.transform;
			}
		}
	}

	public void Flip(){ // This is a function intended to be called right after instantiation
		this.transform.Rotate(0.0f, 0.0f, 180.0f);
		for(int x = 0; x < this.sizex; x++){
			for(int y = 0; y < this.sizey; y++){
				this.cells[x,y].Flip();
			}
		}
	}

	public void RXCellInput(Vector2 pos, CState state){
		if(this.pco != null){
			Debug.Log("RCCellInfo, send to pco. " + this.playerOwnedGrid.ToString() + pos.ToString() + state.ToString());
			this.pco.RXGridInput(this.playerOwnedGrid, pos, state);
		}
		else{
			Debug.Log("RCCellInfo, can't send to pco, don't have one " + this.playerOwnedGrid.ToString() + pos.ToString() + state.ToString());
		}
	}

	public CState[,] GetArrayState(){
		CState[,] output = new CState[this.sizex, this.sizey];
		for(int x = 0; x < this.sizex; x++){
			for(int y = 0; y < this.sizey; y++){
				output[x,y] = this.cells[x,y].state;
			}
		}
		return output;
	}

	public void SetArrayState(CState[,] inStates){
		for(int x = 0; x < inStates.GetLength(0); x++){
			for(int y = 0; y < inStates.GetLength(1); y++){
				this.cells[x,y].SetState(inStates[x,y]);
			}
		}
	}
}
