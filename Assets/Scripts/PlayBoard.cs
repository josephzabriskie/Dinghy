using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayBoard : MonoBehaviour {
	public GameObject gridPrefab;
	GameObject grid1;
	GameObject grid2;
	float midSpace = 1.0f;


	// Use this for initialization
	void Start(){
		this.GetCentersAndDimensions();
	}

	public GameGrid getMyGrid(){
		return this.grid1.GetComponent<GameGrid>();
	}

	public GameGrid getTheirGrid(){
		return this.grid2.GetComponent<GameGrid>();
	}

//void GetCentersAndDimensions (out float w, out float h, out Vector3 pos1, out Vector3 pos2) {
	void GetCentersAndDimensions () {
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
		Debug.Log(string.Format("cent1 {0}, cent2 {1}", center1, center2));
		this.grid1 = Instantiate(gridPrefab, center1, Quaternion.identity);
		this.grid2 = Instantiate(gridPrefab, center2, Quaternion.identity);
		//w = width;
		//h = height;
		//pos1 = center1;
		//pos2 = center2;
		grid1.GetComponent<GameGrid>().PlaceCells(width, height);
		grid2.GetComponent<GameGrid>().PlaceCells(width, height);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
