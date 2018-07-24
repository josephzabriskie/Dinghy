using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SetupLocalPlayer : NetworkBehaviour {

	// Use this for initialization
	void Start () {
		if (isLocalPlayer)
			GetComponent<Jump>().enabled = true;
		else
			GetComponent<Jump>().enabled = false;
	}
	

}
