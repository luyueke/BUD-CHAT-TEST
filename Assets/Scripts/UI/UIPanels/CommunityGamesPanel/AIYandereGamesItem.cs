using System;
using System.Collections;
using System.Collections.Generic;
using AIGame.Base;
using Game.AIResData;
using Game.CommunityGame;
using UnityEngine;
using UnityEngine.UI;

public class AIYandereGamesItem : CommunityGamesSpotlightRightItem
{
    public Button EnterButton;
 

    // Start is called before the first frame update
    void Start()
    {
        this.sectionId = "0";
        EnterButton.onClick.AddListener(OpenAIYandere);
    }

    private void OpenAIYandere()
    {
        UIManager.Inst.OpenPanel<AIYandereStartPanel>(PanelId.AIYandereStartPanel);
    }
}
