using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFader : MonoBehaviour{
    CanvasGroup uiElt;
    Coroutine coroutine;

    public void Awake(){
        this.uiElt = this.GetComponent<CanvasGroup>();
        this.coroutine = null;
    }

    public void ImmediateHide(){
        uiElt.alpha = 0;
    }

    public void FadeIn(float time = 0.5f){
        _StartFade(time, true);
    }

    public void FadeOut(float time = 0.5f){
        _StartFade(time, false);
    }

    private void _StartFade(float time, bool visible){
        if(coroutine != null){
            StopCoroutine(coroutine);
        }
        float endVal = visible ? 1.0f : 0.0f;
        coroutine = StartCoroutine(FadeCanvasGroup(uiElt, endVal, time));
    }

    public IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float endVal, float runTime = 0.5f){
		float eTime = 0.0f; // elapsed time
		float start = canvasGroup.alpha;
		while(eTime < runTime){
			eTime += Time.deltaTime;
			canvasGroup.alpha = Mathf.Lerp(start, endVal, eTime/runTime);
			yield return null;
		}
	}
}
