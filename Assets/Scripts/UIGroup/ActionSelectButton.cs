using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayerActions;
using PlayboardTypes;

public class ActionSelectButton : MonoBehaviour {

    struct ToolTipInfo{
        public string title;
        public string desc;
        public ToolTipInfo(string title, string desc){
            this.title = title;
            this.desc = desc;
        }
    }

    static Dictionary<pAction, ToolTipInfo> tooltips = new Dictionary<pAction, ToolTipInfo>{
            {pAction.noAction, 			new ToolTipInfo("No Action", "Why would you want to do this")},
			{pAction.buildOffenceTower, new ToolTipInfo("Offence Tower", "Build a new Offence tower, gives 1 offence point")},
			{pAction.buildDefenceTower, new ToolTipInfo("Defence Tower", "Build a new Defence tower, gives 1 defence point")},
			{pAction.buildIntelTower, 	new ToolTipInfo("Intel Tower", "Build a new Intel tower, gives 1 Intel point")},
			{pAction.buildWall, 		new ToolTipInfo("Wall", "Build a wall that protects the two spaces behind it")},
			{pAction.fireBasic, 		new ToolTipInfo("Fire", "Launch a single attack at your opponent")},
			{pAction.scout, 			new ToolTipInfo("Scout", "Reveal a target location for 4 turns")},
			{pAction.fireAgain,			new ToolTipInfo("Fire Again", "Fire a second attack at your opponent")},
			{pAction.fireRow,			new ToolTipInfo("Fire Row", "default")},
			{pAction.fireSquare,		new ToolTipInfo("Fire Square", "Fire 4 shots in a square pattern at your opponent")},
			{pAction.blockingShot,		new ToolTipInfo("Blocking Shot", "Fire 4 blockers randomly at empty opponent spaces")},
			{pAction.hellFire,			new ToolTipInfo("Hellfire", "Obliterate 5 random enemy locations")},
			{pAction.flare,				new ToolTipInfo("Flare", "Randomly reveal 2 locations for 4 turns")},
			{pAction.placeMine,			new ToolTipInfo("Mine", "Place a mine that disables your opponents attack for 3 turns")},
			{pAction.buildDefenceGrid,	new ToolTipInfo("Defence Grid", "Build a defence grid that guards a 5x5 square from 3 attacks")},
			{pAction.buildReflector,	new ToolTipInfo("Reflector", "Build a reflector that returns an attack on hit")},
			{pAction.fireReflected,		new ToolTipInfo("fireReflected", "none")},
			{pAction.firePiercing,		new ToolTipInfo("Piercing Shot", "Fire a shot that destroys and travels through walls")},
			{pAction.placeMole,			new ToolTipInfo("Plant Mole", "Plant a mole that will inform you of the number of enemy structurs in a 3x3 grid")},
			{pAction.towerTakeover,		new ToolTipInfo("Tower Takeover", "Claim an active enemy tower as your own, stealing\n the point it provides")},
    };

    //Every action needs a sprite representation of the action
    public pAction action; // Action that this button will set when pressed
    public Sprite s_buildOffenceTower;
    public Sprite s_buildDefenceTower;
    public Sprite s_buildIntelTower;
    public Sprite s_buildWall;
    public Sprite s_fireBasic;
    public Sprite s_scout;
    public Sprite s_fireAgain;
    public Sprite s_fireRow;
    public Sprite s_fireSquare;
    public Sprite s_blockingShot;
    public Sprite s_hellFire;
    public Sprite s_flare;
    public Sprite s_placeMine;
    public Sprite s_buildDefenceGrid;
    public Sprite s_buildReflector;
    public Sprite s_firePiercing;
    public Sprite s_placeMole;
    public Sprite s_towerTakeover;
    public Sprite s_default;

    //Sub GameObjects
    Image image;
    Button button;
    Outline outline;

    //Protected start
    bool init = false;

    //Save position, only used externally
    public int cost = -1;

    //Save when we are selected
    bool selected; // set through Init
    bool buttonEn;

    //Info about action availibility
    ActionAvail actionAvail;

    void Start(){
        Init();
    }

    public void MouseHover(bool hovered){
        if(hovered){
            string cooldown = string.Format("CD: {0}/{1}  ", this.actionAvail.cooldown, this.actionAvail.actionParam.cooldown);
            string uses;
            if(this.actionAvail.usesLeft < 0){
                uses = "Uses: ∞";
            }
            else{
                uses = string.Format("Uses: {0}", this.actionAvail.usesLeft);
            }
            ToolTip.ShowToolTip(tooltips[this.action].title, tooltips[this.action].desc,cooldown, uses);
        }
        else{
            ToolTip.HideToolTip();
        }
    }

    public void Init(){
        if(init){
            return;
        }
        this.init = true;
        this.image = this.transform.Find("Button/Image").GetComponent<Image>();
        this.button = this.GetComponentInChildren<Button>();
        this.outline = this.GetComponentInChildren<Outline>();
        this.transform.Find("Button").GetComponent<Button>().onClick.AddListener(OnClick);
        this.Highlight(false);
        UIController.instance.ActionSelectButtonGrpAdd(this);
        this.buttonEn = false; //Default Button to off
        this.Enable(this.buttonEn); // Refresh enabled state
        //Do this last
        this.updateButton();
    }

    public void SetAction(pAction newAction){
        this.action = newAction;
        this.updateButton();
    }

    public void Enable(bool en){
        this.buttonEn = en;
        this.button.interactable = this.buttonEn && this.actionAvail.available;
    }

    public void Highlight(bool en){
        outline.enabled = en;
        this.selected = en;
    }

    void OnClick(){
        if(!this.selected){
            Debug.Log("Clicked set to: " + this.action);
            InputProcessor.instance.SetActionContext(this.action);
        }
        else{
            Debug.Log("Deselected: " + this.action.ToString());
            InputProcessor.instance.ClearActionContext();
        }
    }

    void updateButton(){
        Sprite newsprite;
        switch(this.action){ //god am I sick of switch statements, someone tell me a better way
        case pAction.noAction:
            newsprite = null;
            break;
        case pAction.buildOffenceTower:
            newsprite = this.s_buildOffenceTower;
            break;
        case pAction.buildDefenceTower:
            newsprite = this.s_buildDefenceTower;
            break;
        case pAction.buildIntelTower:
            newsprite = this.s_buildIntelTower;
            break;
        case pAction.buildWall:
            newsprite = this.s_buildWall;
            break;
        case pAction.fireBasic:
            newsprite = this.s_fireBasic;
            break;
        case pAction.scout:
            newsprite = this.s_scout;
            break;
        case pAction.fireAgain:
            newsprite = this.s_fireAgain;
            break;
        case pAction.fireRow:
            newsprite = this.s_fireRow;
            break;
        case pAction.fireSquare:
            newsprite = this.s_fireSquare;
            break;
        case pAction.blockingShot:
            newsprite = this.s_blockingShot;
            break;
        case pAction.hellFire:
            newsprite = this.s_hellFire;
            break;
        case pAction.flare:
            newsprite = this.s_flare;
            break;
        case pAction.placeMine:
            newsprite = this.s_placeMine;
            break;
        case pAction.buildDefenceGrid:
            newsprite = this.s_buildDefenceGrid;
            break;
        case pAction.buildReflector:
            newsprite = this.s_buildReflector;
            break;
        case pAction.firePiercing:
            newsprite = this.s_firePiercing;
            break;
        case pAction.placeMole:
            newsprite = this.s_placeMole;
            break;
        case pAction.towerTakeover:
            newsprite = this.s_towerTakeover;
            break;
        default:
            newsprite = null;
            Debug.LogError("Unhandled Action: " + this.action);
            break;
        }
        if(newsprite == null){
            //Debug.LogWarning("No sprite for action: " + this.action.ToString());
            newsprite = this.s_default;
        }
        this.image.sprite = newsprite;
    }

    public void UpdationActionAvail(ActionAvail aa){
        this.actionAvail = aa;
        this.Enable(this.buttonEn); // Update the button
    }
}