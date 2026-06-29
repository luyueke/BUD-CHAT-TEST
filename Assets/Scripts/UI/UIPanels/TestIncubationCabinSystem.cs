using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AIGame.Base;
using Basic;
using Basic.Utils;
using BUD.AnimPose;
using BUD.MailBox;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.AIResData;
using Game.Audio;
using Game.Avatar;
using Game.Base;
using Game.Database;
using Game.Event;
using Game.GameSetting;
using Game.Pet;
using Game.Store;
using GameData;
using GameData.Account;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pb.Base;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.GashaponPanel;
using UI.UIPanels.LobbyCharacterIdlePanel;
using UI.UIPanels.ProfilePanel;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;
using View.UI.PopupPanelSystem;
using EventTracking;
using UI.UIPanels.FittingRoom;
using Newbie;
using DG.Tweening;
using GameUI;
using System.Reflection;
using Game.Vehicle.PGCVehicle;

public class TestIncubationCabinSystem
{
    public static void EnterBoxScene()
    {
        xasset.Scene.LoadAsync($"Assets/Arts/EachScene/Box Scene/Box Scene.unity").completed += operation =>
        {
            UIManager.Inst.OpenPanel(PanelId.TestIncubationCabinPanel);
            UIManager.Inst.ClosePanel(PanelId.GameHallPanel);
        };
    }
}
