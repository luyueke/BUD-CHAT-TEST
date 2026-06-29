using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using GameData;
using GameData.BaseInfo;
using GameData.MapData;
using UnityEngine;

public class NewGameStudioMgr : GlobalInstance<NewGameStudioMgr>
{
    public override void Release()
    {
        base.Release();
    }

    public void CreateNewMap()
    {
        var mapName = $"{LocalizationManager.Inst.GetLocalizedText("未命名")}-{DateTime.Now:yyyy-MM-dd}";
        var templateId = "10000";
        var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
        p.Init(new MapInfo()
        {
            name = mapName,
            templateId = templateId,
        }, LoadingType.Map);
        GameController.StartGame(EnterGameModel.CreateEmptyScene, new MapInfo()
        {
            name = mapName,
            templateId = templateId,
        });
    }

    public void OnClickGameItem(DraftListItem data)
    {
        UIManager.Inst.OpenPanel<GameStudioDetailView>(PanelId.UgcLoadingPanel);
    }
}
