using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour {
	SpriteRenderer sr;
	public Sprite fog;
	public Vector2 coords;

	public bool hidden;

	// Use this for initialization
	void Start () {
		this.sr = this.GetComponent<SpriteRenderer>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnMouseEnter(){
		sr.color = Color.black;
	}

	void OnMouseExit(){
		sr.color = Color.white;
	}
}
