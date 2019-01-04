using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayerActions;
using PlayboardTypes;

public class ActionSelectPanel : MonoBehaviour {
	Button button = null;
    public pAction action;
	Text cooldownText;
	Text usesLeftText;
	Text offenceCostText;
	Text defenceCostText;
	Text intelCostText;
	bool started = false;
	ActionAvail latestAA;
	Image panelImage;
	public Color highlightedColor;

	void Start () {
		this.ProtectedStart();
	}

	public void RegisterCallback(InputProcessor ip){
		this.ProtectedStart();
		this.button.onClick.AddListener(delegate{ip.SetActionContext(action);});
	}

	void ProtectedStart(){ // There's a problem where group grabs this and trys to register before it's ready.
		if (this.started){
			return;
		}
		if(this.action == pAction.noAction){
			Debug.LogError("You should know that this button's set to 'noAction'");
		}
		this.panelImage = GetComponent<Image>();
		this.button = GetComponentInChildren<Button>();
		Text[] texts = GetComponentsInChildren<Text>();
		foreach(Text t in texts){
			switch(t.gameObject.name){
			case "CoolDown":
				this.cooldownText = t;
				break;
			case "UsesLeft":
				this.usesLeftText = t;
				break;
			case "OffenceCost":
				this.offenceCostText = t;
				break;
			case "DefenceCost":
				this.defenceCostText = t;
				break;
			case "IntelCost":
				this.intelCostText = t;
				break;
			default:
				//Debug.LogError("Didn't find matching name : " + t.gameObject.name);
				break;
			}
		}
		this.cooldownText.text = "-/-";
		this.usesLeftText.text = "-";
		this.offenceCostText.text = "-";
		this.defenceCostText.text = "-";
		this.intelCostText.text = "-";
		this.started = true;

	}

	public void DeregisterCallbacks(){
		this.button.onClick.RemoveAllListeners();
	}

    public void SetEnabled(bool en){
        this.button.interactable = en && this.latestAA.available;
    }

	public void Highlight(bool en){
		if (en){
			this.panelImage.color = this.highlightedColor;
		}
		else{
			this.panelImage.color = Color.clear;
		}
	}

	void RefreshEnabled(){
		this.button.interactable &= this.latestAA.available;
	}

	public void UpdateActionInfo(ActionAvail aa){
		//Some actions can be set on cooldown by other actions, check both max cd and current before blanking
		this.cooldownText.text = aa.actionParam.cooldown == 0 && aa.cooldown == 0 ? "-" : string.Format("{0}/{1}", aa.cooldown, aa.actionParam.cooldown);
		this.usesLeftText.text = aa.usesLeft < 0 ? "∞" : aa.usesLeft.ToString();
		this.offenceCostText.text = aa.actionParam.offenceCost.ToString();
		this.defenceCostText.text = aa.actionParam.defenceCost.ToString();
		this.intelCostText.text = aa.actionParam.intelCost.ToString();
		this.latestAA = aa;
		this.RefreshEnabled();
	}
}
