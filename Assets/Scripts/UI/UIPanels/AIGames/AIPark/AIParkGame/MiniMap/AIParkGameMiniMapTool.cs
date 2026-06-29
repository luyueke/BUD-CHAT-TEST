using DG.Tweening;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using GameData.BaseInfo;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UI.Catalog;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public static class AIParkGameMiniMapTool
    {
        public static Vector2 SceneSize = new Vector2(1472,880) * 0.08f;

        public static Vector2 UISize;

        public static float RatioX = 15f;

        public static float RatioY = 13f;
        public static Vector2 GetUIPos(Vector2 v2)
        {
            return new Vector2(-v2.x * RatioX, -v2.y * RatioY);
        }
    }
}