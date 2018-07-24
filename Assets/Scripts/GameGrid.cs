using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameGrid : MonoBehaviour {

	public GameObject CellPrefab;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void PlaceCells(float w, float h){
		Debug.Log(string.Format("STARTED GRID w:{0}, h:{1}", w, h));
		int fitx = (int)(w / (this.CellPrefab.GetComponent<SpriteRenderer>().size.x));
		int fity = (int)(h / (this.CellPrefab.GetComponent<SpriteRenderer>().size.y));
		
		//bool evenx = fitx % 2 == 0 ? true : false;
		//bool eveny = fity % 2 == 0 ? true : false;

		float offsetx = w/fitx;
		float offsety = h/fity;
		
		Vector3 start = this.transform.position + new Vector3(offsetx/2.0f - w/2.0f, offsety/2.0f - h/2.0f, 0.0f);

		Debug.Log(string.Format("We can fit x {0}, y {1}", fitx, fity));
		Debug.Log(string.Format("offset x: {0}, y: {1}", offsetx, offsety));
		Debug.Log(string.Format("Start pos: {0}", start));
		int count = 0;
		for(int x = 0; x < fitx; x++){
			for(int y = 0; y < fity; y++){
				count++;
				Vector3 spot = start + new Vector3(offsetx * x, offsety * y, -0.2f);
				Instantiate(this.CellPrefab, spot, Quaternion.identity, this.transform);
				Debug.Log(string.Format("Spawned {0} at {1}", count, spot));
				//n.transform.parent = this.transform;
			}
		}
	}
}
