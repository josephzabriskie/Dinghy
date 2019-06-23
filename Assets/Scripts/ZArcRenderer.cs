using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ZArcRenderer : MonoBehaviour {

	LineRenderer lr;

	[Range(2, 30)]
	public int resolution = 10;
	[Range(0,-10)]
	public float arcHeight = -1f;

	Vector3 shotOrigin; //Here's the coordinates we use to start our arc for shots
	public PlayBoard2D pb; // Here's where the play board sits. We need this to check how far from the camera to raycast

	void Awake(){
		lr = GetComponent<LineRenderer>();
	}

	void Start () {
		shotOrigin = pb.playerShotOrig;
		//Debug.Log("Test1: " + test1.ToString() + ", Test2: " + test2.ToString());
		//RenderArc(test1, test2);
	}

	void Update() {
		if(Input.GetMouseButton(0)){
			Vector3 mousepos = Input.mousePosition;
			mousepos = Camera.main.ScreenToWorldPoint(Input.mousePosition + new Vector3(0,0,pb.transform.position.z - Camera.main.transform.position.z));
			Debug.Log(mousepos);
			RenderArc(mousepos, shotOrigin);
		}
	}

	//Populating the line renderer with the appropriate settings
	//We ignore z here, and just use our zdepth value for now
	public void RenderArc(Vector3 pos1, Vector3 pos2){
		Debug.Log(string.Format("Create line from {0}-{1}, with arch height {2}",pos1.ToString(), pos2.ToString(), arcHeight.ToString()));
		lr.positionCount = resolution + 1;
		lr.SetPositions(CalcArcArray(pos1, pos2));
	}

	//Create array of vector3 positions for arc
	Vector3[] CalcArcArray(Vector3 pos1, Vector3 pos2){
		Vector3[] arcArray = new Vector3[resolution + 1];
		for(int i = 0; i <= resolution; i++){
			float t = (float)i / (float)resolution;
			Vector3 pos = Vector3.Lerp(pos1, pos2, t);
			//Debug.Log(string.Format("{0}-{1}, t={2} pos:{3}",pos1.ToString(), pos2.ToString(), t, pos.ToString()));
			float mult = Mathf.Cos((t - 0.5f) * Mathf.PI);
			float offset = Mathf.Cos((t - 0.5f) * Mathf.PI) * arcHeight;
			//Debug.Log("offset: "+ offset.ToString() + ", mult: " + mult.ToString());
			pos.Set(pos.x, pos.y, pos.z + offset);
			arcArray[i] = pos;
		}
		return arcArray;
	}
}
