using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jump : MonoBehaviour {
	Rigidbody rb;
	// Use this for initialization
	void Start () {
		rb = this.GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
		bool jumpInput = Input.GetKeyDown("space");
		if (jumpInput){
			rb.AddForce(new Vector3(0.0f, 400.0f, 0.0f));
			Debug.Log("Got input");
		}
	}
}
