using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class MenuScript : MonoBehaviour {

	public NetworkManager NM;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void StartGame(){
		SceneManager.LoadScene("PlayScene");
	}

	public void QuitGame(){
#if UNITY_EDITOR
	if(UnityEditor.EditorApplication.isPlaying) 
	{
		UnityEditor.EditorApplication.isPlaying = false;
	}
#endif
			Application.Quit();
	}

	public void LANHost(){
		NM.StartHost();
	}

	public void LANClient(string host){

	}

	public void LANServerOnly(){

	}//drive out of boise, forest road 268LW
}
