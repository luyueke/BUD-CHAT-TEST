using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsManagers;
using GameData;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public static class AIParkGameUgcsetTool
    {
        public static AICommonGameConfig gameConfig { get { return GameDataManager.Inst.mapGlobalData?.curUgcBaseInfo?.gameSetting?.AICommonGameConfig; } }

        public static bool GetParkPopImageColor1(ref Color color)
        {
            if (gameConfig == null || gameConfig.gamePop == null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(gameConfig.gamePop.bgColor1) && ColorUtility.TryParseHtmlString("#" + gameConfig.gamePop.bgColor1, out var c))
            {
                color = c;
                return true;
            }
            return false;
        }
        public static bool GetParkPopImageColor2(ref Color color)
        {
            if (gameConfig == null || gameConfig.gamePop == null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(gameConfig.gamePop.bgColor2) && ColorUtility.TryParseHtmlString("#" + gameConfig.gamePop.bgColor2, out var c))
            {
                color = c;
                return true;
            }
            return false;
        }
        public static void ParkPopImageColor1(this Image image) 
        {
            if (gameConfig == null || gameConfig.gamePop == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(gameConfig.gamePop.bgColor1) && ColorUtility.TryParseHtmlString("#" + gameConfig.gamePop.bgColor1, out var c))
            {
                image.color = c;
            }
        }

        public static void ParkPopImageColor2(this Image image)
        {
            if (gameConfig == null || gameConfig.gamePop == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(gameConfig.gamePop.bgColor2) && ColorUtility.TryParseHtmlString("#" + gameConfig.gamePop.bgColor2, out var c))
            {
                image.color = c;
            }
        }
        public static void ParkPopTextColor(this Text text)
        {
            if (gameConfig == null || gameConfig.gamePop == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(gameConfig.gamePop.fontColor) && ColorUtility.TryParseHtmlString("#" + gameConfig.gamePop.fontColor, out var c))
            {
                text.color = c;
            }
        }

        public static void ParkBookImageColor1(this Image image)
        {
            if (gameConfig == null || gameConfig.book == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(gameConfig.book.bgColor1) && ColorUtility.TryParseHtmlString("#" + gameConfig.book.bgColor1, out var c))
            {
                image.color = c;
            }
        }

        public static void ParkBookImageColor2(this Image image)
        {
            if (gameConfig == null || gameConfig.book == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(gameConfig.book.bgColor2) && ColorUtility.TryParseHtmlString("#" + gameConfig.book.bgColor2, out var c))
            {
                image.color = c;
            }
        }
    }
}