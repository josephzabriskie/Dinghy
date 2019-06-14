using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


//This guy is just for testing c# stuff out
public class Testing : MonoBehaviour {
	public Tile tile1;
	public Tile tile2;

	// Use this for initialization
	void Start () {
		Tilemap tm = GetComponentInChildren<Tilemap>();
		tm.SetTile(new Vector3Int(0,0,0), tile1);
		tm.SetTile(new Vector3Int(1,0,0), tile2);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
