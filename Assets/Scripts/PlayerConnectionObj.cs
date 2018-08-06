using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerConnectionObj : NetworkBehaviour {
	public GameObject baseboard;
	public string playerName = "Anonymous";

	GameGrid myGG;
	GameGrid theirGG;
	
	
	// Use this for initialization
	void Start () {
		Debug.Log("New Player Joined!");
		if (isLocalPlayer){ // object belongs to another player
			GameObject pb = GameObject.Find("PlayBoard"); // Find the playboard in the scene	
			if (!pb){
				Debug.LogError("Couldn't find 'PlayBord'!");
			}
			PlayBoard pbs = pb.GetComponent<PlayBoard>(); // get playboard script
			this.myGG = pbs.getMyGrid();
			this.theirGG = pbs.getTheirGrid();
			this.myGG.SetColor(Color.green);
			this.theirGG.SetColor(Color.cyan);
			Debug.Log("This is the local player");
		}
		//since the player object is invisible and not part of the world
		//give me something physical to interact with.
		//Instantiate()

	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Q)){
			string n = "Joe" + Random.Range(1, 100);
			Debug.Log("sending the server a request to change our name to: " + n);
			CmdChangePlayerName(n);
		}
	}

	///Commands to send to server
	[Command]
	void CmdChangePlayerName(string n){
		Debug.Log("CmdChangePlayerName: " + n);
		this.playerName = n;
	}
}
