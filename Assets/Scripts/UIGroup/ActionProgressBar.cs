using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayerActions;
using PlayboardTypes;
using System.Linq;
using CellTypes;


public class ActionProgressBar : MonoBehaviour {
    //Sub GameObjects
    Text countText;
    Image fill;

    //Prefabs for bar
    public GameObject actionSelButtonPrefab;
    public GameObject seperatorPrefab;
    //Bar setting
    int maxInterval = 8;
    float spacing; // Set in start
    float width; // Set in start
    //Buttons saved for later modifying
    List<ActionSelectButton> buttons;

    //List of buttons to display
    public List<pAction> actions;
    public pAction bldgAction;
    public CBldg displayBldg;
    ActionSelectButton mainBldgButton;

    void Start(){
        this.fill = transform.Find("Bar/Fill").GetComponent<Image>();
        this.countText = transform.Find("Count/Text").GetComponent<Text>();

        this.width = transform.Find("Bar").GetComponent<RectTransform>().rect.width;
        this.spacing = this.width/maxInterval;
        //Initalize seperators based on our specified spacing
        for(int i = 1; i < maxInterval; i++){
            GameObject sep = Instantiate(this.seperatorPrefab, transform.position, transform.rotation, transform.Find("Bar/Lines").transform);
            sep.GetComponent<RectTransform>().localPosition = new Vector3(-this.width/2.0f + spacing * i, 0, 0);
        }
        this.buttons = new List<ActionSelectButton>();
        GameObject mainbutton = Instantiate(this.actionSelButtonPrefab, this.transform);
        RectTransform buttonRect = mainbutton.GetComponent<RectTransform>();
        buttonRect.localPosition = new Vector3(24, -25, 0);
        buttonRect.anchorMin = new Vector2(1,1);
        buttonRect.anchorMax = new Vector2(1,1);
        buttonRect.sizeDelta = new Vector2(50, 50);
        this.mainBldgButton = mainbutton.GetComponent<ActionSelectButton>();
        this.mainBldgButton.Init();
        this.mainBldgButton.SetAction(this.bldgAction);
    }

    public void UpdateActionInfo(List<ActionAvail> aaList){
        //Todo delete old buttons first
        foreach(pAction action in this.actions){ // For each action we're specifying, find place it based on cost
            ActionAvail aa = aaList.Find(x=>x.action == action); // What happens if we don't find it?
            //Determine which parameter we care about
            int cost;
            if(this.bldgAction == pAction.buildOffenceTower)
                cost = aa.actionParam.offenceCost;
            else if (this.bldgAction ==pAction.buildDefenceTower)
                cost = aa.actionParam.defenceCost;
            else if (this.bldgAction == pAction.buildIntelTower)
                cost = aa.actionParam.intelCost;
            else{
                Debug.LogError("Bad ActionType: " + this.bldgAction.ToString());
                return;
            }
            ActionSelectButton button = this.buttons.Find(x=>x.action == action);
            if(button == null){ // If we don't have a button, spawn one and init
                GameObject go = Instantiate(this.actionSelButtonPrefab, transform.position, transform.rotation, transform.Find("Bar/Buttons").transform);
                button = go.GetComponent<ActionSelectButton>();
                button.Init();
                button.SetAction(action);
                this.buttons.Add(button);
            }
            if(button.cost != cost){ //Move button if needed
                button.gameObject.GetComponent<RectTransform>().localPosition = new Vector3(-this.width/2.0f + this.spacing * cost, 0, 0);
                button.cost = cost;
            }
        }
    }

    public void UpdateBar(CellStruct[,] playerState, CellStruct[,] enemyState){
        int count = GUtils.Serialize(playerState).Count(cell => cell.bldg == this.displayBldg && !cell.destroyed && !cell.defected); //count all friendly spaces not taken over
        count += GUtils.Serialize(enemyState).Count(cell => cell.bldg == this.displayBldg && !cell.destroyed && cell.defected); //count all enemy spaces taken over
        this.fill.fillAmount = (float)count/this.maxInterval;
        this.countText.text = count.ToString();
    }

    // public void HighlightButton(pAction action){
    //     foreach(ActionSelectButton button in this.buttons){
    //         if(button.action == action){
    //             button.Highlight(true);
    //         }
    //         else{
    //             button.Highlight(false);
    //         }
    //     }
    //     if(this.mainBldgButton.action == action){
    //         this.mainBldgButton.Highlight(true);
    //     }
    //     else{
    //         this.mainBldgButton.Highlight(false);
    //     }
    // }
}