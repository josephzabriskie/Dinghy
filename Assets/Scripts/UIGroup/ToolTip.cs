using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolTip : MonoBehaviour {

    private static ToolTip instance;

    private Text toolTipTitle;
    private Text toolTipDesc;
    private Text toolTipCD;
    private Text toolTipUses;
    private RectTransform bgRect;

    [SerializeField]
    private RectTransform canvasRectTransform;

    private void Awake(){
        instance = this;
        toolTipTitle = transform.Find("TitleText").GetComponent<Text>();
        toolTipDesc = transform.Find("DescriptionText").GetComponent<Text>();
        toolTipCD = transform.Find("InfoTexts/CooldownText").GetComponent<Text>();
        toolTipUses = transform.Find("InfoTexts/UsesText").GetComponent<Text>();
        bgRect = this.GetComponent<RectTransform>();
        _HideToolTip();
    }

    private void _ShowToolTip(string title, string desc, string cooldown, string uses){
        MovetoMousePos();
        toolTipTitle.text = title;
        toolTipDesc.text = desc;
        toolTipCD.text = cooldown;
        toolTipUses.text = uses;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    private void _HideToolTip(){
        gameObject.SetActive(false);
    }

    private void Update(){
        MovetoMousePos();
    }

    private void MovetoMousePos(){
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(UIController.instance.transform.parent.GetComponent<RectTransform>(), Input.mousePosition, Camera.main, out localPoint);
        localPoint.y += bgRect.rect.height /2.0f;
        transform.localPosition = localPoint;

        Vector2 anchoredPosition = transform.GetComponent<RectTransform>().anchoredPosition;
        if(anchoredPosition.x + bgRect.rect.width/2.0f > canvasRectTransform.rect.width){
            anchoredPosition.x = canvasRectTransform.rect.width - bgRect.rect.width/2.0f;
        }
        else if(anchoredPosition.x - bgRect.rect.width/2.0f < 0.0f){
            anchoredPosition.x = bgRect.rect.width/2.0f;
        }
        if(anchoredPosition.y + bgRect.rect.height/2.0f > canvasRectTransform.rect.height){
            anchoredPosition.y = canvasRectTransform.rect.height - bgRect.rect.height/2.0f;
        }
        else if(anchoredPosition.y - bgRect.rect.height/2.0f < 0.0f){
            anchoredPosition.y = bgRect.rect.height/2.0f;
        }
        transform.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
    }

    public static void ShowToolTip(string title, string desc, string cooldown, string uses){
        instance._ShowToolTip(title, desc, cooldown, uses);
    }

    public static void HideToolTip(){
        instance._HideToolTip();
    }
}