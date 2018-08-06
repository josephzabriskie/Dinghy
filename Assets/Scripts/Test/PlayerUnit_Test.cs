using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// A player unit is a unit controlled by a player
// this could be a character in an FPS, a zergling in a RTS
// or a scout in a turn-based game.

public class PlayerUnit_Test : NetworkBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		//This function runs on ALLL PlayerUnits -- not just the ones that I own.
		if (hasAuthority == false){
			return;
		}
		if(Input.GetKeyDown(KeyCode.Space)){
			this.transform.Translate(0,1,0);
			Debug.Log("this transform" + this.transform.position.ToString());
		}
		if(Input.GetKeyDown(KeyCode.D)){
			Destroy(gameObject);
		}
	}
}
