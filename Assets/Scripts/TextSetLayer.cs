using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextSetLayer : MonoBehaviour {
	MeshRenderer mr;
	public string layerName;
	public int orderInLayer;

	void Start () {
		this.mr = GetComponent<MeshRenderer>();
		this.mr.sortingLayerID = SortingLayer.NameToID(this.layerName);
		this.mr.sortingOrder = this.orderInLayer;
	}
}