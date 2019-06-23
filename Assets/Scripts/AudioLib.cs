using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Singleton audio object that gameobjects can grab audioclips from
//Makes it easy to swap out audio without editing each object/script
public class AudioLib : MonoBehaviour
{
    public static AudioLib instance; // singletonio
    [Header("UI")]
    public AudioClip defaultButtonPress;
    public AudioClip defaultButtonUp;
    [Header("Game")]
    public AudioClip defaultTileHover;
    public AudioClip defaultTileSelect;
    public AudioClip defaultTileHit;
    public AudioClip defaultTileMiss;

	void Awake(){
		if(instance == null){
			//Debug.Log("Setting InputProcessor Singleton");
			instance = this;
		}
		else if(instance != this){
			Destroy(gameObject);
			Debug.LogError("Singleton InputProcessor instantiated multiple times, destroy all but first to awaken");
		}
	}

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
