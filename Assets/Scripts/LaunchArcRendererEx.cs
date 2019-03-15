using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaunchArcRenderer : MonoBehaviour {

	LineRenderer lr;

	public float v; // Velocity
	public float angle;
	public int resolution = 10;

	float g; //Force gravity on the y axis
	float radianAngle; //Angle


	void Awake(){
		lr = GetComponent<LineRenderer>();
		g = Mathf.Abs(Physics2D.gravity.y);
	}

	void Start () {
		RenderArc();
	}

	//Populating the line renderer with the appropriate settings
	void RenderArc(){
		lr.positionCount = resolution + 1;
		lr.SetPositions(CalcArcArray());
	}

	//Create array of vector3 positions for arc
	Vector3[] CalcArcArray(){
		Vector3[] arcArray = new Vector3[resolution + 1];
		radianAngle = Mathf.Deg2Rad * angle;
		float maxDist = (v * v * Mathf.Sin(2*radianAngle)) / g;
		for(int i = 0; i <= resolution; i++){
			float t = (float)i / (float)resolution;
			arcArray[i] = CalculateArcPoint(t, maxDist);
		}
		return arcArray;
	}

	//Calculate height and distance of each vertex
	Vector3 CalculateArcPoint(float t, float maxDist){
		float x = t * maxDist;
		float y = x * Mathf.Tan(radianAngle) - ((g * x * x)/(2 * v * v * Mathf.Pow(Mathf.Cos(radianAngle),2)));
		return new Vector3(x,y);
	}
}
