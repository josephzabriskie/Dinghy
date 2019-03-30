using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitSunkDisplay : MonoBehaviour{
    UIFader uif;
    float displayTime = 3.0f;
    float transitiontime = 0.3f;

    void Awake(){
        uif = GetComponent<UIFader>();
        uif.ImmediateHide();
    }

    public void ShowHitSunk(){
        StartCoroutine(DisplayTimer());
    }

    public void IntialHide(){
        uif.FadeOut(0);
    }

    IEnumerator DisplayTimer(){
        uif.FadeIn(transitiontime);
        yield return new WaitForSeconds(displayTime);
        uif.FadeOut(transitiontime);
    }
}