using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayboardTypes;
using System.Linq;
using PlayerActions;

public class ActionSelectGroup : MonoBehaviour {
    ActionSelectPanel[] panels;
    public InputProcessor ip; // Really don't like that I have to put this here... TODO FIX ME/. IP should find us and register itself

	void Start () {
        this.panels = GetComponentsInChildren<ActionSelectPanel>();
        this.RegisterCallbacks();
	}

    void RegisterCallbacks(){
        foreach(ActionSelectPanel panel in this.panels){
		    panel.RegisterCallback(this.ip);
        }
	}

    public void DeregisterCallbacks(){
        foreach(ActionSelectPanel panel in this.panels){
		    panel.DeregisterCallbacks();
        }
    }

    public void SetButtonEnabled(bool en){
        foreach(ActionSelectPanel panel in this.panels){
            panel.SetEnabled(en);
        }
    }

    public void UpdateActionInfo(List<ActionAvail> aaList){
        foreach(ActionSelectPanel panel in this.panels){
            ActionAvail aa = aaList.Find(elt => elt.action == panel.action);
            panel.UpdateActionInfo(aa);
        }
    }

    public void HighlightPanel(pAction highlight){
        foreach(ActionSelectPanel panel in this.panels){
            if(panel.action == highlight){
                panel.Highlight(true);
            }
            else{
                panel.Highlight(false);
            }
        }
    }
}
