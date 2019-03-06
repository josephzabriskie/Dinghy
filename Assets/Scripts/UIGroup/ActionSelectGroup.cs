using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayboardTypes;
using System.Linq;
using PlayerActions;
using CellTypes;

public class ActionSelectGroup : MonoBehaviour {
    List<ActionProgressBar> bars;

	void Start () {
        this.bars = GetComponentsInChildren<ActionProgressBar>().ToList();
	}

    public void EnableButtons(bool en){

    }

    //This takes the ruleset and places buttons on the bars
    public void UpdateActionInfo(List<ActionAvail> aaList){
        foreach(ActionProgressBar bar in this.bars){
            bar.UpdateActionInfo(aaList);
        }
    }

    //this sends the gameboard to each bar so they can update their counts/fill/button-enabledness
    public void UpdateTowerCounts(CellStruct[,] playerState, CellStruct[,] enemyState){
        foreach(ActionProgressBar bar in this.bars){
            bar.UpdateBar(playerState, enemyState);
        }
    }

    // public void HighlightAction(pAction highlight){
    //     foreach(ActionProgressBar bar in this.bars){
    //         bar.HighlightButton(highlight);
    //     }
    // }
}
