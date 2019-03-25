using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Testing : MonoBehaviour {

	// Use this for initialization
	void Start () {
		List<Vector2>[] arList; // Location list of the very first tower's placed for each player
		arList = new List<Vector2>[]{new List<Vector2>(){},new List<Vector2>(){}};
		arList[0].Add(new Vector2(0,0));
		arList[0].Add(new Vector2(1,1));
		arList[1].Add(new Vector2(2,2));
		Debug.Log("Player 1, element 0: " + arList[1][0]);
		//Prints (2.0,2.0), first index is for player
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
